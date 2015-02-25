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
using ICSharpCode.AvalonEdit.Document;

namespace ICSharpCode.AvalonEdit.Indentation.CSharp
{
	/// <summary>
	/// Smart indentation for C#.
	/// </summary>
	public class CSharpIndentationStrategy : DefaultIndentationStrategy
	{
		/// <summary>
		/// Creates a new CSharpIndentationStrategy.
		/// </summary>
		public CSharpIndentationStrategy()
		{
		}
		
		/// <summary>
		/// Creates a new CSharpIndentationStrategy and initializes the settings using the text editor options.
		/// </summary>
		public CSharpIndentationStrategy(TextEditorOptions options)
		{
			this.IndentationString = options.IndentationString;
		}
		
		string indentationString = "\t";
		
		/// <summary>
		/// Gets/Sets the indentation string.
		/// </summary>
		public string IndentationString {
			get { return indentationString; }
			set {
				if (string.IsNullOrEmpty(value))
					throw new ArgumentException("Indentation string must not be null or empty");
				indentationString = value;
			}
		}
		
		/// <summary>
		/// Performs indentation using the specified document accessor.
		/// </summary>
		/// <param name="document">Object used for accessing the document line-by-line</param>
		/// <param name="keepEmptyLines">Specifies whether empty lines should be kept</param>
		public void Indent(IDocumentAccessor document, bool keepEmptyLines)
		{
			if (document == null)
				throw new ArgumentNullException("document");
			IndentationSettings settings = new IndentationSettings();
			settings.IndentString = this.IndentationString;
			settings.LeaveEmptyLines = keepEmptyLines;
			
			IndentationReformatter r = new IndentationReformatter();
			r.Reformat(document, settings);
		}
		
		/// <inheritdoc cref="IIndentationStrategy.IndentLine"/>
		public override void IndentLine(TextDocument document, DocumentLine line)
		{
			int lineNr = line.LineNumber;
			TextDocumentAccessor acc = new TextDocumentAccessor(document, lineNr, lineNr);
			Indent(acc, false);
			
			string t = acc.Text;
			if (t.Length == 0) {
				// use AutoIndentation for new lines in comments / verbatim strings.
				base.IndentLine(document, line);
			}
		}
		
		/// <inheritdoc cref="IIndentationStrategy.IndentLines"/>
		public override void IndentLines(TextDocument document, int beginLine, int endLine)
		{
			Indent(new TextDocumentAccessor(document, beginLine, endLine), true);
		}
	}
}
