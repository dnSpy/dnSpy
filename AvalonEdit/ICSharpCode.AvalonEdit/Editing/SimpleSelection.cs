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
	public sealed class SimpleSelection : Selection, ISegment
	{
		readonly int startOffset, endOffset;
		
		/// <summary>
		/// Creates a new SimpleSelection instance.
		/// </summary>
		public SimpleSelection(int startOffset, int endOffset)
		{
			this.startOffset = startOffset;
			this.endOffset = endOffset;
		}
		
		/// <summary>
		/// Creates a new SimpleSelection instance.
		/// </summary>
		public SimpleSelection(ISegment segment)
		{
			if (segment == null)
				throw new ArgumentNullException("segment");
			this.startOffset = segment.Offset;
			this.endOffset = startOffset + segment.Length;
		}
		
		/// <inheritdoc/>
		public override IEnumerable<ISegment> Segments {
			get {
				if (!IsEmpty) {
					return ExtensionMethods.Sequence<ISegment>(this);
				} else {
					return Empty<ISegment>.Array;
				}
			}
		}
		
		/// <inheritdoc/>
		public override ISegment SurroundingSegment {
			get {
				if (IsEmpty)
					return null;
				else
					return this;
			}
		}
		
		/// <inheritdoc/>
		public override void ReplaceSelectionWithText(TextArea textArea, string newText)
		{
			if (textArea == null)
				throw new ArgumentNullException("textArea");
			if (newText == null)
				throw new ArgumentNullException("newText");
			using (textArea.Document.RunUpdate()) {
				if (IsEmpty) {
					if (newText.Length > 0) {
						if (textArea.ReadOnlySectionProvider.CanInsert(textArea.Caret.Offset)) {
							textArea.Document.Insert(textArea.Caret.Offset, newText);
						}
					}
				} else {
					ISegment[] segmentsToDelete = textArea.GetDeletableSegments(this);
					for (int i = segmentsToDelete.Length - 1; i >= 0; i--) {
						if (i == segmentsToDelete.Length - 1) {
							textArea.Caret.Offset = segmentsToDelete[i].EndOffset;
							textArea.Document.Replace(segmentsToDelete[i], newText);
						} else {
							textArea.Document.Remove(segmentsToDelete[i]);
						}
					}
					if (segmentsToDelete.Length != 0) {
						textArea.Selection = Selection.Empty;
					}
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
			return new SimpleSelection(
				e.GetNewOffset(startOffset, AnchorMovementType.Default),
				e.GetNewOffset(endOffset, AnchorMovementType.Default)
			);
		}
		
		/// <inheritdoc/>
		public override bool IsEmpty {
			get { return startOffset == endOffset; }
		}
		
		// For segments, Offset must be less than or equal to EndOffset;
		// so we must use Min/Max.
		int ISegment.Offset {
			get { return Math.Min(startOffset, endOffset); }
		}
		
		int ISegment.EndOffset {
			get { return Math.Max(startOffset, endOffset); }
		}
		
		/// <inheritdoc/>
		public override int Length {
			get {
				return Math.Abs(endOffset - startOffset);
			}
		}
		
		/// <inheritdoc/>
		public override Selection SetEndpoint(int newEndOffset)
		{
			if (IsEmpty)
				throw new NotSupportedException();
			else
				return new SimpleSelection(startOffset, newEndOffset);
		}
		
		/// <inheritdoc/>
		public override int GetHashCode()
		{
			return startOffset ^ endOffset;
		}
		
		/// <inheritdoc/>
		public override bool Equals(object obj)
		{
			SimpleSelection other = obj as SimpleSelection;
			if (other == null) return false;
			if (IsEmpty && other.IsEmpty)
				return true;
			return this.startOffset == other.startOffset && this.endOffset == other.endOffset;
		}
		
		/// <inheritdoc/>
		public override string ToString()
		{
			return "[SimpleSelection Start=" + startOffset + " End=" + endOffset + "]";
		}
	}
}
