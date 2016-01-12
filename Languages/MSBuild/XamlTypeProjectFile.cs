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
using dnlib.DotNet;
using dnSpy.Contracts.Languages;
using ICSharpCode.Decompiler;

namespace dnSpy.Languages.MSBuild {
	sealed class XamlTypeProjectFile : TypeProjectFile {
		public XamlTypeProjectFile(TypeDef type, string filename, DecompilationOptions decompilationOptions, ILanguage language)
			: base(type, filename, decompilationOptions, language) {
		}

		protected override void Decompile(DecompileContext ctx, ITextOutput output) {
			var opts = new DecompilePartialType(type, output, decompilationOptions);
			foreach (var d in GetDefsToRemove())
				opts.Definitions.Add(d);
			opts.InterfacesToRemove.Add(new TypeRefUser(type.Module, "System.Windows.Markup", "IComponentConnector", new AssemblyNameInfo("WindowsBase, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35").ToAssemblyRef()));
			opts.InterfacesToRemove.Add(new TypeRefUser(type.Module, "System.Windows.Markup", "IComponentConnector", new AssemblyNameInfo("System.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089").ToAssemblyRef()));
			language.Decompile(DecompilationType.PartialType, opts);
		}

		IEnumerable<IMemberDef> GetDefsToRemove() {
			var ep = type.Module.EntryPoint;
			if (ep != null && ep.DeclaringType == type)
				yield return ep;

			var d = FindInitializeComponent();
			if (d != null) {
				yield return d;
				foreach (var f in GetFields(d)) {
					if (f.FieldType.RemovePinnedAndModifiers().GetElementType() == ElementType.Boolean)
						yield return f;
				}
			}

			var connMeth = FindConnectMethod();
			if (connMeth != null) {
				yield return connMeth;
				foreach (var f in GetFields(connMeth))
					yield return f;
			}
		}

		IEnumerable<FieldDef> GetFields(MethodDef method) {
			var body = method.Body;
			if (body != null) {
				foreach (var instr in body.Instructions) {
					var fd = instr.Operand as FieldDef;
					if (fd != null && fd.DeclaringType == method.DeclaringType)
						yield return fd;
				}
			}
		}

		MethodDef FindInitializeComponent() {
			foreach (var md in type.FindMethods("InitializeComponent")) {
				if (md.IsStatic || md.Parameters.Count != 1)
					continue;
				if (md.ReturnType.RemovePinnedAndModifiers().GetElementType() != ElementType.Void)
					continue;

				return md;
			}
			return null;
		}

		MethodDef FindConnectMethod() {
			foreach (var md in type.Methods) {
				if (IsConnect(md))
					return md;
			}
			return null;
		}

		static bool IsConnect(MethodDef md) {
			if (md == null || md.IsStatic || md.Parameters.Count != 3)
				return false;
			if (md.ReturnType.RemovePinnedAndModifiers().GetElementType() != ElementType.Void)
				return false;

			var sig = md.MethodSig;
			if (sig == null || sig.Params.Count != 2)
				return false;
			if (sig.Params[0].RemovePinnedAndModifiers().GetElementType() != ElementType.I4)
				return false;
			if (sig.Params[1].RemovePinnedAndModifiers().GetElementType() != ElementType.Object)
				return false;

			foreach (var o in md.Overrides) {
				if (o.MethodDeclaration == null || o.MethodDeclaration.DeclaringType == null)
					continue;
				if (o.MethodDeclaration.DeclaringType.FullName != "System.Windows.Markup.IComponentConnector")
					continue;
				return true;
			}

			return md.Name == "Connect";
		}
	}
}
