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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Classification;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Editor.OptionsExtensionMethods;
using dnSpy.Contracts.Hex.Formatting;
using dnSpy.Contracts.Hex.Tagging;
using TWPF = dnSpy.Text.WPF;
using VST = Microsoft.VisualStudio.Text;
using VSTC = Microsoft.VisualStudio.Text.Classification;
using VSTE = Microsoft.VisualStudio.Text.Editor;
using VSUTIL = Microsoft.VisualStudio.Utilities;

namespace dnSpy.Hex.Editor {
	[Export(typeof(WpfHexViewCreationListener))]
	[VSTE.TextViewRole(PredefinedHexViewRoles.Analyzable)]
	[VSTE.TextViewRole(PredefinedHexViewRoles.Interactive)]
	sealed class HexMarkerServiceWpfHexViewCreationListener : WpfHexViewCreationListener {
		readonly HexViewTagAggregatorFactoryService viewTagAggregatorFactoryService;
		readonly HexEditorFormatMapService editorFormatMapService;

		[ImportingConstructor]
		HexMarkerServiceWpfHexViewCreationListener(HexViewTagAggregatorFactoryService viewTagAggregatorFactoryService, HexEditorFormatMapService editorFormatMapService) {
			this.viewTagAggregatorFactoryService = viewTagAggregatorFactoryService;
			this.editorFormatMapService = editorFormatMapService;
		}

		public override void HexViewCreated(WpfHexView hexView) =>
			new HexMarkerService(hexView, viewTagAggregatorFactoryService.CreateTagAggregator<HexMarkerTag>(hexView), editorFormatMapService.GetEditorFormatMap(hexView));
	}

	sealed class HexMarkerService {
#pragma warning disable CS0169
		[Export(typeof(HexAdornmentLayerDefinition))]
		[VSUTIL.Name(PredefinedHexAdornmentLayers.NegativeTextMarker)]
		[VSUTIL.Order(After = PredefinedHexAdornmentLayers.BottomLayer, Before = PredefinedHexAdornmentLayers.TopLayer)]
		[VSUTIL.Order(Before = PredefinedHexAdornmentLayers.GlyphTextMarker, After = PredefinedHexAdornmentLayers.Outlining)]
		[VSUTIL.Order(Before = PredefinedHexAdornmentLayers.TextMarker)]
		[VSUTIL.Order(Before = PredefinedHexAdornmentLayers.CurrentLineHighlighter)]
		static HexAdornmentLayerDefinition? negativeTextMarkerAdornmentLayerDefinition;

		[Export(typeof(HexAdornmentLayerDefinition))]
		[VSUTIL.Name(PredefinedHexAdornmentLayers.TextMarker)]
		[VSUTIL.Order(After = PredefinedHexAdornmentLayers.BottomLayer, Before = PredefinedHexAdornmentLayers.TopLayer)]
		[VSUTIL.Order(Before = PredefinedHexAdornmentLayers.Selection, After = PredefinedHexAdornmentLayers.Outlining)]
		static HexAdornmentLayerDefinition? textMarkerAdornmentLayerDefinition;
#pragma warning restore CS0169

		readonly WpfHexView wpfHexView;
		readonly HexTagAggregator<HexMarkerTag> tagAggregator;
		readonly VSTC.IEditorFormatMap editorFormatMap;
		readonly HexAdornmentLayer textMarkerAdornmentLayer;
		readonly HexAdornmentLayer negativeTextMarkerAdornmentLayer;
		readonly List<MarkerElement> markerElements;
		bool useReducedOpacityForHighContrast;
		bool isInContrastMode;

