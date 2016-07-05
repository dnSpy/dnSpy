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
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using dnSpy.Contracts.Text.Classification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Text.Editor {
	sealed class TextSelectionLayer : UIElement {
		public bool IsActive {
			get { return isActive; }
			set {
				if (isActive != value) {
					isActive = value;
					Repaint();
				}
			}
		}
		bool isActive;

		readonly TextSelection textSelection;
		readonly IAdornmentLayer layer;
		readonly IEditorFormatMap editorFormatMap;

		public TextSelectionLayer(TextSelection textSelection, IAdornmentLayer layer, IEditorFormatMap editorFormatMap) {
			if (textSelection == null)
				throw new ArgumentNullException(nameof(textSelection));
			if (layer == null)
				throw new ArgumentNullException(nameof(layer));
			if (editorFormatMap == null)
				throw new ArgumentNullException(nameof(editorFormatMap));
			this.textSelection = textSelection;
			this.layer = layer;
			this.editorFormatMap = editorFormatMap;
			textSelection.TextView.Options.OptionChanged += Options_OptionChanged;
			textSelection.SelectionChanged += TextSelection_SelectionChanged;
			textSelection.TextView.LayoutChanged += TextView_LayoutChanged;
			editorFormatMap.FormatMappingChanged += EditorFormatMap_FormatMappingChanged;
			UpdateUseReducedOpacityForHighContrastOption();
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
				Repaint();
			}
		}

		void TextView_LayoutChanged(object sender, TextViewLayoutChangedEventArgs e) {
			if (e.NewOrReformattedLines.Count > 0 || e.TranslatedLines.Count > 0 || e.VerticalTranslation)
				Repaint();
		}

		public void OnModeUpdated() => Repaint();
		void TextSelection_SelectionChanged(object sender, EventArgs e) => Repaint();

		List<VirtualSnapshotSpan> GetVisibleSelectedSpans() {
			__selectedSpans.Clear();
			var spans = textSelection.VirtualSelectedSpans;
			// If nothing is selected, an empty span is returned. Don't use it.
			if (spans.Count == 1 && spans[0].Length == 0)
				return __selectedSpans;
			foreach (var span in spans) {
				var visSpan = span.Intersection(new VirtualSnapshotSpan(textSelection.TextView.TextViewLines.FormattedSpan));
				if (visSpan != null)
					__selectedSpans.Add(visSpan.Value);
			}
			return __selectedSpans;
		}
		readonly List<VirtualSnapshotSpan> __selectedSpans = new List<VirtualSnapshotSpan>();

		void Repaint() {
			if (GetVisibleSelectedSpans().Count == 0)
				layer.RemoveAllAdornments();
			else {
				if (layer.IsEmpty)
					layer.AddAdornment(AdornmentPositioningBehavior.OwnerControlled, null, null, this, null);
				InvalidateVisual();
			}
		}

		protected override void OnRender(DrawingContext drawingContext) {
			base.OnRender(drawingContext);

			//TODO: Optimize it to only redraw those lines that changed. Will speed up Remote Desktop.

			const bool clipToViewport = false;
			bool isBoxMode = textSelection.Mode == TextSelectionMode.Box;
			foreach (var span in GetVisibleSelectedSpans()) {
				var geo = SelectionMarkerHelper.CreateGeometry(textSelection.TextView, span, isBoxMode, clipToViewport);
				if (geo != null) {
					var props = editorFormatMap.GetProperties(IsActive ? ThemeEditorFormatTypeNameKeys.SelectedText : ThemeEditorFormatTypeNameKeys.InactiveSelectedText);
					var bg = props[EditorFormatDefinition.BackgroundBrushId] as Brush;
					Debug.Assert(bg != null);
					if (bg == null)
						bg = IsActive ? SystemColors.HighlightBrush : SystemColors.GrayTextBrush;
					if (bg.CanFreeze)
						bg.Freeze();
					drawingContext.DrawGeometry(bg, null, geo);
				}
			}
		}

		public void Dispose() {
			textSelection.TextView.Options.OptionChanged -= Options_OptionChanged;
			textSelection.SelectionChanged -= TextSelection_SelectionChanged;
			textSelection.TextView.LayoutChanged -= TextView_LayoutChanged;
			editorFormatMap.FormatMappingChanged -= EditorFormatMap_FormatMappingChanged;
		}
	}
}
