// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

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
