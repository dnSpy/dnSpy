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
using ICSharpCode.AvalonEdit.Utils;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.Editor;
using ICSharpCode.AvalonEdit.Document;

namespace ICSharpCode.AvalonEdit.Document
{
	/// <summary>
	/// The TextAnchor class references an offset (a position between two characters).
	/// It automatically updates the offset when text is inserted/removed in front of the anchor.
	/// </summary>
	/// <remarks>
	/// <para>Use the <see cref="Offset"/> property to get the offset from a text anchor.
	/// Use the <see cref="TextDocument.CreateAnchor"/> method to create an anchor from an offset.
	/// </para>
	/// <para>
	/// The document will automatically update all text anchors; and because it uses weak references to do so,
	/// the garbage collector can simply collect the anchor object when you don't need it anymore.
	/// </para>
	/// <para>Moreover, the document is able to efficiently update a large number of anchors without having to look
	/// at each anchor object individually. Updating the offsets of all anchors usually only takes time logarithmic
	/// to the number of anchors. Retrieving the <see cref="Offset"/> property also runs in O(lg N).</para>
	/// <inheritdoc cref="IsDeleted" />
	/// <inheritdoc cref="MovementType" />
	/// <para>If you want to track a segment, you can use the <see cref="AnchorSegment"/> class which
	/// implements <see cref="ISegment"/> using two text anchors.</para>
	/// </remarks>
	/// <example>
	/// Usage:
	/// <code>TextAnchor anchor = document.CreateAnchor(offset);
	/// ChangeMyDocument();
	/// int newOffset = anchor.Offset;
	/// </code>
	/// </example>
	public sealed class TextAnchor : ITextAnchor
	{
		readonly TextDocument document;
		internal TextAnchorNode node;
		
		internal TextAnchor(TextDocument document)
		{
			this.document = document;
		}
		
		/// <summary>
		/// Gets the document owning the anchor.
		/// </summary>
		public TextDocument Document {
			get { return document; }
		}
		
		/// <inheritdoc/>
		public AnchorMovementType MovementType { get; set; }
		
		/// <inheritdoc/>
		public bool SurviveDeletion { get; set; }
		
		/// <inheritdoc/>
		public bool IsDeleted {
			get {
				document.DebugVerifyAccess();
				return node == null;
			}
		}
		
		/// <inheritdoc/>
		public event EventHandler Deleted;
		
		internal void OnDeleted(DelayedEvents delayedEvents)
		{
			node = null;
			delayedEvents.DelayedRaise(Deleted, this, EventArgs.Empty);
		}
		
		/// <summary>
		/// Gets the offset of the text anchor.
		/// </summary>
		/// <exception cref="InvalidOperationException">Thrown when trying to get the Offset from a deleted anchor.</exception>
		public int Offset {
			get {
				document.DebugVerifyAccess();
				
				TextAnchorNode n = this.node;
				if (n == null)
					throw new InvalidOperationException();
				
				int offset = n.length;
				if (n.left != null)
					offset += n.left.totalLength;
				while (n.parent != null) {
					if (n == n.parent.right) {
						if (n.parent.left != null)
							offset += n.parent.left.totalLength;
						offset += n.parent.length;
					}
					n = n.parent;
				}
				return offset;
			}
		}
		
		/// <summary>
		/// Gets the line number of the anchor.
		/// </summary>
		/// <exception cref="InvalidOperationException">Thrown when trying to get the Offset from a deleted anchor.</exception>
		public int Line {
			get {
				return document.GetLineByOffset(this.Offset).LineNumber;
			}
		}
		
		/// <summary>
		/// Gets the column number of this anchor.
		/// </summary>
		/// <exception cref="InvalidOperationException">Thrown when trying to get the Offset from a deleted anchor.</exception>
		public int Column {
			get {
				int offset = this.Offset;
				return offset - document.GetLineByOffset(offset).Offset + 1;
			}
		}
		
		/// <summary>
		/// Gets the text location of this anchor.
		/// </summary>
		/// <exception cref="InvalidOperationException">Thrown when trying to get the Offset from a deleted anchor.</exception>
		public TextLocation Location {
			get {
				return document.GetLocation(this.Offset);
			}
		}
		
		/// <inheritdoc/>
		public override string ToString()
		{
			return "[TextAnchor Offset=" + Offset + "]";
		}
	}
}
