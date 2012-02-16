// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using ICSharpCode.AvalonEdit.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;

namespace ICSharpCode.AvalonEdit.Document
{
	/// <summary>
	/// Interface to allow TextSegments to access the TextSegmentCollection - we cannot use a direct reference
	/// because TextSegmentCollection is generic.
	/// </summary>
	interface ISegmentTree
	{
		void Add(TextSegment s);
		void Remove(TextSegment s);
		void UpdateAugmentedData(TextSegment s);
	}
	
	/// <summary>
	/// <para>
	/// A collection of text segments that supports efficient lookup of segments
	/// intersecting with another segment.
	/// </para>
	/// </summary>
	/// <remarks><inheritdoc cref="TextSegment"/></remarks>
	/// <see cref="TextSegment"/>
	public sealed class TextSegmentCollection<T> : ICollection<T>, ISegmentTree, IWeakEventListener where T : TextSegment
	{
		// Implementation: this is basically a mixture of an augmented interval tree
		// and the TextAnchorTree.
		
		// WARNING: you need to understand interval trees (the version with the augmented 'high'/'max' field)
		// and how the TextAnchorTree works before you have any chance of understanding this code.
		
		// This means that every node holds two "segments":
		// one like the segments in the text anchor tree to support efficient offset changes
		// and another that is the interval as seen by the user
		
		// So basically, the tree contains a list of contiguous node segments of the first kind,
		// with interval segments starting at the end of every node segment.
		
		// Performance:
		// Add is O(lg n)
		// Remove is O(lg n)
		// DocumentChanged is O(m * lg n), with m the number of segments that intersect with the changed document section
		// FindFirstSegmentWithStartAfter is O(m + lg n) with m being the number of segments at the same offset as the result segment
		// FindIntersectingSegments is O(m + lg n) with m being the number of intersecting segments.
		
		int count;
		TextSegment root;
		bool isConnectedToDocument;
		
		#region Constructor
		/// <summary>
		/// Creates a new TextSegmentCollection that needs manual calls to <see cref="UpdateOffsets(DocumentChangeEventArgs)"/>.
		/// </summary>
		public TextSegmentCollection()
		{
		}
		
		/// <summary>
		/// Creates a new TextSegmentCollection that updates the offsets automatically.
		/// </summary>
		/// <param name="textDocument">The document to which the text segments
		/// that will be added to the tree belong. When the document changes, the
		/// position of the text segments will be updated accordingly.</param>
		public TextSegmentCollection(TextDocument textDocument)
		{
			if (textDocument == null)
				throw new ArgumentNullException("textDocument");
			
			textDocument.VerifyAccess();
			isConnectedToDocument = true;
			TextDocumentWeakEventManager.Changed.AddListener(textDocument, this);
		}
		#endregion
		
		#region OnDocumentChanged / UpdateOffsets
		bool IWeakEventListener.ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
		{
			if (managerType == typeof(TextDocumentWeakEventManager.Changed)) {
				OnDocumentChanged((DocumentChangeEventArgs)e);
				return true;
			}
			return false;
		}
		
		/// <summary>
		/// Updates the start and end offsets of all segments stored in this collection.
		/// </summary>
		/// <param name="e">DocumentChangeEventArgs instance describing the change to the document.</param>
		public void UpdateOffsets(DocumentChangeEventArgs e)
		{
			if (e == null)
				throw new ArgumentNullException("e");
			if (isConnectedToDocument)
				throw new InvalidOperationException("This TextSegmentCollection will automatically update offsets; do not call UpdateOffsets manually!");
			OnDocumentChanged(e);
			CheckProperties();
		}
		
		void OnDocumentChanged(DocumentChangeEventArgs e)
		{
			OffsetChangeMap map = e.OffsetChangeMapOrNull;
			if (map != null) {
				foreach (OffsetChangeMapEntry entry in map) {
					UpdateOffsetsInternal(entry);
				}
			} else {
				UpdateOffsetsInternal(e.CreateSingleChangeMapEntry());
			}
		}
		
		/// <summary>
		/// Updates the start and end offsets of all segments stored in this collection.
		/// </summary>
		/// <param name="change">OffsetChangeMapEntry instance describing the change to the document.</param>
		public void UpdateOffsets(OffsetChangeMapEntry change)
		{
			if (isConnectedToDocument)
				throw new InvalidOperationException("This TextSegmentCollection will automatically update offsets; do not call UpdateOffsets manually!");
			UpdateOffsetsInternal(change);
			CheckProperties();
		}
		#endregion
		
