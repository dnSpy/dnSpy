/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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

			public int Count => allMarkers.Count;
			public int CountInDocument => inDocMarkers.Count;

			public MarkerAndSpanCollection() {
				this.allMarkers = new List<IGlyphTextMarkerImpl>();
				this.inDocMarkers = new Dictionary<IGlyphTextMarkerImpl, Span>();
			}

			public void UpdateSpans(IMethodOffsetSpanMap map) {
				inDocMarkers.Clear();
				if (map != null) {
					var allMarkers = this.allMarkers;
					for (int i = 0; i < allMarkers.Count; i++) {
						var methodMarker = allMarkers[i] as IGlyphTextMethodMarkerImpl;
						if (methodMarker != null) {
							var span = map.ToSpan(methodMarker.Method, methodMarker.ILOffset);
							if (span != null)
								inDocMarkers.Add(methodMarker, span.Value);
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
				if (span != null)
					inDocMarkers.Add(marker, span.Value);
			}

			public void Remove(IGlyphTextMarkerImpl marker) {
				for (int i = 0; i < allMarkers.Count; i++) {
					if (allMarkers[i] == marker) {
						allMarkers.RemoveAt(i);
						inDocMarkers.Remove(marker);
						return;
					}
				}
				Debug.Fail("Failed to remove marker");
			}

			public void Remove(HashSet<IGlyphTextMarkerImpl> markers) {
				int removed = 0;
				for (int i = allMarkers.Count - 1; i >= 0; i--) {
					var marker = allMarkers[i];
					if (markers.Contains(marker)) {
						allMarkers.RemoveAt(i);
						inDocMarkers.Remove(marker);
						removed++;
						if (removed >= markers.Count)
							break;
					}
				}
			}

			public void Clear() {
				allMarkers.Clear();
				inDocMarkers.Clear();
			}
		}

		GlyphTextViewMarkerService(IGlyphTextMarkerServiceImpl glyphTextMarkerServiceImpl, IWpfTextView wpfTextView, IEnumerable<IGlyphTextMarkerImpl> allMarkers) {
			if (glyphTextMarkerServiceImpl == null)
				throw new ArgumentNullException(nameof(glyphTextMarkerServiceImpl));
			if (wpfTextView == null)
				throw new ArgumentNullException(nameof(wpfTextView));
			this.onRemovedDelegate = OnRemoved;
			this.glyphTextMarkerServiceImpl = glyphTextMarkerServiceImpl;
			this.TextView = wpfTextView;
			this.markerLayer = wpfTextView.GetAdornmentLayer(PredefinedDnSpyAdornmentLayers.GlyphTextMarker);
			this.markerAndSpanCollection = new MarkerAndSpanCollection();
			this.markerElements = new List<MarkerElement>();
			this.editorFormatMap = glyphTextMarkerServiceImpl.EditorFormatMapService.GetEditorFormatMap(wpfTextView);
			this.glyphTextViewMarkerGlyphTagTagger = GlyphTextViewMarkerGlyphTagger.GetOrCreate(this);
			this.glyphTextViewMarkerGlyphTextMarkerTagTagger = GlyphTextViewMarkerGlyphTextMarkerTagger.GetOrCreate(this);
			this.glyphTextViewMarkerClassificationTagTagger = GlyphTextViewMarkerClassificationTagger.GetOrCreate(this);
			this.useReducedOpacityForHighContrast = wpfTextView.Options.GetOptionValue(DefaultWpfViewOptions.UseReducedOpacityForHighContrastOptionId);
			wpfTextView.Closed += WpfTextView_Closed;
			wpfTextView.LayoutChanged += WpfTextView_LayoutChanged;
			wpfTextView.Options.OptionChanged += Options_OptionChanged;
			glyphTextMarkerServiceImpl.MarkerAdded += GlyphTextMarkerServiceImpl_MarkerAdded;
			glyphTextMarkerServiceImpl.MarkerRemoved += GlyphTextMarkerServiceImpl_MarkerRemoved;
			glyphTextMarkerServiceImpl.MarkersRemoved += GlyphTextMarkerServiceImpl_MarkersRemoved;
			editorFormatMap.FormatMappingChanged += EditorFormatMap_FormatMappingChanged;
			foreach (var marker in allMarkers) {
				if (marker.TextViewFilter(TextView))
					markerAndSpanCollection.Add(marker, null);
			}
		}

		void Initialize() {
			glyphTextMarkerTagAggregator = glyphTextMarkerServiceImpl.ViewTagAggregatorFactoryService.CreateTagAggregator<IGlyphTextMarkerTag>(TextView);
			glyphTextMarkerTagAggregator.BatchedTagsChanged += GlyphTextMarkerTagAggregator_BatchedTagsChanged;
			if (glyphTextMarkerTagAggregatorWasNull)
				InvalidateEverything();
		}
		bool glyphTextMarkerTagAggregatorWasNull;

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
			public string Type { get; }
			public int ZIndex { get; }

			public MarkerElement(SnapshotSpan span, string type, int zIndex, Geometry geometry) {
				if (span.Snapshot == null)
					throw new ArgumentException();
				if (type == null)
					throw new ArgumentNullException(nameof(type));
				if (geometry == null)
					throw new ArgumentNullException(nameof(geometry));
				Span = span;
				Type = type;
				ZIndex = zIndex;
				this.geometry = geometry;
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
				foreach (var elem in markerElements)
					hash.Add(elem.Type);
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
				var props = editorFormatMap.GetProperties(markerElement.Type);
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

			if (useReducedOpacityForHighContrast && glyphTextMarkerServiceImpl.ThemeManager.Theme.IsHighContrast) {
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
			var props = editorFormatMap.GetProperties(type);
			var markerElement = new MarkerElement(span, type, tag.ZIndex, geo);
			markerElement.BackgroundBrush = GetBackgroundBrush(props);
			markerElement.Pen = GetPen(props);
			return markerElement;
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
			GlyphTextViewMarkerService service;
			if (wpfTextView.TextBuffer.Properties.TryGetProperty(typeof(GlyphTextViewMarkerService), out service))
				return service;
			service = new GlyphTextViewMarkerService(glyphTextMarkerServiceImpl, wpfTextView, glyphTextMarkerServiceImpl.AllMarkers);
			wpfTextView.TextBuffer.Properties.AddProperty(typeof(GlyphTextViewMarkerService), service);
			service.Initialize();
			return service;
		}

		public static IGlyphTextViewMarkerService TryGet(ITextView textView) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			GlyphTextViewMarkerService service;
			textView.TextBuffer.Properties.TryGetProperty(typeof(GlyphTextViewMarkerService), out service);
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

		void Refresh(IGlyphTextMarker marker) {
			var methodMarker = marker as IGlyphTextMethodMarker;
			if (methodMarker != null) {
				Refresh(methodMarker);
				return;
			}

			Debug.Fail("Unknown marker type: " + marker.GetType());
		}

		void Refresh(IGlyphTextMethodMarker marker) {
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
			var methodMarker = marker as IGlyphTextMethodMarker;
			if (methodMarker != null)
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
			if (e.OptionId == DefaultWpfViewOptions.UseReducedOpacityForHighContrastOptionId.Name) {
				useReducedOpacityForHighContrast = TextView.Options.GetOptionValue(DefaultWpfViewOptions.UseReducedOpacityForHighContrastOptionId);
				if (glyphTextMarkerServiceImpl.ThemeManager.Theme.IsHighContrast)
					RefreshExistingMarkers();
			}
		}

		void GlyphTextMarkerServiceImpl_MarkerAdded(object sender, GlyphTextMarkerAddedEventArgs e) {
			if (TextView.IsClosed)
				return;
			if (!e.Marker.TextViewFilter(TextView))
				return;
			markerAndSpanCollection.Add(e.Marker, GetSnapshotSpan(e.Marker)?.Span);
			Refresh(e.Marker);
		}

		void GlyphTextMarkerServiceImpl_MarkerRemoved(object sender, GlyphTextMarkerRemovedEventArgs e) {
			if (TextView.IsClosed)
				return;
			markerAndSpanCollection.Remove(e.Marker);
			Refresh(e.Marker);
		}

		void GlyphTextMarkerServiceImpl_MarkersRemoved(object sender, GlyphTextMarkersRemovedEventArgs e) {
			if (TextView.IsClosed)
				return;
			markerAndSpanCollection.Remove(e.Markers);
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
			Debug.Assert(glyphTag.ImageReference.Assembly != null && glyphTag.ImageReference.Name != null);
			if (glyphTag.ImageReference.Assembly == null || glyphTag.ImageReference.Name == null)
				return null;

			var source = glyphTextMarkerServiceImpl.ImageManager.GetImage(glyphTag.ImageReference, BackgroundType.GlyphMargin);
			const double DEFAULT_IMAGE_LENGTH = 16;
			const double EXTRA_LENGTH = 2;
			double imageLength = Math.Min(DEFAULT_IMAGE_LENGTH, line.Height + EXTRA_LENGTH);
			var image = new Image {
				Width = imageLength,
				Height = imageLength,
				Source = source,
			};
			Panel.SetZIndex(image, glyphTag.ZIndex);
			return image;
		}

		struct MarkerAndSpan {
			public SnapshotSpan Span { get; }
			public IGlyphTextMarker Marker { get; }
			public MarkerAndSpan(SnapshotSpan span, IGlyphTextMarker marker) {
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
			foreach (var info in GetMarkers(spans, false)) {
				var ct = info.Marker.ClassificationType;
				if (ct != null)
					yield return new TagSpan<IClassificationTag>(info.Span, new ClassificationTag(ct));
			}
		}

		public IEnumerable<ITagSpan<GlyphTextMarkerGlyphTag>> GlyphTextViewMarkerGlyphTags(NormalizedSnapshotSpanCollection spans) {
			foreach (var info in GetMarkers(spans, true)) {
				var imgRef = info.Marker.GlyphImageReference;
				if (imgRef != null)
					yield return new TagSpan<GlyphTextMarkerGlyphTag>(info.Span, new GlyphTextMarkerGlyphTag(imgRef.Value, info.Marker.ZIndex));
			}
		}

		public IEnumerable<ITagSpan<IGlyphTextMarkerTag>> GetGlyphTextMarkerTags(NormalizedSnapshotSpanCollection spans) {
			foreach (var info in GetMarkers(spans, false)) {
				var markerTypeName = info.Marker.MarkerTypeName;
				if (markerTypeName != null)
					yield return new TagSpan<GlyphTextMarkerTag>(info.Span, new GlyphTextMarkerTag(markerTypeName, info.Marker.ZIndex));
			}
		}

		void WpfTextView_Closed(object sender, EventArgs e) {
			TextView.Closed -= WpfTextView_Closed;
			TextView.LayoutChanged -= WpfTextView_LayoutChanged;
			TextView.Options.OptionChanged -= Options_OptionChanged;
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
			markerAndSpanCollection.Clear();
			markerLayer.RemoveAllAdornments();
		}
	}
}
