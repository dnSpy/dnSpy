// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;

namespace ICSharpCode.TreeView
{
	public class SharpTreeView : ListView
	{
		static SharpTreeView()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(SharpTreeView),
				new FrameworkPropertyMetadata(typeof(SharpTreeView)));

			SelectionModeProperty.OverrideMetadata(typeof(SharpTreeView),
				new FrameworkPropertyMetadata(SelectionMode.Extended));

			AlternationCountProperty.OverrideMetadata(typeof(SharpTreeView),
				new FrameworkPropertyMetadata(2));

			DefaultItemContainerStyleKey = 
				new ComponentResourceKey(typeof(SharpTreeView), "DefaultItemContainerStyleKey");

			VirtualizingStackPanel.VirtualizationModeProperty.OverrideMetadata(typeof(SharpTreeView),
				new FrameworkPropertyMetadata(VirtualizationMode.Recycling));
		}

		public static ResourceKey DefaultItemContainerStyleKey { get; private set; }

		public SharpTreeView()
		{
			SetResourceReference(ItemContainerStyleProperty, DefaultItemContainerStyleKey);
		}

		public static readonly DependencyProperty RootProperty =
			DependencyProperty.Register("Root", typeof(SharpTreeNode), typeof(SharpTreeView));

		public SharpTreeNode Root
		{
			get { return (SharpTreeNode)GetValue(RootProperty); }
			set { SetValue(RootProperty, value); }
		}

		public static readonly DependencyProperty ShowRootProperty =
			DependencyProperty.Register("ShowRoot", typeof(bool), typeof(SharpTreeView),
			new FrameworkPropertyMetadata(true));

		public bool ShowRoot
		{
			get { return (bool)GetValue(ShowRootProperty); }
			set { SetValue(ShowRootProperty, value); }
		}

		public static readonly DependencyProperty ShowRootExpanderProperty =
			DependencyProperty.Register("ShowRootExpander", typeof(bool), typeof(SharpTreeView),
			new FrameworkPropertyMetadata(false));

		public bool ShowRootExpander
		{
			get { return (bool)GetValue(ShowRootExpanderProperty); }
			set { SetValue(ShowRootExpanderProperty, value); }
		}

		public static readonly DependencyProperty AllowDropOrderProperty =
			DependencyProperty.Register("AllowDropOrder", typeof(bool), typeof(SharpTreeView));

		public bool AllowDropOrder
		{
			get { return (bool)GetValue(AllowDropOrderProperty); }
			set { SetValue(AllowDropOrderProperty, value); }
		}

		public static readonly DependencyProperty ShowLinesProperty =
			DependencyProperty.Register("ShowLines", typeof(bool), typeof(SharpTreeView),
			new FrameworkPropertyMetadata(true));

		public bool ShowLines
		{
			get { return (bool)GetValue(ShowLinesProperty); }
			set { SetValue(ShowLinesProperty, value); }
		}

		public static bool GetShowAlternation(DependencyObject obj)
		{
			return (bool)obj.GetValue(ShowAlternationProperty);
		}

		public static void SetShowAlternation(DependencyObject obj, bool value)
		{
			obj.SetValue(ShowAlternationProperty, value);
		}

		public static readonly DependencyProperty ShowAlternationProperty =
			DependencyProperty.RegisterAttached("ShowAlternation", typeof(bool), typeof(SharpTreeView),
			new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));
						
		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);
			if (e.Property == RootProperty || 
				e.Property == ShowRootProperty ||
				e.Property == ShowRootExpanderProperty) {
				Reload();
			}
		}

		TreeFlattener flattener;

		void Reload()
		{
			if (flattener != null) {
				flattener.Stop();
			}
			if (Root != null) {
				if (!(ShowRoot && ShowRootExpander)) {
					Root.IsExpanded = true;
				}
				flattener = new TreeFlattener(Root, ShowRoot);
				ItemsSource = flattener.List;
				flattener.Start();
			}
		}

		protected override DependencyObject GetContainerForItemOverride()
		{
			return new SharpTreeViewItem();
		}

		protected override bool IsItemItsOwnContainerOverride(object item)
		{
			return item is SharpTreeViewItem;
		}

		protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
		{
			base.PrepareContainerForItemOverride(element, item);
			SharpTreeViewItem container = element as SharpTreeViewItem;
			container.ParentTreeView = this;
		}

		internal void HandleCollapsing(SharpTreeNode Node)
		{
			var selectedChilds = Node.Descendants().Where(n => SharpTreeNode.SelectedNodes.Contains(n));
			if (selectedChilds.Any()) {
				var list = SelectedItems.Cast<SharpTreeNode>().Except(selectedChilds).ToList();
				list.AddOnce(Node);
				SetSelectedItems(list);
			}
		}

		#region Track selection
		
		protected override void OnSelectionChanged(SelectionChangedEventArgs e)
		{
			foreach (SharpTreeNode node in e.RemovedItems) {
				SharpTreeNode.SelectedNodes.Remove(node);
			}
			foreach (SharpTreeNode node in e.AddedItems) {
				SharpTreeNode.SelectedNodes.AddOnce(node);
			}

			if (IsKeyboardFocusWithin) {
				foreach (SharpTreeNode node in e.RemovedItems) {
					SharpTreeNode.ActiveNodes.Remove(node);
				}
				foreach (SharpTreeNode node in e.AddedItems) {
					SharpTreeNode.ActiveNodes.AddOnce(node);
				}
				SortActiveNodes();
			}
			base.OnSelectionChanged(e);
		}

		protected override void OnPreviewGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
		{
			foreach (SharpTreeNode node in SelectedItems) {
				SharpTreeNode.ActiveNodes.AddOnce(node);
			}
			SortActiveNodes();
		}

		protected override void OnPreviewLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
		{
			foreach (SharpTreeNode node in SelectedItems) {
				SharpTreeNode.ActiveNodes.Remove(node);
			}
		}

		void SortActiveNodes()
		{
			SharpTreeNode.ActiveNodes.Sort(delegate(SharpTreeNode n1, SharpTreeNode n2) {
				var index1 = Items.IndexOf(n1);
				var index2 = Items.IndexOf(n2);
				return index1.CompareTo(index2);
			});
		}
		
		#endregion
		
		#region Drag and Drop

		protected override void OnDragEnter(DragEventArgs e)
		{
			OnDragOver(e);
		}

		protected override void OnDragOver(DragEventArgs e)
		{
			e.Effects = DragDropEffects.None;
			e.Handled = true;

			if (Root != null && !ShowRoot && Root.Children.Count == 0) {
				Root.InternalCanDrop(e, 0);
			}
		}

		protected override void OnDrop(DragEventArgs e)
		{
			e.Effects = DragDropEffects.None;
			e.Handled = true;

			if (Root != null && !ShowRoot && Root.Children.Count == 0) {
				Root.InternalDrop(e, 0);
			}
		}

		internal void HandleDragEnter(SharpTreeViewItem item, DragEventArgs e)
		{
			HandleDragOver(item, e);
		}

		internal void HandleDragOver(SharpTreeViewItem item, DragEventArgs e)
		{
			HidePreview();
			e.Handled = true;

			var target = GetDropTarget(item, e);
			if (target != null) {
				ShowPreview(target.Item, target.Place);
			}
		}

		internal void HandleDrop(SharpTreeViewItem item, DragEventArgs e)
		{
			try {
				HidePreview();
				e.Handled = true;

				var target = GetDropTarget(item, e);
				if (target != null) {
					target.Node.InternalDrop(e, target.Index);
				}
			} catch (Exception ex) {
				Debug.WriteLine(ex.ToString());
				throw;
			}
		}

		internal void HandleDragLeave(SharpTreeViewItem item, DragEventArgs e)
		{
			HidePreview();
			e.Handled = true;
		}

		class DropTarget
		{
			public SharpTreeViewItem Item;
			public DropPlace Place;
			public double Y;			
			public SharpTreeNode Node;
			public int Index;
		}

		DropTarget GetDropTarget(SharpTreeViewItem item, DragEventArgs e)
		{
			var dropTargets = BuildDropTargets(item, e);
			var y = e.GetPosition(item).Y;
			foreach (var target in dropTargets) {
				if (target.Y >= y) {
					return target;
				}
			}
			return null;
		}

		List<DropTarget> BuildDropTargets(SharpTreeViewItem item, DragEventArgs e)
		{
			var result = new List<DropTarget>();
			var node = item.Node;

			if (AllowDropOrder) {
				TryAddDropTarget(result, item, DropPlace.Before, e);				
			}

			TryAddDropTarget(result, item, DropPlace.Inside, e);

			if (AllowDropOrder) {
				if (node.IsExpanded && node.Children.Count > 0) {
					var firstChildItem = ItemContainerGenerator.ContainerFromItem(node.Children[0]) as SharpTreeViewItem;
					TryAddDropTarget(result, firstChildItem, DropPlace.Before, e);
				}
				else {
					TryAddDropTarget(result, item, DropPlace.After, e);
				}
			}

			var h = item.ActualHeight;
			var y1 = 0.2 * h;
			var y2 = h / 2;
			var y3 = h - y1;

			if (result.Count == 2) {
				if (result[0].Place == DropPlace.Inside &&
					result[1].Place != DropPlace.Inside) {
					result[0].Y = y3;
				}
				else if (result[0].Place != DropPlace.Inside &&
					result[1].Place == DropPlace.Inside) {
					result[0].Y = y1;
				}
				else {
					result[0].Y = y2;
				}
			}
			else if (result.Count == 3) {
				result[0].Y = y1;
				result[1].Y = y3;
			}
			if (result.Count > 0) {
				result[result.Count - 1].Y = h;
			}
			return result;
		}

		void TryAddDropTarget(List<DropTarget> targets, SharpTreeViewItem item, DropPlace place, DragEventArgs e)
		{
			SharpTreeNode node;
			int index;

			GetNodeAndIndex(item, place, out node, out index);

			if (node != null) {
				e.Effects = DragDropEffects.None;
				if (node.InternalCanDrop(e, index)) {
					DropTarget target = new DropTarget() {
						Item = item,
						Place = place,
						Node = node,
						Index = index
					};
					targets.Add(target);
				}
			}			
		}

		void GetNodeAndIndex(SharpTreeViewItem item, DropPlace place, out SharpTreeNode node, out int index)
		{
			node = null;
			index = 0;

			if (place == DropPlace.Inside) {
				node = item.Node;
				index = node.Children.Count;
			}
			else if (place == DropPlace.Before) {
				if (item.Node.Parent != null) {
					node = item.Node.Parent;
					index = node.Children.IndexOf(item.Node);
				}
			}
			else {
				if (item.Node.Parent != null) {
					node = item.Node.Parent;
					index = node.Children.IndexOf(item.Node) + 1;
				}
			}
		}

		SharpTreeNodeView previewNodeView;
		InsertMarker insertMarker;
		DropPlace previewPlace;

		enum DropPlace
		{
			Before, Inside, After
		}

		void ShowPreview(SharpTreeViewItem item, DropPlace place)
		{
			previewNodeView = item.NodeView;
			previewPlace = place;			

			if (place == DropPlace.Inside) {
				previewNodeView.TextBackground = SystemColors.HighlightBrush;
				previewNodeView.Foreground = SystemColors.HighlightTextBrush;
			}
			else {
				if (insertMarker == null) {
					var adornerLayer = AdornerLayer.GetAdornerLayer(this);
					var adorner = new GeneralAdorner(this);
					insertMarker = new InsertMarker();
					adorner.Child = insertMarker;
					adornerLayer.Add(adorner);
				}

				insertMarker.Visibility = Visibility.Visible;

				var p1 = previewNodeView.TransformToVisual(this).Transform(new Point());
				var p = new Point(p1.X + previewNodeView.CalculateIndent() + 4.5, p1.Y - 3);

				if (place == DropPlace.After) {
					p.Y += previewNodeView.ActualHeight;
				}

				insertMarker.Margin = new Thickness(p.X, p.Y, 0, 0);
				
				SharpTreeNodeView secondNodeView = null;
				var index = flattener.List.IndexOf(item.Node);

				if (place == DropPlace.Before) {
					if (index > 0) {
						secondNodeView = (ItemContainerGenerator.ContainerFromIndex(index - 1) as SharpTreeViewItem).NodeView;
					}
				}
				else if (index + 1 < flattener.List.Count) {
					secondNodeView = (ItemContainerGenerator.ContainerFromIndex(index + 1) as SharpTreeViewItem).NodeView;
				}
				
				var w = p1.X + previewNodeView.ActualWidth - p.X;

				if (secondNodeView != null) {
					var p2 = secondNodeView.TransformToVisual(this).Transform(new Point());
					w = Math.Max(w, p2.X + secondNodeView.ActualWidth - p.X);
				}

				insertMarker.Width = w + 10;
			}
		}

		void HidePreview()
		{
			if (previewNodeView != null) {
				previewNodeView.ClearValue(SharpTreeNodeView.TextBackgroundProperty);
				previewNodeView.ClearValue(SharpTreeNodeView.ForegroundProperty);
				if (insertMarker != null) {
					insertMarker.Visibility = Visibility.Collapsed;
				}
				previewNodeView = null;
			}
		}

		#endregion
	}
}
