// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
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
using System.Windows.Input;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace ICSharpCode.ILSpy.TextView
{
	/// <summary>
	/// Creates hyperlinks in the text view.
	/// </summary>
	sealed class ReferenceElementGenerator : VisualLineElementGenerator
	{
		readonly Action<ReferenceSegment> referenceClicked;
		readonly Predicate<ReferenceSegment> isLink;
		
		/// <summary>
		/// The collection of references (hyperlinks).
		/// </summary>
		public TextSegmentCollection<ReferenceSegment> References { get; set; }
		
		public ReferenceElementGenerator(Action<ReferenceSegment> referenceClicked, Predicate<ReferenceSegment> isLink)
		{
			if (referenceClicked == null)
				throw new ArgumentNullException("referenceClicked");
			if (isLink == null)
				throw new ArgumentNullException("isLink");
			this.referenceClicked = referenceClicked;
			this.isLink = isLink;
		}
		
		public override int GetFirstInterestedOffset(int startOffset)
		{
			if (this.References == null)
				return -1;
			// inform AvalonEdit about the next position where we want to build a hyperlink
			var segment = this.References.FindFirstSegmentWithStartAfter(startOffset);
			return segment != null ? segment.StartOffset : -1;
		}
		
		public override VisualLineElement ConstructElement(int offset)
		{
			if (this.References == null)
				return null;
			foreach (var segment in this.References.FindSegmentsContaining(offset)) {
				// skip all non-links
				if (!isLink(segment))
					continue;
				// ensure that hyperlinks don't span several lines (VisualLineElements can't contain line breaks)
				int endOffset = Math.Min(segment.EndOffset, CurrentContext.VisualLine.LastDocumentLine.EndOffset);
				// don't create hyperlinks with length 0
				if (offset < endOffset) {
					return new VisualLineReferenceText(CurrentContext.VisualLine, endOffset - offset, this, segment);
				}
			}
			return null;
		}
		
		internal void JumpToReference(ReferenceSegment referenceSegment)
		{
			referenceClicked(referenceSegment);
		}
	}
	
	/// <summary>
	/// VisualLineElement that represents a piece of text and is a clickable link.
	/// </summary>
	sealed class VisualLineReferenceText : VisualLineText
	{
		readonly ReferenceElementGenerator parent;
		readonly ReferenceSegment referenceSegment;
		
		/// <summary>
		/// Creates a visual line text element with the specified length.
		/// It uses the <see cref="ITextRunConstructionContext.VisualLine"/> and its
		/// <see cref="VisualLineElement.RelativeTextOffset"/> to find the actual text string.
		/// </summary>
		public VisualLineReferenceText(VisualLine parentVisualLine, int length, ReferenceElementGenerator parent, ReferenceSegment referenceSegment) : base(parentVisualLine, length)
		{
			this.parent = parent;
			this.referenceSegment = referenceSegment;
		}
		
		/// <inheritdoc/>
		protected override void OnQueryCursor(QueryCursorEventArgs e)
		{
			e.Handled = true;
			e.Cursor = referenceSegment.IsLocal ? Cursors.Arrow : Cursors.Hand;
		}
		
		/// <inheritdoc/>
		protected override void OnMouseDown(MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left && !e.Handled) {
				parent.JumpToReference(referenceSegment);
				if(!referenceSegment.IsLocal)
					e.Handled = true;
			}
		}
		
		/// <inheritdoc/>
		protected override VisualLineText CreateInstance(int length)
		{
			return new VisualLineReferenceText(ParentVisualLine, length, parent, referenceSegment);
		}
	}
}
