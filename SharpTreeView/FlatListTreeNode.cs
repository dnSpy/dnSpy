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
		SharpTreeNode listParent;
		/// <summary>Left/right nodes in the flat list</summary>
		SharpTreeNode left, right;
		
		/// <summary>Subtree height in the flat list tree</summary>
		byte height;
		
		/// <summary>Length in the flat list, including children (children within the flat list).</summary>
		internal int totalListLength = 1;
		
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
			if (left != null) left.CheckInvariants();
			if (right != null) right.CheckInvariants();
		}
		
		#region GetNodeByVisibleIndex / GetVisibleIndexForNode
		internal static SharpTreeNode GetNodeByVisibleIndex(SharpTreeNode root, int index)
		{
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
			int index = 0;
			while (node.listParent != null) {
				if (node == node.listParent.right) {
					if (node.listParent.left != null)
						index += node.listParent.left.totalListLength;
					if (node.isVisible)
						index++;
				}
				node = node.listParent;
			}
			return index;
		}
		#endregion
	}
}
