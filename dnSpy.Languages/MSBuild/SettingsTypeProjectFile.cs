/*
    Copyright (C) 2014-2016 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using dnSpy.Contracts.Languages;
using dnSpy.Decompiler.Shared;
using dnSpy.Languages.Properties;

namespace dnSpy.Languages.MSBuild {
	sealed class SettingsTypeProjectFile : TypeProjectFile {
		public override string Description {
			get { return Languages_Resources.MSBuild_CreateSettingsTypeFile; }
		}

		public ILanguage Language {
			get { return language; }
		}

		public DecompilationContext DecompilationContext {
			get { return decompilationContext; }
		}

		public override BuildAction BuildAction {
			get { return isEmpty ? BuildAction.DontIncludeInProjectFile : base.BuildAction; }
		}
		bool isEmpty;

		public SettingsTypeProjectFile(TypeDef type, string filename, DecompilationContext decompilationContext, ILanguage language)
			: base(type, filename, decompilationContext, language) {
		}

		public override void Create(DecompileContext ctx) {
			InitializeIsEmpty();
			if (!isEmpty)
				base.Create(ctx);
		}

		protected override void Decompile(DecompileContext ctx, ITextOutput output) {
			var opts = new DecompilePartialType(type, output, decompilationContext);
			foreach (var d in GetDefsToRemove())
				opts.Definitions.Add(d);
			language.Decompile(DecompilationType.PartialType, opts);
		}

		void InitializeIsEmpty() {
			var allDefs = new HashSet<object>();
			foreach (var m in type.Methods) allDefs.Add(m);
			foreach (var f in type.Fields) allDefs.Add(f);
			foreach (var p in type.Properties) allDefs.Add(p);
			foreach (var e in type.Events) allDefs.Add(e);
			foreach (var t in type.NestedTypes) allDefs.Add(t);
			foreach (var d in GetDefsToRemove()) {
				allDefs.Remove(d);
				if (d is PropertyDef) {
					foreach (var def in DotNetUtils.GetMethodsAndSelf((PropertyDef)d))
						allDefs.Remove(def);
				}
			}
			allDefs.Remove(type.FindStaticConstructor());
			allDefs.Remove(type.FindDefaultConstructor());
			isEmpty = allDefs.Count == 0;
		}

		public IMemberDef[] GetDefsToRemove() {
			if (defsToRemove != null)
				return defsToRemove;
			lock (defsToRemoveLock) {
				if (defsToRemove == null)
					defsToRemove = CalculateDefsToRemove().Distinct().ToArray();
			}
			return defsToRemove;
		}
		readonly object defsToRemoveLock = new object();
		IMemberDef[] defsToRemove;

		IEnumerable<IMemberDef> CalculateDefsToRemove() {
			var defaultProp = FindDefaultProperty();
			if (defaultProp != null) {
				foreach (var d in DotNetUtils.GetMethodsAndSelf(defaultProp))
					yield return d;
				foreach (var d in DotNetUtils.GetDefs(defaultProp))
					yield return d;
			}
			foreach (var p in type.Properties) {
				if (p.CustomAttributes.IsDefined("System.Configuration.DefaultSettingValueAttribute")) {
					foreach (var d in DotNetUtils.GetMethodsAndSelf(p))
						yield return d;
				}
			}
		}

		PropertyDef FindDefaultProperty() {
			foreach (var p in type.Properties) {
				if (p.Name != "Default")
					continue;
				var g = p.GetMethod;
				if (g == null || !g.IsStatic || p.SetMethod != null || p.OtherMethods.Count != 0)
					continue;
				if (g.MethodSig.GetParamCount() != 0)
					continue;
				if (g.ReturnType.RemovePinnedAndModifiers().TryGetTypeDef() != type)
					continue;
				if (g.Body == null)
					continue;

				return p;
			}

			return null;
		}
	}

	sealed class SettingsDesignerTypeProjectFile : ProjectFile {
		public override string Description {
			get { return Languages_Resources.MSBuild_CreateSettingsDesignerTypeFile; }
		}

		public override BuildAction BuildAction {
			get { return BuildAction.Compile; }
		}

		public override string Filename {
			get { return filename; }
		}
		readonly string filename;

		readonly SettingsTypeProjectFile typeFile;

		public SettingsDesignerTypeProjectFile(SettingsTypeProjectFile typeFile, string filename) {
			this.typeFile = typeFile;
			this.filename = filename;
		}

		public override void Create(DecompileContext ctx) {
			using (var writer = new StreamWriter(Filename, false, Encoding.UTF8)) {
				if (typeFile.Language.CanDecompile(DecompilationType.PartialType)) {
					var output = new PlainTextOutput(writer);
					var opts = new DecompilePartialType(typeFile.Type, output, typeFile.DecompilationContext);
					foreach (var d in typeFile.GetDefsToRemove())
						opts.Definitions.Add(d);
					opts.ShowDefinitions = true;
					typeFile.Language.Decompile(DecompilationType.PartialType, opts);
				}
			}
		}
	}
}
