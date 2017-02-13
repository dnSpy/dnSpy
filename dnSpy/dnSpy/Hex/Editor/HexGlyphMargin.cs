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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Classification;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Editor.OptionsExtensionMethods;
using dnSpy.Contracts.Hex.Formatting;
using dnSpy.Contracts.Hex.Tagging;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Hex.MEF;
using CTC = dnSpy.Contracts.Text.Classification;
using TE = dnSpy.Text.Editor;
using TWPF = dnSpy.Text.WPF;
using VSTC = Microsoft.VisualStudio.Text.Classification;
using VSTE = Microsoft.VisualStudio.Text.Editor;
using VSTF = Microsoft.VisualStudio.Text.Formatting;
using VSUTIL = Microsoft.VisualStudio.Utilities;

namespace dnSpy.Hex.Editor {
	[Export(typeof(WpfHexViewMarginProvider))]
	[VSTE.MarginContainer(PredefinedHexMarginNames.Left)]
	[VSUTIL.Name(PredefinedHexMarginNames.Glyph)]
	[VSTE.TextViewRole(PredefinedHexViewRoles.Interactive)]
	[VSTE.TextViewRole(PredefinedHexViewRoles.CanHaveGlyphMargin)]
	[VSUTIL.Order(Before = PredefinedHexMarginNames.LeftSelection)]
	sealed class GlyphMarginProvider : WpfHexViewMarginProvider {
		readonly IMenuService menuService;
		readonly HexViewTagAggregatorFactoryService viewTagAggregatorFactoryService;
		readonly HexEditorFormatMapService editorFormatMapService;
		readonly Lazy<HexGlyphMouseProcessorProvider, IGlyphMouseProcessorProviderMetadata>[] glyphMouseProcessorProviders;
		readonly Lazy<HexGlyphFactoryProvider, IGlyphMetadata>[] glyphFactoryProviders;
		readonly HexMarginContextMenuService marginContextMenuHandlerProviderService;

		[ImportingConstructor]
		GlyphMarginProvider(IMenuService menuService, HexViewTagAggregatorFactoryService viewTagAggregatorFactoryService, HexEditorFormatMapService editorFormatMapService, [ImportMany] IEnumerable<Lazy<HexGlyphMouseProcessorProvider, IGlyphMouseProcessorProviderMetadata>> glyphMouseProcessorProviders, [ImportMany] IEnumerable<Lazy<HexGlyphFactoryProvider, IGlyphMetadata>> glyphFactoryProviders, HexMarginContextMenuService marginContextMenuHandlerProviderService) {
			this.menuService = menuService;
			this.viewTagAggregatorFactoryService = viewTagAggregatorFactoryService;
			this.editorFormatMapService = editorFormatMapService;
			this.glyphMouseProcessorProviders = VSUTIL.Orderer.Order(glyphMouseProcessorProviders).ToArray();
			this.glyphFactoryProviders = VSUTIL.Orderer.Order(glyphFactoryProviders).ToArray();
			this.marginContextMenuHandlerProviderService = marginContextMenuHandlerProviderService;
		}

		public override WpfHexViewMargin CreateMargin(WpfHexViewHost wpfHexViewHost, WpfHexViewMargin marginContainer) =>
			new HexGlyphMargin(menuService, wpfHexViewHost, viewTagAggregatorFactoryService, editorFormatMapService, glyphMouseProcessorProviders, glyphFactoryProviders, marginContextMenuHandlerProviderService);
	}

	sealed class HexGlyphMargin : WpfHexViewMargin {
		public override bool Enabled => wpfHexViewHost.HexView.Options.IsGlyphMarginEnabled();
		public override double MarginSize => canvas.ActualWidth;
		public override FrameworkElement VisualElement => canvas;

		readonly Canvas canvas;
		readonly WpfHexViewHost wpfHexViewHost;
		readonly HexViewTagAggregatorFactoryService viewTagAggregatorFactoryService;
		readonly HexEditorFormatMapService editorFormatMapService;
		readonly Lazy<HexGlyphMouseProcessorProvider, IGlyphMouseProcessorProviderMetadata>[] lazyGlyphMouseProcessorProviders;
		readonly Lazy<HexGlyphFactoryProvider, IGlyphMetadata>[] lazyGlyphFactoryProviders;
		HexMouseProcessorCollection mouseProcessorCollection;
		readonly Dictionary<Type, GlyphFactoryInfo> glyphFactories;
		HexTagAggregator<HexGlyphTag> tagAggregator;
		VSTC.IEditorFormatMap editorFormatMap;
		Dictionary<object, LineInfo> lineInfos;
		Canvas iconCanvas;
		Canvas[] childCanvases;

