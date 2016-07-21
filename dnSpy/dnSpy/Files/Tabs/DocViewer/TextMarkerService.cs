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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using dnSpy.Contracts.Files.Tabs.DocViewer;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Rendering;
using TextView = ICSharpCode.AvalonEdit.Rendering.TextView;

namespace dnSpy.Files.Tabs.DocViewer {
	sealed class TextMarkerService : DocumentColorizingTransformer, IBackgroundRenderer, ITextMarkerService, IDisposable {
		TextSegmentCollection<TextMarker> markers;
		readonly ITextEditorUIContext uiContext;
		readonly TextEditorControl textEditorControl;
		readonly ITextLineObjectManager textLineObjectManager;
		readonly Dictionary<ITextMarkerObject, ITextMarker> objToMarker = new Dictionary<ITextMarkerObject, ITextMarker>();

		public TextView TextView => textEditorControl.TextEditor.TextArea.TextView;

		public TextMarkerService(TextEditorControl textView, ITextEditorUIContext uiContext, ITextLineObjectManager textLineObjectManager) {
			this.textEditorControl = textView;
			this.uiContext = uiContext;
			this.textLineObjectManager = textLineObjectManager;
			//uiContext.NewTextContent += TextEditorUIContext_NewTextContent;
			textLineObjectManager.OnListModified += TextLineObjectManager_OnListModified;
			OnTextChanged();
		}

		void TextEditorUIContext_NewTextContent(object sender, EventArgs e) => RecreateMarkers();

		public void Dispose() {
			//uiContext.NewTextContent -= TextEditorUIContext_NewTextContent;
			textLineObjectManager.OnListModified -= TextLineObjectManager_OnListModified;
			ClearMarkers();
		}

		void ClearMarkers() {
			foreach (var obj in objToMarker.Keys.ToArray())
				RemoveMarker(obj);
		}

		public void OnTextChanged() {
			ClearMarkers();
			if (TextView.Document != null)
				markers = new TextSegmentCollection<TextMarker>(TextView.Document);
			else
				markers = null;
		}

		void TextLineObjectManager_OnListModified(object sender, TextLineObjectListModifiedEventArgs e) {
			if (e.Added)
				CreateMarker(e.TextLineObject as ITextMarkerObject);
			else
				RemoveMarker(e.TextLineObject as ITextMarkerObject);
		}

		void RecreateMarkers() {
			foreach (var tmo in textLineObjectManager.GetObjectsOfType<ITextMarkerObject>())
				CreateMarker(tmo);
		}

		void CreateMarker(ITextMarkerObject tmo) {
			if (tmo == null || !tmo.IsVisible(uiContext))
				return;

			ITextMarker marker;
			if (!objToMarker.TryGetValue(tmo, out marker)) {
				objToMarker.Add(tmo, marker = tmo.CreateMarker(uiContext, this));
				tmo.ObjPropertyChanged += TextMarkerObject_ObjPropertyChanged;
			}
			Debug.Assert(marker != null);
		}

		void RemoveMarker(ITextMarkerObject tmo) {
			if (tmo == null)
				return;

			tmo.ObjPropertyChanged -= TextMarkerObject_ObjPropertyChanged;

			ITextMarker marker;
			if (objToMarker.TryGetValue(tmo, out marker)) {
				objToMarker.Remove(tmo);
				Remove(marker);
			}
		}

		void TextMarkerObject_ObjPropertyChanged(object sender, TextLineObjectEventArgs e) {
			if (e.Property == TextLineObjectEventArgs.RedrawProperty) {
				var tmo = (ITextMarkerObject)sender;
				ITextMarker marker;
				if (objToMarker.TryGetValue(tmo, out marker))
					marker.Redraw();
			}
		}

