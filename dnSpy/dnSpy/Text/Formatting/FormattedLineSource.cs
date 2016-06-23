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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Media.TextFormatting;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.Text.Formatting;

namespace dnSpy.Text.Formatting {
	sealed class FormattedLineSource : IFormattedLineSource {
		public ITextSnapshot SourceTextSnapshot { get; }
		public ITextSnapshot TopTextSnapshot { get; }
		public bool UseDisplayMode { get; }
		public int TabSize { get; }
		public double BaseIndentation { get; }
		public double WordWrapWidth { get; }
		public double MaxAutoIndent { get; }
		public double ColumnWidth { get; }
		public double LineHeight { get; }
		public double TextHeightAboveBaseline { get; }
		public double TextHeightBelowBaseline { get; }
		public ITextAndAdornmentSequencer TextAndAdornmentSequencer { get; }
		public TextRunProperties DefaultTextProperties => classificationFormatMap.DefaultTextProperties;

		readonly ITextParagraphPropertiesFactoryService textParagraphPropertiesFactoryService;
		readonly IClassifier aggregateClassifier;
		readonly ITextAndAdornmentSequencer textAndAdornmentSequencer;
		readonly IClassificationFormatMap classificationFormatMap;
		readonly FormattedTextCache formattedTextCache;
		readonly TextFormatter textFormatter;
		readonly TextParagraphProperties defaultTextParagraphProperties;

		// Should be enough...
		const int MAX_LINE_LENGTH = 5000;

		public FormattedLineSource(ITextFormatterProvider textFormatterProvider, ITextParagraphPropertiesFactoryService textParagraphPropertiesFactoryService, ITextSnapshot sourceTextSnapshot, ITextSnapshot visualBufferSnapshot, int tabSize, double baseIndent, double wordWrapWidth, double maxAutoIndent, bool useDisplayMode, IClassifier aggregateClassifier, ITextAndAdornmentSequencer sequencer, IClassificationFormatMap classificationFormatMap, bool isViewWrapEnabled) {
			if (textFormatterProvider == null)
				throw new ArgumentNullException(nameof(textFormatterProvider));
			if (sourceTextSnapshot == null)
				throw new ArgumentNullException(nameof(sourceTextSnapshot));
			if (visualBufferSnapshot == null)
				throw new ArgumentNullException(nameof(visualBufferSnapshot));
			if (aggregateClassifier == null)
				throw new ArgumentNullException(nameof(aggregateClassifier));
			if (sequencer == null)
				throw new ArgumentNullException(nameof(sequencer));
			if (classificationFormatMap == null)
				throw new ArgumentNullException(nameof(classificationFormatMap));
			if (tabSize <= 0)
				throw new ArgumentOutOfRangeException(nameof(tabSize));
			if (sourceTextSnapshot != visualBufferSnapshot)
				throw new NotSupportedException("Text snapshot must be identical to visual snapshot");

			this.textFormatter = textFormatterProvider.Create(useDisplayMode);
			this.formattedTextCache = new FormattedTextCache(useDisplayMode);
			this.textParagraphPropertiesFactoryService = textParagraphPropertiesFactoryService;
			SourceTextSnapshot = sourceTextSnapshot;
			TopTextSnapshot = visualBufferSnapshot;
			UseDisplayMode = useDisplayMode;
			TabSize = tabSize;
			BaseIndentation = baseIndent;
			WordWrapWidth = wordWrapWidth;
			MaxAutoIndent = Math.Round(maxAutoIndent);
			ColumnWidth = formattedTextCache.GetColumnWidth(classificationFormatMap.DefaultTextProperties);
			LineHeight = WpfTextViewLine.DEFAULT_TOP_SPACE + WpfTextViewLine.DEFAULT_BOTTOM_SPACE + formattedTextCache.GetLineHeight(classificationFormatMap.DefaultTextProperties);
			TextHeightAboveBaseline = formattedTextCache.GetTextHeightAboveBaseline(classificationFormatMap.DefaultTextProperties);
			TextHeightBelowBaseline = formattedTextCache.GetTextHeightBelowBaseline(classificationFormatMap.DefaultTextProperties);
			TextAndAdornmentSequencer = textAndAdornmentSequencer;
			this.aggregateClassifier = aggregateClassifier;
			this.textAndAdornmentSequencer = sequencer;
			this.classificationFormatMap = classificationFormatMap;
			this.defaultTextParagraphProperties = new TextFormattingParagraphProperties(classificationFormatMap.DefaultTextProperties, ColumnWidth * TabSize);
		}

		ITextSnapshotLine GetBufferLine(ITextAndAdornmentCollection coll) =>
			coll[0].Span.GetSpans(SourceTextSnapshot)[0].Start.GetContainingLine();

