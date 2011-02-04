// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics;

using ICSharpCode.AvalonEdit.Document;

namespace ICSharpCode.AvalonEdit.Rendering
{
	/// <summary>
	/// A node in the text view's height tree.
	/// </summary>
	sealed class HeightTreeNode
	{
		internal readonly DocumentLine documentLine;
		internal HeightTreeLineNode lineNode;
		
		internal HeightTreeNode left, right, parent;
		internal bool color;
		
		internal HeightTreeNode()
		{
		}
		
		internal HeightTreeNode(DocumentLine documentLine, double height)
		{
			this.documentLine = documentLine;
			this.totalCount = 1;
			this.lineNode = new HeightTreeLineNode(height);
			this.totalHeight = height;
		}
		
		internal HeightTreeNode LeftMost {
			get {
				HeightTreeNode node = this;
				while (node.left != null)
					node = node.left;
				return node;
			}
		}
		
		internal HeightTreeNode RightMost {
			get {
				HeightTreeNode node = this;
				while (node.right != null)
					node = node.right;
				return node;
			}
		}
		
		/// <summary>
		/// Gets the inorder successor of the node.
		/// </summary>
		internal HeightTreeNode Successor {
			get {
				if (right != null) {
					return right.LeftMost;
				} else {
					HeightTreeNode node = this;
					HeightTreeNode oldNode;
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
		/// The number of lines in this node and its child nodes.
		/// Invariant:
		///   totalCount = 1 + left.totalCount + right.totalCount
		/// </summary>
		internal int totalCount;
		
		/// <summary>
		/// The total height of this node and its child nodes, excluding directly collapsed nodes.
		/// Invariant:
		///   totalHeight = left.IsDirectlyCollapsed ? 0 : left.totalHeight
		///               + lineNode.IsDirectlyCollapsed ? 0 : lineNode.Height
		///               + right.IsDirectlyCollapsed ? 0 : right.totalHeight
		/// </summary>
		internal double totalHeight;
		
		/// <summary>
		/// List of the sections that hold this node collapsed.
		/// Invariant 1:
		///   For each document line in the range described by a CollapsedSection, exactly one ancestor
		///   contains that CollapsedSection.
		/// Invariant 2:
		///   A CollapsedSection is contained either in left+middle or middle+right or just middle.
		/// Invariant 3:
		///   Start and end of a CollapsedSection always contain the collapsedSection in their
		///   documentLine (middle node).
		/// </summary>
		internal List<CollapsedLineSection> collapsedSections;
		
		internal bool IsDirectlyCollapsed {
			get {
				return collapsedSections != null;
			}
		}
		
		internal void AddDirectlyCollapsed(CollapsedLineSection section)
		{
			if (collapsedSections == null) {
				collapsedSections = new List<CollapsedLineSection>();
				totalHeight = 0;
			}
			Debug.Assert(!collapsedSections.Contains(section));
			collapsedSections.Add(section);
		}
		
		
		internal void RemoveDirectlyCollapsed(CollapsedLineSection section)
		{
			Debug.Assert(collapsedSections.Contains(section));
			collapsedSections.Remove(section);
			if (collapsedSections.Count == 0) {
				collapsedSections = null;
				totalHeight = lineNode.TotalHeight;
				if (left != null)
					totalHeight += left.totalHeight;
				if (right != null)
					totalHeight += right.totalHeight;
			}
		}
		
		#if DEBUG
		public override string ToString()
		{
			return "[HeightTreeNode "
				+ documentLine.LineNumber + " CS=" + GetCollapsedSections(collapsedSections)
				+ " Line.CS=" + GetCollapsedSections(lineNode.collapsedSections)
				+ " Line.Height=" + lineNode.height
				+ " TotalHeight=" + totalHeight
				+ "]";
		}
		
		static string GetCollapsedSections(List<CollapsedLineSection> list)
		{
			if (list == null)
				return "{}";
			return "{" +
				string.Join(",",
				            list.ConvertAll(cs=>cs.ID).ToArray())
				+ "}";
		}
		#endif
	}
}
