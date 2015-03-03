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

namespace ICSharpCode.AvalonEdit.Editing
{
	sealed class SelectionLayer : Layer, IWeakEventListener
	{
		readonly TextArea textArea;
		
		public SelectionLayer(TextArea textArea) : base(textArea.TextView, KnownLayer.Selection)
		{
			this.IsHitTestVisible = false;
			
			this.textArea = textArea;
			TextViewWeakEventManager.VisualLinesChanged.AddListener(textView, this);
			TextViewWeakEventManager.ScrollOffsetChanged.AddListener(textView, this);
		}
		
		bool IWeakEventListener.ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
		{
			if (managerType == typeof(TextViewWeakEventManager.VisualLinesChanged)
			    || managerType == typeof(TextViewWeakEventManager.ScrollOffsetChanged))
			{
				InvalidateVisual();
				return true;
			}
			return false;
		}
		
		protected override void OnRender(DrawingContext drawingContext)
		{
			base.OnRender(drawingContext);
			
			BackgroundGeometryBuilder geoBuilder = new BackgroundGeometryBuilder();
			geoBuilder.AlignToMiddleOfPixels = true;
			geoBuilder.ExtendToFullWidthAtLineEnd = textArea.Selection.EnableVirtualSpace;
			geoBuilder.CornerRadius = textArea.SelectionCornerRadius;
			foreach (var segment in textArea.Selection.Segments) {
				geoBuilder.AddSegment(textView, segment);
			}
			Geometry geometry = geoBuilder.CreateGeometry();
			if (geometry != null) {
				drawingContext.DrawGeometry(textArea.SelectionBrush, textArea.SelectionBorder, geometry);
			}
		}
	}
}
