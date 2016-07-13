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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Contracts.Themes;
using dnSpy.Text.Formatting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Editor {
	[Export(typeof(IWpfTextViewMarginProvider))]
	[MarginContainer(PredefinedMarginNames.LeftSelection)]
	[Name(PredefinedMarginNames.LineNumber)]
	[ContentType(ContentTypes.TEXT)]
	[TextViewRole(PredefinedTextViewRoles.Document)]
	[TextViewRole(LogEditorTextViewRoles.LOG)]
	[Order(Before = PredefinedMarginNames.Spacer)]
	sealed class LineNumberMarginProvider : IWpfTextViewMarginProvider {
		readonly IClassificationFormatMapService classificationFormatMapService;
		readonly IThemeClassificationTypes themeClassificationTypes;
		readonly ITextFormatterProvider textFormatterProvider;

		[ImportingConstructor]
		LineNumberMarginProvider(IClassificationFormatMapService classificationFormatMapService, IThemeClassificationTypes themeClassificationTypes, ITextFormatterProvider textFormatterProvider) {
			this.classificationFormatMapService = classificationFormatMapService;
			this.themeClassificationTypes = themeClassificationTypes;
			this.textFormatterProvider = textFormatterProvider;
		}

		public IWpfTextViewMargin CreateMargin(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin marginContainer) =>
			new LineNumberMargin(wpfTextViewHost, classificationFormatMapService, themeClassificationTypes, textFormatterProvider);
	}

	sealed class LineNumberMargin : Canvas, IWpfTextViewMargin {
		public bool Enabled => wpfTextViewHost.TextView.Options.IsLineNumberMarginEnabled();
		public double MarginSize => ActualWidth;
		public FrameworkElement VisualElement => this;

		readonly IWpfTextViewHost wpfTextViewHost;
		readonly ITextFormatterProvider textFormatterProvider;
		readonly IClassificationFormatMap classificationFormatMap;
		readonly Layer textLayer;
		readonly IClassificationType lineNumberClassificationType;
		Dictionary<object, Line> identityTagToLine;
		TextParagraphProperties defaultTextParagraphProperties;
		TextFormatter textFormatter;
		bool useDisplayMode;
		int currentMaxLineDigits;
		double lineNumberTextRight;
		TextFormattingRunProperties lineNumberTextFormattingRunProperties;

		public LineNumberMargin(IWpfTextViewHost wpfTextViewHost, IClassificationFormatMapService classificationFormatMapService, IThemeClassificationTypes themeClassificationTypes, ITextFormatterProvider textFormatterProvider) {
			if (wpfTextViewHost == null)
				throw new ArgumentNullException(nameof(wpfTextViewHost));
			if (classificationFormatMapService == null)
				throw new ArgumentNullException(nameof(classificationFormatMapService));
			if (themeClassificationTypes == null)
				throw new ArgumentNullException(nameof(themeClassificationTypes));
			if (textFormatterProvider == null)
				throw new ArgumentNullException(nameof(textFormatterProvider));
			this.identityTagToLine = new Dictionary<object, Line>();
			this.wpfTextViewHost = wpfTextViewHost;
			this.classificationFormatMap = classificationFormatMapService.GetClassificationFormatMap(wpfTextViewHost.TextView);
			this.textLayer = new Layer();
			this.lineNumberClassificationType = themeClassificationTypes.GetClassificationType(ColorType.LineNumber);
			this.textFormatterProvider = textFormatterProvider;
			this.Children.Add(textLayer);
			wpfTextViewHost.TextView.Options.OptionChanged += Options_OptionChanged;
			UpdateLineNumberMarginVisible();
		}

		public ITextViewMargin GetTextViewMargin(string marginName) =>
			StringComparer.OrdinalIgnoreCase.Equals(marginName, PredefinedMarginNames.LineNumber) ? this : null;

		void Options_OptionChanged(object sender, EditorOptionChangedEventArgs e) {
			if (e.OptionId == DefaultTextViewHostOptions.LineNumberMarginId.Name)
				UpdateLineNumberMarginVisible();
			else if (!Enabled) {
				// Ignore all other options when it's disabled
			}
			else if (e.OptionId == DefaultDnSpyWpfViewOptions.ForceClearTypeIfNeededId.Name) {
				UpdateForceClearTypeIfNeeded();
				OnTextPropertiesChanged();
			}
		}

		void UpdateForceClearTypeIfNeeded() =>
			TextFormattingUtilities.UpdateForceClearTypeIfNeeded(this, wpfTextViewHost.TextView.Options, classificationFormatMap);

		bool hasHookedEvents;
		void UpdateLineNumberMarginVisible() {
			if (Enabled) {
				Visibility = Visibility.Visible;
				if (!hasHookedEvents) {
					RegisterVisibleMarginEvents();
					SetTop(textLayer, -wpfTextViewHost.TextView.ViewportTop);
					UpdateMaxLineDigits();
					UpdateLineNumberLayerSize();
					UpdateForceClearTypeIfNeeded();
					OnTextPropertiesChanged();
					UpdateLines(Array.Empty<ITextViewLine>());
				}
			}
			else {
				Visibility = Visibility.Collapsed;
				if (hasHookedEvents)
					UnregisterVisibleMarginEvents();
				ClearLines();
				defaultTextParagraphProperties = null;
				textFormatter = null;
				lineNumberTextFormattingRunProperties = null;
			}
		}

		void RegisterVisibleMarginEvents() {
			Debug.Assert(!hasHookedEvents);
			if (hasHookedEvents)
				return;
			hasHookedEvents = true;
			classificationFormatMap.ClassificationFormatMappingChanged += ClassificationFormatMap_ClassificationFormatMappingChanged;
			wpfTextViewHost.TextView.TextBuffer.ChangedLowPriority += TextBuffer_ChangedLowPriority;
			wpfTextViewHost.TextView.LayoutChanged += TextView_LayoutChanged;
		}

		void UnregisterVisibleMarginEvents() {
			hasHookedEvents = false;
			classificationFormatMap.ClassificationFormatMappingChanged -= ClassificationFormatMap_ClassificationFormatMappingChanged;
			wpfTextViewHost.TextView.TextBuffer.ChangedLowPriority -= TextBuffer_ChangedLowPriority;
			wpfTextViewHost.TextView.LayoutChanged -= TextView_LayoutChanged;
		}

		void TextBuffer_ChangedLowPriority(object sender, TextContentChangedEventArgs e) => UpdateMaxLineDigits();

		int GetMaxLineDigits() {
			int lines = wpfTextViewHost.TextView.TextSnapshot.LineCount;
			int digits = (int)Math.Log10(lines) + 1;
			const int MINIMUM_LINE_DIGITS = 3;
			return Math.Max(MINIMUM_LINE_DIGITS, digits);
		}

		void UpdateMaxLineDigits() {
			if (currentMaxLineDigits != GetMaxLineDigits())
				OnTextPropertiesChanged();
		}

		void ClassificationFormatMap_ClassificationFormatMappingChanged(object sender, EventArgs e) {
			UpdateForceClearTypeIfNeeded();
			OnTextPropertiesChanged();
		}

		void TextView_LayoutChanged(object sender, TextViewLayoutChangedEventArgs e) {
			if (useDisplayMode != wpfTextViewHost.TextView.FormattedLineSource.UseDisplayMode)
				OnTextPropertiesChanged();
			if (e.VerticalTranslation)
				SetTop(textLayer, -e.NewViewState.ViewportTop);
			if (e.OldViewState.ViewportHeight != e.NewViewState.ViewportHeight)
				UpdateLineNumberLayerSize();
			UpdateLines(e.NewOrReformattedLines);
		}

		void UpdateLines(IList<ITextViewLine> newOrReformattedLines) {
			if (wpfTextViewHost.IsClosed)
				return;
			var textViewLines = wpfTextViewHost.TextView.TextViewLines;
			if (textViewLines == null)
				return;

			foreach (var line in newOrReformattedLines)
				identityTagToLine.Remove(line.IdentityTag);
			var newDict = new Dictionary<object, Line>();
			ITextSnapshotLine snapshotLine = null;
			foreach (var viewLine in textViewLines) {
				if (!viewLine.IsFirstTextViewLineForSnapshotLine)
					continue;
				if (snapshotLine == null || snapshotLine.EndIncludingLineBreak != viewLine.Start)
					snapshotLine = viewLine.Start.GetContainingLine();
				else
					snapshotLine = snapshotLine.Snapshot.GetLineFromLineNumber(snapshotLine.LineNumber + 1);
				int lineNumber = snapshotLine.LineNumber + 1;

				Line line;
				if (!identityTagToLine.TryGetValue(viewLine.IdentityTag, out line) || line.Number != lineNumber)
					line = CreateLine(viewLine, lineNumber);
				else
					identityTagToLine.Remove(viewLine.IdentityTag);
				newDict[viewLine.IdentityTag] = line;
			}
			foreach (var line in identityTagToLine.Values)
				line.Dispose();

			identityTagToLine = newDict;
			textLayer.UpdateLines(identityTagToLine.Values.ToArray());
		}

		string GetLineNumberString(int lineNumber) => lineNumber.ToString(CultureInfo.CurrentUICulture.NumberFormat);

		Line CreateLine(ITextViewLine viewLine, int lineNumber) {
			var lineNumberString = GetLineNumberString(lineNumber);
			var lineNumberSource = new LineNumberSource(lineNumberString, lineNumberTextFormattingRunProperties);
			var textLine = textFormatter.FormatLine(lineNumberSource, 0, 0, defaultTextParagraphProperties, null);
			return new Line(lineNumber, textLine, lineNumberTextRight, viewLine.TextTop + viewLine.Baseline - textLine.TextBaseline);
		}

		void OnTextPropertiesChanged() {
			useDisplayMode = wpfTextViewHost.TextView.FormattedLineSource.UseDisplayMode;
			var textFormattingMode = useDisplayMode ? TextFormattingMode.Display : TextFormattingMode.Ideal;
			lineNumberTextFormattingRunProperties = classificationFormatMap.GetTextProperties(lineNumberClassificationType);
			var brush = lineNumberTextFormattingRunProperties.BackgroundBrush ?? Brushes.Transparent;
			if (brush.CanFreeze)
				brush.Freeze();
			Background = brush;
			var ft = new FormattedText("8", lineNumberTextFormattingRunProperties.CultureInfo, FlowDirection.LeftToRight, lineNumberTextFormattingRunProperties.Typeface, lineNumberTextFormattingRunProperties.FontRenderingEmSize, lineNumberTextFormattingRunProperties.ForegroundBrush, null, textFormattingMode);
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
			defaultTextParagraphProperties = new TextFormattingParagraphProperties(lineNumberTextFormattingRunProperties);
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

		public void Dispose() {
			wpfTextViewHost.TextView.Options.OptionChanged -= Options_OptionChanged;
			UnregisterVisibleMarginEvents();
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
				if (textLine == null)
					throw new ArgumentNullException(nameof(textLine));
				Number = number;
				this.textLine = textLine;
				this.right = right;
				this.top = top;
			}

			public Visual GetOrCreateVisual() {
				if (drawingVisual == null) {
					drawingVisual = new DrawingVisual();
					var dc = drawingVisual.RenderOpen();
					textLine.Draw(dc, new Point(right - textLine.Width, top), InvertAxes.None);
					dc.Close();
				}
				return drawingVisual;
			}
			DrawingVisual drawingVisual;

			public void Dispose() {
				drawingVisual = null;
				textLine.Dispose();
			}
		}

		sealed class LineCollection : UIElement {
			readonly List<LineInfo> lines;

			struct LineInfo {
				public Line Line { get; }
				public Visual Visual { get; }
				public LineInfo(Line line) {
					Line = line;
					Visual = line.GetOrCreateVisual();
				}
			}

			public LineCollection() {
				this.lines = new List<LineInfo>();
			}

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
				this.lineCollection = new LineCollection();
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
				if (text == null)
					throw new ArgumentNullException(nameof(text));
				if (textRunProperties == null)
					throw new ArgumentNullException(nameof(textRunProperties));
				this.text = text;
				this.textRunProperties = textRunProperties;
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
