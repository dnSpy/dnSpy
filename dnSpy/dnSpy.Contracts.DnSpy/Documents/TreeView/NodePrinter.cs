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
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text;

namespace dnSpy.Contracts.Documents.TreeView {
	/// <summary>
	/// Node printer
	/// </summary>
	public struct NodePrinter {
		static bool IsExe(ModuleDef mod) => mod != null && (mod.Characteristics & Characteristics.Dll) == 0;
		static bool IsExe(IPEImage peImage) => peImage != null && (peImage.ImageNTHeaders.FileHeader.Characteristics & Characteristics.Dll) == 0;

		static string GetFilename(IDsDocument document) {
			string filename = null;
			try {
				filename = Path.GetFileName(document.Filename);
			}
			catch (ArgumentException) {
			}
			if (string.IsNullOrEmpty(filename))
				filename = document.GetShortName();
			return filename;
		}

		/// <summary>
		/// Writes a namespace
		/// </summary>
		/// <param name="output">Output</param>
		/// <param name="decompiler">Decompiler</param>
		/// <param name="namespace">Namespace</param>
		public void WriteNamespace(ITextColorWriter output, IDecompiler decompiler, string @namespace) => output.WriteNamespace(@namespace);

		/// <summary>
		/// Writes a file
		/// </summary>
		/// <param name="output">Output</param>
		/// <param name="decompiler">Decompiler</param>
		/// <param name="document">Document</param>
		public void Write(ITextColorWriter output, IDecompiler decompiler, IDsDocument document) {
			var filename = GetFilename(document);
			var peImage = document.PEImage;
			if (peImage != null)
				output.Write(IsExe(peImage) ? BoxedTextColor.AssemblyExe : BoxedTextColor.Assembly, NameUtilities.CleanName(filename));
			else
				output.Write(BoxedTextColor.Text, NameUtilities.CleanName(filename));
		}

		/// <summary>
		/// Writes an assembly
		/// </summary>
		/// <param name="output">Output</param>
		/// <param name="decompiler">Decompiler</param>
		/// <param name="asm">Assembly</param>
		/// <param name="showToken">true to write tokens</param>
		/// <param name="showAssemblyVersion">true to write version</param>
		/// <param name="showAssemblyPublicKeyToken">true to write public key token</param>
		public void Write(ITextColorWriter output, IDecompiler decompiler, AssemblyDef asm, bool showToken, bool showAssemblyVersion, bool showAssemblyPublicKeyToken) {
			output.Write(IsExe(asm.ManifestModule) ? BoxedTextColor.AssemblyExe : BoxedTextColor.Assembly, asm.Name);

			bool showAsmVer = showAssemblyVersion;
			bool showPublicKeyToken = showAssemblyPublicKeyToken && !PublicKeyBase.IsNullOrEmpty2(asm.PublicKeyToken);

			if (showAsmVer || showPublicKeyToken) {
				output.WriteSpace();
				output.Write(BoxedTextColor.Punctuation, "(");

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
						output.Write(BoxedTextColor.Keyword, "null");
					else
						output.Write(BoxedTextColor.Number, pkt.ToString());
				}

				output.Write(BoxedTextColor.Punctuation, ")");
			}

			WriteToken(output, asm, showToken);
		}

		/// <summary>
		/// Writes a module
		/// </summary>
		/// <param name="output">Output</param>
		/// <param name="decompiler">Decompiler</param>
		/// <param name="module">Module</param>
		/// <param name="showToken">true to write tokens</param>
		public void Write(ITextColorWriter output, IDecompiler decompiler, ModuleDef module, bool showToken) {
			output.WriteModule(module.Name);
			WriteToken(output, module, showToken);
		}

		/// <summary>
		/// Writes a token
		/// </summary>
		/// <param name="output">Output</param>
		/// <param name="tok">Token provider</param>
		/// <param name="showToken">true to write tokens</param>
		void WriteToken(ITextColorWriter output, IMDTokenProvider tok, bool showToken) {
			if (!showToken)
				return;
			output.WriteSpace();
			output.Write(BoxedTextColor.Operator, "@");
			output.Write(BoxedTextColor.Number, string.Format("{0:X8}", tok.MDToken.Raw));
		}

		/// <summary>
		/// Writes an assembly reference
		/// </summary>
		/// <param name="output">Output</param>
		/// <param name="decompiler">Decompiler</param>
		/// <param name="asmRef">Assembly reference</param>
		/// <param name="showToken">true to write tokens</param>
		public void Write(ITextColorWriter output, IDecompiler decompiler, AssemblyRef asmRef, bool showToken) {
			output.Write(BoxedTextColor.Text, NameUtilities.CleanIdentifier(asmRef.Name));
			WriteToken(output, asmRef, showToken);
		}

