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
using Decompiler;
using ICSharpCode.Decompiler;
using Mono.Cecil;

namespace ICSharpCode.ILSpy
{
	/// <summary>
	/// Decompiler logic for C#.
	/// </summary>
	public class CSharpLanguage : Language
	{
		public override string Name {
			get { return "C#"; }
		}
		
		public override string FileExtension {
			get { return ".cs"; }
		}
		
		public override void DecompileMethod(MethodDefinition method, ITextOutput output, DecompilationOptions options)
		{
			AstBuilder codeDomBuilder = new AstBuilder();
			codeDomBuilder.AddMethod(method);
			codeDomBuilder.GenerateCode(output);
		}
		
		public override void DecompileType(TypeDefinition type, ITextOutput output, DecompilationOptions options)
		{
			AstBuilder codeDomBuilder = new AstBuilder();
			codeDomBuilder.AddType(type);
			codeDomBuilder.GenerateCode(output);
		}
	}
}
