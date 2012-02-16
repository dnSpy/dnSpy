// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Utils;

namespace ICSharpCode.AvalonEdit.Rendering
{
	/// <summary>
	/// Red-black tree similar to DocumentLineTree, augmented with collapsing and height data.
	/// </summary>
	sealed class HeightTree : ILineTracker, IDisposable
	{
		// TODO: Optimize this. This tree takes alot of memory.
		// (56 bytes for HeightTreeNode
		// We should try to get rid of the dictionary and find height nodes per index. (DONE!)
		// And we might do much better by compressing lines with the same height into a single node.
		// That would also improve load times because we would always start with just a single node.
		
		/* Idea:
		 class NewHeightTreeNode {
			int totalCount; // =count+left.count+right.count
			int count; // one node can represent multiple lines
			double height; // height of each line in this node
			double totalHeight; // =(collapsedSections!=null?0:height*count) + left.totalHeight + right.totalHeight
			List<CollapsedSection> collapsedSections; // sections holding this line collapsed
			// no "nodeCollapsedSections"/"totalCollapsedSections":
			NewHeightTreeNode left, right, parent;
			bool color;
		}
		totalCollapsedSections: are hard to update and not worth the effort. O(n log n) isn't too bad for
		 collapsing/uncollapsing, especially when compression reduces the n.
		 */
		
		#region Constructor
		readonly TextDocument document;
		HeightTreeNode root;
		WeakLineTracker weakLineTracker;
		
		public HeightTree(TextDocument document, double defaultLineHeight)
		{
			this.document = document;
			weakLineTracker = WeakLineTracker.Register(document, this);
			this.DefaultLineHeight = defaultLineHeight;
			RebuildDocument();
		}
		
		public void Dispose()
		{
			if (weakLineTracker != null)
				weakLineTracker.Deregister();
			this.root = null;
			this.weakLineTracker = null;
		}
		
		double defaultLineHeight;
		
		public double DefaultLineHeight {
			get { return defaultLineHeight; }
			set {
				double oldValue = defaultLineHeight;
				if (oldValue == value)
					return;
				defaultLineHeight = value;
				// update the stored value in all nodes:
				foreach (var node in AllNodes) {
					if (node.lineNode.height == oldValue) {
						node.lineNode.height = value;
						UpdateAugmentedData(node, UpdateAfterChildrenChangeRecursionMode.IfRequired);
					}
				}
			}
		}
		
		HeightTreeNode GetNode(DocumentLine ls)
		{
			return GetNodeByIndex(ls.LineNumber - 1);
		}
		#endregion
		
		#region RebuildDocument
		void ILineTracker.SetLineLength(DocumentLine ls, int newTotalLength)
		{
		}
		
		/// <summary>
		/// Rebuild the tree, in O(n).
		/// </summary>
		public void RebuildDocument()
		{
			foreach (CollapsedLineSection s in GetAllCollapsedSections()) {
				s.Start = null;
				s.End = null;
			}
			
			HeightTreeNode[] nodes = new HeightTreeNode[document.LineCount];
			int lineNumber = 0;
			foreach (DocumentLine ls in document.Lines) {
				nodes[lineNumber++] = new HeightTreeNode(ls, defaultLineHeight);
			}
			Debug.Assert(nodes.Length > 0);
			// now build the corresponding balanced tree
			int height = DocumentLineTree.GetTreeHeight(nodes.Length);
			Debug.WriteLine("HeightTree will have height: " + height);
			root = BuildTree(nodes, 0, nodes.Length, height);
			root.color = BLACK;
			#if DEBUG
			CheckProperties();
			#endif
		}
		
		/// <summary>
		/// build a tree from a list of nodes
		/// </summary>
		HeightTreeNode BuildTree(HeightTreeNode[] nodes, int start, int end, int subtreeHeight)
		{
			Debug.Assert(start <= end);
			if (start == end) {
				return null;
			}
			int middle = (start + end) / 2;
			HeightTreeNode node = nodes[middle];
			node.left = BuildTree(nodes, start, middle, subtreeHeight - 1);
			node.right = BuildTree(nodes, middle + 1, end, subtreeHeight - 1);
			if (node.left != null) node.left.parent = node;
			if (node.right != null) node.right.parent = node;
			if (subtreeHeight == 1)
				node.color = RED;
			UpdateAugmentedData(node, UpdateAfterChildrenChangeRecursionMode.None);
			return node;
		}
		#endregion
		