		/// <summary>
		/// Writes a module reference
		/// </summary>
		/// <param name="output">Output</param>
		/// <param name="decompiler">Decompiler</param>
		/// <param name="modRef">Module reference</param>
		/// <param name="showToken">true to write tokens</param>
		public void Write(ITextColorWriter output, IDecompiler decompiler, ModuleRef modRef, bool showToken) {
			output.Write(BoxedTextColor.Text, NameUtilities.CleanIdentifier(modRef.Name));
			WriteToken(output, modRef, showToken);
		}

		/// <summary>
		/// Writes a type
		/// </summary>
		/// <param name="output">Output</param>
		/// <param name="decompiler">Decompiler</param>
		/// <param name="type">Type</param>
		/// <param name="showToken">true to write tokens</param>
		public void Write(ITextColorWriter output, IDecompiler decompiler, TypeDef type, bool showToken) {
			decompiler.WriteName(output, type);
			WriteToken(output, type, showToken);
		}

		/// <summary>
		/// Writes a type
		/// </summary>
		/// <param name="output">Output</param>
		/// <param name="decompiler">Decompiler</param>
		/// <param name="type">Type</param>
		/// <param name="showToken">true to write tokens</param>
		public void Write(ITextColorWriter output, IDecompiler decompiler, ITypeDefOrRef type, bool showToken) {
			decompiler.WriteType(output, type, false);
			WriteToken(output, type, showToken);
		}

		/// <summary>
		/// Writes an event
		/// </summary>
		/// <param name="output">Output</param>
		/// <param name="decompiler">Decompiler</param>
		/// <param name="event">Event</param>
		/// <param name="showToken">true to write tokens</param>
		public void Write(ITextColorWriter output, IDecompiler decompiler, EventDef @event, bool showToken) {
			output.Write(decompiler.MetadataTextColorProvider.GetColor(@event), NameUtilities.CleanIdentifier(@event.Name));
			output.WriteSpace();
			output.Write(BoxedTextColor.Punctuation, ":");
			output.WriteSpace();
			decompiler.WriteType(output, @event.EventType, false);
			WriteToken(output, @event, showToken);
		}

		/// <summary>
		/// Writes a property
		/// </summary>
		/// <param name="output">Output</param>
		/// <param name="decompiler">Decompiler</param>
		/// <param name="property">Property</param>
		/// <param name="showToken">true to write tokens</param>
		/// <param name="isIndexer">true if it's an indexer</param>
		public void Write(ITextColorWriter output, IDecompiler decompiler, PropertyDef property, bool showToken, bool? isIndexer) {
			decompiler.WriteName(output, property, isIndexer);
			output.WriteSpace();
			output.Write(BoxedTextColor.Punctuation, ":");
			output.WriteSpace();
			decompiler.WriteType(output, property.PropertySig.GetRetType().ToTypeDefOrRef(), false);
			WriteToken(output, property, showToken);
		}

		/// <summary>
		/// Writes a field
		/// </summary>
		/// <param name="output">Output</param>
		/// <param name="decompiler">Decompiler</param>
		/// <param name="field">Field</param>
		/// <param name="showToken">true to write tokens</param>
		public void Write(ITextColorWriter output, IDecompiler decompiler, FieldDef field, bool showToken) {
			output.Write(decompiler.MetadataTextColorProvider.GetColor(field), NameUtilities.CleanIdentifier(field.Name));
			output.WriteSpace();
			output.Write(BoxedTextColor.Punctuation, ":");
			output.WriteSpace();
			decompiler.WriteType(output, field.FieldType.ToTypeDefOrRef(), false);
			WriteToken(output, field, showToken);
		}

		/// <summary>
		/// Writes a method
		/// </summary>
		/// <param name="output">Output</param>
		/// <param name="decompiler">Decompiler</param>
		/// <param name="method">Method</param>
		/// <param name="showToken">true to write tokens</param>
		public void Write(ITextColorWriter output, IDecompiler decompiler, MethodDef method, bool showToken) {
			output.Write(decompiler.MetadataTextColorProvider.GetColor(method), NameUtilities.CleanIdentifier(method.Name));
			output.Write(BoxedTextColor.Punctuation, "(");
			foreach (var p in method.Parameters) {
				if (p.IsHiddenThisParameter)
					continue;
				if (p.MethodSigIndex > 0)
					output.WriteCommaSpace();
				decompiler.WriteType(output, p.Type.ToTypeDefOrRef(), false, p.ParamDef);
			}
			if (method.CallingConvention == CallingConvention.VarArg || method.CallingConvention == CallingConvention.NativeVarArg) {
				if (method.MethodSig.GetParamCount() > 0)
					output.WriteCommaSpace();
				output.Write(BoxedTextColor.Operator, "...");
			}
			output.Write(BoxedTextColor.Punctuation, ")");
			output.WriteSpace();
			output.Write(BoxedTextColor.Punctuation, ":");
			output.WriteSpace();
			decompiler.WriteType(output, method.ReturnType.ToTypeDefOrRef(), false, method.Parameters.ReturnParameter.ParamDef);
			WriteToken(output, method, showToken);
		}
	}
}
