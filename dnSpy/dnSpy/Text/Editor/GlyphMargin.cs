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
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Text.MEF;
using dnSpy.Text.WPF;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Editor {
	[Export(typeof(IWpfTextViewMarginProvider))]
	[MarginContainer(PredefinedMarginNames.Left)]
	[Name(PredefinedMarginNames.Glyph)]
	[ContentType(ContentTypes.TEXT)]
	[TextViewRole(PredefinedTextViewRoles.Interactive)]
	[Order(Before = PredefinedMarginNames.LeftSelection)]
	sealed class GlyphMarginProvider : IWpfTextViewMarginProvider {
		readonly IViewTagAggregatorFactoryService viewTagAggregatorFactoryService;
		readonly IEditorFormatMapService editorFormatMapService;
		readonly Lazy<IGlyphMouseProcessorProvider, IGlyphMouseProcessorProviderMetadata>[] glyphMouseProcessorProviders;
		readonly Lazy<IGlyphFactoryProvider, IGlyphMetadata>[] glyphFactoryProviders;

		[ImportingConstructor]
		GlyphMarginProvider(IViewTagAggregatorFactoryService viewTagAggregatorFactoryService, IEditorFormatMapService editorFormatMapService, [ImportMany] IEnumerable<Lazy<IGlyphMouseProcessorProvider, IGlyphMouseProcessorProviderMetadata>> glyphMouseProcessorProviders, [ImportMany] IEnumerable<Lazy<IGlyphFactoryProvider, IGlyphMetadata>> glyphFactoryProviders) {
			this.viewTagAggregatorFactoryService = viewTagAggregatorFactoryService;
			this.editorFormatMapService = editorFormatMapService;
			this.glyphMouseProcessorProviders = Orderer.Order(glyphMouseProcessorProviders).ToArray();
			this.glyphFactoryProviders = Orderer.Order(glyphFactoryProviders).ToArray();
		}

		public IWpfTextViewMargin CreateMargin(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin marginContainer) =>
			new GlyphMargin(wpfTextViewHost, viewTagAggregatorFactoryService, editorFormatMapService, glyphMouseProcessorProviders, glyphFactoryProviders);
	}

	sealed class GlyphMargin : Canvas, IWpfTextViewMargin {
		public bool Enabled => wpfTextViewHost.TextView.Options.IsGlyphMarginEnabled();
		public double MarginSize => ActualWidth;
		public FrameworkElement VisualElement => this;

		readonly IWpfTextViewHost wpfTextViewHost;
		readonly IViewTagAggregatorFactoryService viewTagAggregatorFactoryService;
		readonly IEditorFormatMapService editorFormatMapService;
		readonly Lazy<IGlyphMouseProcessorProvider, IGlyphMouseProcessorProviderMetadata>[] lazyGlyphMouseProcessorProviders;
		readonly Lazy<IGlyphFactoryProvider, IGlyphMetadata>[] lazyGlyphFactoryProviders;
		MouseProcessorCollection mouseProcessorCollection;
		Dictionary<Type, GlyphFactoryInfo> glyphFactories;
		ITagAggregator<IGlyphTag> tagAggregator;
		IEditorFormatMap editorFormatMap;
		Dictionary<object, LineInfo> lineInfos;
		Canvas iconCanvas;

		struct GlyphFactoryInfo {
			public int Order { get; }
			public IGlyphFactory Factory { get; }
			public GlyphFactoryInfo(int order, IGlyphFactory factory) {
				if (factory == null)
					throw new ArgumentNullException(nameof(factory));
				Order = order;
				Factory = factory;
			}
		}

		struct LineInfo {
			public ITextViewLine Line { get; }
			public List<IconInfo> Icons { get; }

			public LineInfo(ITextViewLine textViewLine, List<IconInfo> icons) {
				if (textViewLine == null)
					throw new ArgumentNullException(nameof(textViewLine));
				if (icons == null)
					throw new ArgumentNullException(nameof(icons));
				Line = textViewLine;
				Icons = icons;
			}
		}

		struct IconInfo {
			public UIElement Element { get; }
			public double BaseTopValue { get; }
			public IconInfo(int order, UIElement element) {
				if (element == null)
					throw new ArgumentNullException(nameof(element));
				Element = element;
				BaseTopValue = GetBaseTopValue(element);
				SetZIndex(element, order);
			}

			static double GetBaseTopValue(UIElement element) {
				double top = GetTop(element);
				return double.IsNaN(top) ? 0 : top;
			}
		}

		// Need to make it a constant since ActualWidth isn't always valid when we need it
		const double MARGIN_WIDTH = 17;

		public GlyphMargin(IWpfTextViewHost wpfTextViewHost, IViewTagAggregatorFactoryService viewTagAggregatorFactoryService, IEditorFormatMapService editorFormatMapService, Lazy<IGlyphMouseProcessorProvider, IGlyphMouseProcessorProviderMetadata>[] glyphMouseProcessorProviders, Lazy<IGlyphFactoryProvider, IGlyphMetadata>[] glyphFactoryProviders) {
			if (wpfTextViewHost == null)
				throw new ArgumentNullException(nameof(wpfTextViewHost));
			if (viewTagAggregatorFactoryService == null)
				throw new ArgumentNullException(nameof(viewTagAggregatorFactoryService));
			if (editorFormatMapService == null)
				throw new ArgumentNullException(nameof(editorFormatMapService));
			if (glyphMouseProcessorProviders == null)
				throw new ArgumentNullException(nameof(glyphMouseProcessorProviders));
			if (glyphFactoryProviders == null)
				throw new ArgumentNullException(nameof(glyphFactoryProviders));
			this.wpfTextViewHost = wpfTextViewHost;
			this.viewTagAggregatorFactoryService = viewTagAggregatorFactoryService;
			this.editorFormatMapService = editorFormatMapService;
			this.lazyGlyphMouseProcessorProviders = glyphMouseProcessorProviders;
			this.lazyGlyphFactoryProviders = glyphFactoryProviders;
			wpfTextViewHost.TextView.Options.OptionChanged += Options_OptionChanged;
			wpfTextViewHost.TextView.ZoomLevelChanged += TextView_ZoomLevelChanged;
			IsVisibleChanged += GlyphMargin_IsVisibleChanged;
			UpdateVisibility();
			Width = MARGIN_WIDTH;
			ClipToBounds = true;
		}

		void UpdateVisibility() => Visibility = Enabled ? Visibility.Visible : Visibility.Collapsed;
		void TextView_ZoomLevelChanged(object sender, ZoomLevelChangedEventArgs e) => LayoutTransform = e.ZoomTransform;

		public ITextViewMargin GetTextViewMargin(string marginName) =>
			StringComparer.OrdinalIgnoreCase.Equals(marginName, PredefinedMarginNames.Glyph) ? this : null;

		void Options_OptionChanged(object sender, EditorOptionChangedEventArgs e) {
			if (e.OptionId == DefaultTextViewHostOptions.GlyphMarginId.Name)
				UpdateVisibility();
		}

		IMouseProcessor[] CreateMouseProcessors() {
			var list = new List<IMouseProcessor>();
			var contentType = wpfTextViewHost.TextView.TextDataModel.ContentType;
			foreach (var lazy in lazyGlyphMouseProcessorProviders) {
				if (!contentType.ContainsAny(lazy.Metadata.ContentTypes))
					continue;
				if (lazy.Metadata.GlyphMargins == null || !lazy.Metadata.GlyphMargins.Any()) {
					// Nothing
				}
				else if (!lazy.Metadata.GlyphMargins.Any(a => StringComparer.OrdinalIgnoreCase.Equals(a, ThemeClassificationTypeNameKeys.GlyphMargin)))
					continue;
				var mouseProcessor = lazy.Value.GetAssociatedMouseProcessor(wpfTextViewHost, this);
				if (mouseProcessor == null)
					continue;
				list.Add(mouseProcessor);
			}
			return list.ToArray();
		}

		Dictionary<Type, GlyphFactoryInfo> CreateGlyphFactories() {
			var dict = new Dictionary<Type, GlyphFactoryInfo>();
			var contentType = wpfTextViewHost.TextView.TextDataModel.ContentType;
			int order = 0;
			foreach (var lazy in lazyGlyphFactoryProviders) {
				if (!contentType.ContainsAny(lazy.Metadata.ContentTypes))
					continue;
				IGlyphFactory glyphFactory = null;
				foreach (var type in lazy.Metadata.TagTypes) {
					Debug.Assert(type != null);
					if (type == null)
						break;
					Debug.Assert(!dict.ContainsKey(type));
					if (dict.ContainsKey(type))
						continue;
					Debug.Assert(typeof(IGlyphTag).IsAssignableFrom(type));
					if (!typeof(IGlyphTag).IsAssignableFrom(type))
						continue;

					if (glyphFactory == null) {
						glyphFactory = lazy.Value.GetGlyphFactory(wpfTextViewHost.TextView, this);
						if (glyphFactory == null)
							break;
					}

					dict.Add(type, new GlyphFactoryInfo(order++, glyphFactory));
				}
			}
			return dict;
		}

		void Initialize() {
			if (mouseProcessorCollection != null)
				return;
			iconCanvas = new Canvas { Background = Brushes.Transparent };
			Children.Add(iconCanvas);
			mouseProcessorCollection = new MouseProcessorCollection(VisualElement, null, new DefaultMouseProcessor(), CreateMouseProcessors());
			glyphFactories = CreateGlyphFactories();
			tagAggregator = viewTagAggregatorFactoryService.CreateTagAggregator<IGlyphTag>(wpfTextViewHost.TextView);
			editorFormatMap = editorFormatMapService.GetEditorFormatMap(wpfTextViewHost.TextView);
			lineInfos = new Dictionary<object, LineInfo>();
		}

		void GlyphMargin_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) {
			if (Visibility == Visibility.Visible && !wpfTextViewHost.IsClosed) {
				Initialize();
				RegisterEvents();
				UpdateBackground();
				SetTop(iconCanvas, -wpfTextViewHost.TextView.ViewportTop);
				RefreshEverything();
			}
			else {
				UnregisterEvents();
				lineInfos?.Clear();
				iconCanvas?.Children.Clear();
			}
		}

		void RefreshEverything() {
			lineInfos.Clear();
			iconCanvas.Children.Clear();
			OnNewLayout(wpfTextViewHost.TextView.TextViewLines, Array.Empty<ITextViewLine>());
		}

		void TextView_LayoutChanged(object sender, TextViewLayoutChangedEventArgs e) {
			if (e.OldViewState.ViewportTop != e.NewViewState.ViewportTop)
				SetTop(iconCanvas, -wpfTextViewHost.TextView.ViewportTop);
			OnNewLayout(e.NewOrReformattedLines, e.TranslatedLines);
		}

		void OnNewLayout(IList<ITextViewLine> newOrReformattedLines, IList<ITextViewLine> translatedLines) {
			var newInfos = new Dictionary<object, LineInfo>();

			foreach (var line in newOrReformattedLines)
				AddLine(newInfos, line);

			foreach (var line in translatedLines) {
				LineInfo info;
				bool b = lineInfos.TryGetValue(line.IdentityTag, out info);
				Debug.Assert(b);
				if (!b)
					continue;
				lineInfos.Remove(line.IdentityTag);
				newInfos.Add(line.IdentityTag, info);
				foreach (var iconInfo in info.Icons)
					SetTop(iconInfo.Element, iconInfo.BaseTopValue + line.TextTop);
			}

			foreach (var line in wpfTextViewHost.TextView.TextViewLines) {
				if (newInfos.ContainsKey(line.IdentityTag))
					continue;
				LineInfo info;
				if (!lineInfos.TryGetValue(line.IdentityTag, out info))
					continue;
				lineInfos.Remove(line.IdentityTag);
				newInfos.Add(line.IdentityTag, info);
			}

			foreach (var info in lineInfos.Values) {
				foreach (var iconInfo in info.Icons)
					iconCanvas.Children.Remove(iconInfo.Element);
			}
			lineInfos = newInfos;
		}

		void AddLine(Dictionary<object, LineInfo> newInfos, ITextViewLine line) {
			var wpfLine = line as IWpfTextViewLine;
			Debug.Assert(wpfLine != null);
			if (wpfLine == null)
				return;
			var info = new LineInfo(line, CreateIconInfos(wpfLine));
			newInfos.Add(line.IdentityTag, info);
			foreach (var iconInfo in info.Icons)
				iconCanvas.Children.Add(iconInfo.Element);
		}

		List<IconInfo> CreateIconInfos(IWpfTextViewLine line) {
			var icons = new List<IconInfo>();
			foreach (var mappingSpan in tagAggregator.GetTags(line.ExtentIncludingLineBreakAsMappingSpan)) {
				var tag = mappingSpan.Tag;
				Debug.Assert(tag != null);
				if (tag == null)
					continue;
				GlyphFactoryInfo factoryInfo;
				// Fails if someone forgot to Export(typeof(IGlyphFactoryProvider)) with the correct tag types
				bool b = glyphFactories.TryGetValue(tag.GetType(), out factoryInfo);
				Debug.Assert(b);
				if (!b)
					continue;
				foreach (var span in mappingSpan.Span.GetSpans(wpfTextViewHost.TextView.TextSnapshot)) {
					if (!line.IntersectsBufferSpan(span))
						continue;
					var elem = factoryInfo.Factory.GenerateGlyph(line, tag);
					if (elem == null)
						continue;
					elem.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
					var iconInfo = new IconInfo(factoryInfo.Order, elem);
					icons.Add(iconInfo);
					// ActualWidth isn't always valid when we're here so use the constant
					SetLeft(elem, (MARGIN_WIDTH - elem.DesiredSize.Width) / 2);
					SetTop(elem, iconInfo.BaseTopValue + line.TextTop);
				}
			}
			return icons;
		}

		void TagAggregator_BatchedTagsChanged(object sender, BatchedTagsChangedEventArgs e) {
			Dispatcher.VerifyAccess();
			HashSet<ITextViewLine> checkedLines = null;
			foreach (var mappingSpan in e.Spans) {
				foreach (var span in mappingSpan.GetSpans(wpfTextViewHost.TextView.TextSnapshot))
					Update(span, ref checkedLines);
			}
		}

		void Update(SnapshotSpan span, ref HashSet<ITextViewLine> checkedLines) {
			Debug.Assert(span.Snapshot == wpfTextViewHost.TextView.TextSnapshot);
			var intersection = span.Intersection(wpfTextViewHost.TextView.TextViewLines.FormattedSpan);
			if (intersection == null)
				return;
			var point = intersection.Value.Start;
			while (point <= intersection.Value.End) {
				var line = wpfTextViewHost.TextView.TextViewLines.GetTextViewLineContainingBufferPosition(point);
				if (line == null)
					break;
				if (checkedLines == null)
					checkedLines = new HashSet<ITextViewLine>();
				if (!checkedLines.Contains(line)) {
					checkedLines.Add(line);
					Update(line);
				}
				if (line.IsLastDocumentLine())
					break;
				point = line.EndIncludingLineBreak;
			}
		}

		void Update(IWpfTextViewLine line) {
			Debug.Assert(line.VisibilityState != VisibilityState.Unattached);
			LineInfo info;
			if (!lineInfos.TryGetValue(line.IdentityTag, out info))
				return;
			lineInfos.Remove(line.IdentityTag);
			foreach (var iconInfo in info.Icons)
				iconCanvas.Children.Remove(iconInfo.Element);
			AddLine(lineInfos, line);
		}

		void EditorFormatMap_FormatMappingChanged(object sender, FormatItemsEventArgs e) {
			if (e.ChangedItems.Contains(ThemeClassificationTypeNameKeys.GlyphMargin))
				UpdateBackground();
		}

		void UpdateBackground() {
			if (editorFormatMap == null)
				return;
			var props = editorFormatMap.GetProperties(ThemeClassificationTypeNameKeys.GlyphMargin);
			var newBackground = ResourceDictionaryUtilities.GetBackgroundBrush(props, Brushes.Transparent);
			if (!BrushComparer.Equals(Background, newBackground)) {
				Background = newBackground;
				// The images could depend on the background color, so recreate every icon
				if (iconCanvas.Children.Count > 0)
					RefreshEverything();
			}
		}

		bool hasRegisteredEvents;
		void RegisterEvents() {
			if (hasRegisteredEvents)
				return;
			if (wpfTextViewHost.IsClosed)
				return;
			hasRegisteredEvents = true;
			editorFormatMap.FormatMappingChanged += EditorFormatMap_FormatMappingChanged;
			tagAggregator.BatchedTagsChanged += TagAggregator_BatchedTagsChanged;
			wpfTextViewHost.TextView.LayoutChanged += TextView_LayoutChanged;
		}

		void UnregisterEvents() {
			hasRegisteredEvents = false;
			if (editorFormatMap != null)
				editorFormatMap.FormatMappingChanged -= EditorFormatMap_FormatMappingChanged;
			if (tagAggregator != null)
				tagAggregator.BatchedTagsChanged -= TagAggregator_BatchedTagsChanged;
			wpfTextViewHost.TextView.LayoutChanged -= TextView_LayoutChanged;
		}

		public void Dispose() {
			wpfTextViewHost.TextView.Options.OptionChanged -= Options_OptionChanged;
			wpfTextViewHost.TextView.ZoomLevelChanged -= TextView_ZoomLevelChanged;
			IsVisibleChanged -= GlyphMargin_IsVisibleChanged;
			UnregisterEvents();
			lineInfos?.Clear();
			iconCanvas?.Children.Clear();
			mouseProcessorCollection?.Dispose();
			tagAggregator?.Dispose();
		}
	}
}
