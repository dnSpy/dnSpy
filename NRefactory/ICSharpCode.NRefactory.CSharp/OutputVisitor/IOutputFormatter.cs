// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
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

namespace ICSharpCode.NRefactory.CSharp
{
	/// <summary>
	/// Output formatter for the Output visitor.
	/// </summary>
	public interface IOutputFormatter
	{
		void StartNode(AstNode node);
		void EndNode(AstNode node);
		
		/// <summary>
		/// Writes an identifier.
		/// If the identifier conflicts with a keyword, the output visitor will
		/// call <c>WriteToken("@")</c> before calling WriteIdentifier().
		/// </summary>
		void WriteIdentifier(string identifier);
		
		/// <summary>
		/// Writes a keyword to the output.
		/// </summary>
		void WriteKeyword(string keyword);
		
		/// <summary>
		/// Writes a token to the output.
		/// </summary>
		void WriteToken(string token);
		void Space();
		
		void OpenBrace(BraceStyle style);
		void CloseBrace(BraceStyle style);
		
		void Indent();
		void Unindent();
		
		void NewLine();
		
		void WriteComment(CommentType commentType, string content);
		void WritePreProcessorDirective(PreProcessorDirectiveType type, string argument);
	}
}
