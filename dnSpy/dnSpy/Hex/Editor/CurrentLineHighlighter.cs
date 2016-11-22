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
using dnSpy.Hex.Formatting;
using CTC = dnSpy.Contracts.Text.Classification;
using TE = dnSpy.Text.Editor;
using TWPF = dnSpy.Text.WPF;
using VSTC = Microsoft.VisualStudio.Text.Classification;
using VSTE = Microsoft.VisualStudio.Text.Editor;
using VSUTIL = Microsoft.VisualStudio.Utilities;

namespace dnSpy.Hex.Editor {
	[Export(typeof(WpfHexViewCreationListener))]
	[VSTE.TextViewRole(PredefinedHexViewRoles.Document)]
	[VSTE.TextViewRole(PredefinedHexViewRoles.CanHaveCurrentLineHighlighter)]
	sealed class CurrentLineHighlighterWpfHexViewCreationListener : WpfHexViewCreationListener {
		readonly HexEditorFormatMapService editorFormatMapService;

		[ImportingConstructor]
		CurrentLineHighlighterWpfHexViewCreationListener(HexEditorFormatMapService editorFormatMapService) {
			this.editorFormatMapService = editorFormatMapService;
		}

		public override void HexViewCreated(WpfHexView hexView) =>
			hexView.Properties.GetOrCreateSingletonProperty(() => new CurrentLineHighlighter(hexView, editorFormatMapService.GetEditorFormatMap(hexView)));

		public static void RemoveFromProperties(WpfHexView hexView) =>
			hexView.Properties.RemoveProperty(typeof(CurrentLineHighlighter));
	}

	sealed class CurrentLineHighlighter {
#pragma warning disable 0169
		[Export(typeof(HexAdornmentLayerDefinition))]
		[VSUTIL.Name(PredefinedHexAdornmentLayers.CurrentLineHighlighter)]
		[VSUTIL.Order(After = PredefinedHexAdornmentLayers.BottomLayer, Before = PredefinedHexAdornmentLayers.TopLayer)]
		[VSUTIL.Order(Before = PredefinedHexAdornmentLayers.Caret)]
		[VSUTIL.Order(Before = PredefinedHexAdornmentLayers.Selection)]
		[VSUTIL.Order(Before = PredefinedHexAdornmentLayers.Text)]
		[VSUTIL.Order(Before = PredefinedHexAdornmentLayers.TextMarker)]
		[VSUTIL.Order(Before = PredefinedHexAdornmentLayers.GlyphTextMarker)]
		[VSUTIL.Order(After = PredefinedHexAdornmentLayers.Outlining)]
		static HexAdornmentLayerDefinition theAdornmentLayerDefinition;
#pragma warning restore 0169

		readonly WpfHexView wpfHexView;
		readonly VSTC.IEditorFormatMap editorFormatMap;
		readonly CurrentLineHighlighterElement currentLineHighlighterElement;
		HexAdornmentLayer adornmentLayer;
		bool isActive;
		bool selectionIsEmpty;
		bool enabled;

		public CurrentLineHighlighter(WpfHexView wpfHexView, VSTC.IEditorFormatMap editorFormatMap) {
			if (wpfHexView == null)
				throw new ArgumentNullException(nameof(wpfHexView));
			if (editorFormatMap == null)
				throw new ArgumentNullException(nameof(editorFormatMap));
			this.wpfHexView = wpfHexView;
			this.editorFormatMap = editorFormatMap;
			this.currentLineHighlighterElement = new CurrentLineHighlighterElement();
			wpfHexView.Closed += WpfHexView_Closed;
			wpfHexView.Options.OptionChanged += Options_OptionChanged;
			UpdateEnableState();
		}

		void Options_OptionChanged(object sender, VSTE.EditorOptionChangedEventArgs e) {
			if (e.OptionId == DefaultWpfHexViewOptions.EnableHighlightCurrentLineName)
				UpdateEnableState();
		}

		bool hasHookedEvents;
		void UpdateEnableState() {
			enabled = wpfHexView.Options.IsHighlightCurrentLineEnabled();
			if (enabled) {
				if (adornmentLayer == null)
					adornmentLayer = wpfHexView.GetAdornmentLayer(PredefinedHexAdornmentLayers.CurrentLineHighlighter);
				if (!hasHookedEvents) {
					RegisterEnabledEvents();
					isActive = wpfHexView.HasAggregateFocus;
					selectionIsEmpty = wpfHexView.Selection.IsEmpty;
					isActive = wpfHexView.HasAggregateFocus;
					UpdateLineElementBrushes();
					PositionLineElement();
				}
			}
			else {
				adornmentLayer?.RemoveAllAdornments();
				if (hasHookedEvents)
					UnregisterEnabledEvents();
			}
		}

		void RegisterEnabledEvents() {
			Debug.Assert(!hasHookedEvents);
			if (hasHookedEvents)
				return;
			editorFormatMap.FormatMappingChanged += EditorFormatMap_FormatMappingChanged;
			wpfHexView.GotAggregateFocus += WpfHexView_GotAggregateFocus;
			wpfHexView.LostAggregateFocus += WpfHexView_LostAggregateFocus;
			wpfHexView.Selection.SelectionChanged += Selection_SelectionChanged;
			wpfHexView.Caret.PositionChanged += Caret_PositionChanged;
			wpfHexView.LayoutChanged += WpfHexView_LayoutChanged;
		}

