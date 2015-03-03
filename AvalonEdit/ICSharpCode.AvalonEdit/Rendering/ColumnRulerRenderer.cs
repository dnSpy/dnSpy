// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
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
using System.Windows.Media;

using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Utils;

namespace ICSharpCode.AvalonEdit.Rendering
{
	/// <summary>
	/// Renders a ruler at a certain column.
	/// </summary>
	sealed class ColumnRulerRenderer : IBackgroundRenderer
	{
		Pen pen;
		int column;
		TextView textView;
		
		public static readonly Color DefaultForeground = Colors.LightGray;
		
		public ColumnRulerRenderer(TextView textView)
		{
			if (textView == null)
				throw new ArgumentNullException("textView");
			
			this.pen = new Pen(new SolidColorBrush(DefaultForeground), 1);
			this.pen.Freeze();
			this.textView = textView;
			this.textView.BackgroundRenderers.Add(this);
		}
		
		public KnownLayer Layer {
			get { return KnownLayer.Background; }
		}
		
		public void SetRuler(int column, Pen pen)
		{
			if (this.column != column) {
				this.column = column;
				textView.InvalidateLayer(this.Layer);
			}
			if (this.pen != pen) {
				this.pen = pen;
				textView.InvalidateLayer(this.Layer);
			}
		}
		
		public void Draw(TextView textView, System.Windows.Media.DrawingContext drawingContext)
		{
			if (column < 1) return;
			double offset = textView.WideSpaceWidth * column;
			Size pixelSize = PixelSnapHelpers.GetPixelSize(textView);
			double markerXPos = PixelSnapHelpers.PixelAlign(offset, pixelSize.Width);
			markerXPos -= textView.ScrollOffset.X;
			Point start = new Point(markerXPos, 0);
			Point end = new Point(markerXPos, Math.Max(textView.DocumentHeight, textView.ActualHeight));
			
			drawingContext.DrawLine(pen, start, end);
		}
	}
}