		public HexMarkerService(WpfHexView wpfHexView, HexTagAggregator<HexMarkerTag> tagAggregator, VSTC.IEditorFormatMap editorFormatMap) {
			this.wpfHexView = wpfHexView ?? throw new ArgumentNullException(nameof(wpfHexView));
			this.tagAggregator = tagAggregator ?? throw new ArgumentNullException(nameof(tagAggregator));
			this.editorFormatMap = editorFormatMap ?? throw new ArgumentNullException(nameof(editorFormatMap));
			textMarkerAdornmentLayer = wpfHexView.GetAdornmentLayer(PredefinedHexAdornmentLayers.TextMarker);
			negativeTextMarkerAdornmentLayer = wpfHexView.GetAdornmentLayer(PredefinedHexAdornmentLayers.NegativeTextMarker);
			markerElements = new List<MarkerElement>();
			useReducedOpacityForHighContrast = wpfHexView.Options.GetOptionValue(DefaultWpfHexViewOptions.UseReducedOpacityForHighContrastOptionId);
			isInContrastMode = wpfHexView.Options.IsInContrastMode();
			onRemovedDelegate = OnRemoved;
			wpfHexView.Closed += WpfHexView_Closed;
			wpfHexView.LayoutChanged += WpfHexView_LayoutChanged;
			wpfHexView.Options.OptionChanged += Options_OptionChanged;
			tagAggregator.BatchedTagsChanged += TagAggregator_BatchedTagsChanged;
			editorFormatMap.FormatMappingChanged += EditorFormatMap_FormatMappingChanged;
		}

		void Options_OptionChanged(object? sender, VSTE.EditorOptionChangedEventArgs e) {
			if (e.OptionId == DefaultWpfHexViewOptions.UseReducedOpacityForHighContrastOptionName) {
				bool old = ShouldUseHighContrastOpacity;
				useReducedOpacityForHighContrast = wpfHexView.Options.GetOptionValue(DefaultWpfHexViewOptions.UseReducedOpacityForHighContrastOptionId);
				if (old != ShouldUseHighContrastOpacity)
					RefreshExistingMarkers();
			}
			else if (e.OptionId == DefaultHexViewHostOptions.IsInContrastModeName) {
				bool old = ShouldUseHighContrastOpacity;
				isInContrastMode = wpfHexView.Options.IsInContrastMode();
				if (old != ShouldUseHighContrastOpacity)
					RefreshExistingMarkers();
			}
		}

		sealed class MarkerElement : UIElement {
			readonly Geometry geometry;