		#region Insert/Remove lines
		void ILineTracker.BeforeRemoveLine(DocumentLine line)
		{
			HeightTreeNode node = GetNode(line);
			if (node.lineNode.collapsedSections != null) {
				foreach (CollapsedLineSection cs in node.lineNode.collapsedSections.ToArray()) {
					if (cs.Start == line && cs.End == line) {
						cs.Start = null;
						cs.End = null;
					} else if (cs.Start == line) {
						Uncollapse(cs);
						cs.Start = line.NextLine;
						AddCollapsedSection(cs, cs.End.LineNumber - cs.Start.LineNumber + 1);
					} else if (cs.End == line) {
						Uncollapse(cs);
						cs.End = line.PreviousLine;
						AddCollapsedSection(cs, cs.End.LineNumber - cs.Start.LineNumber + 1);
					}
				}
			}
			BeginRemoval();
			RemoveNode(node);
			// clear collapsedSections from removed line: prevent damage if removed line is in "nodesToCheckForMerging"
			node.lineNode.collapsedSections = null;
			EndRemoval();
		}
		
//		void ILineTracker.AfterRemoveLine(DocumentLine line)
//		{
//
//		}
		
		void ILineTracker.LineInserted(DocumentLine insertionPos, DocumentLine newLine)
		{
			InsertAfter(GetNode(insertionPos), newLine);
			#if DEBUG
			CheckProperties();
			#endif
		}
		
		HeightTreeNode InsertAfter(HeightTreeNode node, DocumentLine newLine)
		{
			HeightTreeNode newNode = new HeightTreeNode(newLine, defaultLineHeight);
			if (node.right == null) {
				if (node.lineNode.collapsedSections != null) {
					// we are inserting directly after node - so copy all collapsedSections
					// that do not end at node.
					foreach (CollapsedLineSection cs in node.lineNode.collapsedSections) {
						if (cs.End != node.documentLine)
							newNode.AddDirectlyCollapsed(cs);
					}
				}
				InsertAsRight(node, newNode);
			} else {
				node = node.right.LeftMost;
				if (node.lineNode.collapsedSections != null) {
					// we are inserting directly before node - so copy all collapsedSections
					// that do not start at node.
					foreach (CollapsedLineSection cs in node.lineNode.collapsedSections) {
						if (cs.Start != node.documentLine)
							newNode.AddDirectlyCollapsed(cs);
					}
				}
				InsertAsLeft(node, newNode);
			}
			return newNode;
		}
		#endregion
		
		#region Rotation callbacks
		enum UpdateAfterChildrenChangeRecursionMode
		{
			None,
			IfRequired,
			WholeBranch
		}
		
		static void UpdateAfterChildrenChange(HeightTreeNode node)
		{
			UpdateAugmentedData(node, UpdateAfterChildrenChangeRecursionMode.IfRequired);
		}
		
		static void UpdateAugmentedData(HeightTreeNode node, UpdateAfterChildrenChangeRecursionMode mode)
		{
			int totalCount = 1;
			double totalHeight = node.lineNode.TotalHeight;
			if (node.left != null) {
				totalCount += node.left.totalCount;
				totalHeight += node.left.totalHeight;
			}
			if (node.right != null) {
				totalCount += node.right.totalCount;
				totalHeight += node.right.totalHeight;
			}
			if (node.IsDirectlyCollapsed)
				totalHeight = 0;
			if (totalCount != node.totalCount
			    || !totalHeight.IsClose(node.totalHeight)
			    || mode == UpdateAfterChildrenChangeRecursionMode.WholeBranch)
			{
				node.totalCount = totalCount;
				node.totalHeight = totalHeight;
				if (node.parent != null && mode != UpdateAfterChildrenChangeRecursionMode.None)
					UpdateAugmentedData(node.parent, mode);
			}
		}
		
