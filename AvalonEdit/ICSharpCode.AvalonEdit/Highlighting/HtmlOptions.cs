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
#if DOTNET4
using System.Net;
#else
using System.Web;
#endif

namespace ICSharpCode.AvalonEdit.Highlighting
{
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
		public HtmlOptions(TextEditorOptions options) : this()
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
			#if DOTNET4
			WebUtility.HtmlEncode(color.ToCss(), writer);
			#else
			HttpUtility.HtmlEncode(color.ToCss(), writer);
			#endif
			writer.Write('"');
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
