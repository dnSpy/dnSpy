// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

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
			
			if (context.VisualLine.FirstDocumentLine == context.VisualLine.LastDocumentLine) {
				ColorizeLine(currentDocumentLine);
			} else {
				ColorizeLine(currentDocumentLine);
				// ColorizeLine modifies the visual line elements, loop through a copy of the line elements
				foreach (VisualLineElement e in context.VisualLine.Elements.ToArray()) {
					int elementOffset = firstLineStart + e.RelativeTextOffset;
					if (elementOffset >= currentDocumentLineEndOffset) {
						currentDocumentLine = context.Document.GetLineByOffset(elementOffset);
						currentDocumentLineStartOffset = currentDocumentLine.Offset;
						currentDocumentLineEndOffset = currentDocumentLineStartOffset + currentDocumentLine.Length;
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