		void UpdateAfterRotateLeft(HeightTreeNode node)
		{
			// node = old parent
			// node.parent = pivot, new parent
			var collapsedP = node.parent.collapsedSections;
			var collapsedQ = node.collapsedSections;
			// move collapsedSections from old parent to new parent
			node.parent.collapsedSections = collapsedQ;
			node.collapsedSections = null;
			// split the collapsedSections from the new parent into its old children:
			if (collapsedP != null) {
				foreach (CollapsedLineSection cs in collapsedP) {
					if (node.parent.right != null)
						node.parent.right.AddDirectlyCollapsed(cs);
					node.parent.lineNode.AddDirectlyCollapsed(cs);
					if (node.right != null)
						node.right.AddDirectlyCollapsed(cs);
				}
			}
			MergeCollapsedSectionsIfPossible(node);
			
			UpdateAfterChildrenChange(node);
			
			// not required: rotations only happen on insertions/deletions
			// -> totalCount changes -> the parent is always updated
			//UpdateAfterChildrenChange(node.parent);
		}
		
		void UpdateAfterRotateRight(HeightTreeNode node)
		{
			// node = old parent
			// node.parent = pivot, new parent
			var collapsedP = node.parent.collapsedSections;
			var collapsedQ = node.collapsedSections;
			// move collapsedSections from old parent to new parent
			node.parent.collapsedSections = collapsedQ;
			node.collapsedSections = null;
			// split the collapsedSections from the new parent into its old children:
			if (collapsedP != null) {
				foreach (CollapsedLineSection cs in collapsedP) {
					if (node.parent.left != null)
						node.parent.left.AddDirectlyCollapsed(cs);
					node.parent.lineNode.AddDirectlyCollapsed(cs);
					if (node.left != null)
						node.left.AddDirectlyCollapsed(cs);
				}
			}
			MergeCollapsedSectionsIfPossible(node);
			
			UpdateAfterChildrenChange(node);
			
			// not required: rotations only happen on insertions/deletions
			// -> totalCount changes -> the parent is always updated
			//UpdateAfterChildrenChange(node.parent);
		}
		
		// node removal:
		// a node in the middle of the tree is removed as following:
		//  its successor is removed
		//  it is replaced with its successor
		
		void BeforeNodeRemove(HeightTreeNode removedNode)
		{
			Debug.Assert(removedNode.left == null || removedNode.right == null);
			
			var collapsed = removedNode.collapsedSections;
			if (collapsed != null) {
				HeightTreeNode childNode = removedNode.left ?? removedNode.right;
				if (childNode != null) {
					foreach (CollapsedLineSection cs in collapsed)
						childNode.AddDirectlyCollapsed(cs);
				}
			}
			if (removedNode.parent != null)
				MergeCollapsedSectionsIfPossible(removedNode.parent);
		}
		
		void BeforeNodeReplace(HeightTreeNode removedNode, HeightTreeNode newNode, HeightTreeNode newNodeOldParent)
		{
			Debug.Assert(removedNode != null);
			Debug.Assert(newNode != null);
			while (newNodeOldParent != removedNode) {
				if (newNodeOldParent.collapsedSections != null) {
					foreach (CollapsedLineSection cs in newNodeOldParent.collapsedSections) {
						newNode.lineNode.AddDirectlyCollapsed(cs);
					}
				}
				newNodeOldParent = newNodeOldParent.parent;
			}
			if (newNode.collapsedSections != null) {
				foreach (CollapsedLineSection cs in newNode.collapsedSections) {
					newNode.lineNode.AddDirectlyCollapsed(cs);
				}
			}
			newNode.collapsedSections = removedNode.collapsedSections;
			MergeCollapsedSectionsIfPossible(newNode);
		}
		
		bool inRemoval;
		List<HeightTreeNode> nodesToCheckForMerging;
		
		void BeginRemoval()
		{
			Debug.Assert(!inRemoval);
			if (nodesToCheckForMerging == null) {
				nodesToCheckForMerging = new List<HeightTreeNode>();
			}
			inRemoval = true;
		}
		
