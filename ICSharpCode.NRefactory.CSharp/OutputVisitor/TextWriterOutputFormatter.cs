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
using System.IO;

namespace ICSharpCode.NRefactory.CSharp
{
	/// <summary>
	/// Writes C# code into a TextWriter.
	/// </summary>
	public class TextWriterOutputFormatter : IOutputFormatter
	{
		readonly TextWriter textWriter;
		int indentation;
		bool needsIndent = true;

		public int Indentation {
			get {
				return this.indentation;
			}
			set {
				this.indentation = value;
			}
		}
		
		public string IndentationString { get; set; }
		
		public TextWriterOutputFormatter(TextWriter textWriter)
		{
			if (textWriter == null)
				throw new ArgumentNullException("textWriter");
			this.textWriter = textWriter;
			this.IndentationString = "\t";
		}
		
		public void WriteIdentifier(string ident)
		{
			WriteIndentation();
			textWriter.Write(ident);
		}
		
		public void WriteKeyword(string keyword)
		{
			WriteIndentation();
			textWriter.Write(keyword);
		}
		
		public void WriteToken(string token)
		{
			WriteIndentation();
			textWriter.Write(token);
		}
		
		public void Space()
		{
			WriteIndentation();
			textWriter.Write(' ');
		}
		
		public void OpenBrace(BraceStyle style)
		{
			bool isAtStartOfLine = needsIndent;
			switch (style) {
				case BraceStyle.DoNotChange:
				case BraceStyle.EndOfLine:
					WriteIndentation();
					if (!isAtStartOfLine)
						textWriter.Write(' ');
					textWriter.Write('{');
					break;
				case BraceStyle.EndOfLineWithoutSpace:
					WriteIndentation();
					textWriter.Write('{');
					break;
				case BraceStyle.NextLine:
					if (!isAtStartOfLine)
						NewLine();
					WriteIndentation();
					textWriter.Write('{');
					break;
					
				case BraceStyle.NextLineShifted:
					NewLine ();
					Indent();
					WriteIndentation();
					textWriter.Write('{');
					NewLine();
					return;
				case BraceStyle.NextLineShifted2:
					NewLine ();
					Indent();
					WriteIndentation();
					textWriter.Write('{');
					break;
				default:
					throw new ArgumentOutOfRangeException ();
			}
			Indent();
			NewLine();
		}
		
		public void CloseBrace(BraceStyle style)
		{
			switch (style) {
				case BraceStyle.DoNotChange:
				case BraceStyle.EndOfLine:
				case BraceStyle.EndOfLineWithoutSpace:
				case BraceStyle.NextLine:
					Unindent();
					WriteIndentation();
					textWriter.Write('}');
					break;
				case BraceStyle.NextLineShifted:
					WriteIndentation();
					textWriter.Write('}');
					Unindent();
					break;
				case BraceStyle.NextLineShifted2:
					Unindent();
					WriteIndentation();
					textWriter.Write('}');
					Unindent();
					break;
				default:
					throw new ArgumentOutOfRangeException ();
			}
		}
		
		void WriteIndentation()
		{
			if (needsIndent) {
				needsIndent = false;
				for (int i = 0; i < indentation; i++) {
					textWriter.Write(this.IndentationString);
				}
			}
		}
		
		public void NewLine()
		{
			textWriter.WriteLine();
			needsIndent = true;
		}
		
		public void Indent()
		{
			indentation++;
		}
		
		public void Unindent()
		{
			indentation--;
		}
		
		public void WriteComment(CommentType commentType, string content)
		{
			WriteIndentation();
			switch (commentType) {
				case CommentType.SingleLine:
					textWriter.Write("//");
					textWriter.WriteLine(content);
					needsIndent = true;
					break;
				case CommentType.MultiLine:
					textWriter.Write("/*");
					textWriter.Write(content);
					textWriter.Write("*/");
					break;
				case CommentType.Documentation:
					textWriter.Write("///");
					textWriter.WriteLine(content);
					needsIndent = true;
					break;
				default:
					textWriter.Write(content);
					break;
			}
		}
		
		public void WritePreProcessorDirective(PreProcessorDirectiveType type, string argument)
		{
			// pre-processor directive must start on its own line
			if (!needsIndent)
				NewLine();
			WriteIndentation();
			textWriter.Write('#');
			textWriter.Write(type.ToString().ToLowerInvariant());
			if (!string.IsNullOrEmpty(argument)) {
				textWriter.Write(' ');
				textWriter.Write(argument);
			}
			NewLine();
		}
		
		public virtual void StartNode(AstNode node)
		{
		}
		
		public virtual void EndNode(AstNode node)
		{
		}
	}
}