		#region UpdateOffsets (implementation)
		void UpdateOffsetsInternal(OffsetChangeMapEntry change)
		{
			// Special case pure insertions, because they don't always cause a text segment to increase in size when the replaced region
			// is inside a segment (when offset is at start or end of a text semgent).
			if (change.RemovalLength == 0) {
				InsertText(change.Offset, change.InsertionLength);
			} else {
				ReplaceText(change);
			}
		}
		
		void InsertText(int offset, int length)
		{
			if (length == 0)
				return;
			
			// enlarge segments that contain offset (excluding those that have offset as endpoint)
			foreach (TextSegment segment in FindSegmentsContaining(offset)) {
				if (segment.StartOffset < offset && offset < segment.EndOffset) {
					segment.Length += length;
				}
			}
			
			// move start offsets of all segments >= offset
			TextSegment node = FindFirstSegmentWithStartAfter(offset);
			if (node != null) {
				node.nodeLength += length;
				UpdateAugmentedData(node);
			}
		}
		
		void ReplaceText(OffsetChangeMapEntry change)
		{
			Debug.Assert(change.RemovalLength > 0);
			int offset = change.Offset;
			foreach (TextSegment segment in FindOverlappingSegments(offset, change.RemovalLength)) {
				if (segment.StartOffset <= offset) {
					if (segment.EndOffset >= offset + change.RemovalLength) {
						// Replacement inside segment: adjust segment length
						segment.Length += change.InsertionLength - change.RemovalLength;
					} else {
						// Replacement starting inside segment and ending after segment end: set segment end to removal position
						//segment.EndOffset = offset;
						segment.Length = offset - segment.StartOffset;
					}
				} else {
					// Replacement starting in front of text segment and running into segment.
					// Keep segment.EndOffset constant and move segment.StartOffset to the end of the replacement
					int remainingLength = segment.EndOffset - (offset + change.RemovalLength);
					RemoveSegment(segment);
					segment.StartOffset = offset + change.RemovalLength;
					segment.Length = Math.Max(0, remainingLength);
					AddSegment(segment);
				}
			}
			// move start offsets of all segments > offset
			TextSegment node = FindFirstSegmentWithStartAfter(offset + 1);
			if (node != null) {
				Debug.Assert(node.nodeLength >= change.RemovalLength);
				node.nodeLength += change.InsertionLength - change.RemovalLength;
				UpdateAugmentedData(node);
			}
		}
		#endregion
		
		#region Add
		/// <summary>
		/// Adds the specified segment to the tree. This will cause the segment to update when the
		/// document changes.
		/// </summary>
		public void Add(T item)
		{
			if (item == null)
				throw new ArgumentNullException("item");
			if (item.ownerTree != null)
				throw new ArgumentException("The segment is already added to a SegmentCollection.");
			AddSegment(item);
		}
		
		void ISegmentTree.Add(TextSegment s)
		{
			AddSegment(s);
		}
		
		void AddSegment(TextSegment node)
		{
			int insertionOffset = node.StartOffset;
			node.distanceToMaxEnd = node.segmentLength;
			if (root == null) {
				root = node;
				node.totalNodeLength = node.nodeLength;
			} else if (insertionOffset >= root.totalNodeLength) {
				// append segment at end of tree
				node.nodeLength = node.totalNodeLength = insertionOffset - root.totalNodeLength;
				InsertAsRight(root.RightMost, node);
			} else {
				// insert in middle of tree
				TextSegment n = FindNode(ref insertionOffset);
				Debug.Assert(insertionOffset < n.nodeLength);
				// split node segment 'n' at offset
				node.totalNodeLength = node.nodeLength = insertionOffset;
				n.nodeLength -= insertionOffset;
				InsertBefore(n, node);
			}
			node.ownerTree = this;
			count++;
			CheckProperties();
		}
		
		void InsertBefore(TextSegment node, TextSegment newNode)
		{
			if (node.left == null) {
				InsertAsLeft(node, newNode);
			} else {
				InsertAsRight(node.left.RightMost, newNode);
			}
		}
		#endregion
		