		void EndRemoval()
		{
			Debug.Assert(inRemoval);
			inRemoval = false;
			foreach (HeightTreeNode node in nodesToCheckForMerging) {
				MergeCollapsedSectionsIfPossible(node);
			}
			nodesToCheckForMerging.Clear();
		}
		
		void MergeCollapsedSectionsIfPossible(HeightTreeNode node)
		{
			Debug.Assert(node != null);
			if (inRemoval) {
				nodesToCheckForMerging.Add(node);
				return;
			}
			// now check if we need to merge collapsedSections together
			bool merged = false;
			var collapsedL = node.lineNode.collapsedSections;
			if (collapsedL != null) {
				for (int i = collapsedL.Count - 1; i >= 0; i--) {
					CollapsedLineSection cs = collapsedL[i];
					if (cs.Start == node.documentLine || cs.End == node.documentLine)
						continue;
					if (node.left == null
					    || (node.left.collapsedSections != null && node.left.collapsedSections.Contains(cs)))
					{
						if (node.right == null
						    || (node.right.collapsedSections != null && node.right.collapsedSections.Contains(cs)))
						{
							// all children of node contain cs: -> merge!
							if (node.left != null) node.left.RemoveDirectlyCollapsed(cs);
							if (node.right != null) node.right.RemoveDirectlyCollapsed(cs);
							collapsedL.RemoveAt(i);
							node.AddDirectlyCollapsed(cs);
							merged = true;
						}
					}
				}
				if (collapsedL.Count == 0)
					node.lineNode.collapsedSections = null;
			}
			if (merged && node.parent != null) {
				MergeCollapsedSectionsIfPossible(node.parent);
			}
		}
		#endregion
		
		#region GetNodeBy... / Get...FromNode
		HeightTreeNode GetNodeByIndex(int index)
		{
			Debug.Assert(index >= 0);
			Debug.Assert(index < root.totalCount);
			HeightTreeNode node = root;
			while (true) {
				if (node.left != null && index < node.left.totalCount) {
					node = node.left;
				} else {
					if (node.left != null) {
						index -= node.left.totalCount;
					}
					if (index == 0)
						return node;
					index--;
					node = node.right;
				}
			}
		}
		
		HeightTreeNode GetNodeByVisualPosition(double position)
		{
			HeightTreeNode node = root;
			while (true) {
				double positionAfterLeft = position;
				if (node.left != null) {
					positionAfterLeft -= node.left.totalHeight;
					if (positionAfterLeft < 0) {
						// Descend into left
						node = node.left;
						continue;
					}
				}
				double positionBeforeRight = positionAfterLeft - node.lineNode.TotalHeight;
				if (positionBeforeRight < 0) {
					// Found the correct node
					return node;
				}
				if (node.right == null || node.right.totalHeight == 0) {
					// Can happen when position>node.totalHeight,
					// i.e. at the end of the document, or due to rounding errors in previous loop iterations.
					
					// If node.lineNode isn't collapsed, return that.
					// Also return node.lineNode if there is no previous node that we could return instead.
					if (node.lineNode.TotalHeight > 0 || node.left == null)
						return node;
					// Otherwise, descend into left (find the last non-collapsed node)
					node = node.left;
				} else {
					// Descend into right
					position = positionBeforeRight;
					node = node.right;
				}
			}
		}
		
		static double GetVisualPositionFromNode(HeightTreeNode node)
		{
			double position = (node.left != null) ? node.left.totalHeight : 0;
			while (node.parent != null) {
				if (node.IsDirectlyCollapsed)
					position = 0;
				if (node == node.parent.right) {
					if (node.parent.left != null)
						position += node.parent.left.totalHeight;
					position += node.parent.lineNode.TotalHeight;
				}
				node = node.parent;
			}
			return position;
		}
		#endregion
		
		#region Public methods
		public DocumentLine GetLineByNumber(int number)
		{
			return GetNodeByIndex(number - 1).documentLine;
		}
		
		public DocumentLine GetLineByVisualPosition(double position)
		{
			return GetNodeByVisualPosition(position).documentLine;
		}
		
		public double GetVisualPosition(DocumentLine line)
		{
			return GetVisualPositionFromNode(GetNode(line));
		}
		
		public double GetHeight(DocumentLine line)
		{
			return GetNode(line).lineNode.height;
		}
		
