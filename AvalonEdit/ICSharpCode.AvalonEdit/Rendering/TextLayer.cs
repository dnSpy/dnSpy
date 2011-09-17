// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace ICSharpCode.AvalonEdit.Rendering
{
	/// <summary>
	/// The control that contains the text.
	/// 
	/// This control is used to allow other UIElements to be placed inside the TextView but
	/// behind the text.
	/// The text rendering process (VisualLine creation) is controlled by the TextView, this
	/// class simply displays the created Visual Lines.
	/// </summary>
	/// <remarks>
	/// This class does not contain any input handling and is invisible to hit testing. Input
	/// is handled by the TextView.
	/// This allows UIElements that are displayed behind the text, but still can react to mouse input.
	/// </remarks>
	sealed class TextLayer : Layer
	{
		/// <summary>
		/// the index of the text layer in the layers collection
		/// </summary>
		internal int index;
		
		public TextLayer(TextView textView) : base(textView, KnownLayer.Text)
		{
		}
		
		List<VisualLineDrawingVisual> visuals = new List<VisualLineDrawingVisual>();
		
		internal void SetVisualLines(ICollection<VisualLine> visualLines)
		{
			foreach (VisualLineDrawingVisual v in visuals) {
				if (v.VisualLine.IsDisposed)
					RemoveVisualChild(v);
			}
			visuals.Clear();
			foreach (VisualLine newLine in visualLines) {
				VisualLineDrawingVisual v = newLine.Render();
				if (!v.IsAdded) {
					AddVisualChild(v);
					v.IsAdded = true;
				}
				visuals.Add(v);
			}
			InvalidateArrange();
		}
		
		protected override int VisualChildrenCount {
			get { return visuals.Count; }
		}
		
		protected override Visual GetVisualChild(int index)
		{
			return visuals[index];
		}
		
		protected override void ArrangeCore(Rect finalRect)
		{
			textView.ArrangeTextLayer(visuals);
		}
	}
}