		#region GetNextSegment / GetPreviousSegment
		/// <summary>
		/// Gets the next segment after the specified segment.
		/// Segments are sorted by their start offset.
		/// Returns null if segment is the last segment.
		/// </summary>
		public T GetNextSegment(T segment)
		{
			if (!Contains(segment))
				throw new ArgumentException("segment is not inside the segment tree");
			return (T)segment.Successor;
		}
		
		/// <summary>
		/// Gets the previous segment before the specified segment.
		/// Segments are sorted by their start offset.
		/// Returns null if segment is the first segment.
		/// </summary>
		public T GetPreviousSegment(T segment)
		{
			if (!Contains(segment))
				throw new ArgumentException("segment is not inside the segment tree");
			return (T)segment.Predecessor;
		}
		#endregion
		
		#region FirstSegment/LastSegment
		/// <summary>
		/// Returns the first segment in the collection or null, if the collection is empty.
		/// </summary>
		public T FirstSegment {
			get {
				return root == null ? null : (T)root.LeftMost;
			}
		}
		
		/// <summary>
		/// Returns the last segment in the collection or null, if the collection is empty.
		/// </summary>
		public T LastSegment {
			get {
				return root == null ? null : (T)root.RightMost;
			}
		}
		#endregion
		
		#region FindFirstSegmentWithStartAfter
		/// <summary>
		/// Gets the first segment with a start offset greater or equal to <paramref name="startOffset"/>.
		/// Returns null if no such segment is found.
		/// </summary>
		public T FindFirstSegmentWithStartAfter(int startOffset)
		{
			if (root == null)
				return null;
			if (startOffset <= 0)
				return (T)root.LeftMost;
			TextSegment s = FindNode(ref startOffset);
			// startOffset means that the previous segment is starting at the offset we were looking for
			while (startOffset == 0) {
				TextSegment p = (s == null) ? root.RightMost : s.Predecessor;
				// There must always be a predecessor: if we were looking for the first node, we would have already
				// returned it as root.LeftMost above.
				Debug.Assert(p != null);
				startOffset += p.nodeLength;
				s = p;
			}
			return (T)s;
		}
		
		/// <summary>
		/// Finds the node at the specified offset.
		/// After the method has run, offset is relative to the beginning of the returned node.
		/// </summary>
		TextSegment FindNode(ref int offset)
		{
			TextSegment n = root;
			while (true) {
				if (n.left != null) {
					if (offset < n.left.totalNodeLength) {
						n = n.left; // descend into left subtree
						continue;
					} else {
						offset -= n.left.totalNodeLength; // skip left subtree
					}
				}
				if (offset < n.nodeLength) {
					return n; // found correct node
				} else {
					offset -= n.nodeLength; // skip this node
				}
				if (n.right != null) {
					n = n.right; // descend into right subtree
				} else {
					// didn't find any node containing the offset
					return null;
				}
			}
		}
		#endregion
		
		#region FindOverlappingSegments
		/// <summary>
		/// Finds all segments that contain the given offset.
		/// (StartOffset &lt;= offset &lt;= EndOffset)
		/// Segments are returned in the order given by GetNextSegment/GetPreviousSegment.
		/// </summary>
		/// <returns>Returns a new collection containing the results of the query.
		/// This means it is safe to modify the TextSegmentCollection while iterating through the result collection.</returns>
		public ReadOnlyCollection<T> FindSegmentsContaining(int offset)
		{
			return FindOverlappingSegments(offset, 0);
		}
		
		/// <summary>
		/// Finds all segments that overlap with the given segment (including touching segments).
		/// </summary>
		/// <returns>Returns a new collection containing the results of the query.
		/// This means it is safe to modify the TextSegmentCollection while iterating through the result collection.</returns>
		public ReadOnlyCollection<T> FindOverlappingSegments(ISegment segment)
		{
			if (segment == null)
				throw new ArgumentNullException("segment");
			return FindOverlappingSegments(segment.Offset, segment.Length);
		}
		
		/// <summary>
		/// Finds all segments that overlap with the given segment (including touching segments).
		/// Segments are returned in the order given by GetNextSegment/GetPreviousSegment.
		/// </summary>
		/// <returns>Returns a new collection containing the results of the query.
		/// This means it is safe to modify the TextSegmentCollection while iterating through the result collection.</returns>
		public ReadOnlyCollection<T> FindOverlappingSegments(int offset, int length)
		{
			ThrowUtil.CheckNotNegative(length, "length");
			List<T> results = new List<T>();
			if (root != null) {
				FindOverlappingSegments(results, root, offset, offset + length);
			}
			return results.AsReadOnly();
		}
		