		#region ITextMarkerService
		public ITextMarker Create(int startOffset, int length) {
			if (markers == null)
				throw new InvalidOperationException("Cannot create a marker when not attached to a document");

			int textLength = TextView.Document.TextLength;
			if (startOffset < 0 || startOffset > textLength)
				throw new ArgumentOutOfRangeException(nameof(startOffset), startOffset, "Value must be between 0 and " + textLength);
			if (length < 0 || startOffset + length > textLength)
				throw new ArgumentOutOfRangeException(nameof(length), length, "length must not be negative and startOffset+length must not be after the end of the document");

			TextMarker m = new TextMarker(this, startOffset, length);
			markers.Add(m);
			// no need to mark segment for redraw: the text marker is invisible until a property is set
			return m;
		}

		public void Remove(ITextMarker marker) {
			if (marker == null)
				return;
			TextMarker m = marker as TextMarker;
			if (markers != null && markers.Remove(m)) {
				Redraw(m);
				m.OnDeleted();
			}
		}

		/// <summary>
		/// Redraws the specified text segment.
		/// </summary>
		internal void Redraw(ISegment segment) {
			TextView.Redraw(segment, DispatcherPriority.Normal);
			RedrawRequested?.Invoke(this, EventArgs.Empty);
		}

		public event EventHandler RedrawRequested;
		#endregion

		IEnumerable<TextMarker> GetSortedTextMarkers(int lineStart, int lineLength) {
			if (markers == null)
				return Array.Empty<TextMarker>();
			var list = new List<TextMarker>(markers.FindOverlappingSegments(lineStart, lineLength));
			list.Sort((a, b) => a.ZOrder.CompareTo(b.ZOrder));
			return list;
		}

		#region DocumentColorizingTransformer
		protected override void ColorizeLine(DocumentLine line) {
			if (markers == null || markers.Count == 0)
				return;
			int lineStart = line.Offset;
			int lineEnd = lineStart + line.Length;
			foreach (TextMarker marker in GetSortedTextMarkers(lineStart, line.Length)) {
				if (marker.TextMarkerObject != null && !marker.IsVisible(marker.TextMarkerObject))
					continue;

				Brush foregroundBrush = null;
				if (marker.ForegroundColor != null) {
					foregroundBrush = new SolidColorBrush(marker.ForegroundColor.Value);
					foregroundBrush.Freeze();
				}
				ChangeLinePart(
					Math.Max(marker.StartOffset, lineStart),
					Math.Min(marker.EndOffset, lineEnd),
					element => {
						if (foregroundBrush != null)
							element.TextRunProperties.SetForegroundBrush(foregroundBrush);
						Typeface tf = element.TextRunProperties.Typeface;
						element.TextRunProperties.SetTypeface(new Typeface(
							tf.FontFamily,
							marker.FontStyle ?? tf.Style,
							marker.FontWeight ?? tf.Weight,
							tf.Stretch
						));
					}
				);
			}
		}
		#endregion

		#region IBackgroundRenderer
		public KnownLayer Layer => KnownLayer.Selection;

