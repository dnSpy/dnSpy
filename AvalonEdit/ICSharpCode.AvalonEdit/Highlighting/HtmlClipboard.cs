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
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Windows;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.NRefactory.Editor;

namespace ICSharpCode.AvalonEdit.Highlighting
{
	/// <summary>
	/// Allows copying HTML text to the clipboard.
	/// </summary>
	public static class HtmlClipboard
	{
		/// <summary>
		/// Builds a header for the CF_HTML clipboard format.
		/// </summary>
		static string BuildHeader(int startHTML, int endHTML, int startFragment, int endFragment)
		{
			StringBuilder b = new StringBuilder();
			b.AppendLine("Version:0.9");
			b.AppendLine("StartHTML:" + startHTML.ToString("d8", CultureInfo.InvariantCulture));
			b.AppendLine("EndHTML:" + endHTML.ToString("d8", CultureInfo.InvariantCulture));
			b.AppendLine("StartFragment:" + startFragment.ToString("d8", CultureInfo.InvariantCulture));
			b.AppendLine("EndFragment:" + endFragment.ToString("d8", CultureInfo.InvariantCulture));
			return b.ToString();
		}
		
		/// <summary>
		/// Sets the TextDataFormat.Html on the data object to the specified html fragment.
		/// This helper methods takes care of creating the necessary CF_HTML header.
		/// </summary>
		public static void SetHtml(DataObject dataObject, string htmlFragment)
		{
			if (dataObject == null)
				throw new ArgumentNullException("dataObject");
			if (htmlFragment == null)
				throw new ArgumentNullException("htmlFragment");
			
			string htmlStart = @"<!DOCTYPE HTML PUBLIC ""-//W3C//DTD HTML 4.0 Transitional//EN"">" + Environment.NewLine
				+ "<HTML>" + Environment.NewLine
				+ "<BODY>" + Environment.NewLine
				+ "<!--StartFragment-->" + Environment.NewLine;
			string htmlEnd = "<!--EndFragment-->" + Environment.NewLine + "</BODY>" + Environment.NewLine + "</HTML>" + Environment.NewLine;
			string dummyHeader = BuildHeader(0, 0, 0, 0);
			// the offsets are stored as UTF-8 bytes (see CF_HTML documentation)
			int startHTML = dummyHeader.Length;
			int startFragment = startHTML + htmlStart.Length;
			int endFragment = startFragment + Encoding.UTF8.GetByteCount(htmlFragment);
			int endHTML = endFragment + htmlEnd.Length;
			string cf_html = BuildHeader(startHTML, endHTML, startFragment, endFragment) + htmlStart + htmlFragment + htmlEnd;
			Debug.WriteLine(cf_html);
			dataObject.SetText(cf_html, TextDataFormat.Html);
		}
		
		/// <summary>
		/// Creates a HTML fragment from a part of a document.
		/// </summary>
		/// <param name="document">The document to create HTML from.</param>
		/// <param name="highlighter">The highlighter used to highlight the document. <c>null</c> is valid and will create HTML without any highlighting.</param>
		/// <param name="segment">The part of the document to create HTML for. You can pass <c>null</c> to create HTML for the whole document.</param>
		/// <param name="options">The options for the HTML creation.</param>
		/// <returns>HTML code for the document part.</returns>
		public static string CreateHtmlFragment(IDocument document, IHighlighter highlighter, ISegment segment, HtmlOptions options)
		{
			if (document == null)
				throw new ArgumentNullException("document");
			if (options == null)
				throw new ArgumentNullException("options");
			if (highlighter != null && highlighter.Document != document)
				throw new ArgumentException("Highlighter does not belong to the specified document.");
			if (segment == null)
				segment = new SimpleSegment(0, document.TextLength);
			
			StringBuilder html = new StringBuilder();
			int segmentEndOffset = segment.EndOffset;
			IDocumentLine line = document.GetLineByOffset(segment.Offset);
			while (line != null && line.Offset < segmentEndOffset) {
				HighlightedLine highlightedLine;
				if (highlighter != null)
					highlightedLine = highlighter.HighlightLine(line.LineNumber);
				else
					highlightedLine = new HighlightedLine(document, line);
				SimpleSegment s = SimpleSegment.GetOverlap(segment, line);
				if (html.Length > 0)
					html.AppendLine("<br>");
				html.Append(highlightedLine.ToHtml(s.Offset, s.EndOffset, options));
				line = line.NextLine;
			}
			return html.ToString();
		}
	}
}
