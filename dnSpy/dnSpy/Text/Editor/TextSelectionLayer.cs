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
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using dnSpy.Contracts.Text.Classification;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Text.Editor {
	sealed class TextSelectionLayer {
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

		readonly TextSelection textSelection;
		readonly IAdornmentLayer layer;
		readonly IEditorFormatMap editorFormatMap;
		readonly Marker marker;

		public TextSelectionLayer(TextSelection textSelection, IAdornmentLayer layer, IEditorFormatMap editorFormatMap) {
			if (textSelection == null)
				throw new ArgumentNullException(nameof(textSelection));
			if (layer == null)
				throw new ArgumentNullException(nameof(layer));
			if (editorFormatMap == null)
				throw new ArgumentNullException(nameof(editorFormatMap));
			this.textSelection = textSelection;
			this.layer = layer;
			this.marker = new Marker(textSelection.TextView, layer);
			this.editorFormatMap = editorFormatMap;
			textSelection.TextView.Options.OptionChanged += Options_OptionChanged;
			textSelection.SelectionChanged += TextSelection_SelectionChanged;
			textSelection.TextView.LayoutChanged += TextView_LayoutChanged;
			editorFormatMap.FormatMappingChanged += EditorFormatMap_FormatMappingChanged;
			UpdateUseReducedOpacityForHighContrastOption();
			UpdateBackgroundBrush();
		}

		void UpdateBackgroundBrush() => marker.BackgroundBrush = GetBackgroundBrush();

		Brush GetBackgroundBrush() {
			var props = editorFormatMap.GetProperties(IsActive ? ThemeEditorFormatTypeNameKeys.SelectedText : ThemeEditorFormatTypeNameKeys.InactiveSelectedText);
			var bg = props[EditorFormatDefinition.BackgroundBrushId] as Brush;
			Debug.Assert(bg != null);
			if (bg == null)
				bg = IsActive ? SystemColors.HighlightBrush : SystemColors.GrayTextBrush;
			if (bg.CanFreeze)
				bg.Freeze();
			return bg;
		}

		void Options_OptionChanged(object sender, EditorOptionChangedEventArgs e) {
			if (e.OptionId == DefaultWpfViewOptions.UseReducedOpacityForHighContrastOptionId.Name)
				UpdateUseReducedOpacityForHighContrastOption();
		}

		void UpdateUseReducedOpacityForHighContrastOption() {
			bool reducedOpacity = textSelection.TextView.Options.GetOptionValue(DefaultWpfViewOptions.UseReducedOpacityForHighContrastOptionId);
			layer.Opacity = reducedOpacity ? 0.4 : 1;
		}

		void EditorFormatMap_FormatMappingChanged(object sender, FormatItemsEventArgs e) {
			if ((IsActive && e.ChangedItems.Contains(ThemeEditorFormatTypeNameKeys.SelectedText)) ||
				(!IsActive && e.ChangedItems.Contains(ThemeEditorFormatTypeNameKeys.InactiveSelectedText))) {
				UpdateBackgroundBrush();
			}
		}

		void TextView_LayoutChanged(object sender, TextViewLayoutChangedEventArgs e) {
			if (e.OldSnapshot != e.NewSnapshot)
				SetNewSelection();
			if (e.NewOrReformattedLines.Count > 0 || e.TranslatedLines.Count > 0 || e.VerticalTranslation)
				marker.OnLayoutChanged(e.NewOrReformattedLines);
		}

		void SetNewSelection() {
			if (textSelection.IsEmpty)
				marker.SetSpans(NullMarkerSpanCollection.Instance);
			else if (textSelection.Mode == TextSelectionMode.Stream) {
				Debug.Assert(textSelection.StreamSelectionSpan.Length != 0);
				marker.SetSpans(new StreamMarkerSpanCollection(textSelection.TextView, textSelection.StreamSelectionSpan));
			}
			else {
				Debug.Assert(textSelection.Mode == TextSelectionMode.Box);
				marker.SetSpans(new BoxMarkerSpanCollection(textSelection));
			}
		}

		public void OnModeUpdated() => SetNewSelection();
		void TextSelection_SelectionChanged(object sender, EventArgs e) => SetNewSelection();

		public void Dispose() {
			marker.Dispose();
			textSelection.TextView.Options.OptionChanged -= Options_OptionChanged;
			textSelection.SelectionChanged -= TextSelection_SelectionChanged;
			textSelection.TextView.LayoutChanged -= TextView_LayoutChanged;
			editorFormatMap.FormatMappingChanged -= EditorFormatMap_FormatMappingChanged;
		}
	}
}
