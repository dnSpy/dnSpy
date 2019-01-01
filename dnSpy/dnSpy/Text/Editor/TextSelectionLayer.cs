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
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Text.WPF;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;

namespace dnSpy.Text.Editor {
	sealed class TextSelectionLayer {
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

		readonly TextSelection textSelection;
		readonly IAdornmentLayer layer;
		readonly IEditorFormatMap editorFormatMap;

		public TextSelectionLayer(TextSelection textSelection, IAdornmentLayer layer, IEditorFormatMap editorFormatMap) {
			markerElementRemovedCallBack = (tag, element) => OnMarkerElementRemoved();
			this.textSelection = textSelection ?? throw new ArgumentNullException(nameof(textSelection));
			this.layer = layer ?? throw new ArgumentNullException(nameof(layer));
			this.editorFormatMap = editorFormatMap ?? throw new ArgumentNullException(nameof(editorFormatMap));
			textSelection.TextView.Options.OptionChanged += Options_OptionChanged;
			textSelection.SelectionChanged += TextSelection_SelectionChanged;
			textSelection.TextView.LayoutChanged += TextView_LayoutChanged;
			editorFormatMap.FormatMappingChanged += EditorFormatMap_FormatMappingChanged;
			UpdateBackgroundBrush();
		}

		void UpdateBackgroundBrush() {
			UpdateIsInContrastModeOption();
			var newBackgroundBrush = GetBackgroundBrush();
			if (BrushComparer.Equals(newBackgroundBrush, backgroundBrush))
				return;
			backgroundBrush = newBackgroundBrush;
			if (markerElement != null)
				markerElement.BackgroundBrush = backgroundBrush;
		}
		Brush backgroundBrush;
		MarkerElement markerElement;

		Brush GetBackgroundBrush() {
			var props = editorFormatMap.GetProperties(IsActive ? ThemeClassificationTypeNameKeys.SelectedText : ThemeClassificationTypeNameKeys.InactiveSelectedText);
			return ResourceDictionaryUtilities.GetBackgroundBrush(props, IsActive ? SystemColors.HighlightBrush : SystemColors.GrayTextBrush);
		}

		void Options_OptionChanged(object sender, EditorOptionChangedEventArgs e) {
			if (e.OptionId == DefaultTextViewHostOptions.IsInContrastModeName)
				UpdateIsInContrastModeOption();
		}

		void UpdateIsInContrastModeOption() {
			bool isInContrastMode = textSelection.TextView.Options.IsInContrastMode();
			var newValue = isInContrastMode ? 1 : 0.4;
			if (layer.Opacity != newValue)
				layer.Opacity = newValue;
		}

		void EditorFormatMap_FormatMappingChanged(object sender, FormatItemsEventArgs e) {
			if ((IsActive && e.ChangedItems.Contains(ThemeClassificationTypeNameKeys.SelectedText)) ||
				(!IsActive && e.ChangedItems.Contains(ThemeClassificationTypeNameKeys.InactiveSelectedText))) {
				UpdateBackgroundBrush();
			}
		}

		void TextView_LayoutChanged(object sender, TextViewLayoutChangedEventArgs e) {
			if (e.OldSnapshot != e.NewSnapshot)
				SetNewSelection();
			else if (e.NewOrReformattedLines.Count > 0 || e.TranslatedLines.Count > 0 || e.VerticalTranslation)
				SetNewSelection();
		}

		// Very rarely it could be called recursively when CreateStreamSelection() or
		// CreateBoxSelection() accesses the TextViewLines property. That could raise
		// a LayoutChanged event.
		void SetNewSelection() {
			if (setNewSelectionCounter > 0)
				return;
			try {
				setNewSelectionCounter++;
				SetNewSelectionCore();
			}
			finally {
				setNewSelectionCounter--;
			}
		}
		int setNewSelectionCounter;

