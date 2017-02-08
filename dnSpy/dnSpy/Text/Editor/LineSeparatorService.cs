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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Contracts.Text.Editor.OptionsExtensionMethods;
using dnSpy.Text.WPF;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Editor {
	[Export(typeof(IWpfTextViewCreationListener))]
	[TextViewRole(PredefinedTextViewRoles.Document)]
	[TextViewRole(PredefinedDsTextViewRoles.CanHaveLineSeparator)]
	[ContentType(ContentTypes.Text)]
	sealed class LineSeparatorWpfTextViewCreationListener : IWpfTextViewCreationListener {
		readonly ILineSeparatorServiceProvider lineSeparatorServiceProvider;

		[ImportingConstructor]
		LineSeparatorWpfTextViewCreationListener(ILineSeparatorServiceProvider lineSeparatorServiceProvider) {
			this.lineSeparatorServiceProvider = lineSeparatorServiceProvider;
		}

		public void TextViewCreated(IWpfTextView textView) =>
			lineSeparatorServiceProvider.InstallLineSeparatorService(textView);
	}

	interface ILineSeparatorServiceProvider {
		void InstallLineSeparatorService(IWpfTextView wpfTextView);
	}

	[Export(typeof(ILineSeparatorServiceProvider))]
	sealed class LineSeparatorServiceProvider : ILineSeparatorServiceProvider {
		readonly IViewTagAggregatorFactoryService viewTagAggregatorFactoryService;
		readonly IEditorFormatMapService editorFormatMapService;

		[ImportingConstructor]
		LineSeparatorServiceProvider(IViewTagAggregatorFactoryService viewTagAggregatorFactoryService, IEditorFormatMapService editorFormatMapService) {
			this.viewTagAggregatorFactoryService = viewTagAggregatorFactoryService;
			this.editorFormatMapService = editorFormatMapService;
		}

		public void InstallLineSeparatorService(IWpfTextView wpfTextView) {
			if (wpfTextView == null)
				throw new ArgumentNullException(nameof(wpfTextView));
			wpfTextView.Properties.GetOrCreateSingletonProperty(typeof(LineSeparatorService), () => new LineSeparatorService(wpfTextView, viewTagAggregatorFactoryService, editorFormatMapService));
		}
	}

	sealed class LineSeparatorService {
#pragma warning disable 0169
		[Export(typeof(AdornmentLayerDefinition))]
		[Name(PredefinedDsAdornmentLayers.LineSeparator)]
		[Order(After = PredefinedDsAdornmentLayers.BottomLayer, Before = PredefinedDsAdornmentLayers.TopLayer)]
		[Order(After = PredefinedAdornmentLayers.Selection, Before = PredefinedAdornmentLayers.Squiggle)]
		static AdornmentLayerDefinition theAdornmentLayerDefinition;
#pragma warning restore 0169

		readonly IWpfTextView wpfTextView;
		readonly IViewTagAggregatorFactoryService viewTagAggregatorFactoryService;
		readonly IEditorFormatMapService editorFormatMapService;
		readonly List<LineSeparatorElement> lineSeparatorElements;
		readonly HashSet<object> usedLines;
		IEditorFormatMap editorFormatMap;
		IAdornmentLayer adornmentLayer;
		ITagAggregator<ILineSeparatorTag> tagAggregator;
		Brush lineSeparatorBrush;

		public LineSeparatorService(IWpfTextView wpfTextView, IViewTagAggregatorFactoryService viewTagAggregatorFactoryService, IEditorFormatMapService editorFormatMapService) {
			if (wpfTextView == null)
				throw new ArgumentNullException(nameof(wpfTextView));
			if (viewTagAggregatorFactoryService == null)
				throw new ArgumentNullException(nameof(viewTagAggregatorFactoryService));
			if (editorFormatMapService == null)
				throw new ArgumentNullException(nameof(editorFormatMapService));
			this.wpfTextView = wpfTextView;
			this.viewTagAggregatorFactoryService = viewTagAggregatorFactoryService;
			this.editorFormatMapService = editorFormatMapService;
			lineSeparatorElements = new List<LineSeparatorElement>();
			usedLines = new HashSet<object>();
			onRemovedDelegate = OnRemoved;
			wpfTextView.Closed += WpfTextView_Closed;
			wpfTextView.Options.OptionChanged += Options_OptionChanged;
			UpdateLineSeparator();
		}

		void Options_OptionChanged(object sender, EditorOptionChangedEventArgs e) {
			if (e.OptionId == DefaultDsTextViewOptions.LineSeparatorsName)
				UpdateLineSeparator();
		}

		void UpdateLineSeparator() {
			if (wpfTextView.Options.IsLineSeparatorEnabled()) {
				Debug.Assert(tagAggregator == null);
				if (tagAggregator != null)
					throw new InvalidOperationException();
				if (adornmentLayer == null)
					adornmentLayer = wpfTextView.GetAdornmentLayer(PredefinedDsAdornmentLayers.LineSeparator);
				if (editorFormatMap == null)
					editorFormatMap = editorFormatMapService.GetEditorFormatMap(wpfTextView);
				tagAggregator = viewTagAggregatorFactoryService.CreateTagAggregator<ILineSeparatorTag>(wpfTextView);
				tagAggregator.BatchedTagsChanged += TagAggregator_BatchedTagsChanged;
				wpfTextView.LayoutChanged += WpfTextView_LayoutChanged;
				editorFormatMap.FormatMappingChanged += EditorFormatMap_FormatMappingChanged;
				UpdateLineSeparatorBrush();
				UpdateRange(new NormalizedSnapshotSpanCollection(wpfTextView.TextViewLines.FormattedSpan));
			}
			else {
				DisposeTagAggregator();
				wpfTextView.LayoutChanged -= WpfTextView_LayoutChanged;
				if (editorFormatMap != null)
					editorFormatMap.FormatMappingChanged -= EditorFormatMap_FormatMappingChanged;
				RemoveAllLineSeparatorElements();
			}
		}

		void EditorFormatMap_FormatMappingChanged(object sender, FormatItemsEventArgs e) {
			if (e.ChangedItems.Contains(ThemeClassificationTypeNameKeys.LineSeparator))
				UpdateLineSeparatorBrush();
		}

		void UpdateLineSeparatorBrush() {
			if (editorFormatMap == null)
				return;
			var props = editorFormatMap.GetProperties(ThemeClassificationTypeNameKeys.LineSeparator);
			var brush = ResourceDictionaryUtilities.GetForegroundBrush(props);
			if (brush == null)
				brush = new SolidColorBrush(Color.FromRgb(0xA5, 0xA5, 0xA5));
			if (brush.CanFreeze)
				brush.Freeze();
			if (!BrushComparer.Equals(lineSeparatorBrush, brush)) {
				lineSeparatorBrush = brush;
				UpdateLineSeparatorElementsForeground();
			}
		}

		void UpdateLineSeparatorElementsForeground() {
			foreach (var elem in lineSeparatorElements)
				elem.Brush = lineSeparatorBrush;
		}

		sealed class LineSeparatorElement : Border {
			public Brush Brush {
				get { return BorderBrush; }
				set { BorderBrush = value; }
			}

			public SnapshotSpan Span { get; }
			public double Y { get; }
			public object LineIdentityTag { get; }

			const double HEIGHT = 1;

			public LineSeparatorElement(SnapshotSpan span, double yBottom, double width, Brush brush, object lineIdentityTag) {
				if (span.Snapshot == null)
					throw new ArgumentException();
				Span = span;
				Y = yBottom - HEIGHT;
				BorderThickness = new Thickness(0, 0, 0, HEIGHT);
				Width = width;
				Brush = brush;
				LineIdentityTag = lineIdentityTag;
			}
		}

		void TagAggregator_BatchedTagsChanged(object sender, BatchedTagsChangedEventArgs e) {
			wpfTextView.VisualElement.Dispatcher.VerifyAccess();
			if (wpfTextView.IsClosed || tagAggregator != sender)
				return;
			List<SnapshotSpan> intersectionSpans = null;
			var textViewLines = wpfTextView.TextViewLines;
			foreach (var mappingSpan in e.Spans) {
				foreach (var span in mappingSpan.GetSpans(wpfTextView.TextSnapshot)) {
					var intersection = textViewLines.FormattedSpan.Intersection(span);
					if (intersection != null) {
						if (intersectionSpans == null)
							intersectionSpans = new List<SnapshotSpan>();
						var lineStart = intersection.Value.Start.GetContainingLine();
						ITextSnapshotLine lineEnd;
						if (intersection.Value.End <= lineStart.EndIncludingLineBreak)
							lineEnd = lineStart;
						else
							lineEnd = intersection.Value.End.GetContainingLine();
						intersectionSpans.Add(new SnapshotSpan(lineStart.Start, lineEnd.EndIncludingLineBreak));
					}
				}
			}
			if (intersectionSpans != null)
				UpdateRange(new NormalizedSnapshotSpanCollection(intersectionSpans));
		}

		void UpdateRange(NormalizedSnapshotSpanCollection spans) {
			if (spans.Count == 1 && spans[0].Start.Position == 0 && spans[0].Length == spans[0].Snapshot.Length)
				RemoveAllLineSeparatorElements();
			else
				RemoveLineSeparatorElements(spans);
			AddLineSeparatorElements(spans);
		}

		void RemoveAllLineSeparatorElements() {
			// Clear this first so the remove-callback won't try to remove anything from this list (it'll be empty!)
			lineSeparatorElements.Clear();
			usedLines.Clear();
			adornmentLayer?.RemoveAllAdornments();
		}

		void RemoveLineSeparatorElements(NormalizedSnapshotSpanCollection spans) {
			if (adornmentLayer == null)
				return;
			for (int i = lineSeparatorElements.Count - 1; i >= 0; i--) {
				var lineSeparatorElement = lineSeparatorElements[i];
				if (spans.IntersectsWith(lineSeparatorElement.Span))
					adornmentLayer.RemoveAdornment(lineSeparatorElement);
			}
		}

		void AddLineSeparatorElements(NormalizedSnapshotSpanCollection spans) {
			var textViewLines = wpfTextView.TextViewLines;
			// There's always at least one line in the collection
			Debug.Assert(textViewLines.Count > 0);
			var start = textViewLines[0].Start.GetContainingLine().Start;
			var end = textViewLines[textViewLines.Count - 1].End.GetContainingLine().EndIncludingLineBreak;
			var fullSpan = new SnapshotSpan(start, end);
			foreach (var tag in tagAggregator.GetTags(spans)) {
				if (tag.Tag == null)
					continue;
				foreach (var span in tag.Span.GetSpans(wpfTextView.TextSnapshot)) {
					if (!span.IntersectsWith(fullSpan))
						continue;
					var lineSeparatorElement = TryCreateLineSeparatorElement(span, tag.Tag);
					if (lineSeparatorElement == null)
						continue;
					bool added = adornmentLayer.AddAdornment(AdornmentPositioningBehavior.TextRelative, lineSeparatorElement.Span, null, lineSeparatorElement, onRemovedDelegate);
					if (added) {
						lineSeparatorElements.Add(lineSeparatorElement);
						usedLines.Add(lineSeparatorElement.LineIdentityTag);
						Debug.Assert(lineSeparatorElements.Count == usedLines.Count);
					}
				}
			}
		}

		LineSeparatorElement TryCreateLineSeparatorElement(SnapshotSpan span, ILineSeparatorTag tag) {
			if (tag == null)
				return null;
			var line = wpfTextView.TextViewLines.GetTextViewLineContainingBufferPosition(span.Start.TranslateTo(wpfTextView.TextSnapshot, PointTrackingMode.Negative));
			if (line == null)
				return null;
			if (tag.IsPhysicalLine) {
				while (!line.IsLastTextViewLineForSnapshotLine) {
					line = wpfTextView.TextViewLines.GetTextViewLineContainingBufferPosition(line.GetPointAfterLineBreak());
					if (line == null)
						return null;
				}
			}

			// Only one line separator per line
			var lineIdentityTag = line.IdentityTag;
			if (usedLines.Contains(lineIdentityTag))
				return null;

			double yBottom = line.TextBottom + 1;
			var elem = new LineSeparatorElement(new SnapshotSpan(line.Extent.Start, 0), yBottom, wpfTextView.ViewportWidth, lineSeparatorBrush, lineIdentityTag);
			Canvas.SetTop(elem, elem.Y);
			return elem;
		}

		readonly AdornmentRemovedCallback onRemovedDelegate;
		void OnRemoved(object tag, UIElement element) {
			Debug.Assert(lineSeparatorElements.Count == usedLines.Count);
			var lineSepElem = (LineSeparatorElement)element;
			lineSeparatorElements.Remove(lineSepElem);
			usedLines.Remove(lineSepElem.LineIdentityTag);
			Debug.Assert(lineSeparatorElements.Count == usedLines.Count);
		}

		void WpfTextView_LayoutChanged(object sender, TextViewLayoutChangedEventArgs e) {
			UpdateLines(e.NewOrReformattedLines);
			if (e.HorizontalTranslation) {
				foreach (var elem in lineSeparatorElements)
					Canvas.SetLeft(elem, e.NewViewState.ViewportLeft);
			}
		}

		void UpdateLines(IList<ITextViewLine> newOrReformattedLines) {
			if (newOrReformattedLines.Count == wpfTextView.TextViewLines.Count)
				RemoveAllLineSeparatorElements();

			var lineSpans = new List<SnapshotSpan>();
			ITextSnapshotLine snapshotLine = null;
			foreach (var line in newOrReformattedLines) {
				if (snapshotLine != null && line.Start >= snapshotLine.Start && line.EndIncludingLineBreak <= snapshotLine.EndIncludingLineBreak)
					continue;
				snapshotLine = line.Start.GetContainingLine();
				lineSpans.Add(snapshotLine.Extent);
			}
			var spans = new NormalizedSnapshotSpanCollection(lineSpans);
			UpdateRange(spans);
		}

		void DisposeTagAggregator() {
			if (tagAggregator != null) {
				tagAggregator.BatchedTagsChanged -= TagAggregator_BatchedTagsChanged;
				tagAggregator.Dispose();
				tagAggregator = null;
			}
		}

		void WpfTextView_Closed(object sender, EventArgs e) {
			RemoveAllLineSeparatorElements();
			wpfTextView.Closed -= WpfTextView_Closed;
			wpfTextView.Options.OptionChanged -= Options_OptionChanged;
			wpfTextView.LayoutChanged -= WpfTextView_LayoutChanged;
			if (editorFormatMap != null)
				editorFormatMap.FormatMappingChanged -= EditorFormatMap_FormatMappingChanged;
			DisposeTagAggregator();
		}
	}
}
