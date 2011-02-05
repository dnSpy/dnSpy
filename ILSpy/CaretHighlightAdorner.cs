// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit.Editing;

namespace ICSharpCode.ILSpy
{
	/// <summary>
	/// Animated rectangle around the caret.
	/// </summary>
	sealed class CaretHighlightAdorner : Adorner
	{
		Pen pen;
		RectangleGeometry geometry;
		
		public CaretHighlightAdorner(TextArea textArea)
			: base(textArea.TextView)
		{
			Rect min = textArea.Caret.CalculateCaretRectangle();
			min.Offset(-textArea.TextView.ScrollOffset);
			
			Rect max = min;
			double size = Math.Max(min.Width, min.Height) * 0.25;
			max.Inflate(size, size);
			
			pen = new Pen(TextBlock.GetForeground(textArea.TextView).Clone(), 1);
			
			geometry = new RectangleGeometry(min, 2, 2);
			geometry.BeginAnimation(RectangleGeometry.RectProperty, new RectAnimation(min, max, new Duration(TimeSpan.FromMilliseconds(300))) { AutoReverse = true });
			pen.Brush.BeginAnimation(Brush.OpacityProperty, new DoubleAnimation(1, 0, new Duration(TimeSpan.FromMilliseconds(200))) { BeginTime = TimeSpan.FromMilliseconds(450) });
		}
		
		public static void DisplayCaretHighlightAnimation(TextArea textArea)
		{
			AdornerLayer layer = AdornerLayer.GetAdornerLayer(textArea.TextView);
			CaretHighlightAdorner adorner = new CaretHighlightAdorner(textArea);
			layer.Add(adorner);
			
			DispatcherTimer timer = new DispatcherTimer();
			timer.Interval = TimeSpan.FromSeconds(1);
			timer.Tick += delegate {
				timer.Stop();
				layer.Remove(adorner);
			};
			timer.Start();
		}
		
		protected override void OnRender(DrawingContext drawingContext)
		{
			drawingContext.DrawGeometry(null, pen, geometry);
		}
	}
}