		struct GlyphFactoryInfo {
			public int Order { get; }
			public HexGlyphFactory Factory { get; }
			public HexGlyphFactoryProvider FactoryProvider { get; }
			public Canvas Canvas { get; }
			public GlyphFactoryInfo(int order, HexGlyphFactory factory, HexGlyphFactoryProvider glyphFactoryProvider) {
				Order = order;
				Factory = factory ?? throw new ArgumentNullException(nameof(factory));
				FactoryProvider = glyphFactoryProvider ?? throw new ArgumentNullException(nameof(glyphFactoryProvider));
				Canvas = new Canvas { Background = Brushes.Transparent };
			}
		}

		struct LineInfo {
			public HexViewLine Line { get; }
			public List<IconInfo> Icons { get; }

			public LineInfo(HexViewLine hexViewLine, List<IconInfo> icons) {
				Line = hexViewLine ?? throw new ArgumentNullException(nameof(hexViewLine));
				Icons = icons ?? throw new ArgumentNullException(nameof(icons));
			}
		}

		struct IconInfo {
			public UIElement Element { get; }
			public double BaseTopValue { get; }
			public int Order { get; }
			public IconInfo(int order, UIElement element) {
				Element = element ?? throw new ArgumentNullException(nameof(element));
				BaseTopValue = GetBaseTopValue(element);
				Order = order;
			}

			static double GetBaseTopValue(UIElement element) {
				double top = Canvas.GetTop(element);
				return double.IsNaN(top) ? 0 : top;
			}
		}

		// Need to make it a constant since ActualWidth isn't always valid when we need it
		const double MARGIN_WIDTH = 17;

		public HexGlyphMargin(IMenuService menuService, WpfHexViewHost wpfHexViewHost, HexViewTagAggregatorFactoryService viewTagAggregatorFactoryService, HexEditorFormatMapService editorFormatMapService, Lazy<HexGlyphMouseProcessorProvider, IGlyphMouseProcessorProviderMetadata>[] glyphMouseProcessorProviders, Lazy<HexGlyphFactoryProvider, IGlyphMetadata>[] glyphFactoryProviders, HexMarginContextMenuService marginContextMenuHandlerProviderService) {
			if (menuService == null)
				throw new ArgumentNullException(nameof(menuService));
			canvas = new Canvas();
			glyphFactories = new Dictionary<Type, GlyphFactoryInfo>();
			childCanvases = Array.Empty<Canvas>();
			this.wpfHexViewHost = wpfHexViewHost ?? throw new ArgumentNullException(nameof(wpfHexViewHost));
			this.viewTagAggregatorFactoryService = viewTagAggregatorFactoryService ?? throw new ArgumentNullException(nameof(viewTagAggregatorFactoryService));
			this.editorFormatMapService = editorFormatMapService ?? throw new ArgumentNullException(nameof(editorFormatMapService));
			lazyGlyphMouseProcessorProviders = glyphMouseProcessorProviders ?? throw new ArgumentNullException(nameof(glyphMouseProcessorProviders));
			lazyGlyphFactoryProviders = glyphFactoryProviders ?? throw new ArgumentNullException(nameof(glyphFactoryProviders));

			var binding = new Binding {
				Path = new PropertyPath(Panel.BackgroundProperty),
				Source = canvas,
			};
			canvas.SetBinding(DsImage.BackgroundBrushProperty, binding);

			wpfHexViewHost.HexView.Options.OptionChanged += Options_OptionChanged;
			wpfHexViewHost.HexView.ZoomLevelChanged += HexView_ZoomLevelChanged;
			canvas.IsVisibleChanged += GlyphMargin_IsVisibleChanged;
			UpdateVisibility();
			canvas.Width = MARGIN_WIDTH;
			canvas.ClipToBounds = true;
			menuService.InitializeContextMenu(VisualElement, new Guid(MenuConstants.GUIDOBJ_GLYPHMARGIN_GUID), marginContextMenuHandlerProviderService.Create(wpfHexViewHost, this, PredefinedHexMarginNames.Glyph), null, new Guid(MenuConstants.GLYPHMARGIN_GUID));
		}

