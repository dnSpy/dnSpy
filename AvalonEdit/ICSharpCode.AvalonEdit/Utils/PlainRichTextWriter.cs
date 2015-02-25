// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
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

namespace ICSharpCode.AvalonEdit.Utils
{
	/// <summary>
	/// RichTextWriter implementation that writes plain text only
	/// and ignores all formatted spans.
	/// </summary>
	class PlainRichTextWriter : RichTextWriter
	{
		/// <summary>
		/// The text writer that was passed to the PlainRichTextWriter constructor.
		/// </summary>
		protected readonly TextWriter textWriter;
		string indentationString = "\t";
		int indentationLevel;
		char prevChar;
		
		/// <summary>
		/// Creates a new PlainRichTextWriter instance that writes the text to the specified text writer.
		/// </summary>
		public PlainRichTextWriter(TextWriter textWriter)
		{
			if (textWriter == null)
				throw new ArgumentNullException("textWriter");
			this.textWriter = textWriter;
		}
		
		/// <summary>
		/// Gets/Sets the string used to indent by one level.
		/// </summary>
		public string IndentationString {
			get {
				return indentationString;
			}
			set {
				indentationString = value;
			}
		}
		
		/// <inheritdoc/>
		protected override void BeginUnhandledSpan()
		{
		}
		
		/// <inheritdoc/>
		public override void EndSpan()
		{
		}
		
		void WriteIndentation()
		{
			for (int i = 0; i < indentationLevel; i++) {
				textWriter.Write(indentationString);
			}
		}
		
		/// <summary>
		/// Writes the indentation, if necessary.
		/// </summary>
		protected void WriteIndentationIfNecessary()
		{
			if (prevChar == '\n') {
				WriteIndentation();
				prevChar = '\0';
			}
		}
		
		/// <summary>
		/// Is called after a write operation.
		/// </summary>
		protected virtual void AfterWrite()
		{
		}
		
		/// <inheritdoc/>
		public override void Write(char value)
		{
			if (prevChar == '\n')
				WriteIndentation();
			textWriter.Write(value);
			prevChar = value;
			AfterWrite();
		}
		
		/// <inheritdoc/>
		public override void Indent()
		{
			indentationLevel++;
		}
		
		/// <inheritdoc/>
		public override void Unindent()
		{
			if (indentationLevel == 0)
				throw new NotSupportedException();
			indentationLevel--;
		}
		
		/// <inheritdoc/>
		public override Encoding Encoding {
			get { return textWriter.Encoding; }
		}
		
		/// <inheritdoc/>
		public override IFormatProvider FormatProvider {
			get { return textWriter.FormatProvider; }
		}
		
		/// <inheritdoc/>
		public override string NewLine {
			get {
				return textWriter.NewLine;
			}
			set {
				textWriter.NewLine = value;
			}
		}
	}
}
