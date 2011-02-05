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
using System.Text;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;

namespace ICSharpCode.ILSpy.TextView
{
	sealed class ReferenceSegment : TextSegment
	{
		public object Reference;
	}
	
	sealed class DefinitionLookup
	{
		Dictionary<object, int> definitions = new Dictionary<object, int>();
		
		public int GetDefinitionPosition(object definition)
		{
			int val;
			if (definitions.TryGetValue(definition, out val))
				return val;
			else
				return -1;
		}
		
		public void AddDefinition(object definition, int offset)
		{
			definitions[definition] = offset;
		}
	}
	
	sealed class SmartTextOutput : ITextOutput
	{
		readonly StringBuilder b = new StringBuilder();
		int indent;
		bool needsIndent;
		TextSegmentCollection<ReferenceSegment> references = new TextSegmentCollection<ReferenceSegment>();
		Stack<NewFolding> openFoldings = new Stack<NewFolding>();
		
		public readonly List<NewFolding> Foldings = new List<NewFolding>();
		public readonly DefinitionLookup DefinitionLookup = new DefinitionLookup();
		
		public TextSegmentCollection<ReferenceSegment> References {
			get { return references; }
		}
		
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
			WriteIndent();
			b.AppendLine(comment);
			needsIndent = true;
		}
		
		public void WriteLine()
		{
			b.AppendLine();
			needsIndent = true;
		}
		
		public void WriteDefinition(string text, object definition)
		{
			WriteIndent();
			b.Append(text);
			this.DefinitionLookup.AddDefinition(definition, b.Length);
		}
		
		public void WriteReference(string text, object reference)
		{
			WriteIndent();
			int start = b.Length;
			b.Append(text);
			int end = b.Length;
			references.Add(new ReferenceSegment { StartOffset = start, EndOffset = end, Reference = reference });
		}
		
		public void MarkFoldStart(string collapsedText, bool defaultCollapsed)
		{
			WriteIndent();
			openFoldings.Push(new NewFolding { StartOffset = b.Length, Name = collapsedText, DefaultClosed = defaultCollapsed });
		}
		
		public void MarkFoldEnd()
		{
			NewFolding f = openFoldings.Pop();
			f.EndOffset = b.Length;
			this.Foldings.Add(f);
		}
	}
}
