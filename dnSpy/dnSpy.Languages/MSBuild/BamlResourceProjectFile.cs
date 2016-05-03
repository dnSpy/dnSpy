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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnSpy.Contracts.Languages;
using dnSpy.Languages.Properties;

namespace dnSpy.Languages.MSBuild {
	sealed class BamlResourceProjectFile : ProjectFile {
		public override string Description => Languages_Resources.MSBuild_DecompileBaml;
		public bool IsAppDef { get; set; }
		public override BuildAction BuildAction => IsAppDef ? BuildAction.ApplicationDefinition : BuildAction.Page;
		public override string Filename { get; }
		public string TypeFullName { get; }
		public bool IsSatelliteFile { get; set; }

		readonly byte[] bamlData;
		readonly Func<byte[], Stream, IList<string>> decompileBaml;

		public IEnumerable<IAssembly> AssemblyReferences => asmRefs;
		readonly HashSet<IAssembly> asmRefs;

		public BamlResourceProjectFile(string filename, byte[] bamlData, string typeFullName, Func<byte[], Stream, IList<string>> decompileBaml) {
			this.Filename = filename;
			this.bamlData = bamlData;
			this.TypeFullName = typeFullName;
			this.SubType = "Designer";
			this.Generator = "MSBuild:Compile";
			this.decompileBaml = decompileBaml;
			this.asmRefs = new HashSet<IAssembly>(AssemblyNameComparer.CompareAll);
		}

		public override void Create(DecompileContext ctx) {
			IList<string> refs;
			using (var stream = File.Create(Filename))
				refs = decompileBaml(bamlData, stream);
			foreach (var asmRef in refs) {
				var a = new AssemblyNameInfo(asmRef);
				if (!UTF8String.IsNullOrEmpty(a.Name))
					asmRefs.Add(a);
			}
		}
	}

	// App.xaml isn't always created, so we must recreate it from the info found in the class.
	sealed class AppBamlResourceProjectFile : ProjectFile {
		public override string Description => Languages_Resources.MSBuild_CreateAppXaml;
		public override BuildAction BuildAction { get; }
		public override string Filename { get; }

		readonly TypeDef type;
		readonly ILanguage language;

		public AppBamlResourceProjectFile(string filename, TypeDef type, ILanguage language) {
			this.Filename = filename;
			this.type = type;
			this.SubType = "Designer";
			this.Generator = "MSBuild:Compile";
			this.BuildAction = DotNetUtils.IsStartUpClass(type) ? BuildAction.ApplicationDefinition : BuildAction.Page;
			this.language = language;
		}

		CilBody GetInitializeComponentBody() {
			var m = type.FindMethods("InitializeComponent").FirstOrDefault(a => a.Parameters.Count == 1 && !a.IsStatic);
			return m == null ? null : m.Body;
		}

		string GetStartupUri(CilBody body) =>
			body == null ? null : body.Instructions.Where(a => a.Operand is string && ((string)a.Operand).EndsWith(".xaml", StringComparison.OrdinalIgnoreCase)).Select(a => (string)a.Operand).FirstOrDefault();

		public override void Create(DecompileContext ctx) {
			var settings = new XmlWriterSettings {
				Encoding = Encoding.UTF8,
				Indent = true,
				OmitXmlDeclaration = true,
			};
			using (var writer = XmlWriter.Create(Filename, settings)) {
				writer.WriteStartDocument();
				writer.WriteStartElement("Application", "http://schemas.microsoft.com/winfx/2006/xaml/presentation");

				writer.WriteAttributeString("x", "Class", "http://schemas.microsoft.com/winfx/2006/xaml", type.ReflectionFullName);

				if (type.IsNotPublic) {
					var opts = BamlDecompilerOptions.Create(language);
					writer.WriteAttributeString("x", "ClassModifier", "http://schemas.microsoft.com/winfx/2006/xaml", opts.InternalClassModifier);
				}

				var body = GetInitializeComponentBody();
				Debug.Assert(body != null);
				if (body != null) {
					var startupUri = GetStartupUri(body);
					if (startupUri != null)
						writer.WriteAttributeString("StartupUri", startupUri);

					foreach (var info in GetEvents(body))
						writer.WriteAttributeString(info.Item1, info.Item2);
				}

				writer.WriteElementString("Application.Resources", "\r\n");

				writer.WriteEndElement();
				writer.WriteEndDocument();
			}
		}

		IEnumerable<Tuple<string,string>> GetEvents(CilBody body) {
			var instrs = body.Instructions;
			for (int i = 0; i + 2 < instrs.Count; i++) {
				if (instrs[i].OpCode.Code != Code.Ldftn && instrs[i].OpCode.Code != Code.Ldvirtftn)
					continue;
				var m = instrs[i].Operand as MethodDef;
				if (m == null)
					continue;
				if (instrs[i + 1].OpCode.Code != Code.Newobj)
					continue;
				if (instrs[i + 2].OpCode.Code != Code.Call)
					continue;
				var addMethod = instrs[i + 2].Operand as IMethod;
				if (addMethod == null || addMethod.MethodSig.GetParamCount() != 1)
					continue;
				if (!addMethod.Name.StartsWith("add_"))
					continue;
				yield return Tuple.Create(addMethod.Name.String.Substring(4), m.Name.String);
			}
		}
	}
}