		void SetNewSelectionCore() {
			RemoveAllAdornments();
			if (textSelection.IsEmpty)
				return;
			if (textSelection.Mode == TextSelectionMode.Stream) {
				Debug.Assert(textSelection.StreamSelectionSpan.Length != 0);
				var info = CreateStreamSelection();
				if (info == null)
					return;
				CreateMarkerElement(info.Value.span, info.Value.geometry);
			}
			else {
				Debug.Assert(textSelection.Mode == TextSelectionMode.Box);
				var info = CreateBoxSelection();
				if (info == null)
					return;
				CreateMarkerElement(info.Value.span, info.Value.geometry);
			}
		}

		void CreateMarkerElement(SnapshotSpan fullSpan, Geometry geo) {
			Debug.Assert(markerElement == null);
			RemoveAllAdornments();
			markerElement = new MarkerElement(geo);
			markerElement.BackgroundBrush = backgroundBrush;
			if (!layer.AddAdornment(AdornmentPositioningBehavior.TextRelative, fullSpan, null, markerElement, markerElementRemovedCallBack))
				OnMarkerElementRemoved();
		}
		readonly AdornmentRemovedCallback markerElementRemovedCallBack;

		void OnMarkerElementRemoved() => markerElement = null;

		(SnapshotSpan span, Geometry geometry)? CreateStreamSelection() {
			Debug.Assert(!textSelection.IsEmpty && textSelection.Mode == TextSelectionMode.Stream);
			bool isMultiLine = MarkerHelper.IsMultiLineSpan(textSelection.TextView, textSelection.StreamSelectionSpan.SnapshotSpan);
			var span = textSelection.StreamSelectionSpan.Overlap(new VirtualSnapshotSpan(textSelection.TextView.TextViewLines.FormattedSpan));
			if (span == null)
				return null;
			var geo = MarkerHelper.CreateGeometry(textSelection.TextView, span.Value, false, isMultiLine);
			if (geo == null)
				return null;
			return (span.Value.SnapshotSpan, geo);
		}

		(SnapshotSpan span, Geometry geometry)? CreateBoxSelection() {
			Debug.Assert(!textSelection.IsEmpty && textSelection.Mode == TextSelectionMode.Box);
			var allSpans = textSelection.VirtualSelectedSpans;
			var spans = GetVisibleBoxSpans(allSpans);
			if (spans.Count == 0)
				return null;
			var geo = MarkerHelper.CreateBoxGeometry(textSelection.TextView, spans, allSpans.Count > 1);
			if (geo == null)
				return null;
			var fullSpan = new SnapshotSpan(spans[0].SnapshotSpan.Start, spans[spans.Count - 1].SnapshotSpan.End);
			return (fullSpan, geo);
		}

		List<VirtualSnapshotSpan> GetVisibleBoxSpans(IList<VirtualSnapshotSpan> allSpans) {
			var list = new List<VirtualSnapshotSpan>(allSpans.Count);
			var visibleSpan = textSelection.TextView.TextViewLines.FormattedSpan;
			foreach (var span in allSpans) {
				if (visibleSpan.Contains(span.SnapshotSpan))
					list.Add(span);
			}
			return list;
		}

		public void OnModeUpdated() => SetNewSelection();
		void TextSelection_SelectionChanged(object sender, EventArgs e) => SetNewSelection();

		sealed class MarkerElement : UIElement {
			readonly Geometry geometry;

			public Brush BackgroundBrush {
				get => backgroundBrush;
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
				get => pen;
				set {
					if (pen != value) {
						pen = value;
						InvalidateVisual();
					}
				}
			}
			Pen pen;

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
			textSelection.TextView.Options.OptionChanged -= Options_OptionChanged;
			textSelection.SelectionChanged -= TextSelection_SelectionChanged;
			textSelection.TextView.LayoutChanged -= TextView_LayoutChanged;
			editorFormatMap.FormatMappingChanged -= EditorFormatMap_FormatMappingChanged;
		}
	}
}
