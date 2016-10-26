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
using System.Windows.Media;
using System.Windows.Threading;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Themes;
using dnSpy.Contracts.TreeView;
using dnSpy.Controls;
using ICSharpCode.TreeView;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.TreeView {
	sealed class TreeViewImpl : ITreeView, IStackedContentChild {
		public ITreeNode Root => root;
		readonly TreeNodeImpl root;

		public Guid Guid { get; }

		public Control UIObject => sharpTreeView;
		readonly SharpTreeView sharpTreeView;

		object IStackedContentChild.UIObject => sharpTreeView;

		public TreeNodeData SelectedItem {
			get {
				var node = sharpTreeView.SelectedItem as DsSharpTreeNode;
				return node == null ? null : node.TreeNodeImpl.Data;
			}
		}

		public TreeNodeData[] SelectedItems => Convert(sharpTreeView.SelectedItems);
		public TreeNodeData[] TopLevelSelection => Convert(sharpTreeView.GetTopLevelSelection());

		readonly ITreeViewServiceImpl treeViewService;
		readonly ITreeViewListener treeViewListener;
		readonly IClassificationFormatMap classificationFormatMap;
		readonly object foregroundBrushResourceKey;

		public event EventHandler<TreeViewSelectionChangedEventArgs> SelectionChanged;
		public event EventHandler<TreeViewNodeRemovedEventArgs> NodeRemoved;

		public TreeViewImpl(ITreeViewServiceImpl treeViewService, IThemeService themeService, IClassificationFormatMapService classificationFormatMapService, Guid guid, TreeViewOptions options) {
			this.Guid = guid;
			this.treeViewService = treeViewService;
			this.treeViewListener = options.TreeViewListener;
			this.classificationFormatMap = classificationFormatMapService.GetClassificationFormatMap(TreeViewTextEditorFormatDefinition.TreeViewAppearanceCategory);
			this.classificationFormatMap.ClassificationFormatMappingChanged += ClassificationFormatMap_ClassificationFormatMappingChanged;
			this.foregroundBrushResourceKey = options.ForegroundBrushResourceKey ?? "TreeViewForeground";
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
				this.sharpTreeView.SetResourceReference(ItemsControl.ItemContainerStyleProperty, SharpGridView.ItemContainerStyleKey);
				this.sharpTreeView.SetResourceReference(FrameworkElement.StyleProperty, "SharpTreeViewGridViewStyle");
			}
			else {
				// Clear the value set by the constructor. This is required or our style won't be used.
				this.sharpTreeView.ClearValue(ItemsControl.ItemContainerStyleProperty);
				this.sharpTreeView.SetResourceReference(FrameworkElement.StyleProperty, typeof(SharpTreeView));
			}

			this.sharpTreeView.GetPreviewInsideTextBackground = () => themeService.Theme.GetColor(ColorType.SystemColorsHighlight).Background;
			this.sharpTreeView.GetPreviewInsideForeground = () => themeService.Theme.GetColor(ColorType.SystemColorsHighlightText).Foreground;

			// Add the root at the end since Create() requires some stuff to have been initialized
			this.root = Create(options.RootNode ?? new TreeNodeDataImpl(new Guid(DocumentTreeViewConstants.ROOT_NODE_GUID)));
			this.sharpTreeView.Root = this.root.Node;
		}

		void ClassificationFormatMap_ClassificationFormatMappingChanged(object sender, EventArgs e) => RefreshAllNodes();

		void IDisposable.Dispose() {
			classificationFormatMap.ClassificationFormatMappingChanged -= ClassificationFormatMap_ClassificationFormatMappingChanged;
			sharpTreeView.SelectionChanged -= SharpTreeView_SelectionChanged;
		}

		void SharpTreeView_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
			SelectionChanged?.Invoke(this, Convert(e));

		static TreeViewSelectionChangedEventArgs Convert(SelectionChangedEventArgs e) {
			TreeNodeData[] added = null, removed = null;
			if (e.AddedItems != null)
				added = Convert(e.AddedItems);
			if (e.RemovedItems != null)
				removed = Convert(e.RemovedItems);
			return new TreeViewSelectionChangedEventArgs(added, removed);
		}

		static TreeNodeData[] Convert(System.Collections.IEnumerable list) =>
			list.Cast<DsSharpTreeNode>().Select(a => a.TreeNodeImpl.Data).ToArray();

		ITreeNode ITreeView.Create(TreeNodeData data) => Create(data);

		TreeNodeImpl Create(TreeNodeData data) {
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
			foreach (var provider in treeViewService.GetProviders(impl.Data.Guid)) {
				var context = new TreeNodeDataProviderContext(impl);
				foreach (var data in provider.Create(context))
					AddSorted(impl, Create(data));
			}
		}

		internal void AddSorted(TreeNodeImpl owner, ITreeNode node) {
			if (node == null)
				throw new ArgumentNullException(nameof(node));
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
			while (lo <= hi) {
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

		int Compare(TreeNodeData a, TreeNodeData b, ITreeNodeGroup ga, ITreeNodeGroup gb) {
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

		public void SelectItems(IEnumerable<TreeNodeData> items) {
			if (sharpTreeView.SelectionMode == SelectionMode.Single)
				sharpTreeView.SelectedItem = null;
			else
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
			foreach (var node in nodes) {
				if (sharpTreeView.SelectionMode == SelectionMode.Single) {
					sharpTreeView.SelectedItem = node.Node;
					break;
				}
				else
					sharpTreeView.SelectedItems.Add(node.Node);
			}
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

		public TreeNodeData FromImplNode(object selectedItem) {
			var node = selectedItem as DsSharpTreeNode;
			return node == null ? null : node.TreeNodeImpl.Data;
		}

		public object ToImplNode(TreeNodeData node) {
			if (node == null)
				return null;
			var impl = node.TreeNode as TreeNodeImpl;
			Debug.Assert(impl != null);
			return impl?.Node;
		}

		public void OnRemoved(TreeNodeData node) => NodeRemoved?.Invoke(this, new TreeViewNodeRemovedEventArgs(node, true));

		public void CollapseUnusedNodes() {
			var usedNodes = new HashSet<TreeNodeData>(TopLevelSelection);
			CollapseUnusedNodes(Root.DataChildren, usedNodes);
			// Make sure the selected node is visible
			Focus();
		}

		bool CollapseUnusedNodes(IEnumerable<TreeNodeData> nodes, HashSet<TreeNodeData> usedNodes) {
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

		internal Brush GetNodeForegroundBrush() {
			var brush = sharpTreeView.TryFindResource(foregroundBrushResourceKey) as Brush;
			Debug.Assert(brush != null);
			return brush;
		}
	}
}
