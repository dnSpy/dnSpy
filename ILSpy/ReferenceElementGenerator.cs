// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Windows.Input;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace ICSharpCode.ILSpy
{
	sealed class ReferenceElementGenerator : VisualLineElementGenerator
	{
		MainWindow parentWindow;
		public TextSegmentCollection<ReferenceSegment> References { get; set; }
		
		public ReferenceElementGenerator(MainWindow parentWindow)
		{
			if (parentWindow == null)
				throw new ArgumentNullException("parentWindow");
			this.parentWindow = parentWindow;
		}
		
		public override int GetFirstInterestedOffset(int startOffset)
		{
			if (this.References == null)
				return -1;
			var segment = this.References.FindFirstSegmentWithStartAfter(startOffset);
			return segment != null ? segment.StartOffset : -1;
		}
		
		public override VisualLineElement ConstructElement(int offset)
		{
			if (this.References == null)
				return null;
			foreach (var segment in this.References.FindSegmentsContaining(offset)) {
				int endOffset = Math.Min(segment.EndOffset, CurrentContext.VisualLine.LastDocumentLine.EndOffset);
				if (offset < endOffset) {
					return new VisualLineReferenceText(CurrentContext.VisualLine, endOffset - offset, this, segment);
				}
			}
			return null;
		}
		
		internal void JumpToReference(ReferenceSegment referenceSegment)
		{
			parentWindow.JumpToReference(referenceSegment);
		}
	}
	
	/// <summary>
	/// VisualLineElement that represents a piece of text and is a clickable link.
	/// </summary>
	sealed class VisualLineReferenceText : VisualLineText
	{
		ReferenceElementGenerator parent;
		ReferenceSegment referenceSegment;
		
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
			e.Cursor = Cursors.Hand;
		}
		
		/// <inheritdoc/>
		protected override void OnMouseDown(MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left && !e.Handled) {
				parent.JumpToReference(referenceSegment);
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
