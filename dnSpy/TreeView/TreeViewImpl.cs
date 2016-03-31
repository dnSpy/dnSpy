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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Themes;
using dnSpy.Contracts.TreeView;
using dnSpy.Controls;
using ICSharpCode.TreeView;

namespace dnSpy.TreeView {
	sealed class TreeViewImpl : ITreeView, IStackedContentChild {
		public ITreeNode Root {
			get { return root; }
		}
		readonly TreeNodeImpl root;

		public Guid Guid {
			get { return guid; }
		}
		readonly Guid guid;

		public object UIObject {
			get { return sharpTreeView; }
		}
		readonly SharpTreeView sharpTreeView;

		object IStackedContentChild.UIObject {
			get { return sharpTreeView; }
		}

		public ITreeNodeData SelectedItem {
			get {
				var node = sharpTreeView.SelectedItem as DnSpySharpTreeNode;
				return node == null ? null : node.TreeNodeImpl.Data;
			}
		}

		public ITreeNodeData[] SelectedItems {
			get { return Convert(sharpTreeView.SelectedItems); }
		}

		public ITreeNodeData[] TopLevelSelection {
			get { return Convert(sharpTreeView.GetTopLevelSelection()); }
		}

		readonly ITreeViewManager treeViewManager;
		readonly IImageManager imageManager;
		readonly ITreeViewListener treeViewListener;

		public event EventHandler<TVSelectionChangedEventArgs> SelectionChanged;
		public event EventHandler<TVNodeRemovedEventArgs> NodeRemoved;

		public TreeViewImpl(ITreeViewManager treeViewManager, IThemeManager themeManager, IImageManager imageManager, Guid guid, TreeViewOptions options) {
			this.guid = guid;
			this.treeViewManager = treeViewManager;
			this.imageManager = imageManager;
			this.treeViewListener = options.TreeViewListener;
			this.sharpTreeView = new SharpTreeView();
			this.sharpTreeView.SelectionChanged += SharpTreeView_SelectionChanged;
			this.sharpTreeView.CanDragAndDrop = options.CanDragAndDrop;
			this.sharpTreeView.AllowDrop = options.AllowDrop;
			this.sharpTreeView.AllowDropOrder = options.AllowDrop;
			VirtualizingStackPanel.SetIsVirtualizing(this.sharpTreeView, options.IsVirtualizing);
			VirtualizingStackPanel.SetVirtualizationMode(this.sharpTreeView, options.VirtualizationMode);
			this.sharpTreeView.SelectionMode = options.SelectionMode;
			this.sharpTreeView.BorderThickness = new Thickness(0);
			this.sharpTreeView.ShowRoot = false;
			this.sharpTreeView.ShowLines = false;

			if (options.IsGridView) {
				this.sharpTreeView.ItemContainerStyle = (Style)Application.Current.FindResource(SharpGridView.ItemContainerStyleKey);
				this.sharpTreeView.Style = (Style)Application.Current.FindResource("SharpTreeViewGridViewStyle");
			}
			else {
				// Clear the value set by the constructor. This is required or our style won't be used.
				this.sharpTreeView.ClearValue(ItemsControl.ItemContainerStyleProperty);
				this.sharpTreeView.Style = (Style)Application.Current.FindResource(typeof(SharpTreeView));
			}

			this.sharpTreeView.GetPreviewInsideTextBackground = () => themeManager.Theme.GetColor(ColorType.SystemColorsHighlight).Background;
			this.sharpTreeView.GetPreviewInsideForeground = () => themeManager.Theme.GetColor(ColorType.SystemColorsHighlightText).Foreground;

			// Add the root at the end since Create() requires some stuff to have been initialized
			this.root = Create(options.RootNode ?? new TreeNodeDataImpl(new Guid(FileTVConstants.ROOT_NODE_GUID)));
			this.sharpTreeView.Root = this.root.Node;
		}

		void SharpTreeView_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (SelectionChanged != null)
				SelectionChanged(this, Convert(e));
		}

		static TVSelectionChangedEventArgs Convert(SelectionChangedEventArgs e) {
			ITreeNodeData[] added = null, removed = null;
			if (e.AddedItems != null)
				added = Convert(e.AddedItems);
			if (e.RemovedItems != null)
				removed = Convert(e.RemovedItems);
			return new TVSelectionChangedEventArgs(added, removed);
		}

		static ITreeNodeData[] Convert(System.Collections.IEnumerable list) {
			return list.Cast<DnSpySharpTreeNode>().Select(a => a.TreeNodeImpl.Data).ToArray();
		}

		internal object GetIcon(ImageReference imgRef) {
			return imageManager.GetImage(imgRef.Assembly, imgRef.Name, BackgroundType.TreeNode);
		}

		ITreeNode ITreeView.Create(ITreeNodeData data) {
			return Create(data);
		}

		TreeNodeImpl Create(ITreeNodeData data) {
			Debug.Assert(data.TreeNode == null);
			var impl = new TreeNodeImpl(this, data);
			if (treeViewListener != null)
				treeViewListener.OnEvent(this, new TreeViewListenerEventArgs(TreeViewListenerEvent.NodeCreated, impl));
			data.Initialize();
			if (!impl.LazyLoading)
				AddChildren(impl);
			return impl;
		}

