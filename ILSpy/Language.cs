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
using System.Collections.ObjectModel;
using System.Linq;
using ICSharpCode.Decompiler;
using Mono.Cecil;

namespace ICSharpCode.ILSpy
{
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
		
		/// <summary>
		/// Gets the syntax highlighting used for this language.
		/// </summary>
		public virtual ICSharpCode.AvalonEdit.Highlighting.IHighlightingDefinition SyntaxHighlighting {
			get {
				return ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.GetDefinitionByExtension(this.FileExtension);
			}
		}
		
		public virtual void DecompileMethod(MethodDefinition method, ITextOutput output, DecompilationOptions options)
		{
			WriteCommentLine(output, TypeToString(method.DeclaringType, true) + "." + method.Name);
		}
		
		public virtual void DecompileProperty(PropertyDefinition property, ITextOutput output, DecompilationOptions options)
		{
			WriteCommentLine(output, TypeToString(property.DeclaringType, true) + "." + property.Name);
		}
		
		public virtual void DecompileField(FieldDefinition field, ITextOutput output, DecompilationOptions options)
		{
			WriteCommentLine(output, TypeToString(field.DeclaringType, true) + "." + field.Name);
		}
		
		public virtual void DecompileEvent(EventDefinition ev, ITextOutput output, DecompilationOptions options)
		{
			WriteCommentLine(output, TypeToString(ev.DeclaringType, true) + "." + ev.Name);
		}
		
		public virtual void DecompileType(TypeDefinition type, ITextOutput output, DecompilationOptions options)
		{
			WriteCommentLine(output, TypeToString(type, true));
		}
		
		public virtual void DecompileNamespace(string nameSpace, IEnumerable<TypeDefinition> types, ITextOutput output, DecompilationOptions options)
		{
			WriteCommentLine(output, nameSpace);
		}
		
		public virtual void DecompileAssembly(AssemblyDefinition assembly, string fileName, ITextOutput output, DecompilationOptions options)
		{
			WriteCommentLine(output, fileName);
			WriteCommentLine(output, assembly.Name.FullName);
		}
		
		public virtual void WriteCommentLine(ITextOutput output, string comment)
		{
			output.WriteLine("// " + comment);
		}
		
		/// <summary>
		/// Converts a type reference into a string. This method is used by the member tree node for parameter and return types.
		/// </summary>
		public virtual string TypeToString(TypeReference type, bool includeNamespace, ICustomAttributeProvider typeAttributes = null)
		{
			if (includeNamespace)
				return type.FullName;
			else
				return type.Name;
		}
		
		/// <summary>
		/// Used for WPF keyboard navigation.
		/// </summary>
		public override string ToString()
		{
			return Name;
		}
		
		public virtual bool ShowMember(MemberReference member)
		{
			return true;
		}
	}
	
	public static class Languages
	{
		/// <summary>
		/// A list of all languages.
		/// </summary>
		public static readonly ReadOnlyCollection<Language> AllLanguages = new List<Language>(
			new Language[] {
				new CSharpLanguage(),
				new ILLanguage(true)
			}
			#if DEBUG
			.Concat(CSharpLanguage.GetDebugLanguages())
			#endif
		).AsReadOnly();
		
		/// <summary>
		/// Gets a language using its name.
		/// If the language is not found, C# is returned instead.
		/// </summary>
		public static Language GetLanguage(string name)
		{
			return AllLanguages.FirstOrDefault(l => l.Name == name) ?? AllLanguages.First();
		}
	}
}
