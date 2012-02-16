// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;

using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Utils;

namespace ICSharpCode.AvalonEdit.Editing
{
	/// <summary>
	/// A simple selection.
	/// </summary>
	sealed class SimpleSelection : Selection
	{
		readonly TextViewPosition start, end;
		readonly int startOffset, endOffset;
		
		/// <summary>
		/// Creates a new SimpleSelection instance.
		/// </summary>
		internal SimpleSelection(TextArea textArea, TextViewPosition start, TextViewPosition end)
			: base(textArea)
		{
			this.start = start;
			this.end = end;
			this.startOffset = textArea.Document.GetOffset(start.Location);
			this.endOffset = textArea.Document.GetOffset(end.Location);
		}
		
		/// <inheritdoc/>
		public override IEnumerable<SelectionSegment> Segments {
			get {
				return ExtensionMethods.Sequence<SelectionSegment>(new SelectionSegment(startOffset, start.VisualColumn, endOffset, end.VisualColumn));
			}
		}
		
		/// <inheritdoc/>
		public override ISegment SurroundingSegment {
			get {
				return new SelectionSegment(startOffset, endOffset);
			}
		}
		
		/// <inheritdoc/>
		public override void ReplaceSelectionWithText(string newText)
		{
			if (newText == null)
				throw new ArgumentNullException("newText");
			using (textArea.Document.RunUpdate()) {
				ISegment[] segmentsToDelete = textArea.GetDeletableSegments(this.SurroundingSegment);
				for (int i = segmentsToDelete.Length - 1; i >= 0; i--) {
					if (i == segmentsToDelete.Length - 1) {
						if (segmentsToDelete[i].Offset == SurroundingSegment.Offset && segmentsToDelete[i].Length == SurroundingSegment.Length) {
							newText = AddSpacesIfRequired(newText, start, end);
						}
						int vc = textArea.Caret.VisualColumn;
						textArea.Caret.Offset = segmentsToDelete[i].EndOffset;
						if (string.IsNullOrEmpty(newText))
							textArea.Caret.VisualColumn = vc;
						textArea.Document.Replace(segmentsToDelete[i], newText);
					} else {
						textArea.Document.Remove(segmentsToDelete[i]);
					}
				}
				if (segmentsToDelete.Length != 0) {
					textArea.ClearSelection();
				}
			}
		}
		
		/// <summary>
		/// Gets the start offset.
		/// </summary>
		public int StartOffset {
			get { return startOffset; }
		}
		
		/// <summary>
		/// Gets the end offset.
		/// </summary>
		public int EndOffset {
			get { return endOffset; }
		}
		
		/// <inheritdoc/>
		public override Selection UpdateOnDocumentChange(DocumentChangeEventArgs e)
		{
			if (e == null)
				throw new ArgumentNullException("e");
			return Selection.Create(
				textArea,
				new TextViewPosition(textArea.Document.GetLocation(e.GetNewOffset(startOffset, AnchorMovementType.Default)), start.VisualColumn),
				new TextViewPosition(textArea.Document.GetLocation(e.GetNewOffset(endOffset, AnchorMovementType.Default)), end.VisualColumn)
			);
		}
		
		/// <inheritdoc/>
		public override bool IsEmpty {
			get { return startOffset == endOffset; }
		}
		
		/// <inheritdoc/>
		public override int Length {
			get {
				return Math.Abs(endOffset - startOffset);
			}
		}
		
		/// <inheritdoc/>
		public override Selection SetEndpoint(TextViewPosition endPosition)
		{
			return Create(textArea, start, endPosition);
		}
		
		public override Selection StartSelectionOrSetEndpoint(TextViewPosition startPosition, TextViewPosition endPosition)
		{
			var document = textArea.Document;
			if (document == null)
				throw ThrowUtil.NoDocumentAssigned();
			return Create(textArea, start, endPosition);
		}
		
		/// <inheritdoc/>
		public override int GetHashCode()
		{
			unchecked {
				return startOffset * 27811 + endOffset + textArea.GetHashCode();
			}
		}
		
		/// <inheritdoc/>
		public override bool Equals(object obj)
		{
			SimpleSelection other = obj as SimpleSelection;
			if (other == null) return false;
			return this.start.Equals(other.start) && this.end.Equals(other.end)
				&& this.startOffset == other.startOffset && this.endOffset == other.endOffset
				&& this.textArea == other.textArea;
		}
		
		/// <inheritdoc/>
		public override string ToString()
		{
			return "[SimpleSelection Start=" + start + " End=" + end + "]";
		}
	}
}
