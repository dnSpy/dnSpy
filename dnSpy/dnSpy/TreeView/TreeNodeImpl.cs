/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
		public TreeViewImpl TreeView { get; }
		public DsSharpTreeNode Node => nodeList.Node;
		public ITreeNodeData Data { get; }
		public IEnumerable<ITreeNodeData> DataChildren => Children.Select(a => a.Data);

		public IList<ITreeNode> Children => nodeList;
		readonly SharpTreeNodeChildrenList nodeList;

		public ITreeNode Parent {
			get {
				var parent = (DsSharpTreeNode)nodeList.Node.Parent;
				return parent == null ? null : parent.TreeNodeImpl;
			}
		}

		public bool LazyLoading {
			get { return Node.LazyLoading; }
			set { Node.LazyLoading = value; }
		}

		public bool IsExpanded {
			get { return Node.IsExpanded; }
			set { Node.IsExpanded = value; }
		}

		public bool IsHidden {
			get { return Node.IsHidden; }
			set { Node.IsHidden = value; }
		}

		public bool IsVisible => Node.IsVisible;
		ITreeView ITreeNode.TreeView => TreeView;

		public TreeNodeImpl(TreeViewImpl treeViewImpl, ITreeNodeData data) {
			Debug.Assert(data.TreeNode == null);
			this.TreeView = treeViewImpl;
			this.nodeList = new SharpTreeNodeChildrenList(this);
			this.Data = data;
			this.Data.TreeNode = this;
		}

		public void AddChild(ITreeNode node) => TreeView.AddSorted(this, node);
		public IEnumerable<ITreeNode> Descendants() => Node.Descendants().Select(a => ((DsSharpTreeNode)a).TreeNodeImpl);
		public IEnumerable<ITreeNode> DescendantsAndSelf() => Node.DescendantsAndSelf().Select(a => ((DsSharpTreeNode)a).TreeNodeImpl);

		public void RefreshUI() {
			Data.OnRefreshUI();
			Node.RefreshUI();
		}

		public void EnsureChildrenLoaded() {
			Node.EnsureLazyChildren();
			Data.OnEnsureChildrenLoaded();
		}
	}
}
