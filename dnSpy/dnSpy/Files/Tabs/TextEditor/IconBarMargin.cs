// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using dnSpy.Contracts.Files.Tabs.TextEditor;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Themes;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Utils;

namespace dnSpy.Files.Tabs.TextEditor {
	interface IIconBarMargin {
		ITextEditorUIContext UIContext { get; }
		FrameworkElement FrameworkElement { get; }
		int? GetLineFromMousePosition();
	}

	sealed class IconBarMargin : AbstractMargin, IIconBarMargin, IDisposable {
		// 16px wide icon + 1px each side padding + 1px right-side border
		public const int WIDTH = 1 + 16 + 1 + 1;

		ITextEditorUIContext IIconBarMargin.UIContext => uiContext;
		FrameworkElement IIconBarMargin.FrameworkElement => this;

		readonly ITextEditorUIContext uiContext;
		readonly ITextLineObjectManager textLineObjectManager;
		readonly IImageManager imageManager;
		readonly IThemeManager themeManager;

		public IconBarMargin(ITextEditorUIContext uiContext, ITextLineObjectManager textLineObjectManager, IImageManager imageManager, IThemeManager themeManager) {
			this.uiContext = uiContext;
			this.textLineObjectManager = textLineObjectManager;
			this.imageManager = imageManager;
			this.themeManager = themeManager;
		}

		public void Dispose() => this.TextView = null;

		// accept clicks even when clicking on the background
		protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters) => new PointHitTestResult(this, hitTestParameters.HitPoint);

		protected override Size MeasureOverride(Size availableSize) => new Size(WIDTH, 0);

		protected override void OnRender(DrawingContext drawingContext) {
			Size renderSize = this.RenderSize;
			var theme = themeManager.Theme;
			var bgColor = (theme.GetColor(ColorType.IconBar).Background as SolidColorBrush).Color;
			drawingContext.DrawRectangle(theme.GetColor(ColorType.IconBar).Background, null,
										 new Rect(0, 0, renderSize.Width, renderSize.Height));
			drawingContext.DrawLine(new Pen(theme.GetColor(ColorType.IconBarBorder).Background, 1),
									new Point(renderSize.Width - 0.5, 0),
									new Point(renderSize.Width - 0.5, renderSize.Height));

			TextView textView = this.TextView;
			if (textView?.VisualLinesValid == true) {
				// create a dictionary line number => first bookmark
				Dictionary<int, List<IIconBarObject>> bookmarkDict = new Dictionary<int, List<IIconBarObject>>();
				foreach (var obj in textLineObjectManager.GetObjectsOfType<IIconBarObject>()) {
					if (!obj.IsVisible(uiContext))
						continue;
					int line = obj.GetLineNumber(uiContext);
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
							drawingContext.DrawImage(imageManager.GetImage(imgRef.Value, bgColor), rect);
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

		int? IIconBarMargin.GetLineFromMousePosition() {
			var textView = this.TextView;
			if (textView == null)
				return null;
			var vl = textView.GetVisualLineFromVisualTop(Mouse.GetPosition(textView).Y + textView.ScrollOffset.Y);
			if (vl == null)
				return null;
			return vl.FirstDocumentLine.LineNumber;
		}
	}
}
