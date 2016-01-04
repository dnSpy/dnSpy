// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Highlighting;
using dnSpy.Contracts.Languages;
using dnSpy.Decompiler;
using dnSpy.NRefactory;
using dnSpy.Shared.UI.Highlighting;
using ICSharpCode.Decompiler;

namespace dnSpy.Languages {
	/// <summary>
	/// Base class for language-specific decompiler implementations.
	/// </summary>
	public abstract class Language : ILanguage {
		public abstract string NameUI { get; }
		public abstract double OrderUI { get; }
		public abstract Guid Guid { get; }

		/// <summary>
		/// Gets the file extension used by source code files in this language.
		/// </summary>
		public abstract string FileExtension { get; }

		public virtual string ProjectFileExtension {
			get { return null; }
		}

		public void WriteName(ISyntaxHighlightOutput output, TypeDef type) {
			FormatTypeName(SyntaxHighlightOutputToTextOutput.Create(output), type);
		}

		public void WriteType(ISyntaxHighlightOutput output, ITypeDefOrRef type, bool includeNamespace, ParamDef pd = null) {
			TypeToString(SyntaxHighlightOutputToTextOutput.Create(output), type, includeNamespace, pd);
		}

		public void WriteName(ISyntaxHighlightOutput output, PropertyDef property, bool? isIndexer) {
			FormatPropertyName(SyntaxHighlightOutputToTextOutput.Create(output), property, isIndexer);
		}

		public virtual void Decompile(MethodDef method, ITextOutput output, DecompilationOptions options) {
			this.WriteCommentLine(output, TypeToString(method.DeclaringType, true) + "." + method.Name);
		}

		public virtual void Decompile(PropertyDef property, ITextOutput output, DecompilationOptions options) {
			this.WriteCommentLine(output, TypeToString(property.DeclaringType, true) + "." + property.Name);
		}

		public virtual void Decompile(FieldDef field, ITextOutput output, DecompilationOptions options) {
			this.WriteCommentLine(output, TypeToString(field.DeclaringType, true) + "." + field.Name);
		}

		public virtual void Decompile(EventDef ev, ITextOutput output, DecompilationOptions options) {
			this.WriteCommentLine(output, TypeToString(ev.DeclaringType, true) + "." + ev.Name);
		}

		public virtual void Decompile(TypeDef type, ITextOutput output, DecompilationOptions options) {
			this.WriteCommentLine(output, TypeToString(type, true));
		}

		public virtual void DecompileNamespace(string @namespace, IEnumerable<TypeDef> types, ITextOutput output, DecompilationOptions options) {
			this.WriteCommentLine(output, string.IsNullOrEmpty(@namespace) ? string.Empty : IdentifierEscaper.Escape(@namespace));
			this.WriteCommentLine(output, string.Empty);
			this.WriteCommentLine(output, "Types:");
			this.WriteCommentLine(output, string.Empty);
			foreach (var type in types) {
				this.WriteCommentBegin(output, true);
				output.WriteReference(IdentifierEscaper.Escape(type.Name), type, TextTokenType.Comment);
				this.WriteCommentEnd(output, true);
				output.WriteLine();
			}
		}

		public virtual void DecompileAssembly(IDnSpyFile file, ITextOutput output, DecompilationOptions options, DecompileAssemblyFlags flags = DecompileAssemblyFlags.AssemblyAndModule) {
			bool decompileAsm = (flags & DecompileAssemblyFlags.Assembly) != 0;
			bool decompileMod = (flags & DecompileAssemblyFlags.Module) != 0;
			this.WriteCommentLine(output, file.Filename);
			if (decompileAsm && file.AssemblyDef != null) {
				if (file.AssemblyDef.IsContentTypeWindowsRuntime) {
					this.WriteCommentLine(output, file.AssemblyDef.Name + " [WinRT]");
				}
				else {
					this.WriteCommentLine(output, file.AssemblyDef.FullName);
				}
			}
			else if (decompileMod) {
				this.WriteCommentLine(output, file.ModuleDef.Name);
			}
		}

		protected void PrintEntryPoint(IDnSpyFile assembly, ITextOutput output) {
			var ep = GetEntryPoint(assembly.ModuleDef);
			if (ep is uint)
				this.WriteCommentLine(output, string.Format("Native Entry point: 0x{0:x8}", (uint)ep));
			else if (ep is MethodDef) {
				var epMethod = (MethodDef)ep;
				WriteCommentBegin(output, true);
				output.Write("Entry point: ", TextTokenType.Comment);
				if (epMethod.DeclaringType != null) {
					output.WriteReference(IdentifierEscaper.Escape(epMethod.DeclaringType.FullName), epMethod.DeclaringType, TextTokenType.Comment);
					output.Write(".", TextTokenType.Comment);
				}
				output.WriteReference(IdentifierEscaper.Escape(epMethod.Name), epMethod, TextTokenType.Comment);
				WriteCommentEnd(output, true);
				output.WriteLine();
			}
		}

