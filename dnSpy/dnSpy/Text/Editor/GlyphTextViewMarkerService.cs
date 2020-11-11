/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Tagging;

namespace dnSpy.Text.Editor {
	interface IGlyphTextViewMarkerService {
		IWpfTextView TextView { get; }
		void SetDotNetSpanMap(IDotNetSpanMap? map);
	}

	interface IGlyphTextMarkerListener {
		void OnAdded(IEnumerable<IGlyphTextMarkerImpl> markers);
		void OnRemoved(IEnumerable<IGlyphTextMarkerImpl> markers);
	}

	sealed class GlyphTextViewMarkerService : IGlyphTextViewMarkerService {
		public IWpfTextView TextView { get; }

		readonly IGlyphTextMarkerServiceImpl glyphTextMarkerServiceImpl;
		readonly IAdornmentLayer markerLayer;
		ITagAggregator<IGlyphTextMarkerTag>? glyphTextMarkerTagAggregator;
		readonly GlyphTextViewMarkerGlyphTagger glyphTextViewMarkerGlyphTagTagger;
		readonly GlyphTextViewMarkerGlyphTextMarkerTagger glyphTextViewMarkerGlyphTextMarkerTagTagger;
		readonly GlyphTextViewMarkerClassificationTagger glyphTextViewMarkerClassificationTagTagger;
		IDotNetSpanMap? dotNetSpanMap;
		readonly MarkerAndSpanCollection markerAndSpanCollection;
		readonly IEditorFormatMap editorFormatMap;
		readonly List<MarkerElement> markerElements;
		bool useReducedOpacityForHighContrast;
		bool isInContrastMode;
		IGlyphTextMarkerListener? glyphTextMarkerListener;

		sealed class MarkerAndSpanCollection {
			readonly List<IGlyphTextMarkerImpl> allMarkers;
			readonly Dictionary<IGlyphTextMarkerImpl, Span> inDocMarkers;
			readonly GlyphTextViewMarkerService owner;

			public int Count => allMarkers.Count;
			public int CountInDocument => inDocMarkers.Count;