		public void SetHeight(DocumentLine line, double val)
		{
			var node = GetNode(line);
			node.lineNode.height = val;
			UpdateAfterChildrenChange(node);
		}
		
		public bool GetIsCollapsed(int lineNumber)
		{
			var node = GetNodeByIndex(lineNumber - 1);
			return node.lineNode.IsDirectlyCollapsed || GetIsCollapedFromNode(node);
		}
		
		/// <summary>
		/// Collapses the specified text section.
		/// Runtime: O(log n)
		/// </summary>
		public CollapsedLineSection CollapseText(DocumentLine start, DocumentLine end)
		{
			if (!document.Lines.Contains(start))
				throw new ArgumentException("Line is not part of this document", "start");
			if (!document.Lines.Contains(end))
				throw new ArgumentException("Line is not part of this document", "end");
			int length = end.LineNumber - start.LineNumber + 1;
			if (length < 0)
				throw new ArgumentException("start must be a line before end");
			CollapsedLineSection section = new CollapsedLineSection(this, start, end);
			AddCollapsedSection(section, length);
			#if DEBUG
			CheckProperties();
			#endif
			return section;
		}
		#endregion
		
		#region LineCount & TotalHeight
		public int LineCount {
			get {
				return root.totalCount;
			}
		}
		
		public double TotalHeight {
			get {
				return root.totalHeight;
			}
		}
		#endregion
		
		#region GetAllCollapsedSections
		IEnumerable<HeightTreeNode> AllNodes {
			get {
				if (root != null) {
					HeightTreeNode node = root.LeftMost;
					while (node != null) {
						yield return node;
						node = node.Successor;
					}
				}
			}
		}
		
		internal IEnumerable<CollapsedLineSection> GetAllCollapsedSections()
		{
			List<CollapsedLineSection> emptyCSList = new List<CollapsedLineSection>();
			return System.Linq.Enumerable.Distinct(
				System.Linq.Enumerable.SelectMany(
					AllNodes, node => System.Linq.Enumerable.Concat(node.lineNode.collapsedSections ?? emptyCSList,
					                                                node.collapsedSections ?? emptyCSList)
				));
		}
		#endregion
		
		#region CheckProperties
		#if DEBUG
		[Conditional("DATACONSISTENCYTEST")]
		internal void CheckProperties()
		{
			CheckProperties(root);
			
			foreach (CollapsedLineSection cs in GetAllCollapsedSections()) {
				Debug.Assert(GetNode(cs.Start).lineNode.collapsedSections.Contains(cs));
				Debug.Assert(GetNode(cs.End).lineNode.collapsedSections.Contains(cs));
				int endLine = cs.End.LineNumber;
				for (int i = cs.Start.LineNumber; i <= endLine; i++) {
					CheckIsInSection(cs, GetLineByNumber(i));
				}
			}
			
			// check red-black property:
			int blackCount = -1;
			CheckNodeProperties(root, null, RED, 0, ref blackCount);
		}
		
		void CheckIsInSection(CollapsedLineSection cs, DocumentLine line)
		{
			HeightTreeNode node = GetNode(line);
			if (node.lineNode.collapsedSections != null && node.lineNode.collapsedSections.Contains(cs))
				return;
			while (node != null) {
				if (node.collapsedSections != null && node.collapsedSections.Contains(cs))
					return;
				node = node.parent;
			}
			throw new InvalidOperationException(cs + " not found for line " + line);
		}
		
