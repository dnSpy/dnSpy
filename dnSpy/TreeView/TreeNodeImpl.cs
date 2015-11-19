/*
    Copyright (C) 2014-2015 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.TreeView;

namespace dnSpy.TreeView {
	sealed class TreeNodeImpl : ITreeNode {
		public TreeViewImpl TreeView {
			get { return treeViewImpl; }
		}
		readonly TreeViewImpl treeViewImpl;

		public DnSpySharpTreeNode Node {
			get { return nodeList.Node; }
		}

		public IList<ITreeNode> Children {
			get { return nodeList; }
		}
		readonly SharpTreeNodeChildrenList nodeList;

		public ITreeNodeData Data {
			get { return data; }
		}
		readonly ITreeNodeData data;

		public ITreeNode Parent {
			get {
				var parent = (DnSpySharpTreeNode)nodeList.Node.Parent;
				return parent == null ? null : parent.TreeNodeImpl;
			}
		}

		public bool LazyLoading {
			get { return Node.LazyLoading; }
			set { Node.LazyLoading = value; }
		}

		ITreeView ITreeNode.TreeView {
			get { return treeViewImpl; }
		}

		public TreeNodeImpl(TreeViewImpl treeViewImpl, ITreeNodeData data) {
			Debug.Assert(data.TreeNode == null);
			this.treeViewImpl = treeViewImpl;
			this.nodeList = new SharpTreeNodeChildrenList(this);
			this.data = data;
			this.data.TreeNode = this;
		}

		public void AddChild(ITreeNode node) {
			treeViewImpl.AddSorted(this, node);
		}

		public IEnumerable<ITreeNode> Descendants() {
			return Node.Descendants().Select(a => ((DnSpySharpTreeNode)a).TreeNodeImpl);
		}

		public IEnumerable<ITreeNode> DescendantsAndSelf() {
			return Node.DescendantsAndSelf().Select(a => ((DnSpySharpTreeNode)a).TreeNodeImpl);
		}

		public void RefreshUI() {
			Node.RefreshUI();
		}
	}
}
