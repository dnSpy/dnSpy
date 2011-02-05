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
using System.Text;

namespace ICSharpCode.ILSpy
{
	public interface ITextOutput
	{
		void Indent();
		void Unindent();
		void Write(char ch);
		void Write(string text);
		void WriteCommentLine(string comment);
		void WriteLine();
		void WriteDefinition(string text, object definition);
		void WriteReference(string text, object reference);
		
		void MarkFoldStart(string collapsedText = "...", bool defaultCollapsed = false);
		void MarkFoldEnd();
	}
	
	public class PlainTextOutput : ITextOutput
	{
		readonly StringBuilder b = new StringBuilder();
		int indent;
		bool needsIndent;
		
		public override string ToString()
		{
			return b.ToString();
		}
		
		public void Indent()
		{
			indent++;
		}
		
		public void Unindent()
		{
			indent--;
		}
		
		void WriteIndent()
		{
			if (needsIndent) {
				needsIndent = false;
				for (int i = 0; i < indent; i++) {
					b.Append('\t');
				}
			}
		}
		
		public void Write(char ch)
		{
			WriteIndent();
			b.Append(ch);
		}
		
		public void Write(string text)
		{
			WriteIndent();
			b.Append(text);
		}
		
		public void WriteCommentLine(string comment)
		{
			Write(comment);
			WriteLine();
		}
		
		public void WriteLine()
		{
			b.AppendLine();
			needsIndent = true;
		}
		
		public void WriteDefinition(string text, object definition)
		{
			Write(text);
		}
		
		public void WriteReference(string text, object reference)
		{
			Write(text);
		}
		
		void ITextOutput.MarkFoldStart(string collapsedText, bool defaultCollapsed)
		{
		}
		
		void ITextOutput.MarkFoldEnd()
		{
		}
	}
}
