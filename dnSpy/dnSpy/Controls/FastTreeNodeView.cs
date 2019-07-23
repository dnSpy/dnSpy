/*
	Copyright (c) 2015 Ki

	Permission is hereby granted, free of charge, to any person obtaining a copy
	of this software and associated documentation files (the "Software"), to deal
	in the Software without restriction, including without limitation the rights
	to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
	copies of the Software, and to permit persons to whom the Software is
	furnished to do so, subject to the following conditions:

	The above copyright notice and this permission notice shall be included in
	all copies or substantial portions of the Software.

	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
	OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
	THE SOFTWARE.
*/

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using dnSpy.Contracts.Images;
using ICSharpCode.TreeView;

namespace dnSpy.Controls {
	sealed class FastTreeNodeView : SharpTreeNodeView {
		static FastTreeNodeView() => DefaultStyleKeyProperty.OverrideMetadata(typeof(FastTreeNodeView),
				new FrameworkPropertyMetadata(typeof(FastTreeNodeView)));

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
		ToggleButton expander;
		DsImage icon;
		ContentPresenter content;
#pragma warning restore CS8618 // Non-nullable field is uninitialized.

		static readonly object toolTipDummy = new object();

		void InitializeChildrens() {
			if (!(expander is null))
				return;

			ToolTip = toolTipDummy;

			expander = new ToggleButton {
				Style = (Style)FindResource("ExpandCollapseToggleStyle")
			};
			icon = new DsImage {
				Width = 16,
				Height = 16,
				Margin = new Thickness(0, 0, 5, 1),
				VerticalAlignment = VerticalAlignment.Center,
				Focusable = false
			};
			content = new ContentPresenter {
				Margin = new Thickness(2, 0, 6, 0),
				VerticalAlignment = VerticalAlignment.Center,
				Focusable = false
			};

			expander.Checked += (sender, e) => { if (!(Node is null)) Node.IsExpanded = true; };
			expander.Unchecked += (sender, e) => { if (!(Node is null)) Node.IsExpanded = false; };

			AddVisualChild(expander);
			AddVisualChild(icon);
			AddVisualChild(content);

			UpdateChildren(Node);
		}

		protected override void OnToolTipOpening(ToolTipEventArgs e) {
			if (ToolTip == toolTipDummy) {
				ToolTip = Node is null ? null : Node.ToolTip;
				if (ToolTip is null)
					e.Handled = true;
			}
			base.OnToolTipOpening(e);
		}

		protected override int VisualChildrenCount => 3;

		protected override Visual? GetVisualChild(int index) {
			switch (index) {
				case 0:
					return expander;
				case 1:
					return icon;
				case 2:
					return content;
			}
			return null;
		}

		double indent = 0;

		protected override Size ArrangeOverride(Size arrangeBounds) {
			InitializeChildrens();
			double x = indent;
			double w = arrangeBounds.Width;
			double h = arrangeBounds.Height;

			var size = expander.DesiredSize;
			expander.Arrange(new Rect(x, (h - size.Height) / 2, size.Width, size.Height));
			x += size.Width;

			if (!icon.ImageReference.IsDefault) {
				size = icon.DesiredSize;
				icon.Arrange(new Rect(x, (h - size.Height) / 2, size.Width, size.Height));
				x += size.Width;
			}

			size = content.DesiredSize;
			content.Arrange(new Rect(x, (h - size.Height) / 2, size.Width, size.Height));
			x += size.Width;

			return new Size(x, h);
		}

		protected override Size MeasureOverride(Size constraint) {
			InitializeChildrens();
			var width = constraint.Width - indent;
			double height = 0;

			expander.Measure(new Size(width, constraint.Height));
			width -= expander.DesiredSize.Width;
			height = Math.Max(height, expander.DesiredSize.Height);

			if (!icon.ImageReference.IsDefault) {
				icon.Measure(new Size(width, constraint.Height));
				width -= icon.DesiredSize.Width;
				height = Math.Max(height, icon.DesiredSize.Height);
			}

			content.Measure(new Size(width, constraint.Height));
			width -= content.DesiredSize.Width;
			height = Math.Max(height, content.DesiredSize.Height);

			var iconWidth = icon.ImageReference.IsDefault ? 0 : icon.DesiredSize.Width;
			return new Size(indent + expander.DesiredSize.Width + iconWidth + content.DesiredSize.Width, height);
		}

		protected override void OnRender(DrawingContext drawingContext) {
			var x = indent + expander.DesiredSize.Width;
			var w = content.DesiredSize.Width;
			if (!icon.ImageReference.IsDefault)
				w += icon.DesiredSize.Width;
			var h = RenderSize.Height;
			drawingContext.DrawRectangle(Brushes.Transparent, null, new Rect(x, 0, w, h));

			if (!icon.ImageReference.IsDefault) {
				x += icon.DesiredSize.Width;
				w -= icon.DesiredSize.Width;
			}
			drawingContext.DrawRectangle(TextBackground, null, new Rect(x, 0, w, h));
		}

		protected override void UpdateTemplate() => UpdateChildren(Node);

		void UpdateChildren(SharpTreeNode? node) {
			InitializeChildrens();

			if (node is null || ParentItem is null)
				return;

			var newIndent = CalculateIndent(node);
			if (indent != newIndent) {
				indent = newIndent;
				InvalidateMeasure();
			}

			if (expander.IsChecked != node.IsExpanded)
				expander.IsChecked = node.IsExpanded;

			if (ToolTip != toolTipDummy)
				ToolTip = toolTipDummy;

			if (content.Content != node.Text)
				content.Content = node.Text;

			var newSrc = node.IsExpanded ? (ImageReference)node.ExpandedIcon : (ImageReference)node.Icon;
			if (!ImageReference_Equals(icon.ImageReference, newSrc))
				icon.ImageReference = newSrc;

			var newVis = !icon.ImageReference.IsDefault && node.ShowIcon ? Visibility.Visible : Visibility.Collapsed;
			if (icon.Visibility != newVis)
				icon.Visibility = newVis;

			if ((ParentTreeView.ShowRootExpander || ParentTreeView.Root != node) && node.ShowExpander)
				newVis = Visibility.Visible;
			else
				newVis = Visibility.Hidden;
			if (expander.Visibility != newVis)
				expander.Visibility = newVis;
		}

		static bool ImageReference_Equals(ImageReference a, ImageReference b) => a.Assembly == b.Assembly && a.Name == b.Name;

		protected override void Node_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
			base.Node_PropertyChanged(sender, e);

			UpdateChildren(sender as SharpTreeNode);
		}
	}
}