		void UpdateVisibility() => canvas.Visibility = Enabled ? Visibility.Visible : Visibility.Collapsed;
		void HexView_ZoomLevelChanged(object sender, VSTE.ZoomLevelChangedEventArgs e) {
			canvas.LayoutTransform = e.ZoomTransform;
			DsImage.SetZoom(canvas, e.NewZoomLevel / 100);
		}

		public override HexViewMargin GetHexViewMargin(string marginName) =>
			StringComparer.OrdinalIgnoreCase.Equals(marginName, PredefinedHexMarginNames.Glyph) ? this : null;

		void Options_OptionChanged(object sender, VSTE.EditorOptionChangedEventArgs e) {
			if (e.OptionId == DefaultHexViewHostOptions.GlyphMarginName)
				UpdateVisibility();
		}

		HexMouseProcessor[] CreateMouseProcessors() {
			var list = new List<HexMouseProcessor>();
			foreach (var lazy in lazyGlyphMouseProcessorProviders) {
				if (lazy.Metadata.GlyphMargins == null || !lazy.Metadata.GlyphMargins.Any()) {
					// Nothing
				}
				else if (!lazy.Metadata.GlyphMargins.Any(a => StringComparer.OrdinalIgnoreCase.Equals(a, CTC.ThemeClassificationTypeNameKeys.HexGlyphMargin)))
					continue;
				var mouseProcessor = lazy.Value.GetAssociatedMouseProcessor(wpfHexViewHost, this);
				if (mouseProcessor == null)
					continue;
				list.Add(mouseProcessor);
			}
			return list.ToArray();
		}

		void InitializeGlyphFactories() {
			var oldFactories = new Dictionary<HexGlyphFactoryProvider, HexGlyphFactory>();
			foreach (var info in glyphFactories.Values)
				oldFactories[info.FactoryProvider] = info.Factory;
			glyphFactories.Clear();

			bool newFactory = false;
			int order = 0;
			foreach (var lazy in lazyGlyphFactoryProviders) {
				HexGlyphFactory glyphFactory = null;
				foreach (var type in lazy.Metadata.TagTypes) {
					Debug.Assert(type != null);
					if (type == null)
						break;
					Debug.Assert(!glyphFactories.ContainsKey(type));
					if (glyphFactories.ContainsKey(type))
						continue;
					Debug.Assert(typeof(HexGlyphTag).IsAssignableFrom(type));
					if (!typeof(HexGlyphTag).IsAssignableFrom(type))
						continue;

					if (glyphFactory == null) {
						if (oldFactories.TryGetValue(lazy.Value, out glyphFactory))
							oldFactories.Remove(lazy.Value);
						else {
							glyphFactory = lazy.Value.GetGlyphFactory(wpfHexViewHost.HexView, this);
							if (glyphFactory == null)
								break;
							newFactory = true;
						}
					}

					glyphFactories.Add(type, new GlyphFactoryInfo(order++, glyphFactory, lazy.Value));
				}
			}

			foreach (var factory in oldFactories.Values)
				(factory as IDisposable)?.Dispose();
			if (newFactory || oldFactories.Count != 0) {
				childCanvases = glyphFactories.Values.OrderBy(a => a.Order).Select(a => a.Canvas).ToArray();
				iconCanvas.Children.Clear();
				foreach (var c in childCanvases)
					iconCanvas.Children.Add(c);

				RefreshEverything();
			}
		}

		void Initialize() {
			if (mouseProcessorCollection != null)
				return;
			iconCanvas = new Canvas { Background = Brushes.Transparent };
			canvas.Children.Add(iconCanvas);
			mouseProcessorCollection = new HexMouseProcessorCollection(VisualElement, null, new DefaultHexMouseProcessor(), CreateMouseProcessors(), null);
			lineInfos = new Dictionary<object, LineInfo>();
			tagAggregator = viewTagAggregatorFactoryService.CreateTagAggregator<HexGlyphTag>(wpfHexViewHost.HexView);
			editorFormatMap = editorFormatMapService.GetEditorFormatMap(wpfHexViewHost.HexView);
			InitializeGlyphFactories();
		}

