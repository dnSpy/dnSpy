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
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Utils;
#if NREFACTORY
using ICSharpCode.NRefactory.Editor;
#endif

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
						if (string.IsNullOrEmpty(newText)) {
							// place caret at the beginning of the selection
							if (start.CompareTo(end) <= 0)
								textArea.Caret.Position = start;
							else
								textArea.Caret.Position = end;
						} else {
							// place caret so that it ends up behind the new text
							textArea.Caret.Offset = segmentsToDelete[i].EndOffset;
						}
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
		
		public override TextViewPosition StartPosition {
			get { return start; }
		}
		
		public override TextViewPosition EndPosition {
			get { return end; }
		}
		
		/// <inheritdoc/>
		public override Selection UpdateOnDocumentChange(DocumentChangeEventArgs e)
		{
			if (e == null)
				throw new ArgumentNullException("e");
			int newStartOffset, newEndOffset;
			if (startOffset <= endOffset) {
				newStartOffset = e.GetNewOffset(startOffset, AnchorMovementType.Default);
				newEndOffset = Math.Max(newStartOffset, e.GetNewOffset(endOffset, AnchorMovementType.BeforeInsertion));
			} else {
				newEndOffset = e.GetNewOffset(endOffset, AnchorMovementType.Default);
				newStartOffset = Math.Max(newEndOffset, e.GetNewOffset(startOffset, AnchorMovementType.BeforeInsertion));
			}
			return Selection.Create(
				textArea,
				new TextViewPosition(textArea.Document.GetLocation(newStartOffset), start.VisualColumn),
				new TextViewPosition(textArea.Document.GetLocation(newEndOffset), end.VisualColumn)
			);
		}
		
		/// <inheritdoc/>
		public override bool IsEmpty {
			get { return startOffset == endOffset && start.VisualColumn == end.VisualColumn; }
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