		void FindOverlappingSegments(List<T> results, TextSegment node, int low, int high)
		{
			// low and high are relative to node.LeftMost startpos (not node.LeftMost.Offset)
			if (high < 0) {
				// node is irrelevant for search because all intervals in node are after high
				return;
			}
			
			// find values relative to node.Offset
			int nodeLow = low - node.nodeLength;
			int nodeHigh = high - node.nodeLength;
			if (node.left != null) {
				nodeLow -= node.left.totalNodeLength;
				nodeHigh -= node.left.totalNodeLength;
			}
			
			if (node.distanceToMaxEnd < nodeLow) {
				// node is irrelevant for search because all intervals in node are before low
				return;
			}
			
			if (node.left != null)
				FindOverlappingSegments(results, node.left, low, high);
			
			if (nodeHigh < 0) {
				// node and everything in node.right is before low
				return;
			}
			
			if (nodeLow <= node.segmentLength) {
				results.Add((T)node);
			}
			
			if (node.right != null)
				FindOverlappingSegments(results, node.right, nodeLow, nodeHigh);
		}
		#endregion
		
		#region UpdateAugmentedData
		void UpdateAugmentedData(TextSegment node)
		{
			int totalLength = node.nodeLength;
			int distanceToMaxEnd = node.segmentLength;
			if (node.left != null) {
				totalLength += node.left.totalNodeLength;
				
				int leftDTME = node.left.distanceToMaxEnd;
				// dtme is relative, so convert it to the coordinates of node:
				if (node.left.right != null)
					leftDTME -= node.left.right.totalNodeLength;
				leftDTME -= node.nodeLength;
				if (leftDTME > distanceToMaxEnd)
					distanceToMaxEnd = leftDTME;
			}
			if (node.right != null) {
				totalLength += node.right.totalNodeLength;
				
				int rightDTME = node.right.distanceToMaxEnd;
				// dtme is relative, so convert it to the coordinates of node:
				rightDTME += node.right.nodeLength;
				if (node.right.left != null)
					rightDTME += node.right.left.totalNodeLength;
				if (rightDTME > distanceToMaxEnd)
					distanceToMaxEnd = rightDTME;
			}
			if (node.totalNodeLength != totalLength
			    || node.distanceToMaxEnd != distanceToMaxEnd)
			{
				node.totalNodeLength = totalLength;
				node.distanceToMaxEnd = distanceToMaxEnd;
				if (node.parent != null)
					UpdateAugmentedData(node.parent);
			}
		}
		
		void ISegmentTree.UpdateAugmentedData(TextSegment node)
		{
			UpdateAugmentedData(node);
		}
		#endregion
		
		#region Remove
		/// <summary>
		/// Removes the specified segment from the tree. This will cause the segment to not update
		/// anymore when the document changes.
		/// </summary>
		public bool Remove(T item)
		{
			if (!Contains(item))
				return false;
			RemoveSegment(item);
			return true;
		}
		
		void ISegmentTree.Remove(TextSegment s)
		{
			RemoveSegment(s);
		}
		
		void RemoveSegment(TextSegment s)
		{
			int oldOffset = s.StartOffset;
			TextSegment successor = s.Successor;
			if (successor != null)
				successor.nodeLength += s.nodeLength;
			RemoveNode(s);
			if (successor != null)
				UpdateAugmentedData(successor);
			Disconnect(s, oldOffset);
			CheckProperties();
		}
		
		void Disconnect(TextSegment s, int offset)
		{
			s.left = s.right = s.parent = null;
			s.ownerTree = null;
			s.nodeLength = offset;
			count--;
		}
		
		/// <summary>
		/// Removes all segments from the tree.
		/// </summary>
		public void Clear()
		{
			T[] segments = this.ToArray();
			root = null;
			int offset = 0;
			foreach (TextSegment s in segments) {
				offset += s.nodeLength;
				Disconnect(s, offset);
			}
			CheckProperties();
		}
		#endregion
		
		#region CheckProperties
		[Conditional("DATACONSISTENCYTEST")]
		internal void CheckProperties()
		{
			#if DEBUG
			if (root != null) {
				CheckProperties(root);
				
				// check red-black property:
				int blackCount = -1;
				CheckNodeProperties(root, null, RED, 0, ref blackCount);
			}
			
			int expectedCount = 0;
			// we cannot trust LINQ not to call ICollection.Count, so we need this loop
			// to count the elements in the tree
			using (IEnumerator<T> en = GetEnumerator()) {
				while (en.MoveNext()) expectedCount++;
			}
			Debug.Assert(count == expectedCount);
			#endif
		}
		
