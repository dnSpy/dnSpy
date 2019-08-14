/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using System.Diagnostics;
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;

namespace dnSpy.Contracts.Text {
	/// <summary>
	/// Extension methods
	/// </summary>
	public static class ITextColorWriterExtensions {
		/// <summary>
		/// Writes a newline
		/// </summary>
		/// <typeparam name="T">Writer type</typeparam>
		/// <param name="output">Output</param>
		/// <returns></returns>
		public static T WriteLine<T>(this T output) where T : ITextColorWriter {
			output.Write(BoxedTextColor.Text, Environment.NewLine);
			return output;
		}

		/// <summary>
		/// Writes a space
		/// </summary>
		/// <typeparam name="T">Writer type</typeparam>
		/// <param name="output">Output</param>
		/// <returns></returns>
		public static T WriteSpace<T>(this T output) where T : ITextColorWriter {
			output.Write(BoxedTextColor.Text, " ");
			return output;
		}

		/// <summary>
		/// Writes a comma followed by a space
		/// </summary>
		/// <typeparam name="T">Writer type</typeparam>
		/// <param name="output">Output</param>
		/// <returns></returns>
		public static T WriteCommaSpace<T>(this T output) where T : ITextColorWriter {
			output.Write(BoxedTextColor.Punctuation, ",");
			output.WriteSpace();
			return output;
		}

		/// <summary>
		/// Writes a version
		/// </summary>
		/// <typeparam name="T">Writer type</typeparam>
		/// <param name="output">Output</param>
		/// <param name="version">Version</param>
		/// <returns></returns>
		public static T Write<T>(this T output, Version? version) where T : ITextColorWriter {
			if (version is null)
				output.Write(BoxedTextColor.Error, "?.?.?.?");
			else {
				output.Write(BoxedTextColor.Number, version.Major.ToString());
				output.Write(BoxedTextColor.Number, ".");
				output.Write(BoxedTextColor.Number, version.Minor.ToString());
				output.Write(BoxedTextColor.Number, ".");
				output.Write(BoxedTextColor.Number, version.Build.ToString());
				output.Write(BoxedTextColor.Number, ".");
				output.Write(BoxedTextColor.Number, version.Revision.ToString());
			}
			return output;
		}

		/// <summary>
		/// Writes an assembly
		/// </summary>
		/// <typeparam name="T">Writer type</typeparam>
		/// <param name="output">Output</param>
		/// <param name="asm">Assembly</param>
		/// <returns></returns>
		public static T Write<T>(this T output, IAssembly? asm) where T : ITextColorWriter {
			if (asm is null)
				return output;
			var asmDef = asm as AssemblyDef;
			bool isExe = !(asmDef is null) &&
				!(asmDef.ManifestModule is null) &&
				(asmDef.ManifestModule.Characteristics & dnlib.PE.Characteristics.Dll) == 0;
			output.Write(isExe ? BoxedTextColor.AssemblyExe : BoxedTextColor.Assembly, asm.Name);

			output.WriteCommaSpace();

			output.Write(BoxedTextColor.InstanceProperty, "Version");
			output.Write(BoxedTextColor.Operator, "=");
			output.Write(asm.Version);

			output.WriteCommaSpace();

			output.Write(BoxedTextColor.InstanceProperty, "Culture");
			output.Write(BoxedTextColor.Operator, "=");
			output.Write(BoxedTextColor.EnumField, UTF8String.IsNullOrEmpty(asm.Culture) ? "neutral" : asm.Culture.String);

			output.WriteCommaSpace();

			var publicKey = PublicKeyBase.ToPublicKeyToken(asm.PublicKeyOrToken);
			output.Write(BoxedTextColor.InstanceProperty, publicKey is null || publicKey is PublicKeyToken ? "PublicKeyToken" : "PublicKey");
			output.Write(BoxedTextColor.Operator, "=");
			if (PublicKeyBase.IsNullOrEmpty2(publicKey))
				output.Write(BoxedTextColor.Keyword, "null");
			else {
				Debug2.Assert(!(publicKey is null));
				output.Write(BoxedTextColor.Number, publicKey.ToString());
			}

			if ((asm.Attributes & AssemblyAttributes.Retargetable) != 0) {
				output.WriteCommaSpace();
				output.Write(BoxedTextColor.InstanceProperty, "Retargetable");
				output.Write(BoxedTextColor.Operator, "=");
				output.Write(BoxedTextColor.EnumField, "Yes");
			}

			if ((asm.Attributes & AssemblyAttributes.ContentType_Mask) == AssemblyAttributes.ContentType_WindowsRuntime) {
				output.WriteCommaSpace();
				output.Write(BoxedTextColor.InstanceProperty, "ContentType");
				output.Write(BoxedTextColor.Operator, "=");
				output.Write(BoxedTextColor.EnumField, "WindowsRuntime");
			}

			return output;
		}

		/// <summary>
		/// Writes a namespace
		/// </summary>
		/// <typeparam name="T">Writer type</typeparam>
		/// <param name="output">Output</param>
		/// <param name="namespace">Namespace</param>
		/// <returns></returns>
		public static T WriteNamespace<T>(this T output, string? @namespace) where T : ITextColorWriter {
			if (@namespace is null)
				return output;
			if (@namespace.Length == 0)
				output.Write(BoxedTextColor.Punctuation, "-");
			else {
				var parts = @namespace.Split('.');
				for (int i = 0; i < parts.Length; i++) {
					if (i > 0)
						output.Write(BoxedTextColor.Operator, ".");
					output.Write(BoxedTextColor.Namespace, IdentifierEscaper.Escape(parts[i]));
				}
			}
			return output;
		}

		/// <summary>
		/// Writes a module name
		/// </summary>
		/// <typeparam name="T">Writer type</typeparam>
		/// <param name="output">Output</param>
		/// <param name="name">Module name</param>
		/// <returns></returns>
		public static T WriteModule<T>(this T output, string name) where T : ITextColorWriter {
			output.Write(BoxedTextColor.AssemblyModule, NameUtilities.CleanName(name));
			return output;
		}

		/// <summary>
		/// Writes a filename
		/// </summary>
		/// <typeparam name="T">Writer type</typeparam>
		/// <param name="output">Output</param>
		/// <param name="filename">Filename</param>
		/// <returns></returns>
		public static T WriteFilename<T>(this T output, string? filename) where T : ITextColorWriter {
			if (filename is null)
				return output;
			filename = NameUtilities.CleanName(filename)!;
			var s = filename.Replace('\\', '/');
			var parts = s.Split('/');
			int slashIndex = 0;
			for (int i = 0; i < parts.Length - 1; i++) {
				output.Write(BoxedTextColor.DirectoryPart, parts[i]);
				slashIndex += parts[i].Length;
				output.Write(BoxedTextColor.Text, filename[slashIndex].ToString());
				slashIndex++;
			}
			var fn = parts[parts.Length - 1];
			int index = fn.LastIndexOf('.');
			if (index < 0)
				output.Write(BoxedTextColor.FileNameNoExtension, fn);
			else {
				string ext = fn.Substring(index + 1);
				fn = fn.Substring(0, index);
				output.Write(BoxedTextColor.FileNameNoExtension, fn);
				output.Write(BoxedTextColor.Text, ".");
				output.Write(BoxedTextColor.FileExtension, ext);
			}
			return output;
		}
	}
}
