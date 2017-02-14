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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Classification;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Editor.OptionsExtensionMethods;
using dnSpy.Contracts.Hex.Formatting;
using CTC = dnSpy.Contracts.Text.Classification;
using TWPF = dnSpy.Text.WPF;
using VSTC = Microsoft.VisualStudio.Text.Classification;
using VSTE = Microsoft.VisualStudio.Text.Editor;
using VSTF = Microsoft.VisualStudio.Text.Formatting;
using VSUTIL = Microsoft.VisualStudio.Utilities;

namespace dnSpy.Hex.Editor {
	[Export(typeof(WpfHexViewCreationListener))]
	[VSTE.TextViewRole(PredefinedHexViewRoles.CanHighlightActiveColumn)]
	sealed class ActiveColumnHighlighterWpfHexViewCreationListener : WpfHexViewCreationListener {
		readonly ActiveColumnHighlighterServiceProvider activeColumnHighlighterServiceProvider;

		[ImportingConstructor]
		ActiveColumnHighlighterWpfHexViewCreationListener(ActiveColumnHighlighterServiceProvider activeColumnHighlighterServiceProvider) => this.activeColumnHighlighterServiceProvider = activeColumnHighlighterServiceProvider;

		public override void HexViewCreated(WpfHexView wpfHexView) =>
			activeColumnHighlighterServiceProvider.InstallService(wpfHexView);
	}

	abstract class ActiveColumnHighlighterServiceProvider {
		public abstract void InstallService(WpfHexView wpfHexView);
	}

	[Export(typeof(ActiveColumnHighlighterServiceProvider))]
	sealed class ActiveColumnHighlighterServiceProviderImpl : ActiveColumnHighlighterServiceProvider {
		readonly HexEditorFormatMapService editorFormatMapService;

		[ImportingConstructor]
		ActiveColumnHighlighterServiceProviderImpl(HexEditorFormatMapService editorFormatMapService) => this.editorFormatMapService = editorFormatMapService;

		public override void InstallService(WpfHexView wpfHexView) {
			if (wpfHexView == null)
				throw new ArgumentNullException(nameof(wpfHexView));
			wpfHexView.Properties.GetOrCreateSingletonProperty(typeof(ActiveColumnHighlighterService), () => new ActiveColumnHighlighterService(wpfHexView, editorFormatMapService));
		}
	}

	sealed class ActiveColumnHighlighterService {
		readonly WpfHexView wpfHexView;
		readonly VSTC.IEditorFormatMap editorFormatMap;
		HexAdornmentLayer adornmentLayer;
		bool enabled;
		readonly List<RectangleElement> rectangleElements;

		sealed class RectangleElement : UIElement {
			public HexColumnType Column { get; }
			readonly Rect rect;
			readonly Brush brush;
			readonly Pen pen;

			public RectangleElement(HexColumnType column, Rect rect, Brush brush, Pen pen) {
				Canvas.SetTop(this, 0);
				Column = column;
				this.rect = rect;
				this.brush = brush;
				this.pen = pen;
			}

			protected override void OnRender(DrawingContext drawingContext) {
				base.OnRender(drawingContext);
				drawingContext.DrawRectangle(brush, pen, rect);
			}
		}

#pragma warning disable 0169
		[Export(typeof(HexAdornmentLayerDefinition))]
		[VSUTIL.Name(PredefinedHexAdornmentLayers.ActiveColumnHighlighter)]
		[VSUTIL.Order(Before = PredefinedHexAdornmentLayers.BottomLayer)]
		[VSUTIL.Order(Before = PredefinedHexAdornmentLayers.TopLayer)]
		static HexAdornmentLayerDefinition theAdornmentLayerDefinition;
#pragma warning restore 0169

		public ActiveColumnHighlighterService(WpfHexView wpfHexView, HexEditorFormatMapService editorFormatMapService) {
			if (editorFormatMapService == null)
				throw new ArgumentNullException(nameof(editorFormatMapService));
			rectangleElements = new List<RectangleElement>();
			this.wpfHexView = wpfHexView ?? throw new ArgumentNullException(nameof(wpfHexView));
			editorFormatMap = editorFormatMapService.GetEditorFormatMap(wpfHexView);
			wpfHexView.Closed += WpfHexView_Closed;
			wpfHexView.Options.OptionChanged += Options_OptionChanged;
			UpdateEnabled();
		}