		#if DEBUG
		void CheckProperties(TextSegment node)
		{
			int totalLength = node.nodeLength;
			int distanceToMaxEnd = node.segmentLength;
			if (node.left != null) {
				CheckProperties(node.left);
				totalLength += node.left.totalNodeLength;
				distanceToMaxEnd = Math.Max(distanceToMaxEnd,
				                            node.left.distanceToMaxEnd + node.left.StartOffset - node.StartOffset);
			}
			if (node.right != null) {
				CheckProperties(node.right);
				totalLength += node.right.totalNodeLength;
				distanceToMaxEnd = Math.Max(distanceToMaxEnd,
				                            node.right.distanceToMaxEnd + node.right.StartOffset - node.StartOffset);
			}
			Debug.Assert(node.totalNodeLength == totalLength);
			Debug.Assert(node.distanceToMaxEnd == distanceToMaxEnd);
		}
		
		/*
		1. A node is either red or black.
		2. The root is black.
		3. All leaves are black. (The leaves are the NIL children.)
		4. Both children of every red node are black. (So every red node must have a black parent.)
		5. Every simple path from a node to a descendant leaf contains the same number of black nodes. (Not counting the leaf node.)
		 */
		void CheckNodeProperties(TextSegment node, TextSegment parentNode, bool parentColor, int blackCount, ref int expectedBlackCount)
		{
			if (node == null) return;
			
			Debug.Assert(node.parent == parentNode);
			
			if (parentColor == RED) {
				Debug.Assert(node.color == BLACK);
			}
			if (node.color == BLACK) {
				blackCount++;
			}
			if (node.left == null && node.right == null) {
				// node is a leaf node:
				if (expectedBlackCount == -1)
					expectedBlackCount = blackCount;
				else
					Debug.Assert(expectedBlackCount == blackCount);
			}
			CheckNodeProperties(node.left, node, node.color, blackCount, ref expectedBlackCount);
			CheckNodeProperties(node.right, node, node.color, blackCount, ref expectedBlackCount);
		}
		
		static void AppendTreeToString(TextSegment node, StringBuilder b, int indent)
		{
			if (node.color == RED)
				b.Append("RED   ");
			else
				b.Append("BLACK ");
			b.AppendLine(node.ToString() + node.ToDebugString());
			indent += 2;
			if (node.left != null) {
				b.Append(' ', indent);
				b.Append("L: ");
				AppendTreeToString(node.left, b, indent);
			}
			if (node.right != null) {
				b.Append(' ', indent);
				b.Append("R: ");
				AppendTreeToString(node.right, b, indent);
			}
		}
		#endif
		
		internal string GetTreeAsString()
		{
			#if DEBUG
			StringBuilder b = new StringBuilder();
			if (root != null)
				AppendTreeToString(root, b, 0);
			return b.ToString();
			#else
			return "Not available in release build.";
			#endif
		}
		#endregion
		
		#region Red/Black Tree
		internal const bool RED = true;
		internal const bool BLACK = false;
		
		void InsertAsLeft(TextSegment parentNode, TextSegment newNode)
		{
			Debug.Assert(parentNode.left == null);
			parentNode.left = newNode;
			newNode.parent = parentNode;
			newNode.color = RED;
			UpdateAugmentedData(parentNode);
			FixTreeOnInsert(newNode);
		}
		
		void InsertAsRight(TextSegment parentNode, TextSegment newNode)
		{
			Debug.Assert(parentNode.right == null);
			parentNode.right = newNode;
			newNode.parent = parentNode;
			newNode.color = RED;
			UpdateAugmentedData(parentNode);
			FixTreeOnInsert(newNode);
		}
		
