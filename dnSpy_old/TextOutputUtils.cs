/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using dnlib.DotNet;
using dnSpy.Decompiler;
using dnSpy.NRefactory;
using dnSpy.Shared.UI.Highlighting;
using ICSharpCode.Decompiler;

namespace dnSpy {
	public static class TextOutputUtils {
		public static T WriteCommaSpace_OLD<T>(this T output) where T : ITextOutput {
			output.Write(",", TextTokenType.Operator);
			TextOutputExtensions.WriteSpace(output);
			return output;
		}

		public static T Write_OLD<T>(this T output, Version version) where T : ITextOutput {
			if (version == null) {
				output.Write("?", TextTokenType.Error);
				output.Write(".", TextTokenType.Operator);
				output.Write("?", TextTokenType.Error);
				output.Write(".", TextTokenType.Operator);
				output.Write("?", TextTokenType.Error);
				output.Write(".", TextTokenType.Operator);
				output.Write("?", TextTokenType.Error);
			}
			else {
				output.Write(version.Major.ToString(), TextTokenType.Number);
				output.Write(".", TextTokenType.Operator);
				output.Write(version.Minor.ToString(), TextTokenType.Number);
				output.Write(".", TextTokenType.Operator);
				output.Write(version.Build.ToString(), TextTokenType.Number);
				output.Write(".", TextTokenType.Operator);
				output.Write(version.Revision.ToString(), TextTokenType.Number);
			}
			return output;
		}

		public static T Write_OLD<T>(this T output, IAssembly asm) where T : ITextOutput {
			if (asm == null)
				return output;
			var asmDef = asm as AssemblyDef;
			bool isExe = asmDef != null &&
				asmDef.ManifestModule != null &&
				(asmDef.ManifestModule.Characteristics & dnlib.PE.Characteristics.Dll) == 0;
			output.Write(asm.Name, isExe ? TextTokenType.AssemblyExe : TextTokenType.Assembly);

			output.WriteCommaSpace_OLD();

			output.Write("Version", TextTokenType.InstanceProperty);
			output.Write("=", TextTokenType.Operator);
			output.Write_OLD(asm.Version);

			output.WriteCommaSpace_OLD();

			output.Write("Culture", TextTokenType.InstanceProperty);
			output.Write("=", TextTokenType.Operator);
			output.Write(UTF8String.IsNullOrEmpty(asm.Culture) ? "neutral" : asm.Culture.String, TextTokenType.EnumField);

			output.WriteCommaSpace_OLD();

			var publicKey = PublicKeyBase.ToPublicKeyToken(asm.PublicKeyOrToken);
			output.Write(publicKey == null || publicKey is PublicKeyToken ? "PublicKeyToken" : "PublicKey", TextTokenType.InstanceProperty);
			output.Write("=", TextTokenType.Operator);
			if (PublicKeyBase.IsNullOrEmpty2(publicKey))
				output.Write("null", TextTokenType.Keyword);
			else
				output.Write(publicKey.ToString(), TextTokenType.Number);

			if ((asm.Attributes & AssemblyAttributes.Retargetable) != 0) {
				output.WriteCommaSpace_OLD();
				output.Write("Retargetable", TextTokenType.InstanceProperty);
				output.Write("=", TextTokenType.Operator);
				output.Write("Yes", TextTokenType.EnumField);
			}

			if ((asm.Attributes & AssemblyAttributes.ContentType_Mask) == AssemblyAttributes.ContentType_WindowsRuntime) {
				output.WriteCommaSpace_OLD();
				output.Write("ContentType", TextTokenType.InstanceProperty);
				output.Write("=", TextTokenType.Operator);
				output.Write("WindowsRuntime", TextTokenType.EnumField);
			}

			return output;
		}

		public static T WriteNamespace_OLD<T>(this T output, string name) where T : ITextOutput {
			if (name == null)
				return output;
			if (name.Length == 0)
				output.Write("-", TextTokenType.Operator);
			else {
				var parts = name.Split('.');
				for (int i = 0; i < parts.Length; i++) {
					if (i > 0)
						output.Write(".", TextTokenType.Operator);
					output.Write(IdentifierEscaper.Escape(parts[i]), TextTokenType.NamespacePart);
				}
			}
			return output;
		}

		public static T WriteModule_OLD<T>(this T output, string name) where T : ITextOutput {
			output.Write(name, TextTokenType.Module);
			return output;
		}

		public static T WriteFilename_OLD<T>(this T output, string name) where T : ITextOutput {
			if (name == null)
				return output;
			name = NameUtils.CleanName(name);
			var s = name.Replace('\\', '/');
			var parts = s.Split('/');
			int slashIndex = 0;
			for (int i = 0; i < parts.Length - 1; i++) {
				output.Write(parts[i], TextTokenType.DirectoryPart);
				slashIndex += parts[i].Length;
				output.Write(name[slashIndex].ToString(), TextTokenType.Text);
				slashIndex++;
			}
			var fn = parts[parts.Length - 1];
			int index = fn.LastIndexOf('.');
			if (index < 0)
				output.Write(fn, TextTokenType.FileNameNoExtension);
			else {
				string ext = fn.Substring(index + 1);
				fn = fn.Substring(0, index);
				output.Write(fn, TextTokenType.FileNameNoExtension);
				output.Write(".", TextTokenType.Text);
				output.Write(ext, TextTokenType.FileExtension);
			}
			return output;
		}
	}
}
