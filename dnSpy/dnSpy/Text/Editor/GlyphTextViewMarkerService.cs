/*
    Copyright (C) 2014-2017 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Text.WPF;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Tagging;

namespace dnSpy.Text.Editor {
	interface IGlyphTextViewMarkerService {
		IWpfTextView TextView { get; }
		void SetMethodOffsetSpanMap(IMethodOffsetSpanMap map);
	}

	interface IGlyphTextMarkerListener {
		void OnAdded(IEnumerable<IGlyphTextMarkerImpl> markers);
		void OnRemoved(IEnumerable<IGlyphTextMarkerImpl> markers);
	}

	sealed class GlyphTextViewMarkerService : IGlyphTextViewMarkerService {
		public IWpfTextView TextView { get; }

		readonly IGlyphTextMarkerServiceImpl glyphTextMarkerServiceImpl;
		readonly IAdornmentLayer markerLayer;
		ITagAggregator<IGlyphTextMarkerTag> glyphTextMarkerTagAggregator;
		readonly GlyphTextViewMarkerGlyphTagger glyphTextViewMarkerGlyphTagTagger;
		readonly GlyphTextViewMarkerGlyphTextMarkerTagger glyphTextViewMarkerGlyphTextMarkerTagTagger;
		readonly GlyphTextViewMarkerClassificationTagger glyphTextViewMarkerClassificationTagTagger;
		IMethodOffsetSpanMap methodOffsetSpanMap;
		readonly MarkerAndSpanCollection markerAndSpanCollection;
		readonly IEditorFormatMap editorFormatMap;
		readonly List<MarkerElement> markerElements;
		bool useReducedOpacityForHighContrast;
		IGlyphTextMarkerListener glyphTextMarkerListener;

		struct MarkerAndNullableSpan {
			public Span? Span { get; }
			public IGlyphTextMarkerImpl Marker { get; }
			public MarkerAndNullableSpan(Span? span, IGlyphTextMarkerImpl marker) {
				Span = span;
				Marker = marker;
			}
		}

		sealed class MarkerAndSpanCollection {
			readonly List<IGlyphTextMarkerImpl> allMarkers;
			readonly Dictionary<IGlyphTextMarkerImpl, Span> inDocMarkers;
			readonly GlyphTextViewMarkerService owner;

			public int Count => allMarkers.Count;
			public int CountInDocument => inDocMarkers.Count;

			public int SelectedMarkersInDocumentCount {
				get { return selectedMarkersInDocumentCount; }
				set {
					if (selectedMarkersInDocumentCount == value)
						return;
					var oldValue = selectedMarkersInDocumentCount;
					selectedMarkersInDocumentCount = value;
					if ((oldValue == 0) != (selectedMarkersInDocumentCount == 0))
						owner.SelectedMarkersInDocumentCountChanged();
				}
			}
			int selectedMarkersInDocumentCount;

			public MarkerAndSpanCollection(GlyphTextViewMarkerService owner) {
				allMarkers = new List<IGlyphTextMarkerImpl>();
				inDocMarkers = new Dictionary<IGlyphTextMarkerImpl, Span>();
				this.owner = owner;
			}

			public void UpdateSpans(IMethodOffsetSpanMap map) {
				inDocMarkers.Clear();
				SelectedMarkersInDocumentCount = 0;
				if (map != null) {
					var allMarkers = this.allMarkers;
					for (int i = 0; i < allMarkers.Count; i++) {
						if (allMarkers[i] is IGlyphTextMethodMarkerImpl methodMarker) {
							var span = map.ToSpan(methodMarker.Method, methodMarker.ILOffset);
							if (span != null) {
								inDocMarkers.Add(methodMarker, span.Value);
								if (methodMarker.SelectedMarkerTypeName != null)
									SelectedMarkersInDocumentCount++;
							}
						}
					}
				}
			}

			public IEnumerable<MarkerAndSpan> GetMarkerAndSpans(ITextSnapshot snapshot, Span span) {
				// TODO: Not optimized
				// This method gets called three times per line (glyph margin, classifier, text marker).

				foreach (var kv in inDocMarkers) {
					if (!kv.Value.IntersectsWith(span))
						continue;
					if (kv.Value.End <= snapshot.Length)
						yield return new MarkerAndSpan(new SnapshotSpan(snapshot, kv.Value), kv.Key);
				}
			}

			public void Add(IGlyphTextMarkerImpl marker, Span? span) {
				allMarkers.Add(marker);
				if (span != null) {
					inDocMarkers.Add(marker, span.Value);
					if (marker.SelectedMarkerTypeName != null)
						SelectedMarkersInDocumentCount++;
				}
			}

			public bool Remove(IGlyphTextMarkerImpl marker) {
				for (int i = 0; i < allMarkers.Count; i++) {
					if (allMarkers[i] == marker) {
						allMarkers.RemoveAt(i);
						inDocMarkers.Remove(marker);
						if (marker.SelectedMarkerTypeName != null)
							SelectedMarkersInDocumentCount--;
						return true;
					}
				}
				return false;
			}

			public bool Remove(HashSet<IGlyphTextMarkerImpl> markers) {
				int removed = 0;
				for (int i = allMarkers.Count - 1; i >= 0; i--) {
					var marker = allMarkers[i];
					if (markers.Contains(marker)) {
						allMarkers.RemoveAt(i);
						inDocMarkers.Remove(marker);
						if (marker.SelectedMarkerTypeName != null)
							SelectedMarkersInDocumentCount--;
						removed++;
						if (removed >= markers.Count)
							break;
					}
				}
				return removed > 0;
			}

			public void Dispose() {
				allMarkers.Clear();
				inDocMarkers.Clear();
			}
		}

		GlyphTextViewMarkerService(IGlyphTextMarkerServiceImpl glyphTextMarkerServiceImpl, IWpfTextView wpfTextView) {
			onRemovedDelegate = OnRemoved;
			this.glyphTextMarkerServiceImpl = glyphTextMarkerServiceImpl ?? throw new ArgumentNullException(nameof(glyphTextMarkerServiceImpl));
			TextView = wpfTextView ?? throw new ArgumentNullException(nameof(wpfTextView));
			markerLayer = wpfTextView.GetAdornmentLayer(PredefinedDsAdornmentLayers.GlyphTextMarker);
			markerAndSpanCollection = new MarkerAndSpanCollection(this);
			markerElements = new List<MarkerElement>();
			editorFormatMap = glyphTextMarkerServiceImpl.EditorFormatMapService.GetEditorFormatMap(wpfTextView);
			glyphTextViewMarkerGlyphTagTagger = GlyphTextViewMarkerGlyphTagger.GetOrCreate(this);
			glyphTextViewMarkerGlyphTextMarkerTagTagger = GlyphTextViewMarkerGlyphTextMarkerTagger.GetOrCreate(this);
			glyphTextViewMarkerClassificationTagTagger = GlyphTextViewMarkerClassificationTagger.GetOrCreate(this);
			useReducedOpacityForHighContrast = wpfTextView.Options.GetOptionValue(DefaultWpfViewOptions.UseReducedOpacityForHighContrastOptionId);
			wpfTextView.Closed += WpfTextView_Closed;
			wpfTextView.LayoutChanged += WpfTextView_LayoutChanged;
			wpfTextView.Options.OptionChanged += Options_OptionChanged;
			glyphTextMarkerServiceImpl.MarkerAdded += GlyphTextMarkerServiceImpl_MarkerAdded;
			glyphTextMarkerServiceImpl.MarkerRemoved += GlyphTextMarkerServiceImpl_MarkerRemoved;
			glyphTextMarkerServiceImpl.MarkersRemoved += GlyphTextMarkerServiceImpl_MarkersRemoved;
			editorFormatMap.FormatMappingChanged += EditorFormatMap_FormatMappingChanged;
		}

		void SelectedMarkersInDocumentCountChanged() {
			if (TextView.IsClosed)
				return;

			// Our constructor gets called early so we could've been called from the text view's ctor
			if (TextView.Caret == null) {
				TextView.VisualElement.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(SelectedMarkersInDocumentCountChanged));
				return;
			}

			SelectedMarkersInDocumentCountChangedCore();
		}

		void SelectedMarkersInDocumentCountChangedCore() {
			if (TextView.IsClosed)
				return;
			if (markerAndSpanCollection.SelectedMarkersInDocumentCount > 0) {
				if (!hasHookedCaretPositionChanged) {
					hasHookedCaretPositionChanged = true;
					TextView.Caret.PositionChanged += Caret_PositionChanged;
				}
			}
			else {
				if (hasHookedCaretPositionChanged) {
					hasHookedCaretPositionChanged = false;
					TextView.Caret.PositionChanged -= Caret_PositionChanged;
				}
			}
		}
		bool hasHookedCaretPositionChanged;

		void Caret_PositionChanged(object sender, CaretPositionChangedEventArgs e) {
			Debug.Assert(markerAndSpanCollection.SelectedMarkersInDocumentCount > 0);
			var oldMarkers = GetMarkers(e.OldPosition.VirtualBufferPosition);
			var newMarkers = GetMarkers(e.NewPosition.VirtualBufferPosition);
			if (oldMarkers != null) {
				foreach (var marker in oldMarkers) {
					if (ExistsIn(newMarkers, marker))
						continue;
					Refresh(marker);
				}
			}
			if (newMarkers != null) {
				foreach (var marker in newMarkers) {
					if (ExistsIn(oldMarkers, marker))
						continue;
					Refresh(marker);
				}
			}
		}

		bool ExistsIn(List<IGlyphTextMarkerImpl> list, IGlyphTextMarkerImpl marker) {
			if (list == null)
				return false;
			foreach (var m in list) {
				if (m == marker)
					return true;
			}
			return false;
		}

		List<IGlyphTextMarkerImpl> GetMarkers(VirtualSnapshotPoint virtualBufferPosition) {
			if (virtualBufferPosition.VirtualSpaces > 0)
				return null;
			var pos = virtualBufferPosition.Position.TranslateTo(TextView.TextSnapshot, PointTrackingMode.Negative);
			var spans = new NormalizedSnapshotSpanCollection(new SnapshotSpan(pos, 0));
			List<IGlyphTextMarkerImpl> list = null;
			foreach (var info in GetMarkers(spans, startOfSpanOnly: false)) {
				if (list == null)
					list = new List<IGlyphTextMarkerImpl>();
				list.Add(info.Marker);
			}
			return list;
		}

		void Initialize() {
			glyphTextMarkerTagAggregator = glyphTextMarkerServiceImpl.ViewTagAggregatorFactoryService.CreateTagAggregator<IGlyphTextMarkerTag>(TextView);
			glyphTextMarkerTagAggregator.BatchedTagsChanged += GlyphTextMarkerTagAggregator_BatchedTagsChanged;

			foreach (var marker in glyphTextMarkerServiceImpl.AllMarkers) {
				if (marker.TextViewFilter(TextView))
					markerAndSpanCollection.Add(marker, null);
			}

			if (glyphTextMarkerTagAggregatorWasNull)
				InvalidateEverything();
		}
		bool glyphTextMarkerTagAggregatorWasNull;

		internal void AddGlyphTextMarkerListener(IGlyphTextMarkerListener listener) {
			if (glyphTextMarkerListener != null)
				throw new InvalidOperationException("Only one instance is supported");
			glyphTextMarkerListener = listener ?? throw new ArgumentNullException(nameof(listener));
		}

		internal void RemoveGlyphTextMarkerListener(IGlyphTextMarkerListener listener) {
			if (listener == null)
				throw new ArgumentNullException(nameof(listener));
			if (glyphTextMarkerListener != listener)
				throw new ArgumentException();
			glyphTextMarkerListener = null;
		}

		sealed class MarkerElement : UIElement {
			readonly Geometry geometry;

			public Brush BackgroundBrush {
				get { return backgroundBrush; }
				set {
					if (!BrushComparer.Equals(value, backgroundBrush)) {
						backgroundBrush = value;
						InvalidateVisual();
					}
				}
			}
			Brush backgroundBrush;

			public Pen Pen {
				get { return pen; }
				set {
					if (pen != value) {
						pen = value;
						InvalidateVisual();
					}
				}
			}
			Pen pen;

			public SnapshotSpan Span { get; }
			public string FormatType { get; }
			public string Type { get; }
			public string SelectedType { get; }
			public int ZIndex { get; }

			public MarkerElement(SnapshotSpan span, string formatType, string type, string selectedType, int zIndex, Geometry geometry) {
				if (span.Snapshot == null)
					throw new ArgumentException();
				Span = span;
				FormatType = formatType;
				Type = type ?? throw new ArgumentNullException(nameof(type));
				SelectedType = selectedType;
				ZIndex = zIndex;
				this.geometry = geometry ?? throw new ArgumentNullException(nameof(geometry));
				Panel.SetZIndex(this, zIndex);
			}

			protected override void OnRender(DrawingContext drawingContext) {
				base.OnRender(drawingContext);
				drawingContext.DrawGeometry(BackgroundBrush, Pen, geometry);
			}
		}

		void EditorFormatMap_FormatMappingChanged(object sender, FormatItemsEventArgs e) {
			if (TextView.IsClosed)
				return;
			if (markerElements.Count == 0)
				return;

			bool refresh = false;
			if (e.ChangedItems.Count > 50)
				refresh = true;
			if (!refresh) {
				var hash = new HashSet<string>(StringComparer.Ordinal);
				foreach (var elem in markerElements) {
					hash.Add(elem.Type);
					if (elem.SelectedType != null)
						hash.Add(elem.SelectedType);
				}
				foreach (var s in e.ChangedItems) {
					if (hash.Contains(s)) {
						refresh = true;
						break;
					}
				}
			}

			if (refresh)
				RefreshExistingMarkers();
		}

		void RefreshExistingMarkers() {
			foreach (var markerElement in markerElements) {
				var props = editorFormatMap.GetProperties(markerElement.FormatType);
				markerElement.BackgroundBrush = GetBackgroundBrush(props);
				markerElement.Pen = GetPen(props);
			}
		}

		void GlyphTextMarkerTagAggregator_BatchedTagsChanged(object sender, BatchedTagsChangedEventArgs e) {
			TextView.VisualElement.Dispatcher.VerifyAccess();
			if (TextView.IsClosed)
				return;
			List<SnapshotSpan> intersectionSpans = null;
			foreach (var mappingSpan in e.Spans) {
				foreach (var span in mappingSpan.GetSpans(TextView.TextSnapshot)) {
					var intersection = TextView.TextViewLines.FormattedSpan.Intersection(span);
					if (intersection != null) {
						if (intersectionSpans == null)
							intersectionSpans = new List<SnapshotSpan>();
						intersectionSpans.Add(intersection.Value);
					}
				}
			}
			if (intersectionSpans != null)
				UpdateRange(new NormalizedSnapshotSpanCollection(intersectionSpans));
		}

		void RemoveMarkerElements(NormalizedSnapshotSpanCollection spans) {
			for (int i = markerElements.Count - 1; i >= 0; i--) {
				var markerElement = markerElements[i];
				if (spans.IntersectsWith(markerElement.Span))
					markerLayer.RemoveAdornment(markerElement);
			}
		}

		void AddMarkerElements(NormalizedSnapshotSpanCollection spans) {
			Debug.Assert(glyphTextMarkerTagAggregator != null);
			if (glyphTextMarkerTagAggregator == null) {
				glyphTextMarkerTagAggregatorWasNull = true;
				return;
			}
			foreach (var tag in glyphTextMarkerTagAggregator.GetTags(spans)) {
				if (tag.Tag?.MarkerTypeName == null)
					continue;
				foreach (var span in tag.Span.GetSpans(TextView.TextSnapshot)) {
					if (!span.IntersectsWith(TextView.TextViewLines.FormattedSpan))
						continue;
					var markerElement = TryCreateMarkerElement(span, tag.Tag);
					if (markerElement == null)
						continue;
					bool added = markerLayer.AddAdornment(AdornmentPositioningBehavior.TextRelative, markerElement.Span, null, markerElement, onRemovedDelegate);
					if (added)
						markerElements.Add(markerElement);
				}
			}
		}

		readonly AdornmentRemovedCallback onRemovedDelegate;
		void OnRemoved(object tag, UIElement element) => markerElements.Remove((MarkerElement)element);

		void UpdateRange(NormalizedSnapshotSpanCollection spans) {
			if (spans.Count == 1 && spans[0].Start.Position == 0 && spans[0].Length == spans[0].Snapshot.Length)
				RemoveAllMarkerElements();
			else
				RemoveMarkerElements(spans);
			AddMarkerElements(spans);
		}

		void RemoveAllMarkerElements() {
			// Clear this first so the remove-callback won't try to remove anything from this list (it'll be empty!)
			markerElements.Clear();
			markerLayer.RemoveAllAdornments();
		}

		Brush GetBackgroundBrush(ResourceDictionary props) {
			Color? color;
			SolidColorBrush scBrush;
			Brush fillBrush;

			const double BG_BRUSH_OPACITY = 0.8;
			const double BG_BRUSH_HIGHCONTRAST_OPACITY = 0.5;
			Brush newBrush;
			if ((color = props[EditorFormatDefinition.BackgroundColorId] as Color?) != null) {
				newBrush = new SolidColorBrush(color.Value);
				newBrush.Opacity = BG_BRUSH_OPACITY;
				newBrush.Freeze();
			}
			else if ((scBrush = props[EditorFormatDefinition.BackgroundBrushId] as SolidColorBrush) != null) {
				newBrush = new SolidColorBrush(scBrush.Color);
				newBrush.Opacity = BG_BRUSH_OPACITY;
				newBrush.Freeze();
			}
			else if ((fillBrush = props[MarkerFormatDefinition.FillId] as Brush) != null) {
				newBrush = fillBrush;
				if (newBrush.CanFreeze)
					newBrush.Freeze();
			}
			else
				return null;

			if (useReducedOpacityForHighContrast && glyphTextMarkerServiceImpl.ThemeService.Theme.IsHighContrast) {
				newBrush = newBrush.Clone();
				newBrush.Opacity = BG_BRUSH_HIGHCONTRAST_OPACITY;
				if (newBrush.CanFreeze)
					newBrush.Freeze();
			}

			return newBrush;
		}

		Pen GetPen(ResourceDictionary props) {
			Color? color;
			SolidColorBrush scBrush;

			const double PEN_THICKNESS = 1;
			Pen newPen;
			if ((color = props[EditorFormatDefinition.ForegroundColorId] as Color?) != null) {
				var brush = new SolidColorBrush(color.Value);
				brush.Freeze();
				newPen = new Pen(brush, PEN_THICKNESS);
				newPen.Freeze();
			}
			else if ((scBrush = props[EditorFormatDefinition.ForegroundBrushId] as SolidColorBrush) != null) {
				if (scBrush.CanFreeze)
					scBrush.Freeze();
				newPen = new Pen(scBrush, PEN_THICKNESS);
				newPen.Freeze();
			}
			else if ((newPen = props[MarkerFormatDefinition.BorderId] as Pen) != null) {
				if (newPen.CanFreeze)
					newPen.Freeze();
			}

			return newPen;
		}

		MarkerElement TryCreateMarkerElement(SnapshotSpan span, IGlyphTextMarkerTag tag) {
			Debug.Assert(tag.MarkerTypeName != null);
			var geo = TextView.TextViewLines.GetMarkerGeometry(span);
			if (geo == null)
				return null;

			var type = tag.MarkerTypeName ?? string.Empty;
			var selectedType = tag.SelectedMarkerTypeName;
			var propName = (selectedType != null && IsSelected(span) ? selectedType : type) ?? type;
			var props = editorFormatMap.GetProperties(propName);
			var markerElement = new MarkerElement(span, propName, type, selectedType, tag.ZIndex, geo);
			markerElement.BackgroundBrush = GetBackgroundBrush(props);
			markerElement.Pen = GetPen(props);
			return markerElement;
		}

		bool IsSelected(SnapshotSpan span) {
			var pos = TextView.Caret.Position.VirtualBufferPosition;
			if (pos.VirtualSpaces > 0)
				return false;
			return span.Start <= pos.Position && pos.Position <= span.End;
		}

		void WpfTextView_LayoutChanged(object sender, TextViewLayoutChangedEventArgs e) => UpdateLines(e.NewOrReformattedLines);
		void UpdateLines(IList<ITextViewLine> newOrReformattedLines) {
			if (newOrReformattedLines.Count == TextView.TextViewLines.Count)
				RemoveAllMarkerElements();

			var lineSpans = new List<SnapshotSpan>();
			foreach (var line in newOrReformattedLines)
				lineSpans.Add(line.ExtentIncludingLineBreak);
			var spans = new NormalizedSnapshotSpanCollection(lineSpans);
			UpdateRange(spans);
		}

		public static GlyphTextViewMarkerService GetOrCreate(IGlyphTextMarkerServiceImpl glyphTextMarkerServiceImpl, IWpfTextView wpfTextView) {
			if (glyphTextMarkerServiceImpl == null)
				throw new ArgumentNullException(nameof(glyphTextMarkerServiceImpl));
			if (wpfTextView == null)
				throw new ArgumentNullException(nameof(wpfTextView));
			if (wpfTextView.TextBuffer.Properties.TryGetProperty(typeof(GlyphTextViewMarkerService), out GlyphTextViewMarkerService service))
				return service;
			service = new GlyphTextViewMarkerService(glyphTextMarkerServiceImpl, wpfTextView);
			wpfTextView.TextBuffer.Properties.AddProperty(typeof(GlyphTextViewMarkerService), service);
			service.Initialize();
			return service;
		}

		public static IGlyphTextViewMarkerService TryGet(ITextView textView) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			textView.TextBuffer.Properties.TryGetProperty(typeof(GlyphTextViewMarkerService), out GlyphTextViewMarkerService service);
			return service;
		}

		void IGlyphTextViewMarkerService.SetMethodOffsetSpanMap(IMethodOffsetSpanMap map) {
			if (methodOffsetSpanMap == map)
				return;
			methodOffsetSpanMap = map;
			if (markerAndSpanCollection.Count != 0) {
				markerAndSpanCollection.UpdateSpans(methodOffsetSpanMap);
				InvalidateEverything();
			}
		}

		void InvalidateEverything() {
			var snapshot = TextView.TextSnapshot;
			var span = new SnapshotSpan(snapshot, 0, snapshot.Length);
			glyphTextViewMarkerGlyphTagTagger.RaiseTagsChanged(span);
			glyphTextViewMarkerClassificationTagTagger.RaiseTagsChanged(span);
			glyphTextViewMarkerGlyphTextMarkerTagTagger.RaiseTagsChanged(span);
		}

		void Refresh(IGlyphTextMarkerImpl marker) {
			if (!marker.TextViewFilter(TextView))
				return;
			if (marker is IGlyphTextMethodMarkerImpl methodMarker) {
				Refresh(methodMarker);
				return;
			}

			Debug.Fail("Unknown marker type: " + marker.GetType());
		}

		void Refresh(IGlyphTextMethodMarkerImpl marker) {
			var span = GetSnapshotSpan(marker);
			if (span == null)
				return;
			Refresh(marker, span.Value);
		}

		void Refresh(IGlyphTextMarker marker, SnapshotSpan span) {
			if (!TextView.TextViewLines.FormattedSpan.IntersectsWith(span))
				return;
			if (marker.GlyphImageReference != null)
				glyphTextViewMarkerGlyphTagTagger.RaiseTagsChanged(span);
			if (marker.ClassificationType != null)
				glyphTextViewMarkerClassificationTagTagger.RaiseTagsChanged(span);
			if (marker.MarkerTypeName != null)
				glyphTextViewMarkerGlyphTextMarkerTagTagger.RaiseTagsChanged(span);
		}

		SnapshotSpan? GetSnapshotSpan(IGlyphTextMarker marker) {
			if (marker is IGlyphTextMethodMarker methodMarker)
				return GetSnapshotSpan(methodMarker);

			Debug.Fail("Unknown marker type: " + marker.GetType());
			return null;
		}

		SnapshotSpan? GetSnapshotSpan(IGlyphTextMethodMarker marker) {
			if (methodOffsetSpanMap == null)
				return null;
			var span = methodOffsetSpanMap.ToSpan(marker.Method, marker.ILOffset);
			if (span == null)
				return null;
			var snapshot = TextView.TextSnapshot;
			if (span.Value.End > snapshot.Length)
				return null;
			return new SnapshotSpan(snapshot, span.Value);
		}

		void Options_OptionChanged(object sender, EditorOptionChangedEventArgs e) {
			if (e.OptionId == DefaultWpfViewOptions.UseReducedOpacityForHighContrastOptionName) {
				useReducedOpacityForHighContrast = TextView.Options.GetOptionValue(DefaultWpfViewOptions.UseReducedOpacityForHighContrastOptionId);
				if (glyphTextMarkerServiceImpl.ThemeService.Theme.IsHighContrast)
					RefreshExistingMarkers();
			}
		}

		void GlyphTextMarkerServiceImpl_MarkerAdded(object sender, GlyphTextMarkerAddedEventArgs e) {
			if (TextView.IsClosed)
				return;
			if (!e.Marker.TextViewFilter(TextView))
				return;
			markerAndSpanCollection.Add(e.Marker, GetSnapshotSpan(e.Marker)?.Span);
			glyphTextMarkerListener?.OnAdded(new[] { e.Marker });
			Refresh(e.Marker);
		}

		void GlyphTextMarkerServiceImpl_MarkerRemoved(object sender, GlyphTextMarkerRemovedEventArgs e) {
			if (TextView.IsClosed)
				return;
			bool removed = markerAndSpanCollection.Remove(e.Marker);
			Debug.Assert(removed == e.Marker.TextViewFilter(TextView));
			if (removed) {
				Refresh(e.Marker);
				glyphTextMarkerListener?.OnRemoved(new[] { e.Marker });
			}
		}

		void GlyphTextMarkerServiceImpl_MarkersRemoved(object sender, GlyphTextMarkersRemovedEventArgs e) {
			if (TextView.IsClosed)
				return;
			bool removed = markerAndSpanCollection.Remove(e.Markers);
			if (!removed)
				return;
			glyphTextMarkerListener?.OnRemoved(e.Markers);
			if (e.Markers.Count > 10)
				InvalidateEverything();
			else {
				foreach (var marker in e.Markers)
					Refresh(marker);
			}
		}

		public UIElement GenerateGlyph(IWpfTextViewLine line, GlyphTextMarkerGlyphTag glyphTag) {
			if (line == null)
				throw new ArgumentNullException(nameof(line));
			if (glyphTag == null)
				throw new ArgumentNullException(nameof(glyphTag));
			Debug.Assert(!glyphTag.ImageReference.IsDefault);
			if (glyphTag.ImageReference.IsDefault)
				return null;

			const double DEFAULT_IMAGE_LENGTH = 16;
			const double EXTRA_LENGTH = 2;
			double imageLength = Math.Min(DEFAULT_IMAGE_LENGTH, line.Height + EXTRA_LENGTH);

			var image = new DsImage {
				Width = imageLength,
				Height = imageLength,
				ImageReference = glyphTag.ImageReference,
			};
			Panel.SetZIndex(image, glyphTag.ZIndex);
			return image;
		}

		struct MarkerAndSpan {
			public SnapshotSpan Span { get; }
			public IGlyphTextMarkerImpl Marker { get; }
			public MarkerAndSpan(SnapshotSpan span, IGlyphTextMarkerImpl marker) {
				Span = span;
				Marker = marker;
			}
		}

		IEnumerable<MarkerAndSpan> GetMarkers(NormalizedSnapshotSpanCollection spans, bool startOfSpanOnly) {
			if (TextView.IsClosed)
				yield break;
			if (markerAndSpanCollection.CountInDocument == 0)
				yield break;
			if (spans.Count == 0)
				yield break;
			var snapshot = spans[0].Snapshot;
			foreach (var span in spans) {
				if (startOfSpanOnly) {
					foreach (var info in markerAndSpanCollection.GetMarkerAndSpans(snapshot, span.Span)) {
						if (info.Span.Start >= span.Start && info.Span.Start < span.End)
							yield return info;
					}
				}
				else {
					foreach (var info in markerAndSpanCollection.GetMarkerAndSpans(snapshot, span.Span))
						yield return info;
				}
			}
		}

		public IEnumerable<ITagSpan<IClassificationTag>> GetClassificationTags(NormalizedSnapshotSpanCollection spans) {
			foreach (var info in GetMarkers(spans, startOfSpanOnly: false)) {
				var ct = info.Marker.ClassificationType;
				if (ct != null)
					yield return new TagSpan<IClassificationTag>(info.Span, new ClassificationTag(ct));
			}
		}

		public IEnumerable<ITagSpan<GlyphTextMarkerGlyphTag>> GetGlyphTextMarkerGlyphTags(NormalizedSnapshotSpanCollection spans) {
			foreach (var info in GetMarkers(spans, startOfSpanOnly: true)) {
				var imgRef = info.Marker.GlyphImageReference;
				if (imgRef != null)
					yield return new TagSpan<GlyphTextMarkerGlyphTag>(info.Span, new GlyphTextMarkerGlyphTag(imgRef.Value, info.Marker.ZIndex));
			}
		}

		public IEnumerable<ITagSpan<IGlyphTextMarkerTag>> GetGlyphTextMarkerTags(NormalizedSnapshotSpanCollection spans) {
			foreach (var info in GetMarkers(spans, startOfSpanOnly: false)) {
				var markerTypeName = info.Marker.MarkerTypeName;
				if (markerTypeName != null)
					yield return new TagSpan<GlyphTextMarkerTag>(info.Span, new GlyphTextMarkerTag(markerTypeName, info.Marker.SelectedMarkerTypeName, info.Marker.ZIndex));
			}
		}

		internal IGlyphTextMarkerImpl[] GetSortedGlyphTextMarkers(IWpfTextViewLine line) {
			List<IGlyphTextMarkerImpl> markers = null;

			if (markerAndSpanCollection.CountInDocument != 0) {
				var spans = new NormalizedSnapshotSpanCollection(line.Extent);
				foreach (var info in GetMarkers(spans, true)) {
					if (markers == null)
						markers = new List<IGlyphTextMarkerImpl>();
					markers.Add(info.Marker);
				}
			}
			if (markers != null) {
				if (markers.Count == 1)
					return new[] { markers[0] };
				return markers.OrderByDescending(a => a.ZIndex).ToArray();
			}

			return Array.Empty<IGlyphTextMarkerImpl>();
		}

		internal IWpfTextViewLine GetVisibleLine(IGlyphTextMarkerImpl marker) {
			if (marker == null)
				throw new ArgumentNullException(nameof(marker));

			if (marker is IGlyphTextMethodMarkerImpl methodMarker)
				return GetVisibleLine(methodMarker);

			Debug.Fail("Unknown marker type: " + marker.GetType());
			return null;
		}

		IWpfTextViewLine GetVisibleLine(IGlyphTextMethodMarkerImpl marker) {
			var span = GetSnapshotSpan(marker);
			if (span == null)
				return null;
			var line = TextView.TextViewLines.GetTextViewLineContainingBufferPosition(span.Value.Start);
			var wpfLine = line as IWpfTextViewLine;
			Debug.Assert((line != null) == (wpfLine != null));
			if (wpfLine == null || !wpfLine.IsVisible())
				return null;
			return wpfLine;
		}

		void WpfTextView_Closed(object sender, EventArgs e) {
			TextView.Closed -= WpfTextView_Closed;
			TextView.LayoutChanged -= WpfTextView_LayoutChanged;
			TextView.Options.OptionChanged -= Options_OptionChanged;
			TextView.Caret.PositionChanged -= Caret_PositionChanged;
			glyphTextMarkerServiceImpl.MarkerAdded -= GlyphTextMarkerServiceImpl_MarkerAdded;
			glyphTextMarkerServiceImpl.MarkerRemoved -= GlyphTextMarkerServiceImpl_MarkerRemoved;
			glyphTextMarkerServiceImpl.MarkersRemoved -= GlyphTextMarkerServiceImpl_MarkersRemoved;
			editorFormatMap.FormatMappingChanged -= EditorFormatMap_FormatMappingChanged;
			Debug.Assert(glyphTextMarkerTagAggregator != null);
			if (glyphTextMarkerTagAggregator != null) {
				glyphTextMarkerTagAggregator.BatchedTagsChanged -= GlyphTextMarkerTagAggregator_BatchedTagsChanged;
				glyphTextMarkerTagAggregator.Dispose();
			}
			RemoveAllMarkerElements();
			markerAndSpanCollection.Dispose();
			markerLayer.RemoveAllAdornments();
		}
	}
}
