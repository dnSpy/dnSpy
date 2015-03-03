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
using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;

using ICSharpCode.NRefactory.Editor;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;

namespace ICSharpCode.AvalonEdit.Utils
{
	/// <summary>
	/// Helps printing documents.
	/// </summary>
	public static class DocumentPrinter
	{
		#if NREFACTORY
		/// <summary>
		/// Converts a readonly TextDocument to a Block and applies the provided highlighting definition.
		/// </summary>
		public static Block ConvertTextDocumentToBlock(ReadOnlyDocument document, IHighlightingDefinition highlightingDefinition)
		{
			IHighlighter highlighter;
			if (highlightingDefinition != null)
				highlighter = new DocumentHighlighter(document, highlightingDefinition);
			else
				highlighter = null;
			return ConvertTextDocumentToBlock(document, highlighter);
		}
		#endif
		
		/// <summary>
		/// Converts an IDocument to a Block and applies the provided highlighter.
		/// </summary>
		public static Block ConvertTextDocumentToBlock(IDocument document, IHighlighter highlighter)
		{
			if (document == null)
				throw new ArgumentNullException("document");
			Paragraph p = new Paragraph();
			p.TextAlignment = TextAlignment.Left;
			for (int lineNumber = 1; lineNumber <= document.LineCount; lineNumber++) {
				if (lineNumber > 1)
					p.Inlines.Add(new LineBreak());
				var line = document.GetLineByNumber(lineNumber);
				if (highlighter != null) {
					HighlightedLine highlightedLine = highlighter.HighlightLine(lineNumber);
					p.Inlines.AddRange(highlightedLine.ToRichText().CreateRuns());
				} else {
					p.Inlines.Add(document.GetText(line));
				}
			}
			return p;
		}
		
		#if NREFACTORY
		/// <summary>
		/// Converts a readonly TextDocument to a RichText and applies the provided highlighting definition.
		/// </summary>
		public static RichText ConvertTextDocumentToRichText(ReadOnlyDocument document, IHighlightingDefinition highlightingDefinition)
		{
			IHighlighter highlighter;
			if (highlightingDefinition != null)
				highlighter = new DocumentHighlighter(document, highlightingDefinition);
			else
				highlighter = null;
			return ConvertTextDocumentToRichText(document, highlighter);
		}
		#endif
		
		/// <summary>
		/// Converts an IDocument to a RichText and applies the provided highlighter.
		/// </summary>
		public static RichText ConvertTextDocumentToRichText(IDocument document, IHighlighter highlighter)
		{
			if (document == null)
				throw new ArgumentNullException("document");
			var texts = new List<RichText>();
			for (int lineNumber = 1; lineNumber <= document.LineCount; lineNumber++) {
				var line = document.GetLineByNumber(lineNumber);
				if (lineNumber > 1)
					texts.Add(line.PreviousLine.DelimiterLength == 2 ? "\r\n" : "\n");
				if (highlighter != null) {
					HighlightedLine highlightedLine = highlighter.HighlightLine(lineNumber);
					texts.Add(highlightedLine.ToRichText());
				} else {
					texts.Add(document.GetText(line));
				}
			}
			return RichText.Concat(texts.ToArray());
		}
		
		/// <summary>
		/// Creates a flow document from the editor's contents.
		/// </summary>
		public static FlowDocument CreateFlowDocumentForEditor(TextEditor editor)
		{
			IHighlighter highlighter = editor.TextArea.GetService(typeof(IHighlighter)) as IHighlighter;
			FlowDocument doc = new FlowDocument(ConvertTextDocumentToBlock(editor.Document, highlighter));
			doc.FontFamily = editor.FontFamily;
			doc.FontSize = editor.FontSize;
			return doc;
		}
	}
}
