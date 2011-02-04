// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Diagnostics;

namespace ICSharpCode.AvalonEdit.Document
{
	/// <summary>
	/// A segment that can be put into a <see cref="TextSegmentCollection{T}"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// A <see cref="TextSegment"/> can be stand-alone or part of a <see cref="TextSegmentCollection{T}"/>.
	/// If the segment is stored inside a TextSegmentCollection, its Offset and Length will be updated by that collection.
	/// </para>
	/// <para>
	/// When the document changes, the offsets of all text segments in the TextSegmentCollection will be adjusted accordingly.
	/// Start offsets move like <see cref="AnchorMovementType">AnchorMovementType.AfterInsertion</see>,
	/// end offsets move like <see cref="AnchorMovementType">AnchorMovementType.BeforeInsertion</see>
	/// (i.e. the segment will always stay as small as possible).</para>
	/// <para>
	/// If a document change causes a segment to be deleted completely, it will be reduced to length 0, but segments are
	/// never automatically removed from the collection.
	/// Segments with length 0 will never expand due to document changes, and they move as <c>AfterInsertion</c>.
	/// </para>
	/// <para>
	/// Thread-safety: a TextSegmentCollection that is connected to a <see cref="TextDocument"/> may only be used on that document's owner thread.
	/// A disconnected TextSegmentCollection is safe for concurrent reads, but concurrent access is not safe when there are writes.
	/// Keep in mind that reading the Offset properties of a text segment inside the collection is a read access on the
	/// collection; and setting an Offset property of a text segment is a write access on the collection.
	/// </para>
	/// </remarks>
	/// <seealso cref="ISegment"/>
	/// <seealso cref="AnchorSegment"/>
	/// <seealso cref="TextSegmentCollection{T}"/>
	public class TextSegment : ISegment
	{
		internal ISegmentTree ownerTree;
		internal TextSegment left, right, parent;
		
		/// <summary>
		/// The color of the segment in the red/black tree.
		/// </summary>
		internal bool color;
		
		/// <summary>
		/// The "length" of the node (distance to previous node)
		/// </summary>
		internal int nodeLength;
		
		/// <summary>
		/// The total "length" of this subtree.
		/// </summary>
		internal int totalNodeLength; // totalNodeLength = nodeLength + left.totalNodeLength + right.totalNodeLength
		
		/// <summary>
		/// The length of the segment (do not confuse with nodeLength).
		/// </summary>
		internal int segmentLength;
		
		/// <summary>
		/// distanceToMaxEnd = Max(segmentLength,
		///                        left.distanceToMaxEnd + left.Offset - Offset,
		///                        left.distanceToMaxEnd + right.Offset - Offset)
		/// </summary>
		internal int distanceToMaxEnd;
		
		int ISegment.Offset {
			get { return StartOffset; }
		}
		
		/// <summary>
		/// Gets whether this segment is connected to a TextSegmentCollection and will automatically
		/// update its offsets.
		/// </summary>
		protected bool IsConnectedToCollection {
			get {
				return ownerTree != null;
			}
		}
		
		/// <summary>
		/// Gets/Sets the start offset of the segment.
		/// </summary>
		/// <remarks>
		/// When setting the start offset, the end offset will change, too: the Length of the segment will stay constant.
		/// </remarks>
		public int StartOffset {
			get {
				// If the segment is not connected to a tree, we store the offset in "nodeLength".
				// Otherwise, "nodeLength" contains the distance to the start offset of the previous node
				Debug.Assert(!(ownerTree == null && parent != null));
				Debug.Assert(!(ownerTree == null && left != null));
				
				TextSegment n = this;
				int offset = n.nodeLength;
				if (n.left != null)
					offset += n.left.totalNodeLength;
				while (n.parent != null) {
					if (n == n.parent.right) {
						if (n.parent.left != null)
							offset += n.parent.left.totalNodeLength;
						offset += n.parent.nodeLength;
					}
					n = n.parent;
				}
				return offset;
			}
			set {
				if (value < 0)
					throw new ArgumentOutOfRangeException("value", "Offset must not be negative");
				if (this.StartOffset != value) {
					// need a copy of the variable because ownerTree.Remove() sets this.ownerTree to null
					ISegmentTree ownerTree = this.ownerTree;
					if (ownerTree != null) {
						ownerTree.Remove(this);
						nodeLength = value;
						ownerTree.Add(this);
					} else {
						nodeLength = value;
					}
				}
			}
		}
		
		/// <summary>
		/// Gets/Sets the end offset of the segment.
		/// </summary>
		/// <remarks>
		/// Setting the end offset will change the length, the start offset will stay constant.
		/// </remarks>
		public int EndOffset {
			get {
				return StartOffset + Length;
			}
			set {
				int newLength = value - StartOffset;
				if (newLength < 0)
					throw new ArgumentOutOfRangeException("value", "EndOffset must be greater or equal to StartOffset");
				Length = newLength;
			}
		}
		
		/// <summary>
		/// Gets/Sets the length of the segment.
		/// </summary>
		/// <remarks>
		/// Setting the length will change the end offset, the start offset will stay constant.
		/// </remarks>
		public int Length {
			get {
				return segmentLength;
			}
			set {
				if (value < 0)
					throw new ArgumentOutOfRangeException("value", "Length must not be negative");
				segmentLength = value;
				if (ownerTree != null)
					ownerTree.UpdateAugmentedData(this);
			}
		}
		
		internal TextSegment LeftMost {
			get {
				TextSegment node = this;
				while (node.left != null)
					node = node.left;
				return node;
			}
		}
		
		internal TextSegment RightMost {
			get {
				TextSegment node = this;
				while (node.right != null)
					node = node.right;
				return node;
			}
		}
		
		/// <summary>
		/// Gets the inorder successor of the node.
		/// </summary>
		internal TextSegment Successor {
			get {
				if (right != null) {
					return right.LeftMost;
				} else {
					TextSegment node = this;
					TextSegment oldNode;
					do {
						oldNode = node;
						node = node.parent;
						// go up until we are coming out of a left subtree
					} while (node != null && node.right == oldNode);
					return node;
				}
			}
		}
		
		/// <summary>
		/// Gets the inorder predecessor of the node.
		/// </summary>
		internal TextSegment Predecessor {
			get {
				if (left != null) {
					return left.RightMost;
				} else {
					TextSegment node = this;
					TextSegment oldNode;
					do {
						oldNode = node;
						node = node.parent;
						// go up until we are coming out of a right subtree
					} while (node != null && node.left == oldNode);
					return node;
				}
			}
		}
		
		#if DEBUG
		internal string ToDebugString()
		{
			return "[nodeLength=" + nodeLength + " totalNodeLength=" + totalNodeLength
				+ " distanceToMaxEnd=" + distanceToMaxEnd + " MaxEndOffset=" + (StartOffset + distanceToMaxEnd) + "]";
		}
		#endif
		
		/// <inheritdoc/>
		public override string ToString()
		{
			return "[" + GetType().Name + " Offset=" + StartOffset + " Length=" + Length + " EndOffset=" + EndOffset + "]";
		}
	}
}
