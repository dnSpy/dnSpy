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
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Contracts.Text.Editor.OptionsExtensionMethods;
using dnSpy.Text.Formatting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Text.Formatting;

namespace dnSpy.Text.Editor {
	abstract class LineNumberMarginBase : Canvas, IWpfTextViewMargin {
		public bool Enabled => wpfTextViewHost.TextView.Options.IsLineNumberMarginEnabled();
		public double MarginSize => ActualWidth;
		public FrameworkElement VisualElement => this;

		readonly string marginName;
		protected readonly IWpfTextViewHost wpfTextViewHost;
		readonly ITextFormatterProvider textFormatterProvider;
		protected readonly IClassificationFormatMap classificationFormatMap;
		readonly Layer textLayer;
		Dictionary<object, Line> identityTagToLine;
		TextParagraphProperties? defaultTextParagraphProperties;
		TextFormatter? textFormatter;
		bool useDisplayMode;
		int currentMaxLineDigits;
		double lineNumberTextRight;

		protected LineNumberMarginBase(string marginName, IWpfTextViewHost wpfTextViewHost, IClassificationFormatMapService classificationFormatMapService, ITextFormatterProvider textFormatterProvider) {
			if (classificationFormatMapService is null)
				throw new ArgumentNullException(nameof(classificationFormatMapService));
			identityTagToLine = new Dictionary<object, Line>();
			this.marginName = marginName ?? throw new ArgumentNullException(nameof(marginName));
			this.wpfTextViewHost = wpfTextViewHost ?? throw new ArgumentNullException(nameof(wpfTextViewHost));
			classificationFormatMap = classificationFormatMapService.GetClassificationFormatMap(wpfTextViewHost.TextView);
			textLayer = new Layer();
			this.textFormatterProvider = textFormatterProvider ?? throw new ArgumentNullException(nameof(textFormatterProvider));
			Children.Add(textLayer);
			wpfTextViewHost.TextView.Options.OptionChanged += Options_OptionChanged;
			IsVisibleChanged += LineNumberMargin_IsVisibleChanged;
			ClipToBounds = true;
			IsHitTestVisible = false;
			UpdateVisibility();
		}

		void UpdateVisibility() => Visibility = Enabled ? Visibility.Visible : Visibility.Collapsed;

		public ITextViewMargin? GetTextViewMargin(string marginName) =>
			StringComparer.OrdinalIgnoreCase.Equals(marginName, this.marginName) ? this : null;

		void Options_OptionChanged(object? sender, EditorOptionChangedEventArgs e) {
			if (e.OptionId == DefaultTextViewHostOptions.LineNumberMarginName)
				UpdateVisibility();
			else if (!Enabled) {
				// Ignore all other options when it's disabled
			}
			else if (e.OptionId == DefaultDsWpfViewOptions.ForceClearTypeIfNeededName) {
				UpdateForceClearTypeIfNeeded();
				OnTextPropertiesChanged();
			}
		}

		void UpdateForceClearTypeIfNeeded() =>
			TextFormattingUtilities.UpdateForceClearTypeIfNeeded(this, wpfTextViewHost.TextView.Options.IsForceClearTypeIfNeededEnabled(), classificationFormatMap);

		void LineNumberMargin_IsVisibleChanged(object? sender, DependencyPropertyChangedEventArgs e) {
			if (Visibility == Visibility.Visible) {
				if (!hasRegisteredEvents) {
					RegisterEvents();
					SetTop(textLayer, -wpfTextViewHost.TextView.ViewportTop);
					UpdateMaxLineDigits();
					UpdateLineNumberLayerSize();
					UpdateForceClearTypeIfNeeded();
					OnTextPropertiesChanged();
					UpdateLines(Array.Empty<ITextViewLine>(), Array.Empty<ITextViewLine>());
				}
			}
			else {
				if (hasRegisteredEvents)
					UnregisterEvents();
				ClearLines();
				defaultTextParagraphProperties = null;
				textFormatter = null;
			}
		}

		protected void RefreshMargin() {
			if (Visibility == Visibility.Visible) {
				OnTextPropertiesChanged();
				UpdateLines(Array.Empty<ITextViewLine>(), Array.Empty<ITextViewLine>());
			}
		}