		void CheckProperties(HeightTreeNode node)
		{
			int totalCount = 1;
			double totalHeight = node.lineNode.TotalHeight;
			if (node.lineNode.IsDirectlyCollapsed)
				Debug.Assert(node.lineNode.collapsedSections.Count > 0);
			if (node.left != null) {
				CheckProperties(node.left);
				totalCount += node.left.totalCount;
				totalHeight += node.left.totalHeight;
				
				CheckAllContainedIn(node.left.collapsedSections, node.lineNode.collapsedSections);
			}
			if (node.right != null) {
				CheckProperties(node.right);
				totalCount += node.right.totalCount;
				totalHeight += node.right.totalHeight;
				
				CheckAllContainedIn(node.right.collapsedSections, node.lineNode.collapsedSections);
			}
			if (node.left != null && node.right != null) {
				if (node.left.collapsedSections != null && node.right.collapsedSections != null) {
					var intersection = System.Linq.Enumerable.Intersect(node.left.collapsedSections, node.right.collapsedSections);
					Debug.Assert(System.Linq.Enumerable.Count(intersection) == 0);
				}
			}
			if (node.IsDirectlyCollapsed) {
				Debug.Assert(node.collapsedSections.Count > 0);
				totalHeight = 0;
			}
			Debug.Assert(node.totalCount == totalCount);
			Debug.Assert(node.totalHeight.IsClose(totalHeight));
		}
		
		/// <summary>
		/// Checks that all elements in list1 are contained in list2.
		/// </summary>
		static void CheckAllContainedIn(IEnumerable<CollapsedLineSection> list1, ICollection<CollapsedLineSection> list2)
		{
			if (list1 == null) list1 = new List<CollapsedLineSection>();
			if (list2 == null) list2 = new List<CollapsedLineSection>();
			foreach (CollapsedLineSection cs in list1) {
				Debug.Assert(list2.Contains(cs));
			}
		}
		
		/*
		1. A node is either red or black.
		2. The root is black.
		3. All leaves are black. (The leaves are the NIL children.)
		4. Both children of every red node are black. (So every red node must have a black parent.)
		5. Every simple path from a node to a descendant leaf contains the same number of black nodes. (Not counting the leaf node.)
		 */
		void CheckNodeProperties(HeightTreeNode node, HeightTreeNode parentNode, bool parentColor, int blackCount, ref int expectedBlackCount)
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
		
		static void AppendTreeToString(HeightTreeNode node, StringBuilder b, int indent)
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
		
		#region Red/Black Tree
		const bool RED = true;
		const bool BLACK = false;
		
		void InsertAsLeft(HeightTreeNode parentNode, HeightTreeNode newNode)
		{
			Debug.Assert(parentNode.left == null);
			parentNode.left = newNode;
			newNode.parent = parentNode;
			newNode.color = RED;
			UpdateAfterChildrenChange(parentNode);
			FixTreeOnInsert(newNode);
		}
		
		void InsertAsRight(HeightTreeNode parentNode, HeightTreeNode newNode)
		{
			Debug.Assert(parentNode.right == null);
			parentNode.right = newNode;
			newNode.parent = parentNode;
			newNode.color = RED;
			UpdateAfterChildrenChange(parentNode);
			FixTreeOnInsert(newNode);
		}
		
