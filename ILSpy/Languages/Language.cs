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

using ICSharpCode.Decompiler;
using ICSharpCode.NRefactory;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace ICSharpCode.ILSpy
{
	[Flags]
	public enum DecompileAssemblyFlags
	{
		Assembly = 1,
		Module = 2,
		AssemblyAndModule = Assembly | Module,
	}

	/// <summary>
	/// Base class for language-specific decompiler implementations.
	/// </summary>
	public abstract class Language
	{
		/// <summary>
		/// Gets the name of the language (as shown in the UI)
		/// </summary>
		public abstract string Name { get; }

		/// <summary>
		/// Gets the file extension used by source code files in this language.
		/// </summary>
		public abstract string FileExtension { get; }

		public virtual string ProjectFileExtension
		{
			get { return null; }
		}

		/// <summary>
		/// Gets the syntax highlighting used for this language.
		/// </summary>
		public virtual ICSharpCode.AvalonEdit.Highlighting.IHighlightingDefinition SyntaxHighlighting
		{
			get
			{
				return ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.GetDefinitionByExtension(this.FileExtension);
			}
		}

		public virtual void DecompileMethod(MethodDef method, ITextOutput output, DecompilationOptions options)
		{
			WriteCommentLine(output, TypeToString(method.DeclaringType, true) + "." + method.Name);
		}

		public virtual void DecompileProperty(PropertyDef property, ITextOutput output, DecompilationOptions options)
		{
			WriteCommentLine(output, TypeToString(property.DeclaringType, true) + "." + property.Name);
		}

		public virtual void DecompileField(FieldDef field, ITextOutput output, DecompilationOptions options)
		{
			WriteCommentLine(output, TypeToString(field.DeclaringType, true) + "." + field.Name);
		}

		public virtual void DecompileEvent(EventDef ev, ITextOutput output, DecompilationOptions options)
		{
			WriteCommentLine(output, TypeToString(ev.DeclaringType, true) + "." + ev.Name);
		}

		public virtual void DecompileType(TypeDef type, ITextOutput output, DecompilationOptions options)
		{
			WriteCommentLine(output, TypeToString(type, true));
		}

		public virtual void DecompileNamespace(string nameSpace, IEnumerable<TypeDef> types, ITextOutput output, DecompilationOptions options)
		{
			WriteCommentLine(output, nameSpace);
		}

		public virtual void DecompileAssembly(LoadedAssembly assembly, ITextOutput output, DecompilationOptions options, DecompileAssemblyFlags flags = DecompileAssemblyFlags.AssemblyAndModule)
		{
			bool decompileAsm = (flags & DecompileAssemblyFlags.Assembly) != 0;
			bool decompileMod = (flags & DecompileAssemblyFlags.Module) != 0;
			WriteCommentLine(output, assembly.FileName);
			if (decompileAsm && assembly.AssemblyDefinition != null) {
				if (assembly.AssemblyDefinition.IsContentTypeWindowsRuntime) {
					WriteCommentLine(output, assembly.AssemblyDefinition.Name + " [WinRT]");
				} else {
					WriteCommentLine(output, assembly.AssemblyDefinition.FullName);
				}
			} else if (decompileMod) {
				WriteCommentLine(output, assembly.ModuleDefinition.Name);
			}
		}

		protected void PrintEntryPoint(LoadedAssembly assembly, ITextOutput output)
		{
			var ep = GetEntryPoint(assembly.ModuleDefinition);
			if (ep is uint)
				WriteCommentLine(output, string.Format("Native Entry point: 0x{0:x8}", (uint)ep));
			else if (ep is MethodDef) {
				var epMethod = (MethodDef)ep;
				WriteComment(output, "Entry point: ");
				if (epMethod.DeclaringType != null) {
					output.WriteReference(epMethod.DeclaringType.FullName, epMethod.DeclaringType, TextTokenType.Comment);
					output.Write('.', TextTokenType.Comment);
				}
				output.WriteReference(epMethod.Name, epMethod, TextTokenType.Comment);
				output.WriteLine();
			}
		}

		object GetEntryPoint(ModuleDef module)
		{
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

				module = asm.Modules.FirstOrDefault(m => Path.GetFileName(m.Location) == file.Name);
			}

			return null;
		}

		public void WriteCommentLineDeclaringType(ITextOutput output, IMemberDef member)
		{
			WriteComment(output, string.Empty);
			output.WriteReference(TypeToString(member.DeclaringType, includeNamespace: true), member.DeclaringType, TextTokenType.Comment);
			output.WriteLine();
		}

		public virtual void WriteCommentLine(ITextOutput output, string comment)
		{
			output.WriteLine("// " + comment, TextTokenType.Comment);
		}

		public virtual void WriteComment(ITextOutput output, string comment)
		{
			output.Write("// " + comment, TextTokenType.Comment);
		}

		/// <summary>
		/// Converts a type reference into a string. This method is used by the member tree node for parameter and return types.
		/// </summary>
		public string TypeToString(ITypeDefOrRef type, bool includeNamespace, IHasCustomAttribute typeAttributes = null)
		{
			var writer = new StringWriter();
			var output = new PlainTextOutput(writer);
			TypeToString(output, type, includeNamespace, typeAttributes);
			return writer.ToString();
		}

		public virtual void TypeToString(ITextOutput output, ITypeDefOrRef type, bool includeNamespace, IHasCustomAttribute typeAttributes = null)
		{
			if (type == null)
				return;
			if (includeNamespace)
				output.Write(type.FullName, TextTokenHelper.GetTextTokenType(type));
			else
				output.Write(type.Name, TextTokenHelper.GetTextTokenType(type));
		}

		/// <summary>
		/// Converts a member signature to a string.
		/// This is used for displaying the tooltip on a member reference.
		/// </summary>
		public virtual void WriteToolTip(ITextOutput output, IMemberRef member, IHasCustomAttribute typeAttributes)
		{
			if (member is ITypeDefOrRef)
				TypeToString(output, (ITypeDefOrRef)member, true, typeAttributes);
			else if (member is GenericParam) {
				var gp = (GenericParam)member;
				output.Write(gp.Name, TextTokenHelper.GetTextTokenType(gp));
				output.WriteSpace();
				output.Write("in", TextTokenType.Text);
				output.WriteSpace();
				WriteToolTip(output, gp.Owner, typeAttributes);
			}
			else
				output.Write(member.ToString(), TextTokenHelper.GetTextTokenType(member));
		}

		public virtual void WriteToolTip(ITextOutput output, IVariable variable, string name)
		{
			output.Write(variable is Local ? "(local variable)" : "(parameter)", TextTokenType.Text);
			output.WriteSpace();
			WriteToolTip(output, variable.Type.ToTypeDefOrRef(), variable is Parameter ? ((Parameter)variable).ParamDef : null);
			output.WriteSpace();
			output.Write(GetName(variable, name), variable is Local ? TextTokenType.Local : TextTokenType.Parameter);
		}

		protected static string GetName(IVariable variable, string name)
		{
			if (!string.IsNullOrWhiteSpace(name))
				return name;
			var n = variable.Name;
			if (!string.IsNullOrWhiteSpace(n))
				return n;
			return string.Format("#{0}", variable.Index);
		}

		public virtual string FormatPropertyName(PropertyDef property, bool? isIndexer = null)
		{
			if (property == null)
				throw new ArgumentNullException("property");
			return property.Name;
		}
		
		public virtual string FormatTypeName(TypeDef type)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			return type.Name;
		}

		/// <summary>
		/// Used for WPF keyboard navigation.
		/// </summary>
		public override string ToString()
		{
			return Name;
		}

		public virtual bool ShowMember(IMemberRef member)
		{
			return true;
		}

		/// <summary>
		/// Used by the analyzer to map compiler generated code back to the original code's location
		/// </summary>
		public virtual IMemberRef GetOriginalCodeLocation(IMemberRef member)
		{
			return member;
		}
	}
}
