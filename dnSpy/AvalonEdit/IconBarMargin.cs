// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using dnSpy.AvalonEdit;
using dnSpy.dntheme;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Utils;
using ICSharpCode.ILSpy.TextView;

namespace ICSharpCode.ILSpy.AvalonEdit {
	public class IconBarMargin : AbstractMargin, IDisposable
	{
		// 16px wide icon + 1px each side padding + 1px right-side border
		public const int WIDTH = 1 + 16 + 1 + 1;

		readonly DecompilerTextView decompilerTextView;
		
		public IconBarMargin(DecompilerTextView decompilerTextView)
		{
			this.decompilerTextView = decompilerTextView;
		}
		
		public virtual void Dispose()
		{
			this.TextView = null;
		}
		
		/// <inheritdoc/>
		protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
		{
			// accept clicks even when clicking on the background
			return new PointHitTestResult(this, hitTestParameters.HitPoint);
		}
		
		/// <inheritdoc/>
		protected override Size MeasureOverride(Size availableSize)
		{
			return new Size(WIDTH, 0);
		}
		
		protected override void OnRender(DrawingContext drawingContext)
		{
			Size renderSize = this.RenderSize;
			var theme = Themes.Theme;
			var bgColor = theme.GetColor(ColorType.IconBar).InheritedColor.Background.GetColor(null).Value;
			drawingContext.DrawRectangle(theme.GetColor(ColorType.IconBar).InheritedColor.Background.GetBrush(null), null,
			                             new Rect(0, 0, renderSize.Width, renderSize.Height));
			drawingContext.DrawLine(new Pen(theme.GetColor(ColorType.IconBarBorder).InheritedColor.Background.GetBrush(null), 1),
			                        new Point(renderSize.Width - 0.5, 0),
			                        new Point(renderSize.Width - 0.5, renderSize.Height));
			
			ICSharpCode.AvalonEdit.Rendering.TextView textView = this.TextView;
			if (textView != null && textView.VisualLinesValid) {
				// create a dictionary line number => first bookmark
				Dictionary<int, List<IIconBarObject>> bookmarkDict = new Dictionary<int, List<IIconBarObject>>();
				foreach (var obj in TextLineObjectManager.Instance.GetObjectsOfType<IIconBarObject>()) {
					if (!obj.IsVisible(decompilerTextView))
						continue;
					int line = obj.GetLineNumber(decompilerTextView);
					List<IIconBarObject> list;
					if (!bookmarkDict.TryGetValue(line, out list))
						bookmarkDict[line] = list = new List<IIconBarObject>();
					list.Add(obj);
				}
				
				const double imagePadding = 1.0;
				Size pixelSize = PixelSnapHelpers.GetPixelSize(this);
				foreach (VisualLine line in textView.VisualLines) {
					int lineNumber = line.FirstDocumentLine.LineNumber;
					List<IIconBarObject> list;
					if (!bookmarkDict.TryGetValue(lineNumber, out list))
						continue;
					list.Sort((a, b) => a.ZOrder.CompareTo(b.ZOrder));
					foreach (var bm in list) {
						Rect rect = new Rect(imagePadding, PixelSnapHelpers.Round(line.VisualTop - textView.VerticalOffset, pixelSize.Height), 16, 16);
						drawingContext.DrawImage(bm.GetImage(bgColor), rect);
					}
				}
			}
		}
		
		protected override void OnMouseDown(MouseButtonEventArgs e)
		{
			base.OnMouseDown(e);
			// don't allow selecting text through the IconBarMargin
			if (e.ChangedButton == MouseButton.Left)
				e.Handled = true;
		}
		
		public int GetLineFromMousePosition(MouseEventArgs e)
		{
			ICSharpCode.AvalonEdit.Rendering.TextView textView = this.TextView;
			if (textView == null)
				return 0;
			VisualLine vl = textView.GetVisualLineFromVisualTop(e.GetPosition(textView).Y + textView.ScrollOffset.Y);
			if (vl == null)
				return 0;
			return vl.FirstDocumentLine.LineNumber;
		}
	}
}
