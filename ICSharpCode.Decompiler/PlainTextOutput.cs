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

using ICSharpCode.NRefactory;
using Mono.Cecil;

namespace ICSharpCode.Decompiler
{
	/// <summary>
	/// Base text output.
	/// <remarks>Provides access to code mappings.</remarks>
	/// </summary>
	public abstract class BaseTextOutput : ITextOutput
	{
		#region Code mappings
		Dictionary<int, MemberMapping> codeMappings = new Dictionary<int, MemberMapping>();
		
		public Dictionary<int, MemberMapping> CodeMappings {
			get { return codeMappings; }
		}
		
		public virtual void AddDebuggerMemberMapping(MemberMapping memberMapping)
		{
			if (memberMapping == null)
				throw new ArgumentNullException("memberMapping");
			
			int token = memberMapping.MetadataToken;
			codeMappings.Add(token, memberMapping);
		}
		
		#endregion
		
		#region ITextOutput members
		public abstract TextLocation Location { get; }
		public abstract void Indent();
		public abstract void Unindent();
		public abstract void Write(char ch);
		public abstract void Write(string text);
		public abstract void WriteLine();
		public abstract void WriteDefinition(string text, object definition);
		public abstract void WriteReference(string text, object reference, bool isLocal);
		public abstract void MarkFoldStart(string collapsedText, bool defaultCollapsed);
		public abstract void MarkFoldEnd();
		#endregion
	}
	
	/// <summary>
	/// Plain text output.
	/// <remarks>Can be used when there's no UI.</remarks>
	/// </summary>
	public sealed class PlainTextOutput : BaseTextOutput
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
		
		public override TextLocation Location {
			get {
				return new TextLocation(line, column + (needsIndent ? indent : 0));
			}
		}
		
		public override string ToString()
		{
			return writer.ToString();
		}
		
		public override void Indent()
		{
			indent++;
		}
		
		public override void Unindent()
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
		
		public override void Write(char ch)
		{
			WriteIndent();
			writer.Write(ch);
			column++;
		}
		
		public override void Write(string text)
		{
			WriteIndent();
			writer.Write(text);
			column += text.Length;
		}
		
		public override void WriteLine()
		{
			writer.WriteLine();
			needsIndent = true;
			line++;
			column = 1;
		}
		
		public override void WriteDefinition(string text, object definition)
		{
			Write(text);
		}
		
		public override void WriteReference(string text, object reference, bool isLocal)
		{
			Write(text);
		}
		
		public override void MarkFoldStart(string collapsedText, bool defaultCollapsed)
		{
		}
		
		public override void MarkFoldEnd()
		{
		}
	}
}
