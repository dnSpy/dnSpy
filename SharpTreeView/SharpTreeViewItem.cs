// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;
using System.Diagnostics;

namespace ICSharpCode.TreeView
{
	public class SharpTreeViewItem : ListViewItem
	{
		static SharpTreeViewItem()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(SharpTreeViewItem),
			                                         new FrameworkPropertyMetadata(typeof(SharpTreeViewItem)));
		}

		public SharpTreeNode Node
		{
			get { return DataContext as SharpTreeNode; }
		}

		void UpdateAdaptor(SharpTreeNode node)
		{
			if (nodeView == null)
				return;
			if (node == null)
				return;

			var doAdaptor = nodeView.DataContext as SharpTreeNodeProxy;
			if (doAdaptor == null)
				nodeView.DataContext = (doAdaptor = new SharpTreeNodeProxy(node));
			else
				doAdaptor.UpdateObject(node);

			nodeView.UpdateTemplate();
		}

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);
			if (e.Property == DataContextProperty)
			{
				UpdateAdaptor(e.NewValue as SharpTreeNode);
			}
		}


		SharpTreeNodeView nodeView;
		public SharpTreeNodeView NodeView
		{
			get { return nodeView; }
			internal set
			{
				if (nodeView != value)
				{
					nodeView = value;
					UpdateAdaptor(Node);
				}
			}
		}
		public SharpTreeView ParentTreeView { get; internal set; }

		protected override void OnKeyDown(KeyEventArgs e)
		{
			switch (e.Key) {
				case Key.F2:
//					if (SharpTreeNode.ActiveNodes.Count == 1 && Node.IsEditable) {
//						Node.IsEditing = true;
//						e.Handled = true;
//					}
					break;
				case Key.Escape:
					if (Node != null)
						Node.IsEditing = false;
					break;
			}
		}

		#region Mouse

		Point startPoint;
		bool wasSelected;
		bool wasDoubleClick;

		protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
		{
			if (!ParentTreeView.CanDragAndDrop) {
				OnDoubleClick(e);
				e.Handled = true;
				return;
			}

			base.OnMouseDoubleClick(e);
		}

		protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			wasSelected = IsSelected;
			if (!IsSelected) {
				base.OnMouseLeftButtonDown(e);
			}

			if (!ParentTreeView.CanDragAndDrop)
				wasDoubleClick = false;
			else if (Mouse.LeftButton == MouseButtonState.Pressed) {
				startPoint = e.GetPosition(null);
				CaptureMouse();

				if (e.ClickCount == 2) {
					wasDoubleClick = true;
				}
			}
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (IsMouseCaptured) {
				var currentPoint = e.GetPosition(null);
				if (Math.Abs(currentPoint.X - startPoint.X) >= SystemParameters.MinimumHorizontalDragDistance ||
				    Math.Abs(currentPoint.Y - startPoint.Y) >= SystemParameters.MinimumVerticalDragDistance) {

					var selection = ParentTreeView.GetTopLevelSelection().ToArray();
					if (Node != null && Node.CanDrag(selection)) {
						Node.StartDrag(this, selection);
					}
				}
			}
		}

		protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
		{
			if (Node == null) {
				// Ignore it: Node is sometimes null
			}
			else if (wasDoubleClick) {
				wasDoubleClick = false;
				OnDoubleClick(e);
			}
			else if (!Node.IsExpanded && Node.SingleClickExpandsChildren) {
				if (!Node.IsRoot || ParentTreeView.ShowRootExpander) {
					Node.IsExpanded = !Node.IsExpanded;
				}
			}

			ReleaseMouseCapture();
			if (wasSelected) {
				base.OnMouseLeftButtonDown(e);
			}
		}

		void OnDoubleClick(RoutedEventArgs e)
		{
			if (Node == null)
				return;
			Node.ActivateItem(e);
			if (!e.Handled) {
				if (!Node.IsRoot || ParentTreeView.ShowRootExpander) {
					Node.IsExpanded = !Node.IsExpanded;
				}
			}
		}

		#endregion

		#region Drag and Drop

		protected override void OnDragEnter(DragEventArgs e)
		{
			ParentTreeView.HandleDragEnter(this, e);
		}

		protected override void OnDragOver(DragEventArgs e)
		{
			ParentTreeView.HandleDragOver(this, e);
		}

		protected override void OnDrop(DragEventArgs e)
		{
			ParentTreeView.HandleDrop(this, e);
		}

		protected override void OnDragLeave(DragEventArgs e)
		{
			ParentTreeView.HandleDragLeave(this, e);
		}

		#endregion
	}
}
