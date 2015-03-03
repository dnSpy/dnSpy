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
using System.IO;
#if DOTNET4
using System.Net;
#else
using System.Web;
#endif
using System.Text;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Utils;

namespace ICSharpCode.AvalonEdit.Highlighting
{
	/// <summary>
	/// RichTextWriter implementation that produces HTML.
	/// </summary>
	class HtmlRichTextWriter : RichTextWriter
	{
		readonly TextWriter htmlWriter;
		readonly HtmlOptions options;
		Stack<string> endTagStack = new Stack<string>();
		bool spaceNeedsEscaping = true;
		bool hasSpace;
		bool needIndentation = true;
		int indentationLevel;
		
		/// <summary>
		/// Creates a new HtmlRichTextWriter instance.
		/// </summary>
		/// <param name="htmlWriter">
		/// The text writer where the raw HTML is written to.
		/// The HtmlRichTextWriter does not take ownership of the htmlWriter;
		/// disposing the HtmlRichTextWriter will not dispose the underlying htmlWriter!
		/// </param>
		/// <param name="options">Options that control the HTML output.</param>
		public HtmlRichTextWriter(TextWriter htmlWriter, HtmlOptions options = null)
		{
			if (htmlWriter == null)
				throw new ArgumentNullException("htmlWriter");
			this.htmlWriter = htmlWriter;
			this.options = options ?? new HtmlOptions();
		}
		
		/// <inheritdoc/>
		public override Encoding Encoding {
			get { return htmlWriter.Encoding; }
		}
		
		/// <inheritdoc/>
		public override void Flush()
		{
			FlushSpace(true); // next char potentially might be whitespace
			htmlWriter.Flush();
		}
		
		/// <inheritdoc/>
		protected override void Dispose(bool disposing)
		{
			if (disposing) {
				FlushSpace(true);
			}
			base.Dispose(disposing);
		}
		
		void FlushSpace(bool nextIsWhitespace)
		{
			if (hasSpace) {
				if (spaceNeedsEscaping || nextIsWhitespace)
					htmlWriter.Write("&nbsp;");
				else
					htmlWriter.Write(' ');
				hasSpace = false;
				spaceNeedsEscaping = true;
			}
		}
		
		void WriteIndentation()
		{
			if (needIndentation) {
				for (int i = 0; i < indentationLevel; i++) {
					WriteChar('\t');
				}
				needIndentation = false;
			}
		}
		
		/// <inheritdoc/>
		public override void Write(char value)
		{
			WriteIndentation();
			WriteChar(value);
		}
		
		static readonly char[] specialChars = { ' ', '\t', '\r', '\n' };
		
		void WriteChar(char c)
		{
			bool isWhitespace = char.IsWhiteSpace(c);
			FlushSpace(isWhitespace);
			switch (c) {
				case ' ':
					if (spaceNeedsEscaping)
						htmlWriter.Write("&nbsp;");
					else
						hasSpace = true;
					break;
				case '\t':
					for (int i = 0; i < options.TabSize; i++) {
						htmlWriter.Write("&nbsp;");
					}
					break;
				case '\r':
					break; // ignore; we'll write the <br/> with the following \n
				case '\n':
					htmlWriter.Write("<br/>");
					needIndentation = true;
					break;
				default:
					#if DOTNET4
					WebUtility.HtmlEncode(c.ToString(), htmlWriter);
					#else
					HttpUtility.HtmlEncode(c.ToString(), htmlWriter);
					#endif
					break;
			}
			// If we just handled a space by setting hasSpace = true,
			// we mustn't set spaceNeedsEscaping as doing so would affect our own space,
			// not just the following spaces.
			if (c != ' ') {
				// Following spaces must be escaped if c was a newline/tab;
				// and they don't need escaping if c was a normal character.
				spaceNeedsEscaping = isWhitespace;
			}
		}
		
		/// <inheritdoc/>
		public override void Write(string value)
		{
			int pos = 0;
			do {
				int endPos = value.IndexOfAny(specialChars, pos);
				if (endPos < 0) {
					WriteSimpleString(value.Substring(pos));
					return; // reached end of string
				}
				if (endPos > pos)
					WriteSimpleString(value.Substring(pos, endPos - pos));
				WriteChar(value[pos]);
				pos = endPos + 1;
			} while (pos < value.Length);
		}
		
		void WriteIndentationAndSpace()
		{
			WriteIndentation();
			FlushSpace(false);
		}
		
		void WriteSimpleString(string value)
		{
			if (value.Length == 0)
				return;
			WriteIndentationAndSpace();
			#if DOTNET4
			WebUtility.HtmlEncode(value, htmlWriter);
			#else
			HttpUtility.HtmlEncode(value, htmlWriter);
			#endif
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
		protected override void BeginUnhandledSpan()
		{
			endTagStack.Push(null);
		}
		
		/// <inheritdoc/>
		public override void EndSpan()
		{
			htmlWriter.Write(endTagStack.Pop());
		}
		
		/// <inheritdoc/>
		public override void BeginSpan(Color foregroundColor)
		{
			BeginSpan(new HighlightingColor { Foreground = new SimpleHighlightingBrush(foregroundColor) });
		}
		
		/// <inheritdoc/>
		public override void BeginSpan(FontFamily fontFamily)
		{
			BeginUnhandledSpan(); // TODO
		}
		
		/// <inheritdoc/>
		public override void BeginSpan(FontStyle fontStyle)
		{
			BeginSpan(new HighlightingColor { FontStyle = fontStyle });
		}
		
		/// <inheritdoc/>
		public override void BeginSpan(FontWeight fontWeight)
		{
			BeginSpan(new HighlightingColor { FontWeight = fontWeight });
		}
		
		/// <inheritdoc/>
		public override void BeginSpan(HighlightingColor highlightingColor)
		{
			WriteIndentationAndSpace();
			if (options.ColorNeedsSpanForStyling(highlightingColor)) {
				htmlWriter.Write("<span");
				options.WriteStyleAttributeForColor(htmlWriter, highlightingColor);
				htmlWriter.Write('>');
				endTagStack.Push("</span>");
			} else {
				endTagStack.Push(null);
			}
		}
		
		/// <inheritdoc/>
		public override void BeginHyperlinkSpan(Uri uri)
		{
			WriteIndentationAndSpace();
			#if DOTNET4
			string link = WebUtility.HtmlEncode(uri.ToString());
			#else
			string link = HttpUtility.HtmlEncode(uri.ToString());
			#endif
			htmlWriter.Write("<a href=\"" + link + "\">");
			endTagStack.Push("</a>");
		}
	}
}