		public void Draw(ICSharpCode.AvalonEdit.Rendering.TextView textView, DrawingContext drawingContext) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			if (drawingContext == null)
				throw new ArgumentNullException(nameof(drawingContext));
			if (markers == null || markers.Count == 0 || !textView.VisualLinesValid)
				return;
			var visualLines = textView.VisualLines;
			if (visualLines.Count == 0)
				return;
			int viewStart = visualLines.First().FirstDocumentLine.Offset;
			int viewEnd = visualLines.Last().LastDocumentLine.EndOffset;
			foreach (TextMarker marker in GetSortedTextMarkers(viewStart, viewEnd - viewStart)) {
				if (marker.TextMarkerObject != null && !marker.IsVisible(marker.TextMarkerObject))
					continue;

				if (marker.BackgroundColor != null) {
					BackgroundGeometryBuilder geoBuilder = new BackgroundGeometryBuilder();
					geoBuilder.AlignToWholePixels = true;
					geoBuilder.CornerRadius = 0;
					geoBuilder.AddSegment(textView, marker);
					Geometry geometry = geoBuilder.CreateGeometry();
					if (geometry != null) {
						Color color = marker.BackgroundColor.Value;
						SolidColorBrush brush = new SolidColorBrush(color);
						brush.Freeze();
						drawingContext.DrawGeometry(brush, null, geometry);
					}
				}
				var underlineMarkerTypes = TextMarkerTypes.SquigglyUnderline | TextMarkerTypes.NormalUnderline | TextMarkerTypes.DottedUnderline;
				if ((marker.MarkerTypes & underlineMarkerTypes) != 0) {
					foreach (Rect r in BackgroundGeometryBuilder.GetRectsForSegment(textView, marker)) {
						Point startPoint = r.BottomLeft;
						Point endPoint = r.BottomRight;

						Brush usedBrush = new SolidColorBrush(marker.MarkerColor);
						usedBrush.Freeze();
						if ((marker.MarkerTypes & TextMarkerTypes.SquigglyUnderline) != 0) {
							double offset = 2.5;

							int count = Math.Max((int)((endPoint.X - startPoint.X) / offset) + 1, 4);

							StreamGeometry geometry = new StreamGeometry();

							using (StreamGeometryContext ctx = geometry.Open()) {
								ctx.BeginFigure(startPoint, false, false);
								ctx.PolyLineTo(CreatePoints(startPoint, endPoint, offset, count).ToArray(), true, false);
							}

							geometry.Freeze();

							Pen usedPen = new Pen(usedBrush, 1);
							usedPen.Freeze();
							drawingContext.DrawGeometry(Brushes.Transparent, usedPen, geometry);
						}
						if ((marker.MarkerTypes & TextMarkerTypes.NormalUnderline) != 0) {
							Pen usedPen = new Pen(usedBrush, 1);
							usedPen.Freeze();
							drawingContext.DrawLine(usedPen, startPoint, endPoint);
						}
						if ((marker.MarkerTypes & TextMarkerTypes.DottedUnderline) != 0) {
							Pen usedPen = new Pen(usedBrush, 1);
							usedPen.DashStyle = DashStyles.Dot;
							usedPen.Freeze();
							drawingContext.DrawLine(usedPen, startPoint, endPoint);
						}
					}
				}
			}
		}

		IEnumerable<Point> CreatePoints(Point start, Point end, double offset, int count) {
			for (int i = 0; i < count; i++)
				yield return new Point(start.X + i * offset, start.Y - ((i + 1) % 2 == 0 ? offset : 0));
		}
		#endregion
	}

	sealed class TextMarker : TextSegment, ITextMarker {
		readonly TextMarkerService service;

		public TextMarker(TextMarkerService service, int startOffset, int length) {
			if (service == null)
				throw new ArgumentNullException(nameof(service));
			this.service = service;
			this.StartOffset = startOffset;
			this.Length = length;
			this.markerTypes = TextMarkerTypes.None;
		}

		public event EventHandler Deleted;
		public bool IsDeleted => !this.IsConnectedToCollection;
		public void Delete() => service.Remove(this);
		internal void OnDeleted() => Deleted?.Invoke(this, EventArgs.Empty);
		public void Redraw() => service.Redraw(this);

		/// <summary>
		/// Gets the highlighting color
		/// </summary>
		public Func<HighlightingColor> HighlightingColor {
			get { return highlightingColor; }
			set {
				highlightingColor = value;
				Redraw();
			}
		}
		Func<HighlightingColor> highlightingColor;

		public Color? BackgroundColor => HighlightingColor()?.Background?.GetColor(null);
		public Color? ForegroundColor => HighlightingColor()?.Foreground?.GetColor(null);
		public FontWeight? FontWeight => HighlightingColor()?.FontWeight;
		public FontStyle? FontStyle => HighlightingColor()?.FontStyle;

		TextMarkerTypes markerTypes;

		public TextMarkerTypes MarkerTypes {
			get { return markerTypes; }
			set {
				if (markerTypes != value) {
					markerTypes = value;
					Redraw();
				}
			}
		}

		Color markerColor;

		public Color MarkerColor {
			get { return markerColor; }
			set {
				if (markerColor != value) {
					markerColor = value;
					Redraw();
				}
			}
		}

		public Predicate<object> IsVisible { get; set; }
		public ITextMarkerObject TextMarkerObject { get; set; }
		public double ZOrder { get; set; }
	}
}
