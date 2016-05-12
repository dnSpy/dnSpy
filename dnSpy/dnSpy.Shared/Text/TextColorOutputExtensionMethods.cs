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
using dnSpy.Contracts.Text;
using dnSpy.Decompiler.Shared;

namespace dnSpy.Shared.Text {
	public static class TextColorOutputExtensionMethods {
		public static T WriteLine<T>(this T output) where T : IOutputColorWriter {
			output.Write(BoxedOutputColor.Text, Environment.NewLine);
			return output;
		}

		public static T WriteSpace<T>(this T output) where T : IOutputColorWriter {
			output.Write(BoxedOutputColor.Text, " ");
			return output;
		}

		public static T WriteCommaSpace<T>(this T output) where T : IOutputColorWriter {
			output.Write(BoxedOutputColor.Punctuation, ",");
			output.WriteSpace();
			return output;
		}

		public static T Write<T>(this T output, Version version) where T : IOutputColorWriter {
			if (version == null)
				output.Write(BoxedOutputColor.Error, "?.?.?.?");
			else {
				output.Write(BoxedOutputColor.Number, version.Major.ToString());
				output.Write(BoxedOutputColor.Number, ".");
				output.Write(BoxedOutputColor.Number, version.Minor.ToString());
				output.Write(BoxedOutputColor.Number, ".");
				output.Write(BoxedOutputColor.Number, version.Build.ToString());
				output.Write(BoxedOutputColor.Number, ".");
				output.Write(BoxedOutputColor.Number, version.Revision.ToString());
			}
			return output;
		}

		public static T Write<T>(this T output, IAssembly asm) where T : IOutputColorWriter {
			if (asm == null)
				return output;
			var asmDef = asm as AssemblyDef;
			bool isExe = asmDef != null &&
				asmDef.ManifestModule != null &&
				(asmDef.ManifestModule.Characteristics & dnlib.PE.Characteristics.Dll) == 0;
			output.Write(isExe ? BoxedOutputColor.AssemblyExe : BoxedOutputColor.Assembly, asm.Name);

			output.WriteCommaSpace();

			output.Write(BoxedOutputColor.InstanceProperty, "Version");
			output.Write(BoxedOutputColor.Operator, "=");
			output.Write(asm.Version);

			output.WriteCommaSpace();

			output.Write(BoxedOutputColor.InstanceProperty, "Culture");
			output.Write(BoxedOutputColor.Operator, "=");
			output.Write(BoxedOutputColor.EnumField, UTF8String.IsNullOrEmpty(asm.Culture) ? "neutral" : asm.Culture.String);

			output.WriteCommaSpace();

			var publicKey = PublicKeyBase.ToPublicKeyToken(asm.PublicKeyOrToken);
			output.Write(BoxedOutputColor.InstanceProperty, publicKey == null || publicKey is PublicKeyToken ? "PublicKeyToken" : "PublicKey");
			output.Write(BoxedOutputColor.Operator, "=");
			if (PublicKeyBase.IsNullOrEmpty2(publicKey))
				output.Write(BoxedOutputColor.Keyword, "null");
			else
				output.Write(BoxedOutputColor.Number, publicKey.ToString());

			if ((asm.Attributes & AssemblyAttributes.Retargetable) != 0) {
				output.WriteCommaSpace();
				output.Write(BoxedOutputColor.InstanceProperty, "Retargetable");
				output.Write(BoxedOutputColor.Operator, "=");
				output.Write(BoxedOutputColor.EnumField, "Yes");
			}

			if ((asm.Attributes & AssemblyAttributes.ContentType_Mask) == AssemblyAttributes.ContentType_WindowsRuntime) {
				output.WriteCommaSpace();
				output.Write(BoxedOutputColor.InstanceProperty, "ContentType");
				output.Write(BoxedOutputColor.Operator, "=");
				output.Write(BoxedOutputColor.EnumField, "WindowsRuntime");
			}

			return output;
		}

		public static T WriteNamespace<T>(this T output, string name) where T : IOutputColorWriter {
			if (name == null)
				return output;
			if (name.Length == 0)
				output.Write(BoxedOutputColor.Punctuation, "-");
			else {
				var parts = name.Split('.');
				for (int i = 0; i < parts.Length; i++) {
					if (i > 0)
						output.Write(BoxedOutputColor.Operator, ".");
					output.Write(BoxedOutputColor.Namespace, IdentifierEscaper.Escape(parts[i]));
				}
			}
			return output;
		}

		public static T WriteModule<T>(this T output, string name) where T : IOutputColorWriter {
			output.Write(BoxedOutputColor.Module, NameUtils.CleanName(name));
			return output;
		}

		public static T WriteFilename<T>(this T output, string name) where T : IOutputColorWriter {
			if (name == null)
				return output;
			name = NameUtils.CleanName(name);
			var s = name.Replace('\\', '/');
			var parts = s.Split('/');
			int slashIndex = 0;
			for (int i = 0; i < parts.Length - 1; i++) {
				output.Write(BoxedOutputColor.DirectoryPart, parts[i]);
				slashIndex += parts[i].Length;
				output.Write(BoxedOutputColor.Text, name[slashIndex].ToString());
				slashIndex++;
			}
			var fn = parts[parts.Length - 1];
			int index = fn.LastIndexOf('.');
			if (index < 0)
				output.Write(BoxedOutputColor.FileNameNoExtension, fn);
			else {
				string ext = fn.Substring(index + 1);
				fn = fn.Substring(0, index);
				output.Write(BoxedOutputColor.FileNameNoExtension, fn);
				output.Write(BoxedOutputColor.Text, ".");
				output.Write(BoxedOutputColor.FileExtension, ext);
			}
			return output;
		}
	}
}
