// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Utils;

namespace ICSharpCode.AvalonEdit.Folding
{
	sealed class FoldingMarginMarker : UIElement
	{
		internal VisualLine VisualLine;
		internal FoldingSection FoldingSection;
		
		bool isExpanded;
		
		public bool IsExpanded {
			get { return isExpanded; }
			set {
				if (isExpanded != value) {
					isExpanded = value;
					InvalidateVisual();
				}
				if (FoldingSection != null)
					FoldingSection.IsFolded = !value;
			}
		}
		
		protected override void OnMouseDown(MouseButtonEventArgs e)
		{
			base.OnMouseDown(e);
			if (!e.Handled) {
				if (e.ChangedButton == MouseButton.Left) {
					IsExpanded = !IsExpanded;
					e.Handled = true;
				}
			}
		}
		
		const double MarginSizeFactor = 0.7;
		
		protected override Size MeasureCore(Size availableSize)
		{
			double size = MarginSizeFactor * FoldingMargin.SizeFactor * (double)GetValue(TextBlock.FontSizeProperty);
			size = PixelSnapHelpers.RoundToOdd(size, PixelSnapHelpers.GetPixelSize(this).Width);
			return new Size(size, size);
		}
		
		protected override void OnRender(DrawingContext drawingContext)
		{
			FoldingMargin margin = VisualParent as FoldingMargin;
			Pen activePen = new Pen(margin.SelectedFoldingMarkerBrush, 1);
			Pen inactivePen = new Pen(margin.FoldingMarkerBrush, 1);
			activePen.StartLineCap = inactivePen.StartLineCap = PenLineCap.Square;
			activePen.EndLineCap = inactivePen.EndLineCap = PenLineCap.Square;
			Size pixelSize = PixelSnapHelpers.GetPixelSize(this);
			Rect rect = new Rect(pixelSize.Width / 2,
			                     pixelSize.Height / 2,
			                     this.RenderSize.Width - pixelSize.Width,
			                     this.RenderSize.Height - pixelSize.Height);
			drawingContext.DrawRectangle(
				IsMouseDirectlyOver ? margin.SelectedFoldingMarkerBackgroundBrush : margin.FoldingMarkerBackgroundBrush,
				IsMouseDirectlyOver ? activePen : inactivePen, rect);
			double middleX = rect.Left + rect.Width / 2;
			double middleY = rect.Top + rect.Height / 2;
			double space = PixelSnapHelpers.Round(rect.Width / 8, pixelSize.Width) + pixelSize.Width;
			drawingContext.DrawLine(activePen,
			                        new Point(rect.Left + space, middleY),
			                        new Point(rect.Right - space, middleY));
			if (!isExpanded) {
				drawingContext.DrawLine(activePen,
				                        new Point(middleX, rect.Top + space),
				                        new Point(middleX, rect.Bottom - space));
			}
		}
		
		protected override void OnIsMouseDirectlyOverChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnIsMouseDirectlyOverChanged(e);
			InvalidateVisual();
		}
	}
}
