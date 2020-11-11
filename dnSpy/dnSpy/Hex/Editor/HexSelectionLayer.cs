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
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Editor.OptionsExtensionMethods;
using CTC = dnSpy.Contracts.Text.Classification;
using TE = dnSpy.Text.Editor;
using TWPF = dnSpy.Text.WPF;
using VSTC = Microsoft.VisualStudio.Text.Classification;
using VSTE = Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Hex.Editor {
	sealed class HexSelectionLayer {
		public bool IsActive {
			get => isActive;
			set {
				if (isActive != value) {
					isActive = value;
					UpdateBackgroundBrush();
				}
			}
		}
		bool isActive;

		readonly HexSelectionImpl hexSelection;
		readonly HexAdornmentLayer layer;
		readonly VSTC.IEditorFormatMap editorFormatMap;

		public HexSelectionLayer(HexSelectionImpl hexSelection, HexAdornmentLayer layer, VSTC.IEditorFormatMap editorFormatMap) {
			markerElementRemovedCallBack = (tag, element) => OnMarkerElementRemoved();
			this.hexSelection = hexSelection ?? throw new ArgumentNullException(nameof(hexSelection));
			this.layer = layer ?? throw new ArgumentNullException(nameof(layer));
			this.editorFormatMap = editorFormatMap ?? throw new ArgumentNullException(nameof(editorFormatMap));
			hexSelection.HexView.Options.OptionChanged += Options_OptionChanged;
			hexSelection.SelectionChanged += HexSelection_SelectionChanged;
			hexSelection.HexView.LayoutChanged += HexView_LayoutChanged;
			editorFormatMap.FormatMappingChanged += EditorFormatMap_FormatMappingChanged;
			UpdateBackgroundBrush();
		}

		void UpdateBackgroundBrush() {
			UpdateIsInContrastModeOption();
			var newBackgroundBrush = GetBackgroundBrush();
			if (TWPF.BrushComparer.Equals(newBackgroundBrush, backgroundBrush))
				return;
			backgroundBrush = newBackgroundBrush;
			if (markerElement is not null)
				markerElement.BackgroundBrush = backgroundBrush;
		}
		Brush? backgroundBrush;
		MarkerElement? markerElement;

		Brush? GetBackgroundBrush() {
			var props = editorFormatMap.GetProperties(IsActive ? CTC.ThemeClassificationTypeNameKeys.HexSelection : CTC.ThemeClassificationTypeNameKeys.HexInactiveSelectedText);
			return TE.ResourceDictionaryUtilities.GetBackgroundBrush(props, IsActive ? SystemColors.HighlightBrush : SystemColors.GrayTextBrush);
		}

		void Options_OptionChanged(object? sender, VSTE.EditorOptionChangedEventArgs e) {
			if (e.OptionId == DefaultHexViewHostOptions.IsInContrastModeName)
				UpdateIsInContrastModeOption();
		}

		void UpdateIsInContrastModeOption() {
			bool isInContrastMode = hexSelection.HexView.Options.IsInContrastMode();
			var newValue = isInContrastMode ? 1 : 0.4;
			if (layer.Opacity != newValue)
				layer.Opacity = newValue;
		}

		void EditorFormatMap_FormatMappingChanged(object? sender, VSTC.FormatItemsEventArgs e) {
			if ((IsActive && e.ChangedItems.Contains(CTC.ThemeClassificationTypeNameKeys.HexSelection)) ||
				(!IsActive && e.ChangedItems.Contains(CTC.ThemeClassificationTypeNameKeys.HexInactiveSelectedText))) {
				UpdateBackgroundBrush();
			}
		}

		void HexView_LayoutChanged(object? sender, HexViewLayoutChangedEventArgs e) {
			if (e.NewOrReformattedLines.Count > 0 || e.TranslatedLines.Count > 0 || e.VerticalTranslation)
				SetNewSelection();
		}

		void SetNewSelection() {
			RemoveAllAdornments();
			if (hexSelection.IsEmpty)
				return;
			Debug.Assert(hexSelection.StreamSelectionSpan.Length != 0);
			var info = CreateStreamSelection();
			if (info is null)
				return;
			CreateMarkerElement(info.Value.span, info.Value.geometry);
		}

		void CreateMarkerElement(HexBufferSpan fullSpan, Geometry geo) {
			Debug2.Assert(markerElement is null);
			RemoveAllAdornments();
			markerElement = new MarkerElement(geo);
			markerElement.BackgroundBrush = backgroundBrush;
			if (!layer.AddAdornment(VSTE.AdornmentPositioningBehavior.TextRelative, fullSpan, null, markerElement, markerElementRemovedCallBack))
				OnMarkerElementRemoved();
		}
		readonly VSTE.AdornmentRemovedCallback markerElementRemovedCallBack;

		void OnMarkerElementRemoved() => markerElement = null;

		(HexBufferSpan span, Geometry geometry)? CreateStreamSelection() {
			Debug.Assert(!hexSelection.IsEmpty);
			var linesColl = (WpfHexViewLineCollection)hexSelection.HexView.HexViewLines;
			var span = hexSelection.StreamSelectionSpan.Overlap(linesColl.FormattedSpan);
			if (span is null)
				return null;
			var geo = linesColl.GetMarkerGeometry(span.Value, HexSelectionImpl.SelectionFlags);
			if (geo is null)
				return null;
			return (span.Value, geo);
		}

		public void OnModeUpdated() => SetNewSelection();
		void HexSelection_SelectionChanged(object? sender, EventArgs e) => SetNewSelection();

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

			public MarkerElement(Geometry geometry) => this.geometry = geometry ?? throw new ArgumentNullException(nameof(geometry));

			protected override void OnRender(DrawingContext drawingContext) {
				base.OnRender(drawingContext);
				drawingContext.DrawGeometry(BackgroundBrush, Pen, geometry);
			}
		}

		void RemoveAllAdornments() {
			layer.RemoveAllAdornments();
			markerElement = null;
		}

		internal void Dispose() {
			RemoveAllAdornments();
			hexSelection.HexView.Options.OptionChanged -= Options_OptionChanged;
			hexSelection.SelectionChanged -= HexSelection_SelectionChanged;
			hexSelection.HexView.LayoutChanged -= HexView_LayoutChanged;
			editorFormatMap.FormatMappingChanged -= EditorFormatMap_FormatMappingChanged;
		}
	}
}
