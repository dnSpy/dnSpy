// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ICSharpCode.AvalonEdit.Utils
{
	/// <summary>
	/// A IList{T} implementation that has efficient insertion and removal (in O(lg n) time)
	/// and that saves memory by allocating only one node when a value is repeated in adjacent indices.
	/// Based on this "compression", it also supports efficient InsertRange/SetRange/RemoveRange operations.
	/// </summary>
	/// <remarks>
	/// Current memory usage: 5*IntPtr.Size + 12 + sizeof(T) per node.
	/// Use this class only if lots of adjacent values are identical (can share one node).
	/// </remarks>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix",
	                                                 Justification = "It's an IList<T> implementation")]
	public sealed class CompressingTreeList<T> : IList<T>
	{
		// Further memory optimization: this tree could work without parent pointers. But that
		// requires changing most of tree manipulating logic.
		// Also possible is to remove the count field and calculate it as totalCount-left.totalCount-right.totalCount
		// - but that would make tree manipulations more difficult to handle.
		
		#region Node definition
		sealed class Node
		{
			internal Node left, right, parent;
			internal bool color;
			internal int count, totalCount;
			internal T value;
			
			public Node(T value, int count)
			{
				this.value = value;
				this.count = count;
				this.totalCount = count;
			}
			
			internal Node LeftMost {
				get {
					Node node = this;
					while (node.left != null)
						node = node.left;
					return node;
				}
			}
			
			internal Node RightMost {
				get {
					Node node = this;
					while (node.right != null)
						node = node.right;
					return node;
				}
			}
			
			/// <summary>
			/// Gets the inorder predecessor of the node.
			/// </summary>
			internal Node Predecessor {
				get {
					if (left != null) {
						return left.RightMost;
					} else {
						Node node = this;
						Node oldNode;
						do {
							oldNode = node;
							node = node.parent;
							// go up until we are coming out of a right subtree
						} while (node != null && node.left == oldNode);
						return node;
					}
				}
			}
			
			/// <summary>
			/// Gets the inorder successor of the node.
			/// </summary>
			internal Node Successor {
				get {
					if (right != null) {
						return right.LeftMost;
					} else {
						Node node = this;
						Node oldNode;
						do {
							oldNode = node;
							node = node.parent;
							// go up until we are coming out of a left subtree
						} while (node != null && node.right == oldNode);
						return node;
					}
				}
			}
			
			public override string ToString()
			{
				return "[TotalCount=" + totalCount + " Count=" + count + " Value=" + value + "]";
			}
		}
		#endregion
		
		#region Fields and Constructor
		readonly Func<T, T, bool> comparisonFunc;
		Node root;
		
		/// <summary>
		/// Creates a new CompressingTreeList instance.
		/// </summary>
		/// <param name="comparisonFunc">A function that checks two values for equality. If this
		/// function returns true, a single node may be used to store the two values.</param>
		public CompressingTreeList(Func<T, T, bool> comparisonFunc)
		{
			if (comparisonFunc == null)
				throw new ArgumentNullException("comparisonFunc");
			this.comparisonFunc = comparisonFunc;
		}
		#endregion
		
		#region InsertRange
		/// <summary>
		/// Inserts <paramref name="item"/> <paramref name="count"/> times at position
		/// <paramref name="index"/>.
		/// </summary>
		public void InsertRange(int index, int count, T item)
		{
			if (index < 0 || index > this.Count)
				throw new ArgumentOutOfRangeException("index", index, "Value must be between 0 and " + this.Count);
			if (count < 0)
				throw new ArgumentOutOfRangeException("count", count, "Value must not be negative");
			if (count == 0)
				return;
			unchecked {
				if (this.Count + count < 0)
					throw new OverflowException("Cannot insert elements: total number of elements must not exceed int.MaxValue.");
			}
			
			if (root == null) {
				root = new Node(item, count);
			} else {
				Node n = GetNode(ref index);
				// check if we can put the value into the node n:
				if (comparisonFunc(n.value, item)) {
					n.count += count;
					UpdateAugmentedData(n);
				} else if (index == n.count) {
					// this can only happen when appending at the end
					Debug.Assert(n == root.RightMost);
					InsertAsRight(n, new Node(item, count));
				} else if (index == 0) {
					// insert before:
					// maybe we can put the value in the previous node?
					Node p = n.Predecessor;
					if (p != null && comparisonFunc(p.value, item)) {
						p.count += count;
						UpdateAugmentedData(p);
					} else {
						InsertBefore(n, new Node(item, count));
					}
				} else {
					Debug.Assert(index > 0 && index < n.count);
					// insert in the middle:
					// split n into a new node and n
					n.count -= index;
					InsertBefore(n, new Node(n.value, index));
					// then insert the new item in between
					InsertBefore(n, new Node(item, count));
					UpdateAugmentedData(n);
				}
			}
			CheckProperties();
		}
		
		void InsertBefore(Node node, Node newNode)
		{
			if (node.left == null) {
				InsertAsLeft(node, newNode);
			} else {
				InsertAsRight(node.left.RightMost, newNode);
			}
		}
		#endregion
		
		#region RemoveRange
		/// <summary>
		/// Removes <paramref name="count"/> items starting at position
		/// <paramref name="index"/>.
		/// </summary>
		public void RemoveRange(int index, int count)
		{
			if (index < 0 || index > this.Count)
				throw new ArgumentOutOfRangeException("index", index, "Value must be between 0 and " + this.Count);
			if (count < 0 || index + count > this.Count)
				throw new ArgumentOutOfRangeException("count", count, "0 <= length, index(" + index + ")+count <= " + this.Count);
			if (count == 0)
				return;
			
			Node n = GetNode(ref index);
			if (index + count < n.count) {
				// just remove inside a single node
				n.count -= count;
				UpdateAugmentedData(n);
			} else {
				// keep only the part of n from 0 to index
				Node firstNodeBeforeDeletedRange;
				if (index > 0) {
					count -= (n.count - index);
					n.count = index;
					UpdateAugmentedData(n);
					firstNodeBeforeDeletedRange = n;
					n = n.Successor;
				} else {
					Debug.Assert(index == 0);
					firstNodeBeforeDeletedRange = n.Predecessor;
				}
				while (n != null && count >= n.count) {
					count -= n.count;
					Node s = n.Successor;
					RemoveNode(n);
					n = s;
				}
				if (count > 0) {
					Debug.Assert(n != null && count < n.count);
					n.count -= count;
					UpdateAugmentedData(n);
				}
				if (n != null) {
					Debug.Assert(n.Predecessor == firstNodeBeforeDeletedRange);
					if (firstNodeBeforeDeletedRange != null && comparisonFunc(firstNodeBeforeDeletedRange.value, n.value)) {
						firstNodeBeforeDeletedRange.count += n.count;
						RemoveNode(n);
						UpdateAugmentedData(firstNodeBeforeDeletedRange);
					}
				}
			}
			
			CheckProperties();
		}
		#endregion
		
		#region SetRange
		/// <summary>
		/// Sets <paramref name="count"/> indices starting at <paramref name="index"/> to
		/// <paramref name="item"/>
		/// </summary>
		public void SetRange(int index, int count, T item)
		{
			RemoveRange(index, count);
			InsertRange(index, count, item);
		}
		#endregion
		
		#region GetNode
		Node GetNode(ref int index)
		{
			Node node = root;
			while (true) {
				if (node.left != null && index < node.left.totalCount) {
					node = node.left;
				} else {
					if (node.left != null) {
						index -= node.left.totalCount;
					}
					if (index < node.count || node.right == null)
						return node;
					index -= node.count;
					node = node.right;
				}
			}
		}
		#endregion
		
		#region UpdateAugmentedData
		void UpdateAugmentedData(Node node)
		{
			int totalCount = node.count;
			if (node.left != null) totalCount += node.left.totalCount;
			if (node.right != null) totalCount += node.right.totalCount;
			if (node.totalCount != totalCount) {
				node.totalCount = totalCount;
				if (node.parent != null)
					UpdateAugmentedData(node.parent);
			}
		}
		#endregion
		
		#region IList<T> implementation
		/// <summary>
		/// Gets or sets an item by index.
		/// </summary>
		public T this[int index] {
			get {
				if (index < 0 || index >= this.Count)
					throw new ArgumentOutOfRangeException("index", index, "Value must be between 0 and " + (this.Count - 1));
				return GetNode(ref index).value;
			}
			set {
				RemoveAt(index);
				Insert(index, value);
			}
		}
		
		/// <summary>
		/// Gets the number of items in the list.
		/// </summary>
		public int Count {
			get {
				if (root != null)
					return root.totalCount;
				else
					return 0;
			}
		}
		
		bool ICollection<T>.IsReadOnly {
			get {
				return false;
			}
		}
		
		/// <summary>
		/// Gets the index of the specified <paramref name="item"/>.
		/// </summary>
		public int IndexOf(T item)
		{
			int index = 0;
			if (root != null) {
				Node n = root.LeftMost;
				while (n != null) {
					if (comparisonFunc(n.value, item))
						return index;
					index += n.count;
					n = n.Successor;
				}
			}
			Debug.Assert(index == this.Count);
			return -1;
		}
		
		/// <summary>
		/// Gets the the first index so that all values from the result index to <paramref name="index"/>
		/// are equal.
		/// </summary>
		public int GetStartOfRun(int index)
		{
			if (index < 0 || index >= this.Count)
				throw new ArgumentOutOfRangeException("index", index, "Value must be between 0 and " + (this.Count - 1));
			int indexInRun = index;
			GetNode(ref indexInRun);
			return index - indexInRun;
		}

		/// <summary>
		/// Gets the first index after <paramref name="index"/> so that the value at the result index is not
		/// equal to the value at <paramref name="index"/>.
		/// That is, this method returns the exclusive end index of the run of equal values.
		/// </summary>
		public int GetEndOfRun(int index)
		{
			if (index < 0 || index >= this.Count)
				throw new ArgumentOutOfRangeException("index", index, "Value must be between 0 and " + (this.Count - 1));
			int indexInRun = index;
			int runLength = GetNode(ref indexInRun).count;
			return index - indexInRun + runLength;
		}

		/// <summary>
		/// Gets the number of elements after <paramref name="index"/> that have the same value as each other.
		/// </summary>
		[Obsolete("This method may be confusing as it returns only the remaining run length after index. " +
		          "Use GetStartOfRun/GetEndOfRun instead.")]
		public int GetRunLength(int index)
		{
			if (index < 0 || index >= this.Count)
				throw new ArgumentOutOfRangeException("index", index, "Value must be between 0 and " + (this.Count - 1));
			return GetNode(ref index).count - index;
		}
		
		/// <summary>
		/// Applies the conversion function to all elements in this CompressingTreeList.
		/// </summary>
		public void Transform(Func<T, T> converter)
		{
			if (root == null)
				return;
			Node prevNode = null;
			for (Node n = root.LeftMost; n != null; n = n.Successor) {
				n.value = converter(n.value);
				if (prevNode != null && comparisonFunc(prevNode.value, n.value)) {
					n.count += prevNode.count;
					UpdateAugmentedData(n);
					RemoveNode(prevNode);
				}
				prevNode = n;
			}
		}

		
		/// <summary>
		/// Inserts the specified <paramref name="item"/> at <paramref name="index"/>
		/// </summary>
		public void Insert(int index, T item)
		{
			InsertRange(index, 1, item);
		}
		
		/// <summary>
		/// Removes one item at <paramref name="index"/>
		/// </summary>
		public void RemoveAt(int index)
		{
			RemoveRange(index, 1);
		}
		
		/// <summary>
		/// Adds the specified <paramref name="item"/> to the end of the list.
		/// </summary>
		public void Add(T item)
		{
			InsertRange(this.Count, 1, item);
		}
		
		/// <summary>
		/// Removes all items from this list.
		/// </summary>
		public void Clear()
		{
			root = null;
		}
		
		/// <summary>
		/// Gets whether this list contains the specified item.
		/// </summary>
		public bool Contains(T item)
		{
			return IndexOf(item) >= 0;
		}
		
		/// <summary>
		/// Copies all items in this list to the specified array.
		/// </summary>
		public void CopyTo(T[] array, int arrayIndex)
		{
			if (array == null)
				throw new ArgumentNullException("array");
			if (array.Length < this.Count)
				throw new ArgumentException("The array is too small", "array");
			if (arrayIndex < 0 || arrayIndex + this.Count > array.Length)
				throw new ArgumentOutOfRangeException("arrayIndex", arrayIndex, "Value must be between 0 and " + (array.Length - this.Count));
			foreach (T v in this) {
				array[arrayIndex++] = v;
			}
		}
		
		/// <summary>
		/// Removes the specified item from this list.
		/// </summary>
		public bool Remove(T item)
		{
			int index = IndexOf(item);
			if (index >= 0) {
				RemoveAt(index);
				return true;
			} else {
				return false;
			}
		}
		#endregion
		
		#region IEnumerable<T>
		/// <summary>
		/// Gets an enumerator for this list.
		/// </summary>
		public IEnumerator<T> GetEnumerator()
		{
			if (root != null) {
				Node n = root.LeftMost;
				while (n != null) {
					for (int i = 0; i < n.count; i++) {
						yield return n.value;
					}
					n = n.Successor;
				}
			}
		}
		
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		#endregion
		
		#region Red/Black Tree
		internal const bool RED = true;
		internal const bool BLACK = false;
		
		void InsertAsLeft(Node parentNode, Node newNode)
		{
			Debug.Assert(parentNode.left == null);
			parentNode.left = newNode;
			newNode.parent = parentNode;
			newNode.color = RED;
			UpdateAugmentedData(parentNode);
			FixTreeOnInsert(newNode);
		}
		
		void InsertAsRight(Node parentNode, Node newNode)
		{
			Debug.Assert(parentNode.right == null);
			parentNode.right = newNode;
			newNode.parent = parentNode;
			newNode.color = RED;
			UpdateAugmentedData(parentNode);
			FixTreeOnInsert(newNode);
		}
		
		void FixTreeOnInsert(Node node)
		{
			Debug.Assert(node != null);
			Debug.Assert(node.color == RED);
			Debug.Assert(node.left == null || node.left.color == BLACK);
			Debug.Assert(node.right == null || node.right.color == BLACK);
			
			Node parentNode = node.parent;
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
			Node grandparentNode = parentNode.parent;
			Node uncleNode = Sibling(parentNode);
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
		
		void RemoveNode(Node removedNode)
		{
			if (removedNode.left != null && removedNode.right != null) {
				// replace removedNode with it's in-order successor
				
				Node leftMost = removedNode.right.LeftMost;
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
			Node parentNode = removedNode.parent;
			Node childNode = removedNode.left ?? removedNode.right;
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
		
		void FixTreeOnDelete(Node node, Node parentNode)
		{
			Debug.Assert(node == null || node.parent == parentNode);
			if (parentNode == null)
				return;
			
			// warning: node may be null
			Node sibling = Sibling(node, parentNode);
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
		
		void ReplaceNode(Node replacedNode, Node newNode)
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
		
		void RotateLeft(Node p)
		{
			// let q be p's right child
			Node q = p.right;
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
		
		void RotateRight(Node p)
		{
			// let q be p's left child
			Node q = p.left;
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
		
		static Node Sibling(Node node)
		{
			if (node == node.parent.left)
				return node.parent.right;
			else
				return node.parent.left;
		}
		
		static Node Sibling(Node node, Node parentNode)
		{
			Debug.Assert(node == null || node.parent == parentNode);
			if (node == parentNode.left)
				return parentNode.right;
			else
				return parentNode.left;
		}
		
		static bool GetColor(Node node)
		{
			return node != null ? node.color : BLACK;
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
				
				// ensure that the tree is compressed:
				Node p = root.LeftMost;
				Node n = p.Successor;
				while (n != null) {
					Debug.Assert(!comparisonFunc(p.value, n.value));
					p = n;
					n = p.Successor;
				}
			}
			#endif
		}
		
		#if DEBUG
		void CheckProperties(Node node)
		{
			Debug.Assert(node.count > 0);
			int totalCount = node.count;
			if (node.left != null) {
				CheckProperties(node.left);
				totalCount += node.left.totalCount;
			}
			if (node.right != null) {
				CheckProperties(node.right);
				totalCount += node.right.totalCount;
			}
			Debug.Assert(node.totalCount == totalCount);
		}
		
		/*
		1. A node is either red or black.
		2. The root is black.
		3. All leaves are black. (The leaves are the NIL children.)
		4. Both children of every red node are black. (So every red node must have a black parent.)
		5. Every simple path from a node to a descendant leaf contains the same number of black nodes. (Not counting the leaf node.)
		 */
		void CheckNodeProperties(Node node, Node parentNode, bool parentColor, int blackCount, ref int expectedBlackCount)
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
		#endif
		#endregion
		
		#region GetTreeAsString
		internal string GetTreeAsString()
		{
			#if DEBUG
			if (root == null)
				return "<empty tree>";
			StringBuilder b = new StringBuilder();
			AppendTreeToString(root, b, 0);
			return b.ToString();
			#else
			return "Not available in release build.";
			#endif
		}
		
		#if DEBUG
		static void AppendTreeToString(Node node, StringBuilder b, int indent)
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
	}
}
