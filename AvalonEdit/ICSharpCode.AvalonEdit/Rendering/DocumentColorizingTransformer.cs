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
using System.Linq;
using ICSharpCode.AvalonEdit.Document;

namespace ICSharpCode.AvalonEdit.Rendering
{
	/// <summary>
	/// Base class for <see cref="IVisualLineTransformer"/> that helps
	/// colorizing the document. Derived classes can work with document lines
	/// and text offsets and this class takes care of the visual lines and visual columns.
	/// </summary>
	public abstract class DocumentColorizingTransformer : ColorizingTransformer
	{
		DocumentLine currentDocumentLine;
		int firstLineStart;
		int currentDocumentLineStartOffset, currentDocumentLineEndOffset;
		
		/// <summary>
		/// Gets the current ITextRunConstructionContext.
		/// </summary>
		protected ITextRunConstructionContext CurrentContext { get; private set; }
		
		/// <inheritdoc/>
		protected override void Colorize(ITextRunConstructionContext context)
		{
			if (context == null)
				throw new ArgumentNullException("context");
			this.CurrentContext = context;
			
			currentDocumentLine = context.VisualLine.FirstDocumentLine;
			firstLineStart = currentDocumentLineStartOffset = currentDocumentLine.Offset;
			currentDocumentLineEndOffset = currentDocumentLineStartOffset + currentDocumentLine.Length;
			int currentDocumentLineTotalEndOffset = currentDocumentLineStartOffset + currentDocumentLine.TotalLength;
			
			if (context.VisualLine.FirstDocumentLine == context.VisualLine.LastDocumentLine) {
				ColorizeLine(currentDocumentLine);
			} else {
				ColorizeLine(currentDocumentLine);
				// ColorizeLine modifies the visual line elements, loop through a copy of the line elements
				foreach (VisualLineElement e in context.VisualLine.Elements.ToArray()) {
					int elementOffset = firstLineStart + e.RelativeTextOffset;
					if (elementOffset >= currentDocumentLineTotalEndOffset) {
						currentDocumentLine = context.Document.GetLineByOffset(elementOffset);
						currentDocumentLineStartOffset = currentDocumentLine.Offset;
						currentDocumentLineEndOffset = currentDocumentLineStartOffset + currentDocumentLine.Length;
						currentDocumentLineTotalEndOffset = currentDocumentLineStartOffset + currentDocumentLine.TotalLength;
						ColorizeLine(currentDocumentLine);
					}
				}
			}
			currentDocumentLine = null;
			this.CurrentContext = null;
		}
		
		/// <summary>
		/// Override this method to colorize an individual document line.
		/// </summary>
		protected abstract void ColorizeLine(DocumentLine line);
		
		/// <summary>
		/// Changes a part of the current document line.
		/// </summary>
		/// <param name="startOffset">Start offset of the region to change</param>
		/// <param name="endOffset">End offset of the region to change</param>
		/// <param name="action">Action that changes an individual <see cref="VisualLineElement"/>.</param>
		protected void ChangeLinePart(int startOffset, int endOffset, Action<VisualLineElement> action)
		{
			if (startOffset < currentDocumentLineStartOffset || startOffset > currentDocumentLineEndOffset)
				throw new ArgumentOutOfRangeException("startOffset", startOffset, "Value must be between " + currentDocumentLineStartOffset + " and " + currentDocumentLineEndOffset);
			if (endOffset < startOffset || endOffset > currentDocumentLineEndOffset)
				throw new ArgumentOutOfRangeException("endOffset", endOffset, "Value must be between " + startOffset + " and " + currentDocumentLineEndOffset);
			VisualLine vl = this.CurrentContext.VisualLine;
			int visualStart = vl.GetVisualColumn(startOffset - firstLineStart);
			int visualEnd = vl.GetVisualColumn(endOffset - firstLineStart);
			if (visualStart < visualEnd) {
				ChangeVisualElements(visualStart, visualEnd, action);
			}
		}
	}
}