		void UpdateEnabled() {
			var newEnabled = wpfHexView.Options.HighlightActiveColumn();
			if (newEnabled == enabled)
				return;
			enabled = newEnabled;

			if (enabled) {
				if (adornmentLayer == null)
					adornmentLayer = wpfHexView.GetAdornmentLayer(PredefinedHexAdornmentLayers.ActiveColumnHighlighter);
				HookEnabledEvents();
			}
			else
				UnhookEnabledEvents();

			DelayRecreateRectangles();
		}

		void HookEnabledEvents() {
			wpfHexView.LayoutChanged += WpfHexView_LayoutChanged;
			editorFormatMap.FormatMappingChanged += EditorFormatMap_FormatMappingChanged;
			wpfHexView.Caret.PositionChanged += Caret_PositionChanged;
		}

		void UnhookEnabledEvents() {
			wpfHexView.LayoutChanged -= WpfHexView_LayoutChanged;
			editorFormatMap.FormatMappingChanged -= EditorFormatMap_FormatMappingChanged;
			wpfHexView.Caret.PositionChanged -= Caret_PositionChanged;
		}

		void Caret_PositionChanged(object sender, HexCaretPositionChangedEventArgs e) {
			if (e.OldPosition.Position.ActiveColumn != e.NewPosition.Position.ActiveColumn)
				RecreateRectangles();
		}

		void Options_OptionChanged(object sender, VSTE.EditorOptionChangedEventArgs e) {
			switch (e.OptionId) {
			case DefaultHexViewOptions.HighlightActiveColumnName:
				UpdateEnabled();
				break;
			}
		}

		void WpfHexView_LayoutChanged(object sender, HexViewLayoutChangedEventArgs e) {
			bool recreate = false;
			if (latestBufferLines != wpfHexView.BufferLines)
				recreate = true;
			else if (e.OldViewState.ViewportHeight != e.NewViewState.ViewportHeight)
				recreate = true;
			if (recreate)
				RecreateRectangles();
			else
				UpdateRectanglesPositions(e);
		}
		HexBufferLineFormatter latestBufferLines;

		void UpdateRectanglesPositions(HexViewLayoutChangedEventArgs e) {
			var d = e.NewViewState.ViewportTop - e.OldViewState.ViewportTop;
			if (Math.Abs(d) <= 0.001)
				return;
			foreach (var rectElem in rectangleElements)
				Canvas.SetTop(rectElem, Canvas.GetTop(rectElem) + d);
		}

		void EditorFormatMap_FormatMappingChanged(object sender, VSTC.FormatItemsEventArgs e) {
			if (wpfHexView.IsClosed)
				return;
			bool refresh = e.ChangedItems.Contains(CTC.ThemeClassificationTypeNameKeys.HexHighlightedValuesColumn) ||
					e.ChangedItems.Contains(CTC.ThemeClassificationTypeNameKeys.HexHighlightedAsciiColumn);
			if (refresh)
				DelayRecreateRectangles();
		}

		void DelayRecreateRectangles() {
			if (delayRecreateRectanglesCalled)
				return;
			wpfHexView.VisualElement.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(RecreateRectangles));
		}
		bool delayRecreateRectanglesCalled;