			public int SelectedMarkersInDocumentCount {
				get => selectedMarkersInDocumentCount;
				set {
					Debug.Assert(value >= 0);
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

			public void UpdateSpans(IDotNetSpanMap? map) {
				inDocMarkers.Clear();
				SelectedMarkersInDocumentCount = 0;
				if (map is not null) {
					var allMarkers = this.allMarkers;
					for (int i = 0; i < allMarkers.Count; i++) {
						Span? span;
						switch (allMarkers[i]) {
						case IGlyphTextMethodMarkerImpl methodMarker:
							span = map.ToSpan(methodMarker.Method.Module, methodMarker.Method.Token, methodMarker.ILOffset);
							if (span is not null) {
								inDocMarkers.Add(methodMarker, span.Value);
								if (methodMarker.SelectedMarkerTypeName is not null)
									SelectedMarkersInDocumentCount++;
							}
							break;

						case IGlyphTextDotNetTokenMarkerImpl tokenMarker:
							span = map.ToSpan(tokenMarker.Module, tokenMarker.Token);
							if (span is not null) {
								inDocMarkers.Add(tokenMarker, span.Value);
								if (tokenMarker.SelectedMarkerTypeName is not null)
									SelectedMarkersInDocumentCount++;
							}
							break;
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
				if (span is not null) {
					inDocMarkers.Add(marker, span.Value);
					if (marker.SelectedMarkerTypeName is not null)
						SelectedMarkersInDocumentCount++;
				}
			}

			public bool Remove(IGlyphTextMarkerImpl marker) {
				for (int i = 0; i < allMarkers.Count; i++) {
					if (allMarkers[i] == marker) {
						allMarkers.RemoveAt(i);
						if (inDocMarkers.Remove(marker)) {
							if (marker.SelectedMarkerTypeName is not null)
								SelectedMarkersInDocumentCount--;
						}
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
						if (inDocMarkers.Remove(marker)) {
							if (marker.SelectedMarkerTypeName is not null)
								SelectedMarkersInDocumentCount--;
						}
						removed++;
						if (removed >= markers.Count)
							break;
					}
				}
				return removed > 0;
			}

			public Span? GetSpan(IGlyphTextMarkerImpl marker) {
				if (inDocMarkers.TryGetValue(marker, out var span))
					return span;
				return null;
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
			isInContrastMode = wpfTextView.Options.IsInContrastMode();
			wpfTextView.Closed += WpfTextView_Closed;
			wpfTextView.LayoutChanged += WpfTextView_LayoutChanged;
			wpfTextView.Options.OptionChanged += Options_OptionChanged;
			glyphTextMarkerServiceImpl.MarkerAdded += GlyphTextMarkerServiceImpl_MarkerAdded;
			glyphTextMarkerServiceImpl.MarkerRemoved += GlyphTextMarkerServiceImpl_MarkerRemoved;
			glyphTextMarkerServiceImpl.MarkersRemoved += GlyphTextMarkerServiceImpl_MarkersRemoved;
			glyphTextMarkerServiceImpl.GetGlyphTextMarkerAndSpan += GlyphTextMarkerServiceImpl_GetGlyphTextMarkerAndSpan;
			editorFormatMap.FormatMappingChanged += EditorFormatMap_FormatMappingChanged;
		}

		void SelectedMarkersInDocumentCountChanged() {
			if (TextView.IsClosed)
				return;

			// Our constructor gets called early so we could've been called from the text view's ctor
			if (TextView.Caret is null) {
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

		void Caret_PositionChanged(object? sender, CaretPositionChangedEventArgs e) {
			Debug.Assert(markerAndSpanCollection.SelectedMarkersInDocumentCount > 0);
			var oldMarkers = GetMarkers(e.OldPosition.VirtualBufferPosition);
			var newMarkers = GetMarkers(e.NewPosition.VirtualBufferPosition);
			if (oldMarkers is not null) {
				foreach (var marker in oldMarkers) {
					if (ExistsIn(newMarkers, marker))
						continue;
					Refresh(marker);
				}
			}
			if (newMarkers is not null) {
				foreach (var marker in newMarkers) {
					if (ExistsIn(oldMarkers, marker))
						continue;
					Refresh(marker);
				}
			}
		}

		bool ExistsIn(List<IGlyphTextMarkerImpl>? list, IGlyphTextMarkerImpl marker) {
			if (list is null)
				return false;
			foreach (var m in list) {
				if (m == marker)
					return true;
			}
			return false;
		}

		List<IGlyphTextMarkerImpl>? GetMarkers(VirtualSnapshotPoint virtualBufferPosition) {
			if (virtualBufferPosition.VirtualSpaces > 0)
				return null;
			var pos = virtualBufferPosition.Position.TranslateTo(TextView.TextSnapshot, PointTrackingMode.Negative);
			var spans = new NormalizedSnapshotSpanCollection(new SnapshotSpan(pos, 0));
			List<IGlyphTextMarkerImpl>? list = null;
			foreach (var info in GetMarkers(spans, startOfSpanOnly: false)) {
				if (list is null)
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
			if (glyphTextMarkerListener is not null)
				throw new InvalidOperationException("Only one instance is supported");
			glyphTextMarkerListener = listener ?? throw new ArgumentNullException(nameof(listener));
		}

		internal void RemoveGlyphTextMarkerListener(IGlyphTextMarkerListener listener) {
			if (listener is null)
				throw new ArgumentNullException(nameof(listener));
			if (glyphTextMarkerListener != listener)
				throw new ArgumentException();
			glyphTextMarkerListener = null;
		}

		sealed class MarkerElement : UIElement {
			readonly Geometry geometry;

			public Brush? BackgroundBrush {
				get => backgroundBrush;
				set {
					if (!BrushComparer.Equals(value, backgroundBrush)) {
						backgroundBrush = value;
						InvalidateVisual();
					}
				}
			}
			Brush? backgroundBrush;

			public Pen? Pen {
				get => pen;
				set {
					if (pen != value) {
						pen = value;
						InvalidateVisual();
					}
				}
			}
			Pen? pen;

			public SnapshotSpan Span { get; }
			public string FormatType { get; }
			public string Type { get; }
			public string? SelectedType { get; }
			public int ZIndex { get; }

			public MarkerElement(SnapshotSpan span, string formatType, string type, string? selectedType, int zIndex, Geometry geometry) {
				if (span.Snapshot is null)
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

		void EditorFormatMap_FormatMappingChanged(object? sender, FormatItemsEventArgs e) {
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
					if (elem.SelectedType is not null)
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

		void GlyphTextMarkerTagAggregator_BatchedTagsChanged(object? sender, BatchedTagsChangedEventArgs e) {
			TextView.VisualElement.Dispatcher.VerifyAccess();
			if (TextView.IsClosed)
				return;
			List<SnapshotSpan>? intersectionSpans = null;
			foreach (var mappingSpan in e.Spans) {
				foreach (var span in mappingSpan.GetSpans(TextView.TextSnapshot)) {
					var intersection = TextView.TextViewLines.FormattedSpan.Intersection(span);
					if (intersection is not null) {
						if (intersectionSpans is null)
							intersectionSpans = new List<SnapshotSpan>();
						intersectionSpans.Add(intersection.Value);
					}
				}
			}
			if (intersectionSpans is not null)
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
			Debug2.Assert(glyphTextMarkerTagAggregator is not null);
			if (glyphTextMarkerTagAggregator is null) {
				glyphTextMarkerTagAggregatorWasNull = true;
				return;
			}
			foreach (var tag in glyphTextMarkerTagAggregator.GetTags(spans)) {
				if (tag.Tag?.MarkerTypeName is null)
					continue;
				foreach (var span in tag.Span.GetSpans(TextView.TextSnapshot)) {
					if (!span.IntersectsWith(TextView.TextViewLines.FormattedSpan))
						continue;
					var markerElement = TryCreateMarkerElement(span, tag.Tag);
					if (markerElement is null)
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

		bool ShouldUseHighContrastOpacity => useReducedOpacityForHighContrast && isInContrastMode;

		Brush? GetBackgroundBrush(ResourceDictionary props) {
			const double BG_BRUSH_OPACITY = 0.8;
			const double BG_BRUSH_HIGHCONTRAST_OPACITY = 0.5;
			Brush newBrush;
			if (props[EditorFormatDefinition.BackgroundColorId] is Color color) {
				newBrush = new SolidColorBrush(color);
				newBrush.Opacity = BG_BRUSH_OPACITY;
				newBrush.Freeze();
			}
			else if (props[EditorFormatDefinition.BackgroundBrushId] is SolidColorBrush scBrush) {
				newBrush = new SolidColorBrush(scBrush.Color);
				newBrush.Opacity = BG_BRUSH_OPACITY;
				newBrush.Freeze();
			}
			else if (props[MarkerFormatDefinition.FillId] is Brush fillBrush) {
				newBrush = fillBrush;
				if (newBrush.CanFreeze)
					newBrush.Freeze();
			}
			else
				return null;

			if (ShouldUseHighContrastOpacity) {
				newBrush = newBrush.Clone();
				newBrush.Opacity = BG_BRUSH_HIGHCONTRAST_OPACITY;
				if (newBrush.CanFreeze)
					newBrush.Freeze();
			}

			return newBrush;
		}

		Pen? GetPen(ResourceDictionary props) {
			const double PEN_THICKNESS = 1;
			Pen? newPen;
			if (props[EditorFormatDefinition.ForegroundColorId] is Color color) {
				var brush = new SolidColorBrush(color);
				brush.Freeze();
				newPen = new Pen(brush, PEN_THICKNESS);
				newPen.Freeze();
			}
			else if (props[EditorFormatDefinition.ForegroundBrushId] is SolidColorBrush scBrush) {
				if (scBrush.CanFreeze)
					scBrush.Freeze();
				newPen = new Pen(scBrush, PEN_THICKNESS);
				newPen.Freeze();
			}
			else if ((newPen = props[MarkerFormatDefinition.BorderId] as Pen) is not null) {
				if (newPen.CanFreeze)
					newPen.Freeze();
			}

			return newPen;
		}

		MarkerElement? TryCreateMarkerElement(SnapshotSpan span, IGlyphTextMarkerTag tag) {
			Debug2.Assert(tag.MarkerTypeName is not null);
			var geo = TextView.TextViewLines.GetMarkerGeometry(span);
			if (geo is null)
				return null;

			var type = tag.MarkerTypeName ?? string.Empty;
			var selectedType = tag.SelectedMarkerTypeName;
			var propName = (selectedType is not null && IsSelected(span) ? selectedType : type) ?? type;
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

		void WpfTextView_LayoutChanged(object? sender, TextViewLayoutChangedEventArgs e) => UpdateLines(e.NewOrReformattedLines);
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
			if (glyphTextMarkerServiceImpl is null)
				throw new ArgumentNullException(nameof(glyphTextMarkerServiceImpl));
			if (wpfTextView is null)
				throw new ArgumentNullException(nameof(wpfTextView));
			if (wpfTextView.TextBuffer.Properties.TryGetProperty(typeof(GlyphTextViewMarkerService), out GlyphTextViewMarkerService service))
				return service;
			service = new GlyphTextViewMarkerService(glyphTextMarkerServiceImpl, wpfTextView);
			wpfTextView.TextBuffer.Properties.AddProperty(typeof(GlyphTextViewMarkerService), service);
			service.Initialize();
			return service;
		}

		public static IGlyphTextViewMarkerService TryGet(ITextView textView) {
			if (textView is null)
				throw new ArgumentNullException(nameof(textView));
			textView.TextBuffer.Properties.TryGetProperty(typeof(GlyphTextViewMarkerService), out GlyphTextViewMarkerService service);
			return service;
		}

		void IGlyphTextViewMarkerService.SetDotNetSpanMap(IDotNetSpanMap? map) {
			if (dotNetSpanMap == map)
				return;
			dotNetSpanMap = map as IDotNetSpanMap;
			Debug2.Assert((map is null) == (dotNetSpanMap is null));
			if (markerAndSpanCollection.Count != 0) {
				markerAndSpanCollection.UpdateSpans(dotNetSpanMap);
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

			var span = GetSnapshotSpan(marker);
			if (span is null)
				return;
			Refresh(marker, span.Value);
		}

		void Refresh(IGlyphTextMarker marker, SnapshotSpan span) {
			bool visible = TextView.TextViewLines.FormattedSpan.IntersectsWith(span);
			if (visible && marker.GlyphImageReference is not null)
				glyphTextViewMarkerGlyphTagTagger.RaiseTagsChanged(span);
			if (marker.ClassificationType is not null)
				glyphTextViewMarkerClassificationTagTagger.RaiseTagsChanged(span);
			if (visible && marker.MarkerTypeName is not null)
				glyphTextViewMarkerGlyphTextMarkerTagTagger.RaiseTagsChanged(span);
		}

		SnapshotSpan? GetSnapshotSpan(IGlyphTextMarker marker) {
			Span? span;
			if (marker is IGlyphTextMethodMarker methodMarker)
				span = dotNetSpanMap?.ToSpan(methodMarker.Method.Module, methodMarker.Method.Token, methodMarker.ILOffset);
			else if (marker is IGlyphTextDotNetTokenMarker tokenMarker)
				span = dotNetSpanMap?.ToSpan(tokenMarker.Module, tokenMarker.Token);
			else {
				Debug.Fail("Unknown marker type: " + marker.GetType());
				return null;
			}

			if (span is null)
				return null;
			var snapshot = TextView.TextSnapshot;
			if (span.Value.End > snapshot.Length)
				return null;
			return new SnapshotSpan(snapshot, span.Value);
		}

		void Options_OptionChanged(object? sender, EditorOptionChangedEventArgs e) {
			if (e.OptionId == DefaultWpfViewOptions.UseReducedOpacityForHighContrastOptionName) {
				bool old = ShouldUseHighContrastOpacity;
				useReducedOpacityForHighContrast = TextView.Options.GetOptionValue(DefaultWpfViewOptions.UseReducedOpacityForHighContrastOptionId);
				if (old != ShouldUseHighContrastOpacity)
					RefreshExistingMarkers();
			}
			else if (e.OptionId == DefaultTextViewHostOptions.IsInContrastModeName) {
				bool old = ShouldUseHighContrastOpacity;
				isInContrastMode = TextView.Options.IsInContrastMode();
				if (old != ShouldUseHighContrastOpacity)
					RefreshExistingMarkers();
			}
		}

		void GlyphTextMarkerServiceImpl_MarkerAdded(object? sender, GlyphTextMarkerAddedEventArgs e) {
			if (TextView.IsClosed)
				return;
			if (!e.Marker.TextViewFilter(TextView))
				return;
			markerAndSpanCollection.Add(e.Marker, GetSnapshotSpan(e.Marker)?.Span);
			glyphTextMarkerListener?.OnAdded(new[] { e.Marker });
			Refresh(e.Marker);
		}

		void GlyphTextMarkerServiceImpl_MarkerRemoved(object? sender, GlyphTextMarkerRemovedEventArgs e) {
			if (TextView.IsClosed)
				return;
			bool removed = markerAndSpanCollection.Remove(e.Marker);
			Debug.Assert(removed == e.Marker.TextViewFilter(TextView));
			if (removed) {
				Refresh(e.Marker);
				glyphTextMarkerListener?.OnRemoved(new[] { e.Marker });
			}
		}

		void GlyphTextMarkerServiceImpl_MarkersRemoved(object? sender, GlyphTextMarkersRemovedEventArgs e) {
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

		void GlyphTextMarkerServiceImpl_GetGlyphTextMarkerAndSpan(object? sender, GetGlyphTextMarkerAndSpanEventArgs e) {
			if (e.TextView != TextView)
				return;
			e.Result = GetGlyphTextMarkerAndSpan(e.Span);
		}

		GlyphTextMarkerAndSpan[] GetGlyphTextMarkerAndSpan(SnapshotSpan span) {
			List<GlyphTextMarkerAndSpan>? result = null;
			foreach (var info in GetMarkers(new NormalizedSnapshotSpanCollection(span), startOfSpanOnly: false)) {
				if (result is null)
					result = new List<GlyphTextMarkerAndSpan>();
				result.Add(new GlyphTextMarkerAndSpan(info.Marker, info.Span));
			}
			if (result is null)
				return Array.Empty<GlyphTextMarkerAndSpan>();
			else {
				result.Sort((a, b) => {
					var c = a.Span.Start.Position - b.Span.Start.Position;
					if (c != 0)
						return c;
					return a.Span.Span.Length - b.Span.Span.Length;
				});
				return result.ToArray();
			}
		}

		public UIElement? GenerateGlyph(IWpfTextViewLine line, GlyphTextMarkerGlyphTag glyphTag) {
			if (line is null)
				throw new ArgumentNullException(nameof(line));
			if (glyphTag is null)
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
				if (ct is not null)
					yield return new TagSpan<IClassificationTag>(info.Span, new ClassificationTag(ct));
			}
		}

		public IEnumerable<ITagSpan<GlyphTextMarkerGlyphTag>> GetGlyphTextMarkerGlyphTags(NormalizedSnapshotSpanCollection spans) {
			foreach (var info in GetMarkers(spans, startOfSpanOnly: true)) {
				var imgRef = info.Marker.GlyphImageReference;
				if (imgRef is not null)
					yield return new TagSpan<GlyphTextMarkerGlyphTag>(info.Span, new GlyphTextMarkerGlyphTag(imgRef.Value, info.Marker.ZIndex));
			}
		}

		public IEnumerable<ITagSpan<IGlyphTextMarkerTag>> GetGlyphTextMarkerTags(NormalizedSnapshotSpanCollection spans) {
			foreach (var info in GetMarkers(spans, startOfSpanOnly: false)) {
				var markerTypeName = info.Marker.MarkerTypeName;
				if (markerTypeName is not null)
					yield return new TagSpan<GlyphTextMarkerTag>(info.Span, new GlyphTextMarkerTag(markerTypeName, info.Marker.SelectedMarkerTypeName, info.Marker.ZIndex));
			}
		}

		internal IGlyphTextMarkerImpl[] GetSortedGlyphTextMarkers(IWpfTextViewLine line) {
			List<IGlyphTextMarkerImpl>? markers = null;

			if (markerAndSpanCollection.CountInDocument != 0) {
				var spans = new NormalizedSnapshotSpanCollection(line.Extent);
				foreach (var info in GetMarkers(spans, true)) {
					if (markers is null)
						markers = new List<IGlyphTextMarkerImpl>();
					markers.Add(info.Marker);
				}
			}
			if (markers is not null) {
				if (markers.Count == 1)
					return new[] { markers[0] };
				return markers.OrderByDescending(a => a.ZIndex).ToArray();
			}

			return Array.Empty<IGlyphTextMarkerImpl>();
		}

		internal SnapshotSpan GetSpan(IGlyphTextMarker marker) {
			if (marker is null)
				throw new ArgumentNullException(nameof(marker));
			var impl = marker as IGlyphTextMarkerImpl;
			if (impl is null)
				throw new ArgumentException();
			var span = markerAndSpanCollection.GetSpan(impl) ?? new Span(0, 0);
			var snapshot = TextView.TextSnapshot;
			if (span.End <= snapshot.Length)
				return new SnapshotSpan(snapshot, span);
			return new SnapshotSpan(snapshot, 0, 0);
		}

		internal IWpfTextViewLine? GetVisibleLine(IGlyphTextMarkerImpl marker) {
			if (marker is null)
				throw new ArgumentNullException(nameof(marker));

			var span = GetSnapshotSpan(marker);
			if (span is null)
				return null;
			var line = TextView.TextViewLines.GetTextViewLineContainingBufferPosition(span.Value.Start);
			var wpfLine = line as IWpfTextViewLine;
			Debug2.Assert((line is not null) == (wpfLine is not null));
			if (wpfLine is null || !wpfLine.IsVisible())
				return null;
			return wpfLine;
		}

		void WpfTextView_Closed(object? sender, EventArgs e) {
			TextView.Closed -= WpfTextView_Closed;
			TextView.LayoutChanged -= WpfTextView_LayoutChanged;
			TextView.Options.OptionChanged -= Options_OptionChanged;
			TextView.Caret.PositionChanged -= Caret_PositionChanged;
			glyphTextMarkerServiceImpl.MarkerAdded -= GlyphTextMarkerServiceImpl_MarkerAdded;
			glyphTextMarkerServiceImpl.MarkerRemoved -= GlyphTextMarkerServiceImpl_MarkerRemoved;
			glyphTextMarkerServiceImpl.MarkersRemoved -= GlyphTextMarkerServiceImpl_MarkersRemoved;
			glyphTextMarkerServiceImpl.GetGlyphTextMarkerAndSpan -= GlyphTextMarkerServiceImpl_GetGlyphTextMarkerAndSpan;
			editorFormatMap.FormatMappingChanged -= EditorFormatMap_FormatMappingChanged;
			Debug2.Assert(glyphTextMarkerTagAggregator is not null);
			if (glyphTextMarkerTagAggregator is not null) {
				glyphTextMarkerTagAggregator.BatchedTagsChanged -= GlyphTextMarkerTagAggregator_BatchedTagsChanged;
				glyphTextMarkerTagAggregator.Dispose();
			}
			RemoveAllMarkerElements();
			markerAndSpanCollection.Dispose();
			markerLayer.RemoveAllAdornments();
		}
	}
}
