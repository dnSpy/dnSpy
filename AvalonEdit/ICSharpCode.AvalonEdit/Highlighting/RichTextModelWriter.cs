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
using System.Windows.Media;
using ICSharpCode.NRefactory.Editor;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Utils;

namespace ICSharpCode.AvalonEdit.Highlighting
{
	/// <summary>
	/// A RichTextWriter that writes into a document and RichTextModel.
	/// </summary>
	class RichTextModelWriter : PlainRichTextWriter
	{
		readonly RichTextModel richTextModel;
		readonly DocumentTextWriter documentTextWriter;
		readonly Stack<HighlightingColor> colorStack = new Stack<HighlightingColor>();
		HighlightingColor currentColor;
		int currentColorBegin = -1;
		
		/// <summary>
		/// Creates a new RichTextModelWriter that inserts into document, starting at insertionOffset.
		/// </summary>
		public RichTextModelWriter(RichTextModel richTextModel, IDocument document, int insertionOffset)
			: base(new DocumentTextWriter(document, insertionOffset))
		{
			if (richTextModel == null)
				throw new ArgumentNullException("richTextModel");
			this.richTextModel = richTextModel;
			this.documentTextWriter = (DocumentTextWriter)base.textWriter;
			currentColor = richTextModel.GetHighlightingAt(Math.Max(0, insertionOffset - 1));
		}
		
		/// <summary>
		/// Gets/Sets the current insertion offset.
		/// </summary>
		public int InsertionOffset {
			get { return documentTextWriter.InsertionOffset; }
			set { documentTextWriter.InsertionOffset = value; }
		}
		
		
		/// <inheritdoc/>
		protected override void BeginUnhandledSpan()
		{
			colorStack.Push(currentColor);
		}
		
		void BeginColorSpan()
		{
			WriteIndentationIfNecessary();
			colorStack.Push(currentColor);
			currentColor = currentColor.Clone();
			currentColorBegin = documentTextWriter.InsertionOffset;
		}
		
		/// <inheritdoc/>
		public override void EndSpan()
		{
			currentColor = colorStack.Pop();
			currentColorBegin = documentTextWriter.InsertionOffset;
		}
		
		/// <inheritdoc/>
		protected override void AfterWrite()
		{
			base.AfterWrite();
			richTextModel.SetHighlighting(currentColorBegin, documentTextWriter.InsertionOffset - currentColorBegin, currentColor);
		}
		
		/// <inheritdoc/>
		public override void BeginSpan(Color foregroundColor)
		{
			BeginColorSpan();
			currentColor.Foreground = new SimpleHighlightingBrush(foregroundColor);
			currentColor.Freeze();
		}
		
		/// <inheritdoc/>
		public override void BeginSpan(FontFamily fontFamily)
		{
			BeginUnhandledSpan(); // TODO
		}
		
		/// <inheritdoc/>
		public override void BeginSpan(FontStyle fontStyle)
		{
			BeginColorSpan();
			currentColor.FontStyle = fontStyle;
			currentColor.Freeze();
		}
		
		/// <inheritdoc/>
		public override void BeginSpan(FontWeight fontWeight)
		{
			BeginColorSpan();
			currentColor.FontWeight = fontWeight;
			currentColor.Freeze();
		}
		
		/// <inheritdoc/>
		public override void BeginSpan(HighlightingColor highlightingColor)
		{
			BeginColorSpan();
			currentColor.MergeWith(highlightingColor);
			currentColor.Freeze();
		}
	}
}