		void UnregisterEnabledEvents() {
			hasHookedEvents = false;
			editorFormatMap.FormatMappingChanged -= EditorFormatMap_FormatMappingChanged;
			wpfHexView.GotAggregateFocus -= WpfHexView_GotAggregateFocus;
			wpfHexView.LostAggregateFocus -= WpfHexView_LostAggregateFocus;
			wpfHexView.Selection.SelectionChanged -= Selection_SelectionChanged;
			wpfHexView.Caret.PositionChanged -= Caret_PositionChanged;
			wpfHexView.LayoutChanged -= WpfHexView_LayoutChanged;
		}

		void PositionLineElement() {
			if (!selectionIsEmpty || !enabled) {
				adornmentLayer?.RemoveAllAdornments();
				return;
			}

			var line = wpfHexView.Caret.ContainingHexViewLine;
			if (line.IsVisible()) {
				if (adornmentLayer.IsEmpty)
					adornmentLayer.AddAdornment(VSTE.AdornmentPositioningBehavior.OwnerControlled, (HexBufferSpan?)null, null, currentLineHighlighterElement, null);
				Canvas.SetLeft(currentLineHighlighterElement, wpfHexView.ViewportLeft);
				Canvas.SetTop(currentLineHighlighterElement, line.TextTop);
				currentLineHighlighterElement.SetLine(line, wpfHexView.ViewportWidth);
			}
			else
				adornmentLayer.RemoveAllAdornments();
		}

		void WpfHexView_LayoutChanged(object sender, HexViewLayoutChangedEventArgs e) => PositionLineElement();
		void Caret_PositionChanged(object sender, HexCaretPositionChangedEventArgs e) => PositionLineElement();

		void Selection_SelectionChanged(object sender, EventArgs e) {
			bool newSelectionIsEmpty = wpfHexView.Selection.IsEmpty;
			if (selectionIsEmpty == newSelectionIsEmpty)
				return;
			selectionIsEmpty = newSelectionIsEmpty;
			PositionLineElement();
		}

		void WpfHexView_GotAggregateFocus(object sender, EventArgs e) => UpdateFocus();
		void WpfHexView_LostAggregateFocus(object sender, EventArgs e) => UpdateFocus();

		void UpdateFocus() {
			bool newIsActive = wpfHexView.HasAggregateFocus;
			if (newIsActive == isActive)
				return;
			isActive = newIsActive;
			UpdateLineElementBrushes();
		}

		void UpdateLineElementBrushes() {
			var props = editorFormatMap.GetProperties(isActive ? CTC.ThemeClassificationTypeNameKeys.HexCurrentLine : CTC.ThemeClassificationTypeNameKeys.HexCurrentLineNoFocus);
			currentLineHighlighterElement.ForegroundBrush = TE.ResourceDictionaryUtilities.GetForegroundBrush(props);
			currentLineHighlighterElement.BackgroundBrush = TE.ResourceDictionaryUtilities.GetBackgroundBrush(props);
		}

		void EditorFormatMap_FormatMappingChanged(object sender, VSTC.FormatItemsEventArgs e) {
			if ((isActive && e.ChangedItems.Contains(CTC.ThemeClassificationTypeNameKeys.HexCurrentLine)) ||
				(!isActive && e.ChangedItems.Contains(CTC.ThemeClassificationTypeNameKeys.HexCurrentLineNoFocus))) {
				UpdateLineElementBrushes();
			}
		}

		void WpfHexView_Closed(object sender, EventArgs e) {
			wpfHexView.Closed -= WpfHexView_Closed;
			wpfHexView.Options.OptionChanged -= Options_OptionChanged;
			UnregisterEnabledEvents();
			CurrentLineHighlighterWpfHexViewCreationListener.RemoveFromProperties(wpfHexView);
		}
	}

	sealed class CurrentLineHighlighterElement : UIElement {
		const int PEN_THICKNESS = 2;

		public Brush BackgroundBrush {
			get { return backgroundBrush; }
			set {
				if (!TWPF.BrushComparer.Equals(backgroundBrush, value)) {
					backgroundBrush = value;
					InvalidateVisual();
				}
			}
		}
		Brush backgroundBrush;

		public Brush ForegroundBrush {
			get { return foregroundBrush; }
			set {
				if (!TWPF.BrushComparer.Equals(foregroundBrush, value)) {
					foregroundBrush = value;
					if (foregroundBrush == null)
						pen = null;
					else {
						pen = new Pen(foregroundBrush, PEN_THICKNESS);
						pen.DashCap = PenLineCap.Flat;
						if (pen.CanFreeze)
							pen.Freeze();
					}
					InvalidateVisual();
				}
			}
		}
		Brush foregroundBrush;
		Pen pen;

		Rect geometryRect;
		Geometry geometry;

		public void SetLine(HexViewLine line, double width) {
			if (line == null)
				throw new ArgumentNullException(nameof(line));
			var newRect = new Rect(PEN_THICKNESS / 2, PEN_THICKNESS / 2, Math.Max(0, width - PEN_THICKNESS), Math.Max(0, line.TextHeight + HexFormattedLineImpl.DEFAULT_BOTTOM_SPACE - PEN_THICKNESS));
			if (geometry != null && newRect == geometryRect)
				return;
			geometryRect = newRect;
			if (geometryRect.Height == 0 || geometryRect.Width == 0)
				geometry = null;
			else {
				geometry = new RectangleGeometry(geometryRect);
				if (geometry.CanFreeze)
					geometry.Freeze();
			}
			InvalidateVisual();
		}

		protected override void OnRender(DrawingContext drawingContext) {
			base.OnRender(drawingContext);
			if (geometry != null)
				drawingContext.DrawGeometry(BackgroundBrush, pen, geometry);
		}
	}
}