		void TextBuffer_ChangedLowPriority(object? sender, TextContentChangedEventArgs e) => UpdateMaxLineDigits();

		protected virtual int? GetMaxLineDigitsCore() => null;

		int DefaultGetMaxLineDigits() {
			int lines = wpfTextViewHost.TextView.TextSnapshot.LineCount;
			return (int)Math.Log10(lines) + 1;
		}

		int GetMaxLineDigits() {
			const int MINIMUM_LINE_DIGITS = 4;
			var maxDigits = GetMaxLineDigitsCore() ?? DefaultGetMaxLineDigits();
			return Math.Max(MINIMUM_LINE_DIGITS, maxDigits);
		}

		void UpdateMaxLineDigits() {
			if (currentMaxLineDigits != GetMaxLineDigits())
				OnTextPropertiesChanged();
		}

		void ClassificationFormatMap_ClassificationFormatMappingChanged(object? sender, EventArgs e) {
			UpdateForceClearTypeIfNeeded();
			OnTextPropertiesChanged();
		}

		void TextView_LayoutChanged(object? sender, TextViewLayoutChangedEventArgs e) {
			if (useDisplayMode != wpfTextViewHost.TextView.FormattedLineSource.UseDisplayMode)
				OnTextPropertiesChanged();
			if (e.VerticalTranslation)
				SetTop(textLayer, -e.NewViewState.ViewportTop);
			if (e.OldViewState.ViewportHeight != e.NewViewState.ViewportHeight)
				UpdateLineNumberLayerSize();
			UpdateLines(e.NewOrReformattedLines, e.TranslatedLines);
		}

		void UpdateLines(IList<ITextViewLine> newOrReformattedLines, IList<ITextViewLine> translatedLines) {
			if (wpfTextViewHost.IsClosed)
				return;
			var textViewLines = wpfTextViewHost.TextView.TextViewLines;
			if (textViewLines is null)
				return;

			foreach (var viewLine in newOrReformattedLines) {
				if (identityTagToLine.TryGetValue(viewLine.IdentityTag, out var line)) {
					identityTagToLine.Remove(viewLine.IdentityTag);
					line.Dispose();
				}
			}
			foreach (var viewLine in translatedLines) {
				if (identityTagToLine.TryGetValue(viewLine.IdentityTag, out var line)) {
					identityTagToLine.Remove(viewLine.IdentityTag);
					line.Dispose();
				}
			}
			var newDict = new Dictionary<object, Line>();
			LineNumberState? lineNumberState = null;
			foreach (var viewLine in textViewLines) {
				var lineNumber = GetLineNumber(viewLine, ref lineNumberState);
				if (lineNumber is null)
					continue;
				Debug2.Assert(lineNumberState is not null);

				if (!identityTagToLine.TryGetValue(viewLine.IdentityTag, out var line) || line.Number != lineNumber)
					line = CreateLine(viewLine, lineNumberState, lineNumber.Value);
				else
					identityTagToLine.Remove(viewLine.IdentityTag);
				newDict[viewLine.IdentityTag] = line;
			}
			foreach (var line in identityTagToLine.Values)
				line.Dispose();

			identityTagToLine = newDict;
			textLayer.UpdateLines(identityTagToLine.Values.ToArray());
		}

		protected class LineNumberState {
			public ITextSnapshotLine? SnapshotLine;
		}

		protected abstract int? GetLineNumber(ITextViewLine viewLine, ref LineNumberState? state);

		string GetLineNumberString(int lineNumber) => lineNumber.ToString(CultureInfo.CurrentUICulture.NumberFormat);

		Line CreateLine(ITextViewLine viewLine, LineNumberState lineNumberState, int lineNumber) {
			var lineNumberString = GetLineNumberString(lineNumber);
			var lineNumberSource = new LineNumberSource(lineNumberString, GetLineNumberTextFormattingRunProperties(viewLine, lineNumberState, lineNumber));
			var textLine = textFormatter!.FormatLine(lineNumberSource, 0, 0, defaultTextParagraphProperties, null);
			return new Line(lineNumber, textLine, lineNumberTextRight, viewLine.TextTop + viewLine.Baseline - textLine.TextBaseline);
		}

