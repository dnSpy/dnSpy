// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using ICSharpCode.AvalonEdit.Utils;

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
	public sealed class TextAnchor
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
		
		/// <summary>
		/// Controls how the anchor moves.
		/// </summary>
		/// <remarks>Anchor movement is ambiguous if text is inserted exactly at the anchor's location.
		/// Does the anchor stay before the inserted text, or does it move after it?
		/// The property <see cref="MovementType"/> will be used to determine which of these two options the anchor will choose.
		/// The default value is <see cref="AnchorMovementType.Default"/>.</remarks>
		public AnchorMovementType MovementType { get; set; }
		
		/// <summary>
		/// <para>
		/// Specifies whether the anchor survives deletion of the text containing it.
		/// </para><para>
		/// <c>false</c>: The anchor is deleted when the a selection that includes the anchor is deleted.
		/// <c>true</c>: The anchor is not deleted.
		/// </para>
		/// </summary>
		/// <remarks><inheritdoc cref="IsDeleted" /></remarks>
		public bool SurviveDeletion { get; set; }
		
		/// <summary>
		/// Gets whether the anchor was deleted.
		/// </summary>
		/// <remarks>
		/// <para>When a piece of text containing an anchor is removed, then that anchor will be deleted.
		/// First, the <see cref="IsDeleted"/> property is set to true on all deleted anchors,
		/// then the <see cref="Deleted"/> events are raised.
		/// You cannot retrieve the offset from an anchor that has been deleted.</para>
		/// <para>This deletion behavior might be useful when using anchors for building a bookmark feature,
		/// but in other cases you want to still be able to use the anchor. For those cases, set <c><see cref="SurviveDeletion"/> = true</c>.</para>
		/// </remarks>
		public bool IsDeleted {
			get {
				document.DebugVerifyAccess();
				return node == null;
			}
		}
		
		/// <summary>
		/// Occurs after the anchor was deleted.
		/// </summary>
		/// <remarks>
		/// <inheritdoc cref="IsDeleted" />
		/// <para>Due to the 'weak reference' nature of TextAnchor, you will receive the Deleted event only
		/// while your code holds a reference to the TextAnchor object.</para>
		/// </remarks>
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
	
	/// <summary>
	/// Defines how a text anchor moves.
	/// </summary>
	public enum AnchorMovementType
	{
		/// <summary>
		/// When text is inserted at the anchor position, the type of the insertion
		/// determines where the caret moves to. For normal insertions, the anchor will stay
		/// behind the inserted text.
		/// </summary>
		Default,
		/// <summary>
		/// When text is inserted at the anchor position, the anchor will stay
		/// before the inserted text.
		/// </summary>
		BeforeInsertion,
		/// <summary>
		/// When text is insered at the anchor position, the anchor will move
		/// after the inserted text.
		/// </summary>
		AfterInsertion
	}
}
