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
using System.IO;
using dnlib.DotNet;
using dnlib.PE;
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.Text;
using dnSpy.Decompiler.Shared;
using dnSpy.Shared.Text;

namespace dnSpy.Shared.Files.TreeView {
	public struct NodePrinter {
		static bool IsExe(ModuleDef mod) => mod != null && (mod.Characteristics & Characteristics.Dll) == 0;
		static bool IsExe(IPEImage peImage) => peImage != null && (peImage.ImageNTHeaders.FileHeader.Characteristics & Characteristics.Dll) == 0;

		static string GetFilename(IDnSpyFile dnSpyFile) {
			string filename = null;
			try {
				filename = Path.GetFileName(dnSpyFile.Filename);
			}
			catch (ArgumentException) {
			}
			if (string.IsNullOrEmpty(filename))
				filename = dnSpyFile.GetShortName();
			return filename;
		}

		public void WriteNamespace(IOutputColorWriter output, ILanguage language, string name) => output.WriteNamespace(name);

		public void Write(IOutputColorWriter output, ILanguage language, IDnSpyFile file) {
			var filename = GetFilename(file);
			var peImage = file.PEImage;
			if (peImage != null)
				output.Write(IsExe(peImage) ? BoxedOutputColor.AssemblyExe : BoxedOutputColor.Assembly, NameUtils.CleanName(filename));
			else
				output.Write(BoxedOutputColor.Text, NameUtils.CleanName(filename));
		}

		public void Write(IOutputColorWriter output, ILanguage language, AssemblyDef asm, bool showToken, bool showAssemblyVersion, bool showAssemblyPublicKeyToken) {
			output.Write(IsExe(asm.ManifestModule) ? BoxedOutputColor.AssemblyExe : BoxedOutputColor.Assembly, asm.Name);

			bool showAsmVer = showAssemblyVersion;
			bool showPublicKeyToken = showAssemblyPublicKeyToken && !PublicKeyBase.IsNullOrEmpty2(asm.PublicKeyToken);

			if (showAsmVer || showPublicKeyToken) {
				output.WriteSpace();
				output.Write(BoxedOutputColor.Punctuation, "(");

				bool needComma = false;
				if (showAsmVer) {
					if (needComma)
						output.WriteCommaSpace();
					needComma = true;

					output.Write(asm.Version);
				}

				if (showPublicKeyToken) {
					if (needComma)
						output.WriteCommaSpace();
					needComma = true;

					var pkt = asm.PublicKeyToken;
					if (PublicKeyBase.IsNullOrEmpty2(pkt))
						output.Write(BoxedOutputColor.Keyword, "null");
					else
						output.Write(BoxedOutputColor.Number, pkt.ToString());
				}

				output.Write(BoxedOutputColor.Punctuation, ")");
			}

			WriteToken(output, asm, showToken);
		}

		public void Write(IOutputColorWriter output, ILanguage language, ModuleDef mod, bool showToken) {
			output.WriteModule(mod.Name);
			WriteToken(output, mod, showToken);
		}

		void WriteToken(IOutputColorWriter output, IMDTokenProvider tok, bool showToken) {
			if (!showToken)
				return;
			output.WriteSpace();
			output.Write(BoxedOutputColor.Operator, "@");
			output.Write(BoxedOutputColor.Number, string.Format("{0:X8}", tok.MDToken.Raw));
		}

		public void Write(IOutputColorWriter output, ILanguage language, AssemblyRef asmRef, bool showToken) {
			output.Write(BoxedOutputColor.Text, NameUtils.CleanIdentifier(asmRef.Name));
			WriteToken(output, asmRef, showToken);
		}

		public void Write(IOutputColorWriter output, ILanguage language, ModuleRef modRef, bool showToken) {
			output.Write(BoxedOutputColor.Text, NameUtils.CleanIdentifier(modRef.Name));
			WriteToken(output, modRef, showToken);
		}

		public void Write(IOutputColorWriter output, ILanguage language, TypeDef type, bool showToken) {
			language.WriteName(output, type);
			WriteToken(output, type, showToken);
		}

		public void Write(IOutputColorWriter output, ILanguage language, ITypeDefOrRef type, bool showToken) {
			language.WriteType(output, type, false);
			WriteToken(output, type, showToken);
		}

		public void Write(IOutputColorWriter output, ILanguage language, EventDef @event, bool showToken) {
			output.Write(TextTokenKindUtils.GetTextTokenKind(@event), NameUtils.CleanIdentifier(@event.Name));
			output.WriteSpace();
			output.Write(BoxedOutputColor.Punctuation, ":");
			output.WriteSpace();
			language.WriteType(output, @event.EventType, false);
			WriteToken(output, @event, showToken);
		}

		public void Write(IOutputColorWriter output, ILanguage language, PropertyDef property, bool showToken, bool? isIndexer) {
			language.WriteName(output, property, isIndexer);
			output.WriteSpace();
			output.Write(BoxedOutputColor.Punctuation, ":");
			output.WriteSpace();
			language.WriteType(output, property.PropertySig.GetRetType().ToTypeDefOrRef(), false);
			WriteToken(output, property, showToken);
		}

		public void Write(IOutputColorWriter output, ILanguage language, FieldDef field, bool showToken) {
			output.Write(TextTokenKindUtils.GetTextTokenKind(field), NameUtils.CleanIdentifier(field.Name));
			output.WriteSpace();
			output.Write(BoxedOutputColor.Punctuation, ":");
			output.WriteSpace();
			language.WriteType(output, field.FieldType.ToTypeDefOrRef(), false);
			WriteToken(output, field, showToken);
		}

		public void Write(IOutputColorWriter output, ILanguage language, MethodDef method, bool showToken) {
			output.Write(TextTokenKindUtils.GetTextTokenKind(method), NameUtils.CleanIdentifier(method.Name));
			output.Write(BoxedOutputColor.Punctuation, "(");
			foreach (var p in method.Parameters) {
				if (p.IsHiddenThisParameter)
					continue;
				if (p.MethodSigIndex > 0)
					output.WriteCommaSpace();
				language.WriteType(output, p.Type.ToTypeDefOrRef(), false, p.ParamDef);
			}
			if (method.CallingConvention == CallingConvention.VarArg || method.CallingConvention == CallingConvention.NativeVarArg) {
				if (method.MethodSig.GetParamCount() > 0)
					output.WriteCommaSpace();
				output.Write(BoxedOutputColor.Operator, "...");
			}
			output.Write(BoxedOutputColor.Punctuation, ")");
			output.WriteSpace();
			output.Write(BoxedOutputColor.Punctuation, ":");
			output.WriteSpace();
			language.WriteType(output, method.ReturnType.ToTypeDefOrRef(), false, method.Parameters.ReturnParameter.ParamDef);
			WriteToken(output, method, showToken);
		}
	}
}
