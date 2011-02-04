// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ICSharpCode.AvalonEdit.Document
{
	using LineNode = DocumentLine;
	
	/// <summary>
	/// Data structure for efficient management of the document lines (most operations are O(lg n)).
	/// This implements an augmented red-black tree.
	/// See <see cref="LineNode"/> for the augmented data.
	/// 
	/// NOTE: The tree is never empty, initially it contains an empty line.
	/// </summary>
	sealed class DocumentLineTree : IList<DocumentLine>
	{
		#region Constructor
		readonly TextDocument document;
		LineNode root;
		
		public DocumentLineTree(TextDocument document)
		{
			this.document = document;
			
			DocumentLine emptyLine = new DocumentLine(document);
			root = emptyLine.InitLineNode();
		}
		#endregion
		
		#region Rotation callbacks
		internal static void UpdateAfterChildrenChange(LineNode node)
		{
			int totalCount = 1;
			int totalLength = node.TotalLength;
			if (node.left != null) {
				totalCount += node.left.nodeTotalCount;
				totalLength += node.left.nodeTotalLength;
			}
			if (node.right != null) {
				totalCount += node.right.nodeTotalCount;
				totalLength += node.right.nodeTotalLength;
			}
			if (totalCount != node.nodeTotalCount
			    || totalLength != node.nodeTotalLength)
			{
				node.nodeTotalCount = totalCount;
				node.nodeTotalLength = totalLength;
				if (node.parent != null) UpdateAfterChildrenChange(node.parent);
			}
		}
		
		static void UpdateAfterRotateLeft(LineNode node)
		{
			UpdateAfterChildrenChange(node);
			
			// not required: rotations only happen on insertions/deletions
			// -> totalCount changes -> the parent is always updated
			//UpdateAfterChildrenChange(node.parent);
		}
		
		static void UpdateAfterRotateRight(LineNode node)
		{
			UpdateAfterChildrenChange(node);
			
			// not required: rotations only happen on insertions/deletions
			// -> totalCount changes -> the parent is always updated
			//UpdateAfterChildrenChange(node.parent);
		}
		#endregion
		
		#region RebuildDocument
		/// <summary>
		/// Rebuild the tree, in O(n).
		/// </summary>
		public void RebuildTree(List<DocumentLine> documentLines)
		{
			LineNode[] nodes = new LineNode[documentLines.Count];
			for (int i = 0; i < documentLines.Count; i++) {
				DocumentLine ls = documentLines[i];
				LineNode node = ls.InitLineNode();
				nodes[i] = node;
			}
			Debug.Assert(nodes.Length > 0);
			// now build the corresponding balanced tree
			int height = GetTreeHeight(nodes.Length);
			Debug.WriteLine("DocumentLineTree will have height: " + height);
			root = BuildTree(nodes, 0, nodes.Length, height);
			root.color = BLACK;
			#if DEBUG
			CheckProperties();
			#endif
		}
		
		internal static int GetTreeHeight(int size)
		{
			if (size == 0)
				return 0;
			else
				return GetTreeHeight(size / 2) + 1;
		}
		
		/// <summary>
		/// build a tree from a list of nodes
		/// </summary>
		LineNode BuildTree(LineNode[] nodes, int start, int end, int subtreeHeight)
		{
			Debug.Assert(start <= end);
			if (start == end) {
				return null;
			}
			int middle = (start + end) / 2;
			LineNode node = nodes[middle];
			node.left = BuildTree(nodes, start, middle, subtreeHeight - 1);
			node.right = BuildTree(nodes, middle + 1, end, subtreeHeight - 1);
			if (node.left != null) node.left.parent = node;
			if (node.right != null) node.right.parent = node;
			if (subtreeHeight == 1)
				node.color = RED;
			UpdateAfterChildrenChange(node);
			return node;
		}
		#endregion
		
		#region GetNodeBy... / Get...FromNode
		LineNode GetNodeByIndex(int index)
		{
			Debug.Assert(index >= 0);
			Debug.Assert(index < root.nodeTotalCount);
			LineNode node = root;
			while (true) {
				if (node.left != null && index < node.left.nodeTotalCount) {
					node = node.left;
				} else {
					if (node.left != null) {
						index -= node.left.nodeTotalCount;
					}
					if (index == 0)
						return node;
					index--;
					node = node.right;
				}
			}
		}
		
		internal static int GetIndexFromNode(LineNode node)
		{
			int index = (node.left != null) ? node.left.nodeTotalCount : 0;
			while (node.parent != null) {
				if (node == node.parent.right) {
					if (node.parent.left != null)
						index += node.parent.left.nodeTotalCount;
					index++;
				}
				node = node.parent;
			}
			return index;
		}
		
		LineNode GetNodeByOffset(int offset)
		{
			Debug.Assert(offset >= 0);
			Debug.Assert(offset <= root.nodeTotalLength);
			if (offset == root.nodeTotalLength) {
				return root.RightMost;
			}
			LineNode node = root;
			while (true) {
				if (node.left != null && offset < node.left.nodeTotalLength) {
					node = node.left;
				} else {
					if (node.left != null) {
						offset -= node.left.nodeTotalLength;
					}
					offset -= node.TotalLength;
					if (offset < 0)
						return node;
					node = node.right;
				}
			}
		}
		
		internal static int GetOffsetFromNode(LineNode node)
		{
			int offset = (node.left != null) ? node.left.nodeTotalLength : 0;
			while (node.parent != null) {
				if (node == node.parent.right) {
					if (node.parent.left != null)
						offset += node.parent.left.nodeTotalLength;
					offset += node.parent.TotalLength;
				}
				node = node.parent;
			}
			return offset;
		}
		#endregion
		
		#region GetLineBy
		public DocumentLine GetByNumber(int number)
		{
			return GetNodeByIndex(number - 1);
		}
		
		public DocumentLine GetByOffset(int offset)
		{
			return GetNodeByOffset(offset);
		}
		#endregion
		
		#region LineCount
		public int LineCount {
			get {
				return root.nodeTotalCount;
			}
		}
		#endregion
		
		#region CheckProperties
		#if DEBUG
		[Conditional("DATACONSISTENCYTEST")]
		internal void CheckProperties()
		{
			Debug.Assert(root.nodeTotalLength == document.TextLength);
			CheckProperties(root);
			
			// check red-black property:
			int blackCount = -1;
			CheckNodeProperties(root, null, RED, 0, ref blackCount);
		}
		
		void CheckProperties(LineNode node)
		{
			int totalCount = 1;
			int totalLength = node.TotalLength;
			if (node.left != null) {
				CheckProperties(node.left);
				totalCount += node.left.nodeTotalCount;
				totalLength += node.left.nodeTotalLength;
			}
			if (node.right != null) {
				CheckProperties(node.right);
				totalCount += node.right.nodeTotalCount;
				totalLength += node.right.nodeTotalLength;
			}
			Debug.Assert(node.nodeTotalCount == totalCount);
			Debug.Assert(node.nodeTotalLength == totalLength);
		}
		
		/*
		1. A node is either red or black.
		2. The root is black.
		3. All leaves are black. (The leaves are the NIL children.)
		4. Both children of every red node are black. (So every red node must have a black parent.)
		5. Every simple path from a node to a descendant leaf contains the same number of black nodes. (Not counting the leaf node.)
		 */
		void CheckNodeProperties(LineNode node, LineNode parentNode, bool parentColor, int blackCount, ref int expectedBlackCount)
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
		
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
		public string GetTreeAsString()
		{
			StringBuilder b = new StringBuilder();
			AppendTreeToString(root, b, 0);
			return b.ToString();
		}
		
		static void AppendTreeToString(LineNode node, StringBuilder b, int indent)
		{
			if (node.color == RED)
				b.Append("RED   ");
			else
				b.Append("BLACK ");
			b.AppendLine(node.ToString());
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
		#endregion
		
		#region Insert/Remove lines
		public void RemoveLine(DocumentLine line)
		{
			RemoveNode(line);
			line.isDeleted = true;
		}
		
		public DocumentLine InsertLineAfter(DocumentLine line, int totalLength)
		{
			DocumentLine newLine = new DocumentLine(document);
			newLine.TotalLength = totalLength;
			
			InsertAfter(line, newLine);
			return newLine;
		}
		
		void InsertAfter(LineNode node, DocumentLine newLine)
		{
			LineNode newNode = newLine.InitLineNode();
			if (node.right == null) {
				InsertAsRight(node, newNode);
			} else {
				InsertAsLeft(node.right.LeftMost, newNode);
			}
		}
		#endregion
		
		#region Red/Black Tree
		internal const bool RED = true;
		internal const bool BLACK = false;
		
		void InsertAsLeft(LineNode parentNode, LineNode newNode)
		{
			Debug.Assert(parentNode.left == null);
			parentNode.left = newNode;
			newNode.parent = parentNode;
			newNode.color = RED;
			UpdateAfterChildrenChange(parentNode);
			FixTreeOnInsert(newNode);
		}
		
		void InsertAsRight(LineNode parentNode, LineNode newNode)
		{
			Debug.Assert(parentNode.right == null);
			parentNode.right = newNode;
			newNode.parent = parentNode;
			newNode.color = RED;
			UpdateAfterChildrenChange(parentNode);
			FixTreeOnInsert(newNode);
		}
		
		void FixTreeOnInsert(LineNode node)
		{
			Debug.Assert(node != null);
			Debug.Assert(node.color == RED);
			Debug.Assert(node.left == null || node.left.color == BLACK);
			Debug.Assert(node.right == null || node.right.color == BLACK);
			
			LineNode parentNode = node.parent;
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
			LineNode grandparentNode = parentNode.parent;
			LineNode uncleNode = Sibling(parentNode);
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
		
		void RemoveNode(LineNode removedNode)
		{
			if (removedNode.left != null && removedNode.right != null) {
				// replace removedNode with it's in-order successor
				
				LineNode leftMost = removedNode.right.LeftMost;
				RemoveNode(leftMost); // remove leftMost from its current location
				
				// and overwrite the removedNode with it
				ReplaceNode(removedNode, leftMost);
				leftMost.left = removedNode.left;
				if (leftMost.left != null) leftMost.left.parent = leftMost;
				leftMost.right = removedNode.right;
				if (leftMost.right != null) leftMost.right.parent = leftMost;
				leftMost.color = removedNode.color;
				
				UpdateAfterChildrenChange(leftMost);
				if (leftMost.parent != null) UpdateAfterChildrenChange(leftMost.parent);
				return;
			}
			
			// now either removedNode.left or removedNode.right is null
			// get the remaining child
			LineNode parentNode = removedNode.parent;
			LineNode childNode = removedNode.left ?? removedNode.right;
			ReplaceNode(removedNode, childNode);
			if (parentNode != null) UpdateAfterChildrenChange(parentNode);
			if (removedNode.color == BLACK) {
				if (childNode != null && childNode.color == RED) {
					childNode.color = BLACK;
				} else {
					FixTreeOnDelete(childNode, parentNode);
				}
			}
		}
		
		void FixTreeOnDelete(LineNode node, LineNode parentNode)
		{
			Debug.Assert(node == null || node.parent == parentNode);
			if (parentNode == null)
				return;
			
			// warning: node may be null
			LineNode sibling = Sibling(node, parentNode);
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
		
		void ReplaceNode(LineNode replacedNode, LineNode newNode)
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
		
		void RotateLeft(LineNode p)
		{
			// let q be p's right child
			LineNode q = p.right;
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
			UpdateAfterRotateLeft(p);
		}
		
		void RotateRight(LineNode p)
		{
			// let q be p's left child
			LineNode q = p.left;
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
			UpdateAfterRotateRight(p);
		}
		
		static LineNode Sibling(LineNode node)
		{
			if (node == node.parent.left)
				return node.parent.right;
			else
				return node.parent.left;
		}
		
		static LineNode Sibling(LineNode node, LineNode parentNode)
		{
			Debug.Assert(node == null || node.parent == parentNode);
			if (node == parentNode.left)
				return parentNode.right;
			else
				return parentNode.left;
		}
		
		static bool GetColor(LineNode node)
		{
			return node != null ? node.color : BLACK;
		}
		#endregion
		
		#region IList implementation
		DocumentLine IList<DocumentLine>.this[int index] {
			get {
				document.VerifyAccess();
				return GetByNumber(1 + index);
			}
			set {
				throw new NotSupportedException();
			}
		}
		
		int ICollection<DocumentLine>.Count {
			get {
				document.VerifyAccess();
				return LineCount;
			}
		}
		
		bool ICollection<DocumentLine>.IsReadOnly {
			get { return true; }
		}
		
		int IList<DocumentLine>.IndexOf(DocumentLine item)
		{
			document.VerifyAccess();
			if (item == null || item.IsDeleted)
				return -1;
			int index = item.LineNumber - 1;
			if (index < LineCount && GetNodeByIndex(index) == item)
				return index;
			else
				return -1;
		}
		
		void IList<DocumentLine>.Insert(int index, DocumentLine item)
		{
			throw new NotSupportedException();
		}
		
		void IList<DocumentLine>.RemoveAt(int index)
		{
			throw new NotSupportedException();
		}
		
		void ICollection<DocumentLine>.Add(DocumentLine item)
		{
			throw new NotSupportedException();
		}
		
		void ICollection<DocumentLine>.Clear()
		{
			throw new NotSupportedException();
		}
		
		bool ICollection<DocumentLine>.Contains(DocumentLine item)
		{
			IList<DocumentLine> self = this;
			return self.IndexOf(item) >= 0;
		}
		
		void ICollection<DocumentLine>.CopyTo(DocumentLine[] array, int arrayIndex)
		{
			if (array == null)
				throw new ArgumentNullException("array");
			if (array.Length < LineCount)
				throw new ArgumentException("The array is too small", "array");
			if (arrayIndex < 0 || arrayIndex + LineCount > array.Length)
				throw new ArgumentOutOfRangeException("arrayIndex", arrayIndex, "Value must be between 0 and " + (array.Length - LineCount));
			foreach (DocumentLine ls in this) {
				array[arrayIndex++] = ls;
			}
		}
		
		bool ICollection<DocumentLine>.Remove(DocumentLine item)
		{
			throw new NotSupportedException();
		}
		
		public IEnumerator<DocumentLine> GetEnumerator()
		{
			document.VerifyAccess();
			return Enumerate();
		}
		
		IEnumerator<DocumentLine> Enumerate()
		{
			document.VerifyAccess();
			DocumentLine line = root.LeftMost;
			while (line != null) {
				yield return line;
				line = line.NextLine;
			}
		}
		
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}
		#endregion
	}
}