		void FixTreeOnInsert(TextSegment node)
		{
			Debug.Assert(node != null);
			Debug.Assert(node.color == RED);
			Debug.Assert(node.left == null || node.left.color == BLACK);
			Debug.Assert(node.right == null || node.right.color == BLACK);
			
			TextSegment parentNode = node.parent;
			if (parentNode == null) {
				// we inserted in the root -> the node must be black
				// since this is a root node, making the node black increments the number of black nodes
				// on all paths by one, so it is still the same for all paths.
				node.color = BLACK;
				return;
			}
			if (parentNode.color == BLACK) {
				// if the parent node where we inserted was black, our red node is placed correctly.
				// since we inserted a red node, the number of black nodes on each path is unchanged
				// -> the tree is still balanced
				return;
			}
			// parentNode is red, so there is a conflict here!
			
			// because the root is black, parentNode is not the root -> there is a grandparent node
			TextSegment grandparentNode = parentNode.parent;
			TextSegment uncleNode = Sibling(parentNode);
			if (uncleNode != null && uncleNode.color == RED) {
				parentNode.color = BLACK;
				uncleNode.color = BLACK;
				grandparentNode.color = RED;
				FixTreeOnInsert(grandparentNode);
				return;
			}
			// now we know: parent is red but uncle is black
			// First rotation:
			if (node == parentNode.right && parentNode == grandparentNode.left) {
				RotateLeft(parentNode);
				node = node.left;
			} else if (node == parentNode.left && parentNode == grandparentNode.right) {
				RotateRight(parentNode);
				node = node.right;
			}
			// because node might have changed, reassign variables:
			parentNode = node.parent;
			grandparentNode = parentNode.parent;
			
			// Now recolor a bit:
			parentNode.color = BLACK;
			grandparentNode.color = RED;
			// Second rotation:
			if (node == parentNode.left && parentNode == grandparentNode.left) {
				RotateRight(grandparentNode);
			} else {
				// because of the first rotation, this is guaranteed:
				Debug.Assert(node == parentNode.right && parentNode == grandparentNode.right);
				RotateLeft(grandparentNode);
			}
		}
		
		void RemoveNode(TextSegment removedNode)
		{
			if (removedNode.left != null && removedNode.right != null) {
				// replace removedNode with it's in-order successor
				
				TextSegment leftMost = removedNode.right.LeftMost;
				RemoveNode(leftMost); // remove leftMost from its current location
				
				// and overwrite the removedNode with it
				ReplaceNode(removedNode, leftMost);
				leftMost.left = removedNode.left;
				if (leftMost.left != null) leftMost.left.parent = leftMost;
				leftMost.right = removedNode.right;
				if (leftMost.right != null) leftMost.right.parent = leftMost;
				leftMost.color = removedNode.color;
				
				UpdateAugmentedData(leftMost);
				if (leftMost.parent != null) UpdateAugmentedData(leftMost.parent);
				return;
			}
			
			// now either removedNode.left or removedNode.right is null
			// get the remaining child
			TextSegment parentNode = removedNode.parent;
			TextSegment childNode = removedNode.left ?? removedNode.right;
			ReplaceNode(removedNode, childNode);
			if (parentNode != null) UpdateAugmentedData(parentNode);
			if (removedNode.color == BLACK) {
				if (childNode != null && childNode.color == RED) {
					childNode.color = BLACK;
				} else {
					FixTreeOnDelete(childNode, parentNode);
				}
			}
		}
		
		void FixTreeOnDelete(TextSegment node, TextSegment parentNode)
		{
			Debug.Assert(node == null || node.parent == parentNode);
			if (parentNode == null)
				return;
			
			// warning: node may be null
			TextSegment sibling = Sibling(node, parentNode);
			if (sibling.color == RED) {
				parentNode.color = RED;
				sibling.color = BLACK;
				if (node == parentNode.left) {
					RotateLeft(parentNode);
				} else {
					RotateRight(parentNode);
				}
				
				sibling = Sibling(node, parentNode); // update value of sibling after rotation
			}
			
			if (parentNode.color == BLACK
			    && sibling.color == BLACK
			    && GetColor(sibling.left) == BLACK
			    && GetColor(sibling.right) == BLACK)
			{
				sibling.color = RED;
				FixTreeOnDelete(parentNode, parentNode.parent);
				return;
			}
			
			if (parentNode.color == RED
			    && sibling.color == BLACK
			    && GetColor(sibling.left) == BLACK
			    && GetColor(sibling.right) == BLACK)
			{
				sibling.color = RED;
				parentNode.color = BLACK;
				return;
			}
			
			if (node == parentNode.left &&
			    sibling.color == BLACK &&
			    GetColor(sibling.left) == RED &&
			    GetColor(sibling.right) == BLACK)
			{
				sibling.color = RED;
				sibling.left.color = BLACK;
				RotateRight(sibling);
			}
			else if (node == parentNode.right &&
			         sibling.color == BLACK &&
			         GetColor(sibling.right) == RED &&
			         GetColor(sibling.left) == BLACK)
			{
				sibling.color = RED;
				sibling.right.color = BLACK;
				RotateLeft(sibling);
			}
			sibling = Sibling(node, parentNode); // update value of sibling after rotation
			
			sibling.color = parentNode.color;
			parentNode.color = BLACK;
			if (node == parentNode.left) {
				if (sibling.right != null) {
					Debug.Assert(sibling.right.color == RED);
					sibling.right.color = BLACK;
				}
				RotateLeft(parentNode);
			} else {
				if (sibling.left != null) {
					Debug.Assert(sibling.left.color == RED);
					sibling.left.color = BLACK;
				}
				RotateRight(parentNode);
			}
		}
		
