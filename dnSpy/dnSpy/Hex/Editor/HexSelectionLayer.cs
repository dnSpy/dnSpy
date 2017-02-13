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
using System.Windows;
using System.Windows.Media;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Editor;
using CTC = dnSpy.Contracts.Text.Classification;
using TE = dnSpy.Text.Editor;
using TWPF = dnSpy.Text.WPF;
using VSTC = Microsoft.VisualStudio.Text.Classification;
using VSTE = Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Hex.Editor {
	sealed class HexSelectionLayer {
		public bool IsActive {
			get { return isActive; }
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
			UpdateUseReducedOpacityForHighContrastOption();
			UpdateBackgroundBrush();
		}

		void UpdateBackgroundBrush() {
			var newBackgroundBrush = GetBackgroundBrush();
			if (TWPF.BrushComparer.Equals(newBackgroundBrush, backgroundBrush))
				return;
			backgroundBrush = newBackgroundBrush;
			if (markerElement != null)
				markerElement.BackgroundBrush = backgroundBrush;
		}
		Brush backgroundBrush;
		MarkerElement markerElement;

		Brush GetBackgroundBrush() {
			var props = editorFormatMap.GetProperties(IsActive ? CTC.ThemeClassificationTypeNameKeys.HexSelection : CTC.ThemeClassificationTypeNameKeys.HexInactiveSelectedText);
			return TE.ResourceDictionaryUtilities.GetBackgroundBrush(props, IsActive ? SystemColors.HighlightBrush : SystemColors.GrayTextBrush);
		}

		void Options_OptionChanged(object sender, VSTE.EditorOptionChangedEventArgs e) {
			if (e.OptionId == DefaultWpfHexViewOptions.UseReducedOpacityForHighContrastOptionName)
				UpdateUseReducedOpacityForHighContrastOption();
		}

		void UpdateUseReducedOpacityForHighContrastOption() {
			bool reducedOpacity = hexSelection.HexView.Options.GetOptionValue(DefaultWpfHexViewOptions.UseReducedOpacityForHighContrastOptionId);
			layer.Opacity = reducedOpacity ? 0.4 : 1;
		}

		void EditorFormatMap_FormatMappingChanged(object sender, VSTC.FormatItemsEventArgs e) {
			if ((IsActive && e.ChangedItems.Contains(CTC.ThemeClassificationTypeNameKeys.HexSelection)) ||
				(!IsActive && e.ChangedItems.Contains(CTC.ThemeClassificationTypeNameKeys.HexInactiveSelectedText))) {
				UpdateBackgroundBrush();
			}
		}

		void HexView_LayoutChanged(object sender, HexViewLayoutChangedEventArgs e) {
			if (e.NewOrReformattedLines.Count > 0 || e.TranslatedLines.Count > 0 || e.VerticalTranslation)
				SetNewSelection();
		}

		void SetNewSelection() {
			RemoveAllAdornments();
			if (hexSelection.IsEmpty)
				return;
			Debug.Assert(hexSelection.StreamSelectionSpan.Length != 0);
			var info = CreateStreamSelection();
			if (info == null)
				return;
			CreateMarkerElement(info.Value.Key, info.Value.Value);
		}

		void CreateMarkerElement(HexBufferSpan fullSpan, Geometry geo) {
			Debug.Assert(markerElement == null);
			RemoveAllAdornments();
			markerElement = new MarkerElement(geo);
			markerElement.BackgroundBrush = backgroundBrush;
			if (!layer.AddAdornment(VSTE.AdornmentPositioningBehavior.TextRelative, fullSpan, null, markerElement, markerElementRemovedCallBack))
				OnMarkerElementRemoved();
		}
		readonly VSTE.AdornmentRemovedCallback markerElementRemovedCallBack;

		void OnMarkerElementRemoved() => markerElement = null;

		KeyValuePair<HexBufferSpan, Geometry>? CreateStreamSelection() {
			Debug.Assert(!hexSelection.IsEmpty);
			var linesColl = (WpfHexViewLineCollection)hexSelection.HexView.HexViewLines;
			var span = hexSelection.StreamSelectionSpan.Overlap(linesColl.FormattedSpan);
			if (span == null)
				return null;
			var geo = linesColl.GetMarkerGeometry(span.Value, HexSelectionImpl.SelectionFlags);
			if (geo == null)
				return null;
			return new KeyValuePair<HexBufferSpan, Geometry>(span.Value, geo);
		}

		public void OnModeUpdated() => SetNewSelection();
		void HexSelection_SelectionChanged(object sender, EventArgs e) => SetNewSelection();

		sealed class MarkerElement : UIElement {
			readonly Geometry geometry;

			public Brush BackgroundBrush {
				get { return backgroundBrush; }
				set {
					if (value == null)
						throw new ArgumentNullException(nameof(value));
					if (!TWPF.BrushComparer.Equals(value, backgroundBrush)) {
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

			public MarkerElement(Geometry geometry) {
				this.geometry = geometry ?? throw new ArgumentNullException(nameof(geometry));
			}

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