		protected abstract TextFormattingRunProperties GetLineNumberTextFormattingRunProperties(ITextViewLine viewLine, LineNumberState state, int lineNumber);
		protected abstract TextFormattingRunProperties? GetDefaultTextFormattingRunProperties();
		protected virtual void OnTextPropertiesChangedCore() { }

		void OnTextPropertiesChanged() {
			OnTextPropertiesChangedCore();
			useDisplayMode = wpfTextViewHost.TextView.FormattedLineSource.UseDisplayMode;
			var textFormattingMode = useDisplayMode ? TextFormattingMode.Display : TextFormattingMode.Ideal;
			var defaultProps = GetDefaultTextFormattingRunProperties();
			if (defaultProps is null)
				return;
			var brush = defaultProps.BackgroundBrush ?? Brushes.Transparent;
			if (brush.CanFreeze)
				brush.Freeze();
			Background = brush;
#pragma warning disable CS0618 // Type or member is obsolete
			var ft = new FormattedText("8", defaultProps.CultureInfo, FlowDirection.LeftToRight, defaultProps.Typeface, defaultProps.FontRenderingEmSize, defaultProps.ForegroundBrush, null, textFormattingMode);
#pragma warning restore CS0618 // Type or member is obsolete
			currentMaxLineDigits = GetMaxLineDigits();
			int maxLineNumberValue = Math.Min(int.MaxValue, (int)(Math.Pow(10, currentMaxLineDigits) - 1));
			// Just in case non-digits are part of the string, calculate max string length
			var lineNumberString = GetLineNumberString(maxLineNumberValue);
			double leftMarginWidth = ft.Width;
			double rightMarginWidth = ft.Width;
			double width = leftMarginWidth + rightMarginWidth + ft.Width * lineNumberString.Length;
			lineNumberTextRight = width - rightMarginWidth;
			Width = width;
			ClearLines();
			defaultTextParagraphProperties = new TextFormattingParagraphProperties(defaultProps);
			textFormatter = textFormatterProvider.Create(useDisplayMode);
		}

		protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo) {
			base.OnRenderSizeChanged(sizeInfo);
			if (Enabled)
				UpdateLineNumberLayerSize();
		}

		void UpdateLineNumberLayerSize() =>
			textLayer.OnSizeChanged(ActualWidth, wpfTextViewHost.TextView.ViewportHeight);

		void ClearLines() {
			foreach (var line in identityTagToLine.Values)
				line.Dispose();
			identityTagToLine.Clear();
			textLayer.Clear();
		}

		protected virtual void RegisterEventsCore() { }
		protected virtual void UnregisterEventsCore() { }

		bool hasRegisteredEvents;
		void RegisterEvents() {
			if (hasRegisteredEvents)
				return;
			if (wpfTextViewHost.IsClosed)
				return;
			hasRegisteredEvents = true;
			RegisterEventsCore();
			classificationFormatMap.ClassificationFormatMappingChanged += ClassificationFormatMap_ClassificationFormatMappingChanged;
			wpfTextViewHost.TextView.TextBuffer.ChangedLowPriority += TextBuffer_ChangedLowPriority;
			wpfTextViewHost.TextView.LayoutChanged += TextView_LayoutChanged;
		}

		void UnregisterEvents() {
			hasRegisteredEvents = false;
			UnregisterEventsCore();
			classificationFormatMap.ClassificationFormatMappingChanged -= ClassificationFormatMap_ClassificationFormatMappingChanged;
			wpfTextViewHost.TextView.TextBuffer.ChangedLowPriority -= TextBuffer_ChangedLowPriority;
			wpfTextViewHost.TextView.LayoutChanged -= TextView_LayoutChanged;
		}

		protected virtual void DisposeCore() { }

		public void Dispose() {
			DisposeCore();
			wpfTextViewHost.TextView.Options.OptionChanged -= Options_OptionChanged;
			IsVisibleChanged -= LineNumberMargin_IsVisibleChanged;
			UnregisterEvents();
			ClearLines();
			textLayer.Dispose();
		}

