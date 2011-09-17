// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;

using ICSharpCode.AvalonEdit.Document;

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
			b.AppendLine("Version:1.0");
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
		public static string CreateHtmlFragment(TextDocument document, IHighlighter highlighter, ISegment segment, HtmlOptions options)
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
			DocumentLine line = document.GetLineByOffset(segment.Offset);
			while (line != null && line.Offset < segmentEndOffset) {
				HighlightedLine highlightedLine;
				if (highlighter != null)
					highlightedLine = highlighter.HighlightLine(line.LineNumber);
				else
					highlightedLine = new HighlightedLine(document, line);
				SimpleSegment s = segment.GetOverlap(line);
				if (html.Length > 0)
					html.AppendLine("<br>");
				html.Append(highlightedLine.ToHtml(s.Offset, s.EndOffset, options));
				line = line.NextLine;
			}
			return html.ToString();
		}
		
		/// <summary>
		/// Escapes text and writes the result to the StringBuilder.
		/// </summary>
		internal static void EscapeHtml(StringWriter w, string text, HtmlOptions options)
		{
			int spaceCount = -1;
			foreach (char c in text) {
				if (c == ' ') {
					if (spaceCount < 0)
						w.Write("&nbsp;");
					else
						spaceCount++;
				} else if (c == '\t') {
					if (spaceCount < 0)
						spaceCount = 0;
					spaceCount += options.TabSize;
				} else {
					if (spaceCount == 1) {
						w.Write(' ');
					} else if (spaceCount >= 1) {
						for (int i = 0; i < spaceCount; i++) {
							w.Write("&nbsp;");
						}
					}
					spaceCount = 0;
					switch (c) {
						case '<':
							w.Write("&lt;");
							break;
						case '>':
							w.Write("&gt;");
							break;
						case '&':
							w.Write("&amp;");
							break;
						case '"':
							w.Write("&quot;");
							break;
						default:
							w.Write(c);
							break;
					}
				}
			}
			for (int i = 0; i < spaceCount; i++) {
				w.Write("&nbsp;");
			}
		}
	}
	
	/// <summary>
	/// Holds options for converting text to HTML.
	/// </summary>
	public class HtmlOptions
	{
		/// <summary>
		/// Creates a default HtmlOptions instance.
		/// </summary>
		public HtmlOptions()
		{
			this.TabSize = 4;
		}
		
		/// <summary>
		/// Creates a new HtmlOptions instance that copies applicable options from the <see cref="TextEditorOptions"/>.
		/// </summary>
		public HtmlOptions(TextEditorOptions options)
			: this()
		{
			if (options == null)
				throw new ArgumentNullException("options");
			this.TabSize = options.IndentationSize;
		}
		
		/// <summary>
		/// The amount of spaces a tab gets converted to.
		/// </summary>
		public int TabSize { get; set; }
		
		/// <summary>
		/// Writes the HTML attribute for the style to the text writer.
		/// </summary>
		public virtual void WriteStyleAttributeForColor(TextWriter writer, HighlightingColor color)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");
			if (color == null)
				throw new ArgumentNullException("color");
			writer.Write(" style=\"");
			writer.Write(color.ToCss());
			writer.Write("\"");
		}
		
		/// <summary>
		/// Gets whether the color needs to be written out to HTML.
		/// </summary>
		public virtual bool ColorNeedsSpanForStyling(HighlightingColor color)
		{
			if (color == null)
				throw new ArgumentNullException("color");
			return !string.IsNullOrEmpty(color.ToCss());
		}
	}
}
