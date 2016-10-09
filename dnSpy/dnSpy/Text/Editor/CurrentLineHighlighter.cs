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
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Text.Formatting;
using dnSpy.Text.WPF;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Editor {
	[Export(typeof(IWpfTextViewCreationListener))]
	[TextViewRole(PredefinedTextViewRoles.Document)]
	[TextViewRole(PredefinedDsTextViewRoles.CanHaveCurrentLineHighlighter)]
	[ContentType(ContentTypes.Any)]
	sealed class CurrentLineHighlighterWpfTextViewCreationListener : IWpfTextViewCreationListener {
		readonly IEditorFormatMapService editorFormatMapService;

		[ImportingConstructor]
		CurrentLineHighlighterWpfTextViewCreationListener(IEditorFormatMapService editorFormatMapService) {
			this.editorFormatMapService = editorFormatMapService;
		}

		public void TextViewCreated(IWpfTextView textView) =>
			textView.Properties.GetOrCreateSingletonProperty(() => new CurrentLineHighlighter(textView, editorFormatMapService.GetEditorFormatMap(textView)));

		public static void RemoveFromProperties(IWpfTextView textView) =>
			textView.Properties.RemoveProperty(typeof(CurrentLineHighlighter));
	}

	sealed class CurrentLineHighlighter {
#pragma warning disable 0169
		[Export(typeof(AdornmentLayerDefinition))]
		[Name(PredefinedAdornmentLayers.CurrentLineHighlighter)]
		[Order(After = PredefinedDsAdornmentLayers.BottomLayer, Before = PredefinedDsAdornmentLayers.TopLayer)]
		[Order(Before = PredefinedAdornmentLayers.Caret)]
		[Order(Before = PredefinedAdornmentLayers.Selection)]
		[Order(Before = PredefinedAdornmentLayers.Text)]
		[Order(Before = PredefinedAdornmentLayers.TextMarker)]
		[Order(Before = PredefinedDsAdornmentLayers.GlyphTextMarker)]
		[Order(After = PredefinedAdornmentLayers.Outlining)]
		static AdornmentLayerDefinition theAdornmentLayerDefinition;
#pragma warning restore 0169

		readonly IWpfTextView wpfTextView;
		readonly IEditorFormatMap editorFormatMap;
		readonly CurrentLineHighlighterElement currentLineHighlighterElement;
		IAdornmentLayer adornmentLayer;
		bool isActive;
		bool selectionIsEmpty;
		bool enabled;

		public CurrentLineHighlighter(IWpfTextView wpfTextView, IEditorFormatMap editorFormatMap) {
			if (wpfTextView == null)
				throw new ArgumentNullException(nameof(wpfTextView));
			if (editorFormatMap == null)
				throw new ArgumentNullException(nameof(editorFormatMap));
			this.wpfTextView = wpfTextView;
			this.editorFormatMap = editorFormatMap;
			this.currentLineHighlighterElement = new CurrentLineHighlighterElement();
			wpfTextView.Closed += WpfTextView_Closed;
			wpfTextView.Options.OptionChanged += Options_OptionChanged;
			UpdateEnableState();
		}

		void Options_OptionChanged(object sender, EditorOptionChangedEventArgs e) {
			if (e.OptionId == DefaultWpfViewOptions.EnableHighlightCurrentLineName)
				UpdateEnableState();
		}

		bool hasHookedEvents;
		void UpdateEnableState() {
			enabled = wpfTextView.Options.IsHighlightCurrentLineEnabled();
			if (enabled) {
				if (adornmentLayer == null)
					adornmentLayer = wpfTextView.GetAdornmentLayer(PredefinedAdornmentLayers.CurrentLineHighlighter);
				if (!hasHookedEvents) {
					RegisterEnabledEvents();
					isActive = wpfTextView.HasAggregateFocus;
					selectionIsEmpty = wpfTextView.Selection.IsEmpty;
					isActive = wpfTextView.HasAggregateFocus;
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
			wpfTextView.GotAggregateFocus += WpfTextView_GotAggregateFocus;
			wpfTextView.LostAggregateFocus += WpfTextView_LostAggregateFocus;
			wpfTextView.Selection.SelectionChanged += Selection_SelectionChanged;
			wpfTextView.Caret.PositionChanged += Caret_PositionChanged;
			wpfTextView.LayoutChanged += WpfTextView_LayoutChanged;
		}

		void UnregisterEnabledEvents() {
			hasHookedEvents = false;
			editorFormatMap.FormatMappingChanged -= EditorFormatMap_FormatMappingChanged;
			wpfTextView.GotAggregateFocus -= WpfTextView_GotAggregateFocus;
			wpfTextView.LostAggregateFocus -= WpfTextView_LostAggregateFocus;
			wpfTextView.Selection.SelectionChanged -= Selection_SelectionChanged;
			wpfTextView.Caret.PositionChanged -= Caret_PositionChanged;
			wpfTextView.LayoutChanged -= WpfTextView_LayoutChanged;
		}

		void PositionLineElement() {
			if (!selectionIsEmpty || !enabled) {
				adornmentLayer?.RemoveAllAdornments();
				return;
			}

			var line = wpfTextView.Caret.ContainingTextViewLine;
			if (line.IsVisible()) {
				if (adornmentLayer.IsEmpty)
					adornmentLayer.AddAdornment(AdornmentPositioningBehavior.OwnerControlled, null, null, currentLineHighlighterElement, null);
				Canvas.SetLeft(currentLineHighlighterElement, wpfTextView.ViewportLeft);
				Canvas.SetTop(currentLineHighlighterElement, line.TextTop);
				currentLineHighlighterElement.SetLine(line, wpfTextView.ViewportWidth);
			}
			else
				adornmentLayer.RemoveAllAdornments();
		}

		void WpfTextView_LayoutChanged(object sender, TextViewLayoutChangedEventArgs e) => PositionLineElement();
		void Caret_PositionChanged(object sender, CaretPositionChangedEventArgs e) => PositionLineElement();

		void Selection_SelectionChanged(object sender, EventArgs e) {
			bool newSelectionIsEmpty = wpfTextView.Selection.IsEmpty;
			if (selectionIsEmpty == newSelectionIsEmpty)
				return;
			selectionIsEmpty = newSelectionIsEmpty;
			PositionLineElement();
		}

		void WpfTextView_GotAggregateFocus(object sender, EventArgs e) => UpdateFocus();
		void WpfTextView_LostAggregateFocus(object sender, EventArgs e) => UpdateFocus();

		void UpdateFocus() {
			bool newIsActive = wpfTextView.HasAggregateFocus;
			if (newIsActive == isActive)
				return;
			isActive = newIsActive;
			UpdateLineElementBrushes();
		}

		void UpdateLineElementBrushes() {
			var props = editorFormatMap.GetProperties(isActive ? ThemeClassificationTypeNameKeys.CurrentLine : ThemeClassificationTypeNameKeys.CurrentLineNoFocus);
			currentLineHighlighterElement.ForegroundBrush = ResourceDictionaryUtilities.GetForegroundBrush(props);
			currentLineHighlighterElement.BackgroundBrush = ResourceDictionaryUtilities.GetBackgroundBrush(props);
		}

		void EditorFormatMap_FormatMappingChanged(object sender, FormatItemsEventArgs e) {
			if ((isActive && e.ChangedItems.Contains(ThemeClassificationTypeNameKeys.CurrentLine)) ||
				(!isActive && e.ChangedItems.Contains(ThemeClassificationTypeNameKeys.CurrentLineNoFocus))) {
				UpdateLineElementBrushes();
			}
		}

		void WpfTextView_Closed(object sender, EventArgs e) {
			wpfTextView.Closed -= WpfTextView_Closed;
			wpfTextView.Options.OptionChanged -= Options_OptionChanged;
			UnregisterEnabledEvents();
			CurrentLineHighlighterWpfTextViewCreationListener.RemoveFromProperties(wpfTextView);
		}
	}

	sealed class CurrentLineHighlighterElement : UIElement {
		const int PEN_THICKNESS = 2;

		public Brush BackgroundBrush {
			get { return backgroundBrush; }
			set {
				if (!BrushComparer.Equals(backgroundBrush, value)) {
					backgroundBrush = value;
					InvalidateVisual();
				}
			}
		}
		Brush backgroundBrush;

		public Brush ForegroundBrush {
			get { return foregroundBrush; }
			set {
				if (!BrushComparer.Equals(foregroundBrush, value)) {
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

		public void SetLine(ITextViewLine line, double width) {
			if (line == null)
				throw new ArgumentNullException(nameof(line));
			var newRect = new Rect(PEN_THICKNESS / 2, PEN_THICKNESS / 2, Math.Max(0, width - PEN_THICKNESS), Math.Max(0, line.TextHeight + WpfTextViewLine.DEFAULT_BOTTOM_SPACE - PEN_THICKNESS));
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