		sealed class Line {
			public int Number { get; }

			readonly TextLine textLine;
			readonly double right;
			readonly double top;

			public Line(int number, TextLine textLine, double right, double top) {
				if (number <= 0)
					throw new ArgumentOutOfRangeException(nameof(number));
				Number = number;
				this.textLine = textLine ?? throw new ArgumentNullException(nameof(textLine));
				this.right = right;
				this.top = top;
			}

			public Visual GetOrCreateVisual() {
				if (drawingVisual is null) {
					drawingVisual = new DrawingVisual();
					var dc = drawingVisual.RenderOpen();
					textLine.Draw(dc, new Point(right - textLine.Width, top), InvertAxes.None);
					dc.Close();
				}
				return drawingVisual;
			}
			DrawingVisual? drawingVisual;

			public void Dispose() {
				drawingVisual = null;
				textLine.Dispose();
			}
		}

		sealed class LineCollection : UIElement {
			readonly List<LineInfo> lines;

			readonly struct LineInfo {
				public Line Line { get; }
				public Visual Visual { get; }
				public LineInfo(Line line) {
					Line = line;
					Visual = line.GetOrCreateVisual();
				}
			}

			public LineCollection() => lines = new List<LineInfo>();

			protected override int VisualChildrenCount => lines.Count;
			protected override Visual GetVisualChild(int index) => lines[index].Visual;

			public void AddVisibleLines(Line[] allVisibleLines) {
				var currentLinesHash = new HashSet<Line>();
				foreach (var info in lines)
					currentLinesHash.Add(info.Line);
				var newLinesHash = new HashSet<Line>(allVisibleLines);
				foreach (var info in lines) {
					if (!newLinesHash.Contains(info.Line))
						RemoveVisualChild(info.Visual);
				}
				lines.Clear();
				foreach (var line in allVisibleLines) {
					lines.Add(new LineInfo(line));
					if (!currentLinesHash.Contains(line))
						AddVisualChild(line.GetOrCreateVisual());
				}
			}
		}

		sealed class Layer : Canvas {
			readonly LineCollection lineCollection;

			public Layer() {
				lineCollection = new LineCollection();
				Children.Add(lineCollection);
			}

			public void OnSizeChanged(double width, double height) {
				if (Width != width || Height != height) {
					Width = width;
					Height = height;
					VisualScrollableAreaClip = new Rect(0, 0, Width, Height);
				}
			}

			public void Clear() =>
				lineCollection.AddVisibleLines(Array.Empty<Line>());

			public void UpdateLines(Line[] lines) =>
				lineCollection.AddVisibleLines(lines);

			public void Dispose() => Clear();
		}

		sealed class LineNumberSource : TextSource {
			readonly string text;
			readonly TextRunProperties textRunProperties;

			public LineNumberSource(string text, TextRunProperties textRunProperties) {
				this.text = text ?? throw new ArgumentNullException(nameof(text));
				this.textRunProperties = textRunProperties ?? throw new ArgumentNullException(nameof(textRunProperties));
			}

			public override TextSpan<CultureSpecificCharacterBufferRange> GetPrecedingText(int textSourceCharacterIndexLimit) =>
				new TextSpan<CultureSpecificCharacterBufferRange>(0, new CultureSpecificCharacterBufferRange(CultureInfo.CurrentUICulture, new CharacterBufferRange(string.Empty, 0, 0)));
			public override int GetTextEffectCharacterIndexFromTextSourceCharacterIndex(int textSourceCharacterIndex) => textSourceCharacterIndex;

			public override TextRun GetTextRun(int textSourceCharacterIndex) {
				if (textSourceCharacterIndex < 0)
					throw new ArgumentOutOfRangeException(nameof(textSourceCharacterIndex));
				if (textSourceCharacterIndex >= text.Length)
					return endOfLine;
				return new TextCharacters(text, textSourceCharacterIndex, text.Length - textSourceCharacterIndex, textRunProperties);
			}
			static readonly TextEndOfLine endOfLine = new TextEndOfLine(1);
		}
	}
}