		void GlyphMargin_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) {
			if (canvas.Visibility == Visibility.Visible && !wpfHexViewHost.IsClosed) {
				Initialize();
				RegisterEvents();
				UpdateBackground();
				Canvas.SetTop(iconCanvas, -wpfHexViewHost.HexView.ViewportTop);
				RefreshEverything();
			}
			else {
				UnregisterEvents();
				lineInfos?.Clear();
				foreach (var c in childCanvases)
					c.Children.Clear();
			}
		}

		void RefreshEverything() {
			lineInfos.Clear();
			foreach (var c in childCanvases)
				c.Children.Clear();
			OnNewLayout(wpfHexViewHost.HexView.HexViewLines, Array.Empty<HexViewLine>());
		}

		void HexView_LayoutChanged(object sender, HexViewLayoutChangedEventArgs e) {
			if (e.OldViewState.ViewportTop != e.NewViewState.ViewportTop)
				Canvas.SetTop(iconCanvas, -wpfHexViewHost.HexView.ViewportTop);
			OnNewLayout(e.NewOrReformattedLines, e.TranslatedLines);
		}

		void OnNewLayout(IReadOnlyList<HexViewLine> newOrReformattedLines, IReadOnlyList<HexViewLine> translatedLines) {
			var newInfos = new Dictionary<object, LineInfo>();

			foreach (var line in newOrReformattedLines)
				AddLine(newInfos, line);

			foreach (var line in translatedLines) {
				bool b = lineInfos.TryGetValue(line.IdentityTag, out var info);
				Debug.Assert(b);
				if (!b)
					continue;
				lineInfos.Remove(line.IdentityTag);
				newInfos.Add(line.IdentityTag, info);
				foreach (var iconInfo in info.Icons)
					Canvas.SetTop(iconInfo.Element, iconInfo.BaseTopValue + line.TextTop);
			}

			foreach (var line in wpfHexViewHost.HexView.HexViewLines) {
				if (newInfos.ContainsKey(line.IdentityTag))
					continue;
				if (!lineInfos.TryGetValue(line.IdentityTag, out var info))
					continue;
				lineInfos.Remove(line.IdentityTag);
				newInfos.Add(line.IdentityTag, info);
			}

			foreach (var info in lineInfos.Values) {
				foreach (var iconInfo in info.Icons)
					childCanvases[iconInfo.Order].Children.Remove(iconInfo.Element);
			}
			lineInfos = newInfos;
		}

		void AddLine(Dictionary<object, LineInfo> newInfos, HexViewLine line) {
			var wpfLine = line as WpfHexViewLine;
			Debug.Assert(wpfLine != null);
			if (wpfLine == null)
				return;
			var info = new LineInfo(line, CreateIconInfos(wpfLine));
			newInfos.Add(line.IdentityTag, info);
			foreach (var iconInfo in info.Icons)
				childCanvases[iconInfo.Order].Children.Add(iconInfo.Element);
		}

		List<IconInfo> CreateIconInfos(WpfHexViewLine line) {
			var icons = new List<IconInfo>();
			foreach (var glyphTag in GetGlyphTags(line)) {
				Debug.Assert(glyphTag != null);
				if (glyphTag == null)
					continue;
				// Fails if someone forgot to Export(typeof(HexGlyphFactoryProvider)) with the correct tag types
				bool b = glyphFactories.TryGetValue(glyphTag.GetType(), out var factoryInfo);
				Debug.Assert(b);
				if (!b)
					continue;
				var elem = factoryInfo.Factory.GenerateGlyph(line, glyphTag);
				if (elem == null)
					continue;
				elem.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
				var iconInfo = new IconInfo(factoryInfo.Order, elem);
				icons.Add(iconInfo);
				// ActualWidth isn't always valid when we're here so use the constant
				Canvas.SetLeft(elem, (MARGIN_WIDTH - elem.DesiredSize.Width) / 2);
				Canvas.SetTop(elem, iconInfo.BaseTopValue + line.TextTop);
			}
			return icons;
		}