		public Collection<IFormattedLine> FormatLineInVisualBuffer(ITextSnapshotLine visualLine) {
			if (visualLine == null)
				throw new ArgumentNullException(nameof(visualLine));
			if (visualLine.Snapshot != TopTextSnapshot)
				throw new ArgumentException();

			var seqColl = textAndAdornmentSequencer.CreateTextAndAdornmentCollection(visualLine, visualLine.Snapshot);
			var bufferLine = GetBufferLine(seqColl);
			var linePartsCollection = CreateLinePartsCollection(seqColl, bufferLine.ExtentIncludingLineBreak);
			var textSource = new LinePartsTextSource(linePartsCollection);
			var lines = new Collection<IFormattedLine>();

			TextLineBreak previousLineBreak = null;
			double autoIndent = BaseIndentation;
			int column = 0;
			for (int lineSegment = 0; ; lineSegment++) {
				var paragraphProperties = textParagraphPropertiesFactoryService?.Create(this,
					classificationFormatMap.DefaultTextProperties,
					new MappingSpan(bufferLine.Extent, SpanTrackingMode.EdgeNegative),
					new MappingPoint(textSource.ConvertColumnToBufferPosition(column), PointTrackingMode.Negative), lineSegment)
					?? defaultTextParagraphProperties;

				double paragraphWidth = WordWrapWidth == 0 ? 0 : Math.Max(1, WordWrapWidth - autoIndent);

				textSource.SetMaxLineLength(MAX_LINE_LENGTH);
				var textLine = textFormatter.FormatLine(textSource, column, paragraphWidth, paragraphProperties, previousLineBreak);

				int startColumn = column;
				int length = textLine.GetLength(textSource.EndOfLine);
				column += length;

				var startPos = textSource.ConvertColumnToBufferPosition(startColumn);
				var endPos = textSource.ConvertColumnToBufferPosition(column);
				if (column >= textSource.Length)
					endPos = bufferLine.ExtentIncludingLineBreak.End;

				var lineSpan = new SnapshotSpan(startPos, endPos);
				var wpfLine = new WpfTextViewLine(linePartsCollection, bufferLine, lineSpan, visualLine.Snapshot, textLine, autoIndent, ColumnWidth);
				lines.Add(wpfLine);

				if (column >= textSource.Length) {
					Debug.Assert(column == textSource.Length);
					break;
				}
				if (startColumn == column)
					throw new InvalidOperationException();

				if (lineSegment == 0) {
					autoIndent = 0;
					var firstCharColumn = textSource.GetColumnOfFirstNonWhitespace();
					if (firstCharColumn < column)
						autoIndent += textLine.GetDistanceFromCharacterHit(new CharacterHit(firstCharColumn, 0));
					autoIndent += TabSize / 2 * ColumnWidth;
					if (autoIndent > MaxAutoIndent)
						autoIndent = MaxAutoIndent;
					// Base indentation should always be included
					autoIndent += BaseIndentation;
				}

				previousLineBreak = textLine.GetTextLineBreak();
			}

			return lines;
		}

		LinePartsCollection CreateLinePartsCollection(ITextAndAdornmentCollection coll, SnapshotSpan lineExtent) {
			if (coll.Count == 0)
				return new LinePartsCollection(emptyLineParts, lineExtent);

			var list = new List<LinePart>();

			int column = 0;
			int startOffs = lineExtent.Start.Position;
			foreach (var seqElem in coll) {
				var seqSpans = seqElem.Span.GetSpans(SourceTextSnapshot);
				if (seqElem.ShouldRenderText) {
					foreach (var span in seqSpans) {
						var cspans = aggregateClassifier.GetClassificationSpans(span);
						int lastOffs = span.Start.Position;
						for (int i = 0; i < cspans.Count; i++) {
							var cspan = cspans[i];
							int otherSize = cspan.Span.Start.Position - lastOffs;
							if (otherSize != 0) {
								Debug.Assert(otherSize > 0);
								list.Add(new LinePart(column, new Span(lastOffs - startOffs, otherSize), DefaultTextProperties));
								column += otherSize;
							}
							Add(list, column, cspan, lineExtent);
							column += cspan.Span.Length;
							lastOffs = cspan.Span.End.Position;
						}
						int lastSize = span.End.Position - lastOffs;
						if (lastSize != 0) {
							list.Add(new LinePart(column, new Span(lastOffs - startOffs, lastSize), DefaultTextProperties));
							column += lastSize;
						}
					}
				}
				else {
					var adornmentElement = seqElem as IAdornmentElement;
					if (adornmentElement != null && seqSpans.Count == 1) {
						var span = seqSpans[0].Span;
						list.Add(new LinePart(column, new Span(span.Start - startOffs, span.Length), adornmentElement));
						column += span.Length;
					}
				}
			}
			Debug.Assert(list.Sum(a => a.Span.Length) == column);

			return new LinePartsCollection(list, lineExtent);
		}
		static readonly List<LinePart> emptyLineParts = new List<LinePart>();

		void Add(List<LinePart> list, int column, ClassificationSpan cspan, SnapshotSpan lineExtent) {
			if (cspan.Span.Length == 0)
				return;
			int startOffs = lineExtent.Start.Position;
			var props = classificationFormatMap.GetTextProperties(cspan.ClassificationType);
			if (list.Count > 0) {
				var last = list[list.Count - 1];
				// The classification format map caches the props, so we can use ref equality when comparing them
				if (last.TextRunProperties == props && last.Span.End == cspan.Span.Start) {
					list[list.Count - 1] = new LinePart(last.Column, Span.FromBounds(last.Span.Start - startOffs, cspan.Span.End - startOffs), last.TextRunProperties);
					return;
				}
			}
			list.Add(new LinePart(column, new Span(cspan.Span.Span.Start - startOffs, cspan.Span.Span.Length), props));
		}
	}
}
