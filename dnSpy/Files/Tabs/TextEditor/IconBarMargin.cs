// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using dnSpy.Contracts;
using dnSpy.Contracts.Files.Tabs.TextEditor;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Themes;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Utils;

namespace dnSpy.Files.Tabs.TextEditor {
	sealed class IconBarMargin : AbstractMargin, IDisposable {
		// 16px wide icon + 1px each side padding + 1px right-side border
		public const int WIDTH = 1 + 16 + 1 + 1;

		readonly ITextEditorUIContext textEditorUIContext;
		readonly ITextLineObjectManager textLineObjectManager;
		readonly IImageManager imageManager;

		public IconBarMargin(ITextEditorUIContext textEditorUIContext, ITextLineObjectManager textLineObjectManager, IImageManager imageManager) {
			this.textEditorUIContext = textEditorUIContext;
			this.textLineObjectManager = textLineObjectManager;
			this.imageManager = imageManager;
		}

		public void Dispose() {
			this.TextView = null;
		}

		protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters) {
			// accept clicks even when clicking on the background
			return new PointHitTestResult(this, hitTestParameters.HitPoint);
		}

		protected override Size MeasureOverride(Size availableSize) {
			return new Size(WIDTH, 0);
		}

		protected override void OnRender(DrawingContext drawingContext) {
			Size renderSize = this.RenderSize;
			var theme = DnSpy.App.ThemeManager.Theme;
			var bgColor = (theme.GetColor(ColorType.IconBar).Background as SolidColorBrush).Color;
			drawingContext.DrawRectangle(theme.GetColor(ColorType.IconBar).Background, null,
										 new Rect(0, 0, renderSize.Width, renderSize.Height));
			drawingContext.DrawLine(new Pen(theme.GetColor(ColorType.IconBarBorder).Background, 1),
									new Point(renderSize.Width - 0.5, 0),
									new Point(renderSize.Width - 0.5, renderSize.Height));

			TextView textView = this.TextView;
			if (textView != null && textView.VisualLinesValid) {
				// create a dictionary line number => first bookmark
				Dictionary<int, List<IIconBarObject>> bookmarkDict = new Dictionary<int, List<IIconBarObject>>();
				foreach (var obj in textLineObjectManager.GetObjectsOfType<IIconBarObject>()) {
					if (!obj.IsVisible(textEditorUIContext))
						continue;
					int line = obj.GetLineNumber(textEditorUIContext);
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
						var imgRef = bm.ImageReference;
						if (imgRef != null)
							drawingContext.DrawImage(imageManager.GetImage(imgRef.Value.Assembly, imgRef.Value.Name, bgColor), rect);
					}
				}
			}
		}

		protected override void OnMouseDown(MouseButtonEventArgs e) {
			base.OnMouseDown(e);
			// don't allow selecting text through the IconBarMargin
			if (e.ChangedButton == MouseButton.Left)
				e.Handled = true;
		}

		public int GetLineFromMousePosition(MouseEventArgs e) {
			TextView textView = this.TextView;
			if (textView == null)
				return 0;
			VisualLine vl = textView.GetVisualLineFromVisualTop(e.GetPosition(textView).Y + textView.ScrollOffset.Y);
			if (vl == null)
				return 0;
			return vl.FirstDocumentLine.LineNumber;
		}
	}
}
