// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Windows;
using System.Windows.Media;

using ICSharpCode.AvalonEdit.Rendering;

namespace ICSharpCode.AvalonEdit.Rendering
{
	sealed class CurrentLineHighlightRenderer : IBackgroundRenderer
	{
		#region Fields
		
		int line;
		TextView textView;
		
		public static readonly Color DefaultBackground = Color.FromArgb(22, 20, 220, 224);
		public static readonly Color DefaultBorder = Color.FromArgb(52, 0, 255, 110);
		
		#endregion

		#region Properties
		
		public int Line {
			get { return this.line; } 
			set {
				if (this.line != value) {
					this.line = value;
					this.textView.InvalidateLayer(this.Layer);
				}
			}
		}
		
		public KnownLayer Layer
		{
			get { return KnownLayer.Selection; }
		}
		
		public Brush BackgroundBrush {
			get; set;
		}
		
		public Pen BorderPen {
			get; set;
		}
		
		#endregion		
		
		public CurrentLineHighlightRenderer(TextView textView)
		{
			if (textView == null)
				throw new ArgumentNullException("textView");
			
			this.BorderPen = new Pen(new SolidColorBrush(DefaultBorder), 1);
			this.BorderPen.Freeze();
			
			this.BackgroundBrush = new SolidColorBrush(DefaultBackground);
			this.BackgroundBrush.Freeze();
			
			this.textView = textView;
			this.textView.BackgroundRenderers.Add(this);
			
			this.line = 0;
		}
		
		public void Draw(TextView textView, DrawingContext drawingContext)
		{
			if(!this.textView.Options.HighlightCurrentLine)
				return;
			
			BackgroundGeometryBuilder builder = new BackgroundGeometryBuilder();
			
			var visualLine = this.textView.GetVisualLine(line);
			if (visualLine == null) return;
			
			var linePosY = visualLine.VisualTop - this.textView.ScrollOffset.Y;
			
			builder.AddRectangle(textView, new Rect(0, linePosY, textView.ActualWidth, visualLine.Height));
			
			Geometry geometry = builder.CreateGeometry();
			if (geometry != null) {
				drawingContext.DrawGeometry(this.BackgroundBrush, this.BorderPen, geometry);
			}
		}
	}
}
