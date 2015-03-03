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
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Highlighting;

namespace ICSharpCode.AvalonEdit.Utils
{
	// TODO: This class (and derived classes) is currently unused; decide whether to keep it.
	// (until this is decided, keep the class internal)
	
	/// <summary>
	/// A text writer that supports creating spans of highlighted text.
	/// </summary>
	abstract class RichTextWriter : TextWriter
	{
		/// <summary>
		/// Gets called by the RichTextWriter base class when a BeginSpan() method
		/// that is not overwritten gets called.
		/// </summary>
		protected abstract void BeginUnhandledSpan();
		
		/// <summary>
		/// Writes the RichText instance.
		/// </summary>
		public void Write(RichText richText)
		{
			Write(richText, 0, richText.Length);
		}
		
		/// <summary>
		/// Writes the RichText instance.
		/// </summary>
		public virtual void Write(RichText richText, int offset, int length)
		{
			foreach (var section in richText.GetHighlightedSections(offset, length)) {
				BeginSpan(section.Color);
				Write(richText.Text.Substring(section.Offset, section.Length));
				EndSpan();
			}
		}
		
		/// <summary>
		/// Begin a colored span.
		/// </summary>
		public virtual void BeginSpan(Color foregroundColor)
		{
			BeginUnhandledSpan();
		}
		
		/// <summary>
		/// Begin a span with modified font weight.
		/// </summary>
		public virtual void BeginSpan(FontWeight fontWeight)
		{
			BeginUnhandledSpan();
		}
		
		/// <summary>
		/// Begin a span with modified font style.
		/// </summary>
		public virtual void BeginSpan(FontStyle fontStyle)
		{
			BeginUnhandledSpan();
		}
		
		/// <summary>
		/// Begin a span with modified font family.
		/// </summary>
		public virtual void BeginSpan(FontFamily fontFamily)
		{
			BeginUnhandledSpan();
		}
		
		/// <summary>
		/// Begin a highlighted span.
		/// </summary>
		public virtual void BeginSpan(Highlighting.HighlightingColor highlightingColor)
		{
			BeginUnhandledSpan();
		}
		
		/// <summary>
		/// Begin a span that links to the specified URI.
		/// </summary>
		public virtual void BeginHyperlinkSpan(Uri uri)
		{
			BeginUnhandledSpan();
		}
		
		/// <summary>
		/// Marks the end of the current span.
		/// </summary>
		public abstract void EndSpan();
		
		/// <summary>
		/// Increases the indentation level.
		/// </summary>
		public abstract void Indent();
		
		/// <summary>
		/// Decreases the indentation level.
		/// </summary>
		public abstract void Unindent();
	}
}
