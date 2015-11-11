// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Input;
using System.ComponentModel;
using System.Collections.Specialized;

namespace ICSharpCode.TreeView
{
	public class SharpTreeNodeView : Control
	{
		static SharpTreeNodeView()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(SharpTreeNodeView),
			                                         new FrameworkPropertyMetadata(typeof(SharpTreeNodeView)));
		}

		public static readonly DependencyProperty TextBackgroundProperty =
			DependencyProperty.Register("TextBackground", typeof(Brush), typeof(SharpTreeNodeView));

		public Brush TextBackground
		{
			get { return (Brush)GetValue(TextBackgroundProperty); }
			set { SetValue(TextBackgroundProperty, value); }
		}

		SharpTreeNode GetNode(object dataCtx)
		{
			if (dataCtx is SharpTreeNode)
				return (SharpTreeNode)dataCtx;
			else if (dataCtx is SharpTreeNodeProxy)
				return ((SharpTreeNodeProxy)dataCtx).Object;
			else
				return null;
		}

		public SharpTreeNode Node
		{
			get { return GetNode(DataContext); }
		}

		public SharpTreeViewItem ParentItem { get; private set; }
		
		public static readonly DependencyProperty CellEditorProperty =
			DependencyProperty.Register("CellEditor", typeof(Control), typeof(SharpTreeNodeView),
			                            new FrameworkPropertyMetadata());
		
		public Control CellEditor {
			get { return (Control)GetValue(CellEditorProperty); }
			set { SetValue(CellEditorProperty, value); }
		}

		public SharpTreeView ParentTreeView
		{
			get { return ParentItem.ParentTreeView; }
		}

		internal LinesRenderer LinesRenderer { get; private set; }

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			LinesRenderer = Template.FindName("linesRenderer", this) as LinesRenderer;
			UpdateTemplate();
		}

		protected override void OnVisualParentChanged(DependencyObject oldParent)
		{
			base.OnVisualParentChanged(oldParent);
			ParentItem = this.FindAncestor<SharpTreeViewItem>();
			ParentItem.NodeView = this;
		}

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);
			if (e.Property == DataContextProperty) {
				UpdateDataContext(GetNode(e.OldValue), GetNode(e.NewValue));
			}
		}

		void UpdateDataContext(SharpTreeNode oldNode, SharpTreeNode newNode)
		{
			if (newNode != null) {
				newNode.PropertyChanged += Node_PropertyChanged;
				UpdateTemplate();
			}
			if (oldNode != null) {
				oldNode.PropertyChanged -= Node_PropertyChanged;
			}
		}

		void Node_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			var node = (SharpTreeNode)sender;
			if (e.PropertyName == "IsEditing") {
				OnIsEditingChanged();
			} else if (e.PropertyName == "IsLast") {
				if (ParentTreeView.ShowLines) {
					foreach (var child in Node.VisibleDescendantsAndSelf()) {
						var container = ParentTreeView.ItemContainerGenerator.ContainerFromItem(child) as SharpTreeViewItem;
						if (container != null && container.NodeView != null && container.NodeView.LinesRenderer != null) {
							container.NodeView.LinesRenderer.InvalidateVisual();
						}
					}
				}
			} else if (e.PropertyName == "IsExpanded") {
				if (node.IsExpanded)
					ParentTreeView.HandleExpanding(node);
			}
		}

		void OnIsEditingChanged()
		{
			var textEditorContainer = Template.FindName("textEditorContainer", this) as Border;
			if (Node.IsEditing) {
				if (CellEditor == null)
					textEditorContainer.Child = new EditTextBox() { Item = ParentItem };
				else
					textEditorContainer.Child = CellEditor;
			}
			else {
				textEditorContainer.Child = null;
			}
		}

		internal void UpdateTemplate()
		{
			if (Template == null || Node == null)
				return;

			var spacer = Template.FindName("spacer", this) as FrameworkElement;
			spacer.Width = CalculateIndent();

			var expander = Template.FindName("expander", this) as ToggleButton;
			if (ParentTreeView.Root == Node && !ParentTreeView.ShowRootExpander) {
				expander.Visibility = Visibility.Collapsed;
			}
			else {
				expander.ClearValue(VisibilityProperty);
			}
		}

		internal double CalculateIndent()
		{
			var result = 19 * Node.Level;
			if (ParentTreeView.ShowRoot) {
				if (!ParentTreeView.ShowRootExpander) {
					if (ParentTreeView.Root != Node) {
						result -= 15;
					}
				}
			}
			else {
				result -= 19;
			}
			if (result < 0) {
				// BUG: Node is sometimes an already deleted node so result will be set to 0 above
				//		and will eventually get a negative value.
				result = 0;
			}
			if (result < 0)
				throw new InvalidOperationException();
			return result;
		}
	}
}