		object GetEntryPoint(ModuleDef module) {
			int maxIters = 1;
			for (int i = 0; module != null && i < maxIters; i++) {
				var rva = module.NativeEntryPoint;
				if (rva != 0)
					return (uint)rva;

				var manEp = module.ManagedEntryPoint;
				var ep = manEp as MethodDef;
				if (ep != null)
					return ep;

				var file = manEp as FileDef;
				if (file == null)
					return null;

				var asm = module.Assembly;
				if (asm == null)
					return null;
				maxIters = asm.Modules.Count;

				module = asm.Modules.FirstOrDefault(m => File.Exists(m.Location) && StringComparer.OrdinalIgnoreCase.Equals(Path.GetFileName(m.Location), file.Name));
			}

			return null;
		}

		protected void WriteCommentLineDeclaringType(ITextOutput output, IMemberDef member) {
			WriteCommentBegin(output, true);
			output.WriteReference(TypeToString(member.DeclaringType, includeNamespace: true), member.DeclaringType, TextTokenType.Comment);
			WriteCommentEnd(output, true);
			output.WriteLine();
		}

		public virtual void WriteCommentBegin(ITextOutput output, bool addSpace) {
			if (addSpace)
				output.Write("// ", TextTokenType.Comment);
			else
				output.Write("//", TextTokenType.Comment);
		}

		public virtual void WriteCommentEnd(ITextOutput output, bool addSpace) {
		}

		string TypeToString(ITypeDefOrRef type, bool includeNamespace, IHasCustomAttribute typeAttributes = null) {
			var writer = new StringWriter();
			var output = new PlainTextOutput(writer);
			TypeToString(output, type, includeNamespace, typeAttributes);
			return writer.ToString();
		}

		protected virtual void TypeToString(ITextOutput output, ITypeDefOrRef type, bool includeNamespace, IHasCustomAttribute typeAttributes = null) {
			if (type == null)
				return;
			if (includeNamespace)
				output.Write(IdentifierEscaper.Escape(type.FullName), TextTokenHelper.GetTextTokenType(type));
			else
				output.Write(IdentifierEscaper.Escape(type.Name), TextTokenHelper.GetTextTokenType(type));
		}

		public virtual void WriteToolTip(ISyntaxHighlightOutput output, IMemberRef member, IHasCustomAttribute typeAttributes) {
			var newOutput = SyntaxHighlightOutputToTextOutput.Create(output);
			if (member is ITypeDefOrRef)
				TypeToString(newOutput, (ITypeDefOrRef)member, true, typeAttributes);
			else if (member is GenericParam) {
				var gp = (GenericParam)member;
				output.Write(IdentifierEscaper.Escape(gp.Name), TextTokenHelper.GetTextTokenType(gp));
				output.WriteSpace();
				output.Write("in", TextTokenType.Text);
				output.WriteSpace();
				WriteToolTip(output, gp.Owner, typeAttributes);
			}
			else {
				//TODO: This should be escaped but since it contains whitespace, parens, etc,
				//		we can't pass it to IdentifierEscaper.Escape().
				output.Write(member.ToString(), TextTokenHelper.GetTextTokenType(member));
			}
		}

		public virtual void WriteToolTip(ISyntaxHighlightOutput output, IVariable variable, string name) {
			output.Write(variable is Local ? "(local variable)" : "(parameter)", TextTokenType.Text);
			output.WriteSpace();
			WriteToolTip(output, variable.Type.ToTypeDefOrRef(), variable is Parameter ? ((Parameter)variable).ParamDef : null);
			output.WriteSpace();
			output.Write(IdentifierEscaper.Escape(GetName(variable, name)), variable is Local ? TextTokenType.Local : TextTokenType.Parameter);
		}

		protected static string GetName(IVariable variable, string name) {
			if (!string.IsNullOrWhiteSpace(name))
				return name;
			var n = variable.Name;
			if (!string.IsNullOrWhiteSpace(n))
				return n;
			return string.Format("#{0}", variable.Index);
		}

		protected virtual void FormatPropertyName(ITextOutput output, PropertyDef property, bool? isIndexer = null) {
			if (property == null)
				throw new ArgumentNullException("property");
			output.Write(IdentifierEscaper.Escape(property.Name), TextTokenHelper.GetTextTokenType(property));
		}

		protected virtual void FormatTypeName(ITextOutput output, TypeDef type) {
			if (type == null)
				throw new ArgumentNullException("type");
			output.Write(IdentifierEscaper.Escape(type.Name), TextTokenHelper.GetTextTokenType(type));
		}

		public virtual bool ShowMember(IMemberRef member, DecompilerSettings decompilerSettings) {
			return true;
		}
	}
}