		void ReplaceNode(TextSegment replacedNode, TextSegment newNode)
		{
			if (replacedNode.parent == null) {
				Debug.Assert(replacedNode == root);
				root = newNode;
			} else {
				if (replacedNode.parent.left == replacedNode)
					replacedNode.parent.left = newNode;
				else
					replacedNode.parent.right = newNode;
			}
			if (newNode != null) {
				newNode.parent = replacedNode.parent;
			}
			replacedNode.parent = null;
		}
		
		void RotateLeft(TextSegment p)
		{
			// let q be p's right child
			TextSegment q = p.right;
			Debug.Assert(q != null);
			Debug.Assert(q.parent == p);
			// set q to be the new root
			ReplaceNode(p, q);
			
			// set p's right child to be q's left child
			p.right = q.left;
			if (p.right != null) p.right.parent = p;
			// set q's left child to be p
			q.left = p;
			p.parent = q;
			UpdateAugmentedData(p);
			UpdateAugmentedData(q);
		}
		
		void RotateRight(TextSegment p)
		{
			// let q be p's left child
			TextSegment q = p.left;
			Debug.Assert(q != null);
			Debug.Assert(q.parent == p);
			// set q to be the new root
			ReplaceNode(p, q);
			
			// set p's left child to be q's right child
			p.left = q.right;
			if (p.left != null) p.left.parent = p;
			// set q's right child to be p
			q.right = p;
			p.parent = q;
			UpdateAugmentedData(p);
			UpdateAugmentedData(q);
		}
		
		static TextSegment Sibling(TextSegment node)
		{
			if (node == node.parent.left)
				return node.parent.right;
			else
				return node.parent.left;
		}
		
		static TextSegment Sibling(TextSegment node, TextSegment parentNode)
		{
			Debug.Assert(node == null || node.parent == parentNode);
			if (node == parentNode.left)
				return parentNode.right;
			else
				return parentNode.left;
		}
		
		static bool GetColor(TextSegment node)
		{
			return node != null ? node.color : BLACK;
		}
		#endregion
		
		#region ICollection<T> implementation
		/// <summary>
		/// Gets the number of segments in the tree.
		/// </summary>
		public int Count {
			get { return count; }
		}
		
		bool ICollection<T>.IsReadOnly {
			get { return false; }
		}
		
		/// <summary>
		/// Gets whether this tree contains the specified item.
		/// </summary>
		public bool Contains(T item)
		{
			return item != null && item.ownerTree == this;
		}
		
		/// <summary>
		/// Copies all segments in this SegmentTree to the specified array.
		/// </summary>
		public void CopyTo(T[] array, int arrayIndex)
		{
			if (array == null)
				throw new ArgumentNullException("array");
			if (array.Length < this.Count)
				throw new ArgumentException("The array is too small", "array");
			if (arrayIndex < 0 || arrayIndex + count > array.Length)
				throw new ArgumentOutOfRangeException("arrayIndex", arrayIndex, "Value must be between 0 and " + (array.Length - count));
			foreach (T s in this) {
				array[arrayIndex++] = s;
			}
		}
		
		/// <summary>
		/// Gets an enumerator to enumerate the segments.
		/// </summary>
		public IEnumerator<T> GetEnumerator()
		{
			if (root != null) {
				TextSegment current = root.LeftMost;
				while (current != null) {
					yield return (T)current;
					// TODO: check if collection was modified during enumeration
					current = current.Successor;
				}
			}
		}
		
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}
		#endregion
	}
}
