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
using System.IO;
using ICSharpCode.NRefactory;

namespace ICSharpCode.Decompiler
{
	public sealed class PlainTextOutput : ITextOutput
	{
		readonly TextWriter writer;
		int indent;
		bool needsIndent;
		
		int line = 1;
		int column = 1;
		
		public PlainTextOutput(TextWriter writer)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");
			this.writer = writer;
		}
		
		public PlainTextOutput()
		{
			this.writer = new StringWriter();
		}
		
		public TextLocation Location {
			get {
				return new TextLocation(line, column + (needsIndent ? indent : 0));
			}
		}
		
		public override string ToString()
		{
			return writer.ToString();
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
					writer.Write('\t');
				}
				column += indent;
			}
		}
		
		public void Write(char ch)
		{
			WriteIndent();
			writer.Write(ch);
			column++;
		}
		
		public void Write(string text)
		{
			WriteIndent();
			writer.Write(text);
			column += text.Length;
		}
		
		public void WriteLine()
		{
			writer.WriteLine();
			needsIndent = true;
			line++;
			column = 1;
		}
		
		public void WriteDefinition(string text, object definition, bool isLocal)
		{
			Write(text);
		}
		
		public void WriteReference(string text, object reference, bool isLocal)
		{
			Write(text);
		}
		
		void ITextOutput.MarkFoldStart(string collapsedText, bool defaultCollapsed)
		{
		}
		
		void ITextOutput.MarkFoldEnd()
		{
		}
		
		void ITextOutput.AddDebugSymbols(MethodDebugSymbols methodDebugSymbols)
		{
		}
	}
}
