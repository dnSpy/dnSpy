// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit.Editing;

namespace ICSharpCode.ILSpy.TextView
{
	/// <summary>
	/// Animated rectangle around the caret.
	/// This is used after clicking links that lead to another location within the text view.
	/// </summary>
	sealed class CaretHighlightAdorner : Adorner
	{
		readonly Pen pen;
		readonly RectangleGeometry geometry;
		
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
