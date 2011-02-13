// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Diagnostics;

namespace ICSharpCode.TreeView
{
	// This part of SharpTreeNode controls the 'flat list' data structure, which emulates
	// a big flat list containing the whole tree; allowing access by visible index.
	partial class SharpTreeNode
	{
		/// <summary>The parent in the flat list</summary>
		internal SharpTreeNode listParent;
		/// <summary>Left/right nodes in the flat list</summary>
		SharpTreeNode left, right;
		
		internal TreeFlattener treeFlattener;
		
		/// <summary>Subtree height in the flat list tree</summary>
		byte height = 1;
		
		/// <summary>Length in the flat list, including children (children within the flat list). -1 = invalidated</summary>
		int totalListLength = -1;
		
		int Balance {
			get { return Height(right) - Height(left); }
		}
		
		static int Height(SharpTreeNode node)
		{
			return node != null ? node.height : 0;
		}
		
		[Conditional("DEBUG")]
		void CheckInvariants()
		{
			Debug.Assert(left == null || left.listParent == this);
			Debug.Assert(right == null || right.listParent == this);
			Debug.Assert(height == 1 + Math.Max(Height(left), Height(right)));
			Debug.Assert(Math.Abs(this.Balance) <= 1);
			Debug.Assert(totalListLength == -1 || totalListLength == (left != null ? left.totalListLength : 0) + (isVisible ? 1 : 0) + (right != null ? right.totalListLength : 0));
			if (left != null) left.CheckInvariants();
			if (right != null) right.CheckInvariants();
		}
		
		SharpTreeNode GetListRoot()
		{
			SharpTreeNode node = this;
			while (node.listParent != null)
				node = node.listParent;
			return node;
		}
		
		#region GetNodeByVisibleIndex / GetVisibleIndexForNode
		internal static SharpTreeNode GetNodeByVisibleIndex(SharpTreeNode root, int index)
		{
			root.GetTotalListLength(); // ensure all list lengths are calculated
			Debug.Assert(index >= 0);
			Debug.Assert(index < root.totalListLength);
			SharpTreeNode node = root;
			while (true) {
				if (node.left != null && index < node.left.totalListLength) {
					node = node.left;
				} else {
					if (node.left != null) {
						index -= node.left.totalListLength;
					}
					if (node.isVisible) {
						if (index == 0)
							return node;
						index--;
					}
					node = node.right;
				}
			}
		}
		
		internal static int GetVisibleIndexForNode(SharpTreeNode node)
		{
			int index = node.left != null ? node.left.GetTotalListLength() : 0;
			while (node.listParent != null) {
				if (node == node.listParent.right) {
					if (node.listParent.left != null)
						index += node.listParent.left.GetTotalListLength();
					if (node.listParent.isVisible)
						index++;
				}
				node = node.listParent;
			}
			return index;
		}
		#endregion
		
		#region Balancing
		/// <summary>
		/// Balances the subtree rooted in <paramref name="node"/> and recomputes the 'height' field.
		/// This method assumes that the children of this node are already balanced and have an up-to-date 'height' value.
		/// </summary>
		/// <returns>The new root node</returns>
		static SharpTreeNode Rebalance(SharpTreeNode node)
		{
			Debug.Assert(node.left == null || Math.Abs(node.left.Balance) <= 1);
			Debug.Assert(node.right == null || Math.Abs(node.right.Balance) <= 1);
			// Keep looping until it's balanced. Not sure if this is stricly required; this is based on
			// the Rope code where node merging made this necessary.
			while (Math.Abs(node.Balance) > 1) {
				// AVL balancing
				// note: because we don't care about the identity of concat nodes, this works a little different than usual
				// tree rotations: in our implementation, the "this" node will stay at the top, only its children are rearranged
				if (node.Balance > 1) {
					if (node.right.Balance < 0) {
						node.right = node.right.RotateRight();
					}
					node = node.RotateLeft();
					// If 'node' was unbalanced by more than 2, we've shifted some of the inbalance to the left node; so rebalance that.
					node.left = Rebalance(node.left);
				} else if (node.Balance < -1) {
					if (node.left.Balance > 0) {
						node.left = node.left.RotateLeft();
					}
					node = node.RotateRight();
					// If 'node' was unbalanced by more than 2, we've shifted some of the inbalance to the right node; so rebalance that.
					node.right = Rebalance(node.right);
				}
			}
			Debug.Assert(Math.Abs(node.Balance) <= 1);
			node.height = (byte)(1 + Math.Max(Height(node.left), Height(node.right)));
			node.totalListLength = -1; // mark for recalculation
			// since balancing checks the whole tree up to the root, the whole path will get marked as invalid
			return node;
		}
		