		void RecreateRectangles() {
			delayRecreateRectanglesCalled = false;
			if (wpfHexView.IsClosed)
				return;

			RemoveAllRectangles();
			if (!enabled)
				return;

			if (wpfHexView.ViewportHeight == 0)
				return;

			var line = wpfHexView.HexViewLines.FirstVisibleLine;
			var top = wpfHexView.ViewportTop;
			var bottom = wpfHexView.ViewportBottom;
			foreach (var info in GetRectanglePositions(line)) {
				var props = editorFormatMap.GetProperties(GetClassificationTypeName(info.type));
				var bgBrush = GetBackgroundBrush(props);
				if (bgBrush == null || TWPF.BrushComparer.Equals(bgBrush, Brushes.Transparent))
					continue;
				var lineElem = new RectangleElement(info.type, info.rect, bgBrush, null);
				bool added = adornmentLayer.AddAdornment(VSTE.AdornmentPositioningBehavior.OwnerControlled, (HexBufferSpan?)null, null, lineElem, null);
				if (added)
					rectangleElements.Add(lineElem);
			}

			latestBufferLines = wpfHexView.BufferLines;
		}

		static string GetClassificationTypeName(HexColumnType column) {
			switch (column) {
			case HexColumnType.Values:	return CTC.ThemeClassificationTypeNameKeys.HexHighlightedValuesColumn;
			case HexColumnType.Ascii:	return CTC.ThemeClassificationTypeNameKeys.HexHighlightedAsciiColumn;
			case HexColumnType.Offset:
			default:
				throw new ArgumentOutOfRangeException(nameof(column));
			}
		}

		Brush GetBackgroundBrush(ResourceDictionary props) {
			Color? color;
			SolidColorBrush scBrush;
			Brush fillBrush;

			const double BG_BRUSH_OPACITY = 0.4;
			Brush newBrush;
			if ((color = props[VSTC.EditorFormatDefinition.BackgroundColorId] as Color?) != null) {
				newBrush = new SolidColorBrush(color.Value);
				newBrush.Opacity = BG_BRUSH_OPACITY;
				newBrush.Freeze();
			}
			else if ((scBrush = props[VSTC.EditorFormatDefinition.BackgroundBrushId] as SolidColorBrush) != null) {
				newBrush = new SolidColorBrush(scBrush.Color);
				newBrush.Opacity = BG_BRUSH_OPACITY;
				newBrush.Freeze();
			}
			else if ((fillBrush = props[VSTC.MarkerFormatDefinition.FillId] as Brush) != null) {
				newBrush = fillBrush;
				if (newBrush.CanFreeze)
					newBrush.Freeze();
			}
			else
				return null;

			return newBrush;
		}

		static Rect? GetBounds(IList<VSTF.TextBounds> boundsColl) {
			double left = double.PositiveInfinity, right = double.NegativeInfinity;
			double top = double.PositiveInfinity, bottom = double.NegativeInfinity;
			foreach (var bounds in boundsColl) {
				left = Math.Min(left, bounds.Left);
				right = Math.Max(right, bounds.Right);
				top = Math.Min(top, bounds.TextTop);
				bottom = Math.Max(bottom, bounds.TextBottom);
			}
			bool b = left != double.PositiveInfinity && right != double.NegativeInfinity &&
				top != double.PositiveInfinity && bottom != double.NegativeInfinity;
			if (!b)
				return null;
			if (left > right)
				right = left;
			if (top > bottom)
				bottom = top;
			return new Rect(left, top, right - left, bottom - top);
		}

		IEnumerable<(HexColumnType type, Rect rect)> GetRectanglePositions(HexViewLine line) {
			var column = wpfHexView.Caret.Position.Position.ActiveColumn;
			if (!line.BufferLine.IsColumnPresent(column))
				yield break;
			var span = line.BufferLine.GetSpan(column, onlyVisibleCells: false);
			var rect = GetBounds(line.GetNormalizedTextBounds(span));
			if (rect == null || rect.Value.Width <= 0)
				yield break;
			yield return (column, new Rect(rect.Value.X, wpfHexView.ViewportTop, rect.Value.Width, wpfHexView.ViewportHeight));
		}

		void RemoveAllRectangles() {
			adornmentLayer?.RemoveAllAdornments();
			rectangleElements.Clear();
		}

		void WpfHexView_Closed(object sender, EventArgs e) {
			RemoveAllRectangles();
			latestBufferLines = null;
			wpfHexView.Closed -= WpfHexView_Closed;
			wpfHexView.Options.OptionChanged -= Options_OptionChanged;
			UnhookEnabledEvents();
		}
	}
}
