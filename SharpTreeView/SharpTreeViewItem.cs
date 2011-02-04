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

			RegisterCommands();
		}

		public SharpTreeNode Node
		{
			get { return DataContext as SharpTreeNode; }
		}

		public SharpTreeNodeView NodeView { get; internal set; }
		public SharpTreeView ParentTreeView { get; internal set; }		

		protected override void OnKeyDown(KeyEventArgs e)
		{
			switch (e.Key) {
				case Key.F2:
					if (SharpTreeNode.ActiveNodes.Count == 1 && Node.IsEditable) {
						Node.IsEditing = true;
					}
					break;
				case Key.Escape:
					Node.IsEditing = false;
					break;
				case Key.Left:
					Node.IsExpanded = false;
					break;
				case Key.Right:
					Node.IsExpanded = true;
					break;
			}
		}

		protected override void OnContextMenuOpening(ContextMenuEventArgs e)
		{
			ContextMenu = Node.GetContextMenu();
		}

		protected override void OnContextMenuClosing(ContextMenuEventArgs e)
		{
			ClearValue(ContextMenuProperty);
		}

		#region Mouse

		Point startPoint;
		bool wasSelected;

		protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			wasSelected = IsSelected;
			if (!IsSelected) {
				base.OnMouseLeftButtonDown(e);
			}

			if (Mouse.LeftButton == MouseButtonState.Pressed) {
				startPoint = e.GetPosition(null);
				CaptureMouse();

				if (e.ClickCount == 2) {
					if (!Node.IsRoot || ParentTreeView.ShowRootExpander) {
						Node.IsExpanded = !Node.IsExpanded;
					}
				}
			}
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (IsMouseCaptured) {
				var currentPoint = e.GetPosition(null);
				if (Math.Abs(currentPoint.X - startPoint.X) >= SystemParameters.MinimumHorizontalDragDistance ||
					Math.Abs(currentPoint.Y - startPoint.Y) >= SystemParameters.MinimumVerticalDragDistance) {

					if (Node.InternalCanDrag()) {
						Node.InternalDrag(this);
					}
				}
			}
		}

		protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
		{
			ReleaseMouseCapture();
			if (wasSelected) {
				base.OnMouseLeftButtonDown(e);
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

		#region Cut / Copy / Paste / Delete Commands

		static void RegisterCommands()
		{
			CommandManager.RegisterClassCommandBinding(typeof(SharpTreeViewItem),
				new CommandBinding(ApplicationCommands.Cut, HandleExecuted_Cut, HandleCanExecute_Cut));

			CommandManager.RegisterClassCommandBinding(typeof(SharpTreeViewItem),
				new CommandBinding(ApplicationCommands.Copy, HandleExecuted_Copy, HandleCanExecute_Copy));

			CommandManager.RegisterClassCommandBinding(typeof(SharpTreeViewItem),
				new CommandBinding(ApplicationCommands.Paste, HandleExecuted_Paste, HandleCanExecute_Paste));

			CommandManager.RegisterClassCommandBinding(typeof(SharpTreeViewItem),
				new CommandBinding(ApplicationCommands.Delete, HandleExecuted_Delete, HandleCanExecute_Delete));
		}

		static void HandleExecuted_Cut(object sender, ExecutedRoutedEventArgs e)
		{
			(sender as SharpTreeViewItem).Node.InternalCut();
		}

		static void HandleCanExecute_Cut(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = (sender as SharpTreeViewItem).Node.InternalCanCut();
		}

		static void HandleExecuted_Copy(object sender, ExecutedRoutedEventArgs e)
		{
			(sender as SharpTreeViewItem).Node.InternalCopy();
		}

		static void HandleCanExecute_Copy(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = (sender as SharpTreeViewItem).Node.InternalCanCopy();
		}

		static void HandleExecuted_Paste(object sender, ExecutedRoutedEventArgs e)
		{
			(sender as SharpTreeViewItem).Node.InternalPaste();
		}

		static void HandleCanExecute_Paste(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = (sender as SharpTreeViewItem).Node.InternalCanPaste();
		}

		static void HandleExecuted_Delete(object sender, ExecutedRoutedEventArgs e)
		{
			(sender as SharpTreeViewItem).Node.InternalDelete();
		}

		static void HandleCanExecute_Delete(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = (sender as SharpTreeViewItem).Node.InternalCanDelete();
		}

		#endregion
	}
}