		internal int GetTotalListLength()
		{
			if (totalListLength >= 0)
				return totalListLength;
			int length = (isVisible ? 1 : 0);
			if (left != null) {
				length += left.GetTotalListLength();
			}
			if (right != null)  {
				length += right.GetTotalListLength();
			}
			return totalListLength = length;
		}
		
		SharpTreeNode RotateLeft()
		{
			/* Rotate tree to the left
			 * 
			 *       this               right
			 *       /  \               /  \
			 *      A   right   ===>  this  C
			 *           / \          / \
			 *          B   C        A   B
			 */
			SharpTreeNode b = right.left;
			SharpTreeNode newTop = right;
			
			if (b != null) b.listParent = this;
			this.right = b;
			newTop.left = this;
			newTop.listParent = this.listParent;
			this.listParent = newTop;
			// rebalance the 'this' node - this is necessary in some bulk insertion cases:
			newTop.left = Rebalance(this);
			return newTop;
		}
		
		SharpTreeNode RotateRight()
		{
			/* Rotate tree to the right
			 * 
			 *       this             left
			 *       /  \             /  \
			 *     left  C   ===>    A   this
			 *     / \                   /  \
			 *    A   B                 B    C
			 */
			SharpTreeNode b = left.right;
			SharpTreeNode newTop = left;
			
			if (b != null) b.listParent = this;
			this.left = b;
			newTop.right = this;
			newTop.listParent = this.listParent;
			this.listParent = newTop;
			newTop.right = Rebalance(this);
			return newTop;
		}
		#endregion
		
		#region Insertion
		static void InsertNodeAfter(SharpTreeNode pos, SharpTreeNode newNode)
		{
			// newNode might be the model root of a whole subtree, so go to the list root of that subtree:
			newNode = newNode.GetListRoot();
			if (pos.right == null) {
				pos.right = newNode;
				newNode.listParent = pos;
			} else {
				// insert before pos.right's leftmost:
				pos = pos.right;
				while (pos.left != null)
					pos = pos.left;
				Debug.Assert(pos.left == null);
				pos.left = newNode;
				newNode.listParent = pos;
			}
			RebalanceUntilRoot(pos);
		}
		
		static void RebalanceUntilRoot(SharpTreeNode pos)
		{
			while (pos.listParent != null) {
				if (pos == pos.listParent.left) {
					pos = pos.listParent.left = Rebalance(pos);
				} else {
					Debug.Assert(pos == pos.listParent.right);
					pos = pos.listParent.right = Rebalance(pos);
				}
				pos = pos.listParent;
			}
			SharpTreeNode newRoot = Rebalance(pos);
			if (newRoot != pos && pos.treeFlattener != null) {
				Debug.Assert(newRoot.treeFlattener == null);
				newRoot.treeFlattener = pos.treeFlattener;
				pos.treeFlattener = null;
				newRoot.treeFlattener.root = newRoot;
			}
			Debug.Assert(newRoot.listParent == null);
			newRoot.CheckInvariants();
		}
		
		[Conditional("DEBUG")]
		static void DumpTree(SharpTreeNode node)
		{
			node.GetListRoot().DumpTree();
		}
		
		[Conditional("DEBUG")]
		void DumpTree()
		{
			Debug.Indent();
			if (left != null)
				left.DumpTree();
			Debug.Unindent();
			Debug.WriteLine("{0}, totalListLength={1}, height={2}, Balance={3}, isVisible={4}", ToString(), totalListLength, height, Balance, isVisible);
			Debug.Indent();
			if (right != null)
				right.DumpTree();
			Debug.Unindent();
		}
		#endregion
	}
}