		IEnumerable<HexGlyphTag> GetGlyphTags(WpfHexViewLine line) {
			foreach (var tagSpan in tagAggregator.GetTags(line.BufferSpan)) {
				if (line.IntersectsBufferSpan(tagSpan.Span))
					yield return tagSpan.Tag;
			}
			var taggerContext = new HexTaggerContext(line.BufferLine, line.BufferLine.TextSpan);
			foreach (var tagSpan in tagAggregator.GetLineTags(taggerContext))
				yield return tagSpan.Tag;
		}

		void TagAggregator_BatchedTagsChanged(object sender, HexBatchedTagsChangedEventArgs e) {
			canvas.Dispatcher.VerifyAccess();
			HashSet<HexViewLine> checkedLines = null;
			foreach (var span in e.Spans)
				Update(span, ref checkedLines);
		}

		void Update(HexBufferSpan span, ref HashSet<HexViewLine> checkedLines) {
			Debug.Assert(span.Buffer == wpfHexViewHost.HexView.Buffer);
			var intersection = span.Intersection(wpfHexViewHost.HexView.HexViewLines.FormattedSpan);
			if (intersection == null)
				return;
			var point = intersection.Value.Start;
			while (point <= intersection.Value.End) {
				var line = wpfHexViewHost.HexView.WpfHexViewLines.GetWpfHexViewLineContainingBufferPosition(point);
				if (line == null)
					break;
				if (checkedLines == null)
					checkedLines = new HashSet<HexViewLine>();
				if (!checkedLines.Contains(line)) {
					checkedLines.Add(line);
					Update(line);
				}
				if (line.IsLastDocumentLine())
					break;
				point = line.BufferEnd;
			}
		}

		void Update(WpfHexViewLine line) {
			Debug.Assert(line.VisibilityState != VSTF.VisibilityState.Unattached);
			if (!lineInfos.TryGetValue(line.IdentityTag, out var info))
				return;
			lineInfos.Remove(line.IdentityTag);
			foreach (var iconInfo in info.Icons)
				childCanvases[iconInfo.Order].Children.Remove(iconInfo.Element);
			AddLine(lineInfos, line);
		}

		void EditorFormatMap_FormatMappingChanged(object sender, VSTC.FormatItemsEventArgs e) {
			if (e.ChangedItems.Contains(CTC.ThemeClassificationTypeNameKeys.HexGlyphMargin))
				UpdateBackground();
		}

		void UpdateBackground() {
			if (editorFormatMap == null)
				return;
			var props = editorFormatMap.GetProperties(CTC.ThemeClassificationTypeNameKeys.HexGlyphMargin);
			var newBackground = TE.ResourceDictionaryUtilities.GetBackgroundBrush(props, Brushes.Transparent);
			if (!TWPF.BrushComparer.Equals(canvas.Background, newBackground)) {
				canvas.Background = newBackground;
				// The images could depend on the background color, so recreate every icon
				if (childCanvases.Any(a => a.Children.Count > 0))
					canvas.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(RefreshEverything));
			}
		}

		bool hasRegisteredEvents;
		void RegisterEvents() {
			if (hasRegisteredEvents)
				return;
			if (wpfHexViewHost.IsClosed)
				return;
			hasRegisteredEvents = true;
			editorFormatMap.FormatMappingChanged += EditorFormatMap_FormatMappingChanged;
			tagAggregator.BatchedTagsChanged += TagAggregator_BatchedTagsChanged;
			wpfHexViewHost.HexView.LayoutChanged += HexView_LayoutChanged;
		}

		void UnregisterEvents() {
			hasRegisteredEvents = false;
			if (editorFormatMap != null)
				editorFormatMap.FormatMappingChanged -= EditorFormatMap_FormatMappingChanged;
			if (tagAggregator != null)
				tagAggregator.BatchedTagsChanged -= TagAggregator_BatchedTagsChanged;
			wpfHexViewHost.HexView.LayoutChanged -= HexView_LayoutChanged;
		}

		protected override void DisposeCore() {
			wpfHexViewHost.HexView.Options.OptionChanged -= Options_OptionChanged;
			wpfHexViewHost.HexView.ZoomLevelChanged -= HexView_ZoomLevelChanged;
			canvas.IsVisibleChanged -= GlyphMargin_IsVisibleChanged;
			UnregisterEvents();
			lineInfos?.Clear();
			iconCanvas?.Children.Clear();
			mouseProcessorCollection?.Dispose();
			tagAggregator?.Dispose();
		}
	}
}