			public Brush? BackgroundBrush {
				get => backgroundBrush;
				set {
					if (value is null)
						throw new ArgumentNullException(nameof(value));
					if (!TWPF.BrushComparer.Equals(value, backgroundBrush)) {
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

			public HexBufferSpan Span { get; }
			public string Type { get; }
			public int ZIndex { get; }

			public MarkerElement(HexBufferSpan span, string type, int zIndex, Geometry geometry) {
				if (span.IsDefault)
					throw new ArgumentException();
				Span = span;
				Type = type ?? throw new ArgumentNullException(nameof(type));
				ZIndex = zIndex;
				this.geometry = geometry ?? throw new ArgumentNullException(nameof(geometry));
				Panel.SetZIndex(this, zIndex);
			}

			protected override void OnRender(DrawingContext drawingContext) {
				base.OnRender(drawingContext);
				drawingContext.DrawGeometry(BackgroundBrush, Pen, geometry);
			}
		}

		void EditorFormatMap_FormatMappingChanged(object? sender, VSTC.FormatItemsEventArgs e) {
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
				int zIndex = props[VSTC.MarkerFormatDefinition.ZOrderId] as int? ?? 0;
				if (markerElement.ZIndex != zIndex) {
					UpdateRange(new NormalizedHexBufferSpanCollection(wpfHexView.HexViewLines.FormattedSpan));
					return;
				}
			}
		}

		void TagAggregator_BatchedTagsChanged(object? sender, HexBatchedTagsChangedEventArgs e) {
			if (wpfHexView.IsClosed)
				return;
			wpfHexView.VisualElement.Dispatcher.VerifyAccess();
			List<HexBufferSpan>? intersectionSpans = null;
			foreach (var span in e.Spans) {
				var intersection = wpfHexView.HexViewLines.FormattedSpan.Intersection(span);
				if (intersection is not null) {
					if (intersectionSpans is null)
						intersectionSpans = new List<HexBufferSpan>();
					intersectionSpans.Add(intersection.Value);
				}
			}
			if (intersectionSpans is not null)
				UpdateRange(new NormalizedHexBufferSpanCollection(intersectionSpans));
		}

		void RemoveMarkerElements(NormalizedHexBufferSpanCollection spans) {
			if (spans.Count == 0)
				return;
			for (int i = markerElements.Count - 1; i >= 0; i--) {
				var markerElement = markerElements[i];
				if (spans.IntersectsWithButDoesNotStartAtItsEnd(markerElement.Span)) {
					var layer = markerElement.ZIndex < 0 ? negativeTextMarkerAdornmentLayer : textMarkerAdornmentLayer;
					layer.RemoveAdornment(markerElement);
				}
			}
		}

		void AddMarkerElements(NormalizedHexBufferSpanCollection spans) {
			foreach (var tag in tagAggregator.GetTags(spans)) {
				if (tag.Tag?.Type is null)
					continue;
				if (!tag.Span.IntersectsWith(wpfHexView.HexViewLines.FormattedSpan))
					continue;
				var markerElement = TryCreateMarkerElement(tag.Span, tag.Flags, tag.Tag);
				if (markerElement is null)
					continue;
				var layer = markerElement.ZIndex < 0 ? negativeTextMarkerAdornmentLayer : textMarkerAdornmentLayer;
				bool added = layer.AddAdornment(VSTE.AdornmentPositioningBehavior.TextRelative, markerElement.Span, null, markerElement, onRemovedDelegate);
				if (added)
					markerElements.Add(markerElement);
			}
			var formattedEnd = wpfHexView.HexViewLines.FormattedSpan.End;
			foreach (var span in spans) {
				var overlap = wpfHexView.HexViewLines.FormattedSpan.Overlap(span);
				if (overlap is null)
					continue;
				var pos = overlap.Value.Start;
				for (;;) {
					var line = wpfHexView.WpfHexViewLines.GetWpfHexViewLineContainingBufferPosition(pos);
					Debug2.Assert(line is not null);
					if (line is not null) {
						var taggerContext = new HexTaggerContext(line.BufferLine, line.BufferLine.TextSpan);
						foreach (var tag in tagAggregator.GetLineTags(taggerContext)) {
							if (tag.Tag?.Type is null)
								continue;
							var markerElement = TryCreateMarkerElement(line, tag.Span, tag.Tag);
							if (markerElement is null)
								continue;
							var layer = markerElement.ZIndex < 0 ? negativeTextMarkerAdornmentLayer : textMarkerAdornmentLayer;
							bool added = layer.AddAdornment(VSTE.AdornmentPositioningBehavior.TextRelative, markerElement.Span, null, markerElement, onRemovedDelegate);
							if (added)
								markerElements.Add(markerElement);
						}
					}
					Debug2.Assert(line is not null);

					pos = line.BufferEnd;
					if (pos > overlap.Value.End || pos >= formattedEnd)
						break;
				}
			}
		}

		readonly VSTE.AdornmentRemovedCallback onRemovedDelegate;
		void OnRemoved(object tag, UIElement element) => markerElements.Remove((MarkerElement)element);

		void UpdateRange(NormalizedHexBufferSpanCollection spans) {
			if (spans.Count == 1 && spans[0].Start <= wpfHexView.BufferLines.BufferStart && spans[0].End >= wpfHexView.BufferLines.BufferEnd)
				RemoveAllMarkerElements();
			else
				RemoveMarkerElements(spans);
			AddMarkerElements(spans);
		}

		void RemoveAllMarkerElements() {
			// Clear this first so the remove-callback won't try to remove anything from this list (it'll be empty!)
			markerElements.Clear();
			negativeTextMarkerAdornmentLayer.RemoveAllAdornments();
			textMarkerAdornmentLayer.RemoveAllAdornments();
		}

		bool ShouldUseHighContrastOpacity => useReducedOpacityForHighContrast && isInContrastMode;

		Brush GetBackgroundBrush(ResourceDictionary props) {
			const double BG_BRUSH_OPACITY = 0.8;
			const double BG_BRUSH_HIGHCONTRAST_OPACITY = 0.5;
			Brush newBrush;
			if (props[VSTC.EditorFormatDefinition.BackgroundColorId] is Color color) {
				newBrush = new SolidColorBrush(color);
				newBrush.Opacity = BG_BRUSH_OPACITY;
				newBrush.Freeze();
			}
			else if (props[VSTC.EditorFormatDefinition.BackgroundBrushId] is SolidColorBrush scBrush) {
				newBrush = new SolidColorBrush(scBrush.Color);
				newBrush.Opacity = BG_BRUSH_OPACITY;
				newBrush.Freeze();
			}
			else if (props[VSTC.MarkerFormatDefinition.FillId] is Brush fillBrush) {
				newBrush = fillBrush;
				if (newBrush.CanFreeze)
					newBrush.Freeze();
			}
			else {
				newBrush = new SolidColorBrush(Colors.DarkGray);
				newBrush.Opacity = BG_BRUSH_OPACITY;
				newBrush.Freeze();
			}

			if (ShouldUseHighContrastOpacity) {
				newBrush = newBrush.Clone();
				newBrush.Opacity = BG_BRUSH_HIGHCONTRAST_OPACITY;
				if (newBrush.CanFreeze)
					newBrush.Freeze();
			}

			return newBrush;
		}

		Pen? GetPen(ResourceDictionary props) {
			const double PEN_THICKNESS = 0.5;
			Pen? newPen;
			if (props[VSTC.EditorFormatDefinition.ForegroundColorId] is Color color) {
				var brush = new SolidColorBrush(color);
				brush.Freeze();
				newPen = new Pen(brush, PEN_THICKNESS);
				newPen.Freeze();
			}
			else if (props[VSTC.EditorFormatDefinition.ForegroundBrushId] is SolidColorBrush scBrush) {
				if (scBrush.CanFreeze)
					scBrush.Freeze();
				newPen = new Pen(scBrush, PEN_THICKNESS);
				newPen.Freeze();
			}
			else if ((newPen = props[VSTC.MarkerFormatDefinition.BorderId] as Pen) is not null) {
				if (newPen.CanFreeze)
					newPen.Freeze();
			}

			return newPen;
		}

		MarkerElement? TryCreateMarkerElement(HexBufferSpan span, HexSpanSelectionFlags flags, HexMarkerTag tag) {
			Debug2.Assert(tag.Type is not null);
			var overlap = wpfHexView.WpfHexViewLines.FormattedSpan.Overlap(span);
			if (overlap is null)
				return null;
			return TryCreateMarkerElementCore(wpfHexView.WpfHexViewLines.GetMarkerGeometry(overlap.Value, flags), overlap.Value, tag);
		}

		MarkerElement? TryCreateMarkerElement(WpfHexViewLine line, VST.Span span, HexMarkerTag tag) {
			Debug2.Assert(tag.Type is not null);
			return TryCreateMarkerElementCore(wpfHexView.WpfHexViewLines.GetLineMarkerGeometry(line, span), line.BufferSpan, tag);
		}

		MarkerElement? TryCreateMarkerElementCore(Geometry? geo, HexBufferSpan span, HexMarkerTag tag) {
			if (geo is null)
				return null;

			var type = tag.Type ?? string.Empty;
			var props = editorFormatMap.GetProperties(type);
			int zIndex = props[VSTC.MarkerFormatDefinition.ZOrderId] as int? ?? 0;
			var markerElement = new MarkerElement(span, type, zIndex, geo);
			markerElement.BackgroundBrush = GetBackgroundBrush(props);
			markerElement.Pen = GetPen(props);
			return markerElement;
		}

		void WpfHexView_LayoutChanged(object? sender, HexViewLayoutChangedEventArgs e) => UpdateLines(e.NewOrReformattedLines);
		void UpdateLines(IList<HexViewLine> newOrReformattedLines) {
			if (newOrReformattedLines.Count == wpfHexView.HexViewLines.Count)
				RemoveAllMarkerElements();

			var lineSpans = new List<HexBufferSpan>();
			foreach (var line in newOrReformattedLines)
				lineSpans.Add(line.BufferSpan);
			var spans = new NormalizedHexBufferSpanCollection(lineSpans);
			UpdateRange(spans);
		}

		void WpfHexView_Closed(object? sender, EventArgs e) {
			RemoveAllMarkerElements();
			wpfHexView.Closed -= WpfHexView_Closed;
			wpfHexView.LayoutChanged -= WpfHexView_LayoutChanged;
			wpfHexView.Options.OptionChanged -= Options_OptionChanged;
			tagAggregator.BatchedTagsChanged -= TagAggregator_BatchedTagsChanged;
			editorFormatMap.FormatMappingChanged -= EditorFormatMap_FormatMappingChanged;
		}
	}
}
