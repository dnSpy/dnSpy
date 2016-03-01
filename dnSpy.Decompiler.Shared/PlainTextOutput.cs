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
using System.Text;

namespace dnSpy.Decompiler.Shared {
	public sealed class PlainTextOutput : ITextOutput {
		readonly TextWriter writer;
		int indent;
		bool needsIndent;
		readonly char[] outputBuffer;

		int line = 1;
		int column = 1;

		public PlainTextOutput(TextWriter writer) {
			if (writer == null)
				throw new ArgumentNullException("writer");
			this.writer = writer;
			this.outputBuffer = new char[256];
		}

		public PlainTextOutput() {
			this.writer = new StringWriter();
		}

		public TextPosition Location {
			get {
				return new TextPosition(line, column + (needsIndent ? indent : 0));
			}
		}

		public override string ToString() {
			return writer.ToString();
		}

		public void Indent() {
			indent++;
		}

		public void Unindent() {
			indent--;
		}

		void WriteIndent() {
			if (needsIndent) {
				needsIndent = false;
				for (int i = 0; i < indent; i++) {
					writer.Write('\t');
				}
				column += indent;
			}
		}

		public void Write(string text, TextTokenKind tokenKind) {
			WriteIndent();
			writer.Write(text);
			column += text.Length;
		}

		public void Write(string text, int index, int count, TextTokenKind tokenKind) {
			WriteIndent();
			if (index == 0 && text.Length == count)
				writer.Write(text);
			else if (count == 1)
				writer.Write(text[index]);
			else {
				int left = count;
				while (left > 0) {
					int len = Math.Min(outputBuffer.Length, left);
					text.CopyTo(index, outputBuffer, 0, len);
					writer.Write(outputBuffer, 0, len);
					left -= len;
				}
			}
			column += count;
		}

		public void Write(StringBuilder sb, int index, int count, TextTokenKind tokenKind) {
			WriteIndent();
			if (count == 1)
				writer.Write(sb[index]);
			else {
				int left = count;
				while (left > 0) {
					int len = Math.Min(outputBuffer.Length, left);
					sb.CopyTo(index, outputBuffer, 0, len);
					writer.Write(outputBuffer, 0, len);
					left -= len;
				}
			}
			column += count;
		}

		public void WriteLine() {
			writer.WriteLine();
			needsIndent = true;
			line++;
			column = 1;
		}

		public void WriteDefinition(string text, object definition, TextTokenKind tokenKind, bool isLocal) {
			Write(text, TextTokenKind.Text);
		}

		public void WriteReference(string text, object reference, TextTokenKind tokenKind, bool isLocal) {
			Write(text, TextTokenKind.Text);
		}

		void ITextOutput.AddDebugSymbols(MemberMapping methodDebugSymbols) {
		}
	}
}
