// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Diagnostics;
using System.Globalization;

namespace ICSharpCode.AvalonEdit.Document
{
	/// <summary>
	/// Represents a line inside a <see cref="TextDocument"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The <see cref="TextDocument.Lines"/> collection contains one DocumentLine instance
	/// for every line in the document. This collection is read-only to user code and is automatically
	/// updated to reflect the current document content.
	/// </para>
	/// <para>
	/// Internally, the DocumentLine instances are arranged in a binary tree that allows for both efficient updates and lookup.
	/// Converting between offset and line number is possible in O(lg N) time,
	/// and the data structure also updates all offsets in O(lg N) whenever a line is inserted or removed.
	/// </para>
	/// </remarks>
	public sealed partial class DocumentLine : ISegment
	{
		#region Constructor
		#if DEBUG
		// Required for thread safety check which is done only in debug builds.
		// To save space, we don't store the document reference in release builds as we don't need it there.
		readonly TextDocument document;
		#endif
		
		internal bool isDeleted;
		
		internal DocumentLine(TextDocument document)
		{
			#if DEBUG
			Debug.Assert(document != null);
			this.document = document;
			#endif
		}
		
		[Conditional("DEBUG")]
		void DebugVerifyAccess()
		{
			#if DEBUG
			document.DebugVerifyAccess();
			#endif
		}
		#endregion
		
		#region Events
//		/// <summary>
//		/// Is raised when the line is deleted.
//		/// </summary>
//		public event EventHandler Deleted;
//
//		/// <summary>
//		/// Is raised when the line's text changes.
//		/// </summary>
//		public event EventHandler TextChanged;
//
//		/// <summary>
//		/// Raises the Deleted or TextChanged event.
//		/// </summary>
//		internal void RaiseChanged()
//		{
//			if (IsDeleted) {
//				if (Deleted != null)
//					Deleted(this, EventArgs.Empty);
//			} else {
//				if (TextChanged != null)
//					TextChanged(this, EventArgs.Empty);
//			}
//		}
		#endregion
		
		#region Properties stored in tree
		/// <summary>
		/// Gets if this line was deleted from the document.
		/// </summary>
		public bool IsDeleted {
			get {
				DebugVerifyAccess();
				return isDeleted;
			}
		}
		
		/// <summary>
		/// Gets the number of this line.
		/// Runtime: O(log n)
		/// </summary>
		/// <exception cref="InvalidOperationException">The line was deleted.</exception>
		public int LineNumber {
			get {
				if (IsDeleted)
					throw new InvalidOperationException();
				return DocumentLineTree.GetIndexFromNode(this) + 1;
			}
		}
		
		/// <summary>
		/// Gets the starting offset of the line in the document's text.
		/// Runtime: O(log n)
		/// </summary>
		/// <exception cref="InvalidOperationException">The line was deleted.</exception>
		public int Offset {
			get {
				if (IsDeleted)
					throw new InvalidOperationException();
				return DocumentLineTree.GetOffsetFromNode(this);
			}
		}
		
		/// <summary>
		/// Gets the end offset of the line in the document's text (the offset before the line delimiter).
		/// Runtime: O(log n)
		/// </summary>
		/// <exception cref="InvalidOperationException">The line was deleted.</exception>
		/// <remarks>EndOffset = <see cref="Offset"/> + <see cref="Length"/>.</remarks>
		public int EndOffset {
			get { return this.Offset + this.Length; }
		}
		#endregion
		
		#region Length
		int totalLength;
		byte delimiterLength;
		
		/// <summary>
		/// Gets the length of this line. The length does not include the line delimiter. O(1)
		/// </summary>
		/// <remarks>This property is still available even if the line was deleted;
		/// in that case, it contains the line's length before the deletion.</remarks>
		public int Length {
			get {
				DebugVerifyAccess();
				return totalLength - delimiterLength;
			}
		}
		
		/// <summary>
		/// Gets the length of this line, including the line delimiter. O(1)
		/// </summary>
		/// <remarks>This property is still available even if the line was deleted;
		/// in that case, it contains the line's length before the deletion.</remarks>
		public int TotalLength {
			get {
				DebugVerifyAccess();
				return totalLength;
			}
			internal set {
				// this is set by DocumentLineTree
				totalLength = value;
			}
		}
		
		/// <summary>
		/// <para>Gets the length of the line delimiter.</para>
		/// <para>The value is 1 for single <c>"\r"</c> or <c>"\n"</c>, 2 for the <c>"\r\n"</c> sequence;
		/// and 0 for the last line in the document.</para>
		/// </summary>
		/// <remarks>This property is still available even if the line was deleted;
		/// in that case, it contains the line delimiter's length before the deletion.</remarks>
		public int DelimiterLength {
			get {
				DebugVerifyAccess();
				return delimiterLength;
			}
			internal set {
				Debug.Assert(value >= 0 && value <= 2);
				delimiterLength = (byte)value;
			}
		}
		#endregion
		
		#region Previous / Next Line
		/// <summary>
		/// Gets the next line in the document.
		/// </summary>
		/// <returns>The line following this line, or null if this is the last line.</returns>
		public DocumentLine NextLine {
			get {
				DebugVerifyAccess();
				
				if (right != null) {
					return right.LeftMost;
				} else {
					DocumentLine node = this;
					DocumentLine oldNode;
					do {
						oldNode = node;
						node = node.parent;
						// we are on the way up from the right part, don't output node again
					} while (node != null && node.right == oldNode);
					return node;
				}
			}
		}
		
		/// <summary>
		/// Gets the previous line in the document.
		/// </summary>
		/// <returns>The line before this line, or null if this is the first line.</returns>
		public DocumentLine PreviousLine {
			get {
				DebugVerifyAccess();
				
				if (left != null) {
					return left.RightMost;
				} else {
					DocumentLine node = this;
					DocumentLine oldNode;
					do {
						oldNode = node;
						node = node.parent;
						// we are on the way up from the left part, don't output node again
					} while (node != null && node.left == oldNode);
					return node;
				}
			}
		}
		#endregion
		
		#region ToString
		/// <summary>
		/// Gets a string with debug output showing the line number and offset.
		/// Does not include the line's text.
		/// </summary>
		public override string ToString()
		{
			if (IsDeleted)
				return "[DocumentLine deleted]";
			else
				return string.Format(
					CultureInfo.InvariantCulture,
					"[DocumentLine Number={0} Offset={1} Length={2}]", LineNumber, Offset, Length);
		}
		#endregion
	}
}
