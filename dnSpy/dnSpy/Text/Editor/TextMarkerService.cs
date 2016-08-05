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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Contracts.Themes;
using dnSpy.Text.WPF;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Editor {
	[Export(typeof(ITextMarkerProviderFactory))]
	sealed class TextMarkerProviderFactory : ITextMarkerProviderFactory {
		public SimpleTagger<TextMarkerTag> GetTextMarkerTagger(ITextBuffer textBuffer) {
			if (textBuffer == null)
				throw new ArgumentNullException(nameof(textBuffer));
			return textBuffer.Properties.GetOrCreateSingletonProperty(() => new SimpleTagger<TextMarkerTag>(textBuffer));
		}
	}

	[Export(typeof(ITaggerProvider))]
	[ContentType(ContentTypes.Text)]
	[TextViewRole(PredefinedTextViewRoles.Analyzable)]
	[TextViewRole(PredefinedTextViewRoles.Interactive)]
	[TagType(typeof(TextMarkerTag))]
	sealed class TextMarkerServiceTaggerProvider : ITaggerProvider {
		readonly ITextMarkerProviderFactory textMarkerProviderFactory;

		[ImportingConstructor]
		TextMarkerServiceTaggerProvider(ITextMarkerProviderFactory textMarkerProviderFactory) {
			this.textMarkerProviderFactory = textMarkerProviderFactory;
		}

		public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag {
			if (buffer == null)
				throw new ArgumentNullException(nameof(buffer));
			return textMarkerProviderFactory.GetTextMarkerTagger(buffer) as ITagger<T>;
		}
	}

	[Export(typeof(IWpfTextViewCreationListener))]
	[ContentType(ContentTypes.Text)]
	[TextViewRole(PredefinedTextViewRoles.Analyzable)]
	[TextViewRole(PredefinedTextViewRoles.Interactive)]
	sealed class TextMarkerServiceWpfTextViewCreationListener : IWpfTextViewCreationListener {
		readonly IViewTagAggregatorFactoryService viewTagAggregatorFactoryService;
		readonly IEditorFormatMapService editorFormatMapService;
		readonly IThemeManager themeManager;

		[ImportingConstructor]
		TextMarkerServiceWpfTextViewCreationListener(IViewTagAggregatorFactoryService viewTagAggregatorFactoryService, IEditorFormatMapService editorFormatMapService, IThemeManager themeManager) {
			this.viewTagAggregatorFactoryService = viewTagAggregatorFactoryService;
			this.editorFormatMapService = editorFormatMapService;
			this.themeManager = themeManager;
		}

		public void TextViewCreated(IWpfTextView textView) =>
			new TextMarkerService(textView, viewTagAggregatorFactoryService.CreateTagAggregator<ITextMarkerTag>(textView), editorFormatMapService.GetEditorFormatMap(textView), themeManager);
	}

	sealed class TextMarkerService {
		const string NEGATIVE_TEXT_MARK_LAYER_NAME = "negativetextmarkerlayer";

#pragma warning disable 0169
		[Export(typeof(AdornmentLayerDefinition))]
		[Name(NEGATIVE_TEXT_MARK_LAYER_NAME)]
		[Order(After = PredefinedDnSpyAdornmentLayers.BottomLayer, Before = PredefinedDnSpyAdornmentLayers.TopLayer)]
		[Order(Before = PredefinedDnSpyAdornmentLayers.GlyphTextMarker, After = PredefinedAdornmentLayers.Outlining)]
		[Order(Before = PredefinedAdornmentLayers.TextMarker)]
		static AdornmentLayerDefinition negativeTextMarkerAdornmentLayerDefinition;

		[Export(typeof(AdornmentLayerDefinition))]
		[Name(PredefinedAdornmentLayers.TextMarker)]
		[Order(After = PredefinedDnSpyAdornmentLayers.BottomLayer, Before = PredefinedDnSpyAdornmentLayers.TopLayer)]
		[Order(Before = PredefinedAdornmentLayers.Selection, After = PredefinedAdornmentLayers.Outlining)]
		static AdornmentLayerDefinition textMarkerAdornmentLayerDefinition;
#pragma warning restore 0169

		readonly IWpfTextView wpfTextView;
		readonly ITagAggregator<ITextMarkerTag> tagAggregator;
		readonly IEditorFormatMap editorFormatMap;
		readonly IAdornmentLayer textMarkerAdornmentLayer;
		readonly IAdornmentLayer negativeTextMarkerAdornmentLayer;
		readonly List<MarkerElement> markerElements;
		readonly IThemeManager themeManager;
		bool useReducedOpacityForHighContrast;

		public TextMarkerService(IWpfTextView wpfTextView, ITagAggregator<ITextMarkerTag> tagAggregator, IEditorFormatMap editorFormatMap, IThemeManager themeManager) {
			if (wpfTextView == null)
				throw new ArgumentNullException(nameof(wpfTextView));
			if (tagAggregator == null)
				throw new ArgumentNullException(nameof(tagAggregator));
			if (editorFormatMap == null)
				throw new ArgumentNullException(nameof(editorFormatMap));
			if (themeManager == null)
				throw new ArgumentNullException(nameof(themeManager));
			this.wpfTextView = wpfTextView;
			this.tagAggregator = tagAggregator;
			this.editorFormatMap = editorFormatMap;
			this.themeManager = themeManager;
			this.textMarkerAdornmentLayer = wpfTextView.GetAdornmentLayer(PredefinedAdornmentLayers.TextMarker);
			this.negativeTextMarkerAdornmentLayer = wpfTextView.GetAdornmentLayer(NEGATIVE_TEXT_MARK_LAYER_NAME);
			this.markerElements = new List<MarkerElement>();
			this.useReducedOpacityForHighContrast = wpfTextView.Options.GetOptionValue(DefaultWpfViewOptions.UseReducedOpacityForHighContrastOptionId);
			this.onRemovedDelegate = OnRemoved;
			wpfTextView.Closed += WpfTextView_Closed;
			wpfTextView.LayoutChanged += WpfTextView_LayoutChanged;
			wpfTextView.Options.OptionChanged += Options_OptionChanged;
			tagAggregator.BatchedTagsChanged += TagAggregator_BatchedTagsChanged;
			editorFormatMap.FormatMappingChanged += EditorFormatMap_FormatMappingChanged;
		}

		void Options_OptionChanged(object sender, EditorOptionChangedEventArgs e) {
			if (e.OptionId == DefaultWpfViewOptions.UseReducedOpacityForHighContrastOptionId.Name) {
				useReducedOpacityForHighContrast = wpfTextView.Options.GetOptionValue(DefaultWpfViewOptions.UseReducedOpacityForHighContrastOptionId);
				if (themeManager.Theme.IsHighContrast)
					RefreshExistingMarkers();
			}
		}

		sealed class MarkerElement : UIElement {
			readonly Geometry geometry;

			public Brush BackgroundBrush {
				get { return backgroundBrush; }
				set {
					if (value == null)
						throw new ArgumentNullException(nameof(value));
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
				int zIndex = props[MarkerFormatDefinition.ZOrderId] as int? ?? 0;
				if (markerElement.ZIndex != zIndex) {
					UpdateRange(new NormalizedSnapshotSpanCollection(wpfTextView.TextViewLines.FormattedSpan));
					return;
				}
			}
		}

		void TagAggregator_BatchedTagsChanged(object sender, BatchedTagsChangedEventArgs e) {
			if (wpfTextView.IsClosed)
				return;
			wpfTextView.VisualElement.Dispatcher.VerifyAccess();
			List<SnapshotSpan> intersectionSpans = null;
			foreach (var mappingSpan in e.Spans) {
				foreach (var span in mappingSpan.GetSpans(wpfTextView.TextSnapshot)) {
					var intersection = wpfTextView.TextViewLines.FormattedSpan.Intersection(span);
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
				if (spans.IntersectsWith(markerElement.Span)) {
					var layer = markerElement.ZIndex < 0 ? negativeTextMarkerAdornmentLayer : textMarkerAdornmentLayer;
					layer.RemoveAdornment(markerElement);
				}
			}
		}

		void AddMarkerElements(NormalizedSnapshotSpanCollection spans) {
			foreach (var tag in tagAggregator.GetTags(spans)) {
				if (tag.Tag?.Type == null)
					continue;
				foreach (var span in tag.Span.GetSpans(wpfTextView.TextSnapshot)) {
					if (!span.IntersectsWith(wpfTextView.TextViewLines.FormattedSpan))
						continue;
					var markerElement = TryCreateMarkerElement(span, tag.Tag);
					if (markerElement == null)
						continue;
					var layer = markerElement.ZIndex < 0 ? negativeTextMarkerAdornmentLayer : textMarkerAdornmentLayer;
					bool added = layer.AddAdornment(AdornmentPositioningBehavior.TextRelative, markerElement.Span, null, markerElement, onRemovedDelegate);
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
			negativeTextMarkerAdornmentLayer.RemoveAllAdornments();
			textMarkerAdornmentLayer.RemoveAllAdornments();
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
			else {
				newBrush = new SolidColorBrush(Colors.DarkGray);
				newBrush.Opacity = BG_BRUSH_OPACITY;
				newBrush.Freeze();
			}

			if (useReducedOpacityForHighContrast && themeManager.Theme.IsHighContrast) {
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

			const double PEN_THICKNESS = 0.5;
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

		MarkerElement TryCreateMarkerElement(SnapshotSpan span, ITextMarkerTag tag) {
			Debug.Assert(tag.Type != null);
			var geo = wpfTextView.TextViewLines.GetMarkerGeometry(span);
			if (geo == null)
				return null;

			var type = tag.Type ?? string.Empty;
			var props = editorFormatMap.GetProperties(type);
			int zIndex = props[MarkerFormatDefinition.ZOrderId] as int? ?? 0;
			var markerElement = new MarkerElement(span, type, zIndex, geo);
			markerElement.BackgroundBrush = GetBackgroundBrush(props);
			markerElement.Pen = GetPen(props);
			return markerElement;
		}

		void WpfTextView_LayoutChanged(object sender, TextViewLayoutChangedEventArgs e) => UpdateLines(e.NewOrReformattedLines);
		void UpdateLines(IList<ITextViewLine> newOrReformattedLines) {
			if (newOrReformattedLines.Count == wpfTextView.TextViewLines.Count)
				RemoveAllMarkerElements();

			var lineSpans = new List<SnapshotSpan>();
			foreach (var line in newOrReformattedLines)
				lineSpans.Add(line.ExtentIncludingLineBreak);
			var spans = new NormalizedSnapshotSpanCollection(lineSpans);
			UpdateRange(spans);
		}

		void WpfTextView_Closed(object sender, EventArgs e) {
			RemoveAllMarkerElements();
			wpfTextView.Closed -= WpfTextView_Closed;
			wpfTextView.LayoutChanged -= WpfTextView_LayoutChanged;
			wpfTextView.Options.OptionChanged -= Options_OptionChanged;
			tagAggregator.BatchedTagsChanged -= TagAggregator_BatchedTagsChanged;
			editorFormatMap.FormatMappingChanged -= EditorFormatMap_FormatMappingChanged;
		}
	}
}