		void FixTreeOnInsert(HeightTreeNode node)
		{
			Debug.Assert(node != null);
			Debug.Assert(node.color == RED);
			Debug.Assert(node.left == null || node.left.color == BLACK);
			Debug.Assert(node.right == null || node.right.color == BLACK);
			
			HeightTreeNode parentNode = node.parent;
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
			HeightTreeNode grandparentNode = parentNode.parent;
			HeightTreeNode uncleNode = Sibling(parentNode);
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
		
		void RemoveNode(HeightTreeNode removedNode)
		{
			if (removedNode.left != null && removedNode.right != null) {
				// replace removedNode with it's in-order successor
				
				HeightTreeNode leftMost = removedNode.right.LeftMost;
				HeightTreeNode parentOfLeftMost = leftMost.parent;
				RemoveNode(leftMost); // remove leftMost from its current location
				
				BeforeNodeReplace(removedNode, leftMost, parentOfLeftMost);
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
			HeightTreeNode parentNode = removedNode.parent;
			HeightTreeNode childNode = removedNode.left ?? removedNode.right;
			BeforeNodeRemove(removedNode);
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
		
		void FixTreeOnDelete(HeightTreeNode node, HeightTreeNode parentNode)
		{
			Debug.Assert(node == null || node.parent == parentNode);
			if (parentNode == null)
				return;
			
			// warning: node may be null
			HeightTreeNode sibling = Sibling(node, parentNode);
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
		
		void ReplaceNode(HeightTreeNode replacedNode, HeightTreeNode newNode)
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
		
		void RotateLeft(HeightTreeNode p)
		{
			// let q be p's right child
			HeightTreeNode q = p.right;
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
		
		void RotateRight(HeightTreeNode p)
		{
			// let q be p's left child
			HeightTreeNode q = p.left;
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
		
		static HeightTreeNode Sibling(HeightTreeNode node)
		{
			if (node == node.parent.left)
				return node.parent.right;
			else
				return node.parent.left;
		}
		
		static HeightTreeNode Sibling(HeightTreeNode node, HeightTreeNode parentNode)
		{
			Debug.Assert(node == null || node.parent == parentNode);
			if (node == parentNode.left)
				return parentNode.right;
			else
				return parentNode.left;
		}
		
		static bool GetColor(HeightTreeNode node)
		{
			return node != null ? node.color : BLACK;
		}
		#endregion
		
		#region Collapsing support
		static bool GetIsCollapedFromNode(HeightTreeNode node)
		{
			while (node != null) {
				if (node.IsDirectlyCollapsed)
					return true;
				node = node.parent;
			}
			return false;
		}
		
		internal void AddCollapsedSection(CollapsedLineSection section, int sectionLength)
		{
			AddRemoveCollapsedSection(section, sectionLength, true);
		}
		
		void AddRemoveCollapsedSection(CollapsedLineSection section, int sectionLength, bool add)
		{
			Debug.Assert(sectionLength > 0);
			
			HeightTreeNode node = GetNode(section.Start);
			// Go up in the tree.
			while (true) {
				// Mark all middle nodes as collapsed
				if (add)
					node.lineNode.AddDirectlyCollapsed(section);
				else
					node.lineNode.RemoveDirectlyCollapsed(section);
				sectionLength -= 1;
				if (sectionLength == 0) {
					// we are done!
					Debug.Assert(node.documentLine == section.End);
					break;
				}
				// Mark all right subtrees as collapsed.
				if (node.right != null) {
					if (node.right.totalCount < sectionLength) {
						if (add)
							node.right.AddDirectlyCollapsed(section);
						else
							node.right.RemoveDirectlyCollapsed(section);
						sectionLength -= node.right.totalCount;
					} else {
						// mark partially into the right subtree: go down the right subtree.
						AddRemoveCollapsedSectionDown(section, node.right, sectionLength, add);
						break;
					}
				}
				// go up to the next node
				HeightTreeNode parentNode = node.parent;
				Debug.Assert(parentNode != null);
				while (parentNode.right == node) {
					node = parentNode;
					parentNode = node.parent;
					Debug.Assert(parentNode != null);
				}
				node = parentNode;
			}
			UpdateAugmentedData(GetNode(section.Start), UpdateAfterChildrenChangeRecursionMode.WholeBranch);
			UpdateAugmentedData(GetNode(section.End), UpdateAfterChildrenChangeRecursionMode.WholeBranch);
		}
		
		static void AddRemoveCollapsedSectionDown(CollapsedLineSection section, HeightTreeNode node, int sectionLength, bool add)
		{
			while (true) {
				if (node.left != null) {
					if (node.left.totalCount < sectionLength) {
						// mark left subtree
						if (add)
							node.left.AddDirectlyCollapsed(section);
						else
							node.left.RemoveDirectlyCollapsed(section);
						sectionLength -= node.left.totalCount;
					} else {
						// mark only inside the left subtree
						node = node.left;
						Debug.Assert(node != null);
						continue;
					}
				}
				if (add)
					node.lineNode.AddDirectlyCollapsed(section);
				else
					node.lineNode.RemoveDirectlyCollapsed(section);
				sectionLength -= 1;
				if (sectionLength == 0) {
					// done!
					Debug.Assert(node.documentLine == section.End);
					break;
				}
				// mark inside right subtree:
				node = node.right;
				Debug.Assert(node != null);
			}
		}
		
		public void Uncollapse(CollapsedLineSection section)
		{
			int sectionLength = section.End.LineNumber - section.Start.LineNumber + 1;
			AddRemoveCollapsedSection(section, sectionLength, false);
			// do not call CheckProperties() in here - Uncollapse is also called during line removals
		}
		#endregion
	}
}
