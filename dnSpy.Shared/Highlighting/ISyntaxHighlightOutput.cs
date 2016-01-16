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
using dnlib.DotNet;
using dnSpy.Contracts.Highlighting;
using dnSpy.Decompiler.Shared;

namespace dnSpy.Shared.Highlighting {
	public static class SyntaxHighlightOutputExtensionMethods {
		public static T WriteLine<T>(this T output) where T : ISyntaxHighlightOutput {
			output.Write(Environment.NewLine, TextTokenKind.Text);
			return output;
		}

		public static T WriteSpace<T>(this T output) where T : ISyntaxHighlightOutput {
			output.Write(" ", TextTokenKind.Text);
			return output;
		}

		public static T WriteCommaSpace<T>(this T output) where T : ISyntaxHighlightOutput {
			output.Write(",", TextTokenKind.Operator);
			output.WriteSpace();
			return output;
		}

		public static T Write<T>(this T output, Version version) where T : ISyntaxHighlightOutput {
			if (version == null) {
				output.Write("?", TextTokenKind.Error);
				output.Write(".", TextTokenKind.Operator);
				output.Write("?", TextTokenKind.Error);
				output.Write(".", TextTokenKind.Operator);
				output.Write("?", TextTokenKind.Error);
				output.Write(".", TextTokenKind.Operator);
				output.Write("?", TextTokenKind.Error);
			}
			else {
				output.Write(version.Major.ToString(), TextTokenKind.Number);
				output.Write(".", TextTokenKind.Operator);
				output.Write(version.Minor.ToString(), TextTokenKind.Number);
				output.Write(".", TextTokenKind.Operator);
				output.Write(version.Build.ToString(), TextTokenKind.Number);
				output.Write(".", TextTokenKind.Operator);
				output.Write(version.Revision.ToString(), TextTokenKind.Number);
			}
			return output;
		}

		public static T Write<T>(this T output, IAssembly asm) where T : ISyntaxHighlightOutput {
			if (asm == null)
				return output;
			var asmDef = asm as AssemblyDef;
			bool isExe = asmDef != null &&
				asmDef.ManifestModule != null &&
				(asmDef.ManifestModule.Characteristics & dnlib.PE.Characteristics.Dll) == 0;
			output.Write(asm.Name, isExe ? TextTokenKind.AssemblyExe : TextTokenKind.Assembly);

			output.WriteCommaSpace();

			output.Write("Version", TextTokenKind.InstanceProperty);
			output.Write("=", TextTokenKind.Operator);
			output.Write(asm.Version);

			output.WriteCommaSpace();

			output.Write("Culture", TextTokenKind.InstanceProperty);
			output.Write("=", TextTokenKind.Operator);
			output.Write(UTF8String.IsNullOrEmpty(asm.Culture) ? "neutral" : asm.Culture.String, TextTokenKind.EnumField);

			output.WriteCommaSpace();

			var publicKey = PublicKeyBase.ToPublicKeyToken(asm.PublicKeyOrToken);
			output.Write(publicKey == null || publicKey is PublicKeyToken ? "PublicKeyToken" : "PublicKey", TextTokenKind.InstanceProperty);
			output.Write("=", TextTokenKind.Operator);
			if (PublicKeyBase.IsNullOrEmpty2(publicKey))
				output.Write("null", TextTokenKind.Keyword);
			else
				output.Write(publicKey.ToString(), TextTokenKind.Number);

			if ((asm.Attributes & AssemblyAttributes.Retargetable) != 0) {
				output.WriteCommaSpace();
				output.Write("Retargetable", TextTokenKind.InstanceProperty);
				output.Write("=", TextTokenKind.Operator);
				output.Write("Yes", TextTokenKind.EnumField);
			}

			if ((asm.Attributes & AssemblyAttributes.ContentType_Mask) == AssemblyAttributes.ContentType_WindowsRuntime) {
				output.WriteCommaSpace();
				output.Write("ContentType", TextTokenKind.InstanceProperty);
				output.Write("=", TextTokenKind.Operator);
				output.Write("WindowsRuntime", TextTokenKind.EnumField);
			}

			return output;
		}

		public static T WriteNamespace<T>(this T output, string name) where T : ISyntaxHighlightOutput {
			if (name == null)
				return output;
			if (name.Length == 0)
				output.Write("-", TextTokenKind.Operator);
			else {
				var parts = name.Split('.');
				for (int i = 0; i < parts.Length; i++) {
					if (i > 0)
						output.Write(".", TextTokenKind.Operator);
					output.Write(IdentifierEscaper.Escape(parts[i]), TextTokenKind.NamespacePart);
				}
			}
			return output;
		}

		public static T WriteModule<T>(this T output, string name) where T : ISyntaxHighlightOutput {
			output.Write(NameUtils.CleanName(name), TextTokenKind.Module);
			return output;
		}

		public static T WriteFilename<T>(this T output, string name) where T : ISyntaxHighlightOutput {
			if (name == null)
				return output;
			name = NameUtils.CleanName(name);
			var s = name.Replace('\\', '/');
			var parts = s.Split('/');
			int slashIndex = 0;
			for (int i = 0; i < parts.Length - 1; i++) {
				output.Write(parts[i], TextTokenKind.DirectoryPart);
				slashIndex += parts[i].Length;
				output.Write(name[slashIndex].ToString(), TextTokenKind.Text);
				slashIndex++;
			}
			var fn = parts[parts.Length - 1];
			int index = fn.LastIndexOf('.');
			if (index < 0)
				output.Write(fn, TextTokenKind.FileNameNoExtension);
			else {
				string ext = fn.Substring(index + 1);
				fn = fn.Substring(0, index);
				output.Write(fn, TextTokenKind.FileNameNoExtension);
				output.Write(".", TextTokenKind.Text);
				output.Write(ext, TextTokenKind.FileExtension);
			}
			return output;
		}
	}
}