		internal void AddChildren(TreeNodeImpl impl) {
			foreach (var data in impl.Data.CreateChildren())
				AddSorted(impl, Create(data));
			foreach (var creator in treeViewManager.GetCreators(impl.Data.Guid)) {
				var context = new TreeNodeDataCreatorContext(impl);
				foreach (var data in creator.Create(context))
					AddSorted(impl, Create(data));
			}
		}

		internal void AddSorted(TreeNodeImpl owner, ITreeNode node) {
			if (node == null)
				throw new ArgumentNullException();
			if (node.TreeView != this)
				throw new InvalidOperationException("You can only add a ITreeNode to a treeview that created it");
			AddSorted(owner, (TreeNodeImpl)node);
		}

		internal void AddSorted(TreeNodeImpl owner, TreeNodeImpl impl) {
			var group = impl.Data.TreeNodeGroup;
			if (group == null)
				owner.Children.Add(impl);
			else {
				int index = GetInsertIndex(owner, impl, group);
				owner.Children.Insert(index, impl);
			}
		}

		int GetInsertIndex(TreeNodeImpl owner, TreeNodeImpl impl, ITreeNodeGroup group) {
			var children = owner.Children;

			// At creation time, it's most likely inserted last, so check that position first.
			if (children.Count >= 1) {
				var lastData = children[children.Count - 1].Data;
				var lastGroup = lastData.TreeNodeGroup;
				if (lastGroup != null) {
					int x = Compare(impl.Data, lastData, group, lastGroup);
					if (x > 0)
						return children.Count;
				}
			}

			int lo = 0, hi = children.Count - 1;
			while (lo <= hi && hi != -1) {
				int i = (lo + hi) / 2;

				var otherData = children[i].Data;
				var otherGroup = otherData.TreeNodeGroup;
				int x;
				if (otherGroup == null)
					x = -1;
				else
					x = Compare(impl.Data, otherData, group, otherGroup);

				if (x == 0)
					return i;
				if (x < 0)
					hi = i - 1;
				else
					lo = i + 1;
			}
			return hi + 1;
		}

		int Compare(ITreeNodeData a, ITreeNodeData b, ITreeNodeGroup ga, ITreeNodeGroup gb) {
			if (ga.Order < gb.Order)
				return -1;
			if (ga.Order > gb.Order)
				return 1;
			if (ga.GetType() != gb.GetType()) {
				Debug.Fail(string.Format("Two different groups have identical order: {0} vs {1}", ga.GetType(), gb.GetType()));
				return ga.GetType().GetHashCode().CompareTo(gb.GetType().GetHashCode());
			}
			return ga.Compare(a, b);
		}

		public void SelectItems(IEnumerable<ITreeNodeData> items) {
			sharpTreeView.SelectedItems.Clear();
			var nodes = items.Where(a => a != null).Select(a => (TreeNodeImpl)a.TreeNode).ToArray();
			if (nodes.Length > 0) {
				sharpTreeView.FocusNode(nodes[0].Node);
				sharpTreeView.SelectedItem = nodes[0].Node;

				// FocusNode() should already call ScrollIntoView() but for some reason,
				// ScrollIntoView() does nothing so add another call.
				// Background priority won't work, we need ContextIdle prio
				sharpTreeView.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => {
					var item = sharpTreeView.SelectedItem as SharpTreeNode;
					if (item != null)
						sharpTreeView.ScrollIntoView(item);
				}));
			}
			foreach (var node in nodes)
				sharpTreeView.SelectedItems.Add(node.Node);
		}

		public void Focus() {
			Focus2();
			// This is needed if the treeview was hidden and just got visible. It's disabled because
			// it also prevents the text editor from getting focus when dnSpy starts.
			// sharpTreeView.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(Focus2));
		}

		void Focus2() {
			var node = sharpTreeView.SelectedItem as SharpTreeNode;
			if (node != null)
				sharpTreeView.FocusNode(node);
			else
				sharpTreeView.Focus();
		}

		public void RefreshAllNodes() {
			foreach (var node in this.Root.Descendants())
				node.RefreshUI();
		}

		public ITreeNodeData FromImplNode(object selectedItem) {
			var node = selectedItem as DnSpySharpTreeNode;
			return node == null ? null : node.TreeNodeImpl.Data;
		}

		public object ToImplNode(ITreeNodeData node) {
			if (node == null)
				return null;
			var impl = node.TreeNode as TreeNodeImpl;
			Debug.Assert(impl != null);
			return impl == null ? null : impl.Node;
		}

		public void OnRemoved(ITreeNodeData node) {
			if (NodeRemoved != null)
				NodeRemoved(this, new TVNodeRemovedEventArgs(node, true));
		}

		public void CollapseUnusedNodes() {
			var usedNodes = new HashSet<ITreeNodeData>(TopLevelSelection);
			CollapseUnusedNodes(Root.DataChildren, usedNodes);
		}

		bool CollapseUnusedNodes(IEnumerable<ITreeNodeData> nodes, HashSet<ITreeNodeData> usedNodes) {
			bool isExpanded = false;
			foreach (var node in nodes) {
				var tn = node.TreeNode;
				if (usedNodes.Contains(node))
					isExpanded = true;
				if (!tn.IsExpanded)
					continue;
				if (CollapseUnusedNodes(tn.DataChildren, usedNodes)) {
					isExpanded = true;
					tn.IsExpanded = true;
				}
				else
					tn.IsExpanded = false;
			}
			return isExpanded;
		}
	}
}
