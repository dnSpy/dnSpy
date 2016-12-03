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
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using dnSpy.Contracts.Text.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Projection;
using TF = Microsoft.VisualStudio.Text.Formatting;

namespace dnSpy.Text.Formatting {
	sealed class WpfTextViewLine : IFormattedLine, IDsTextViewLine {
		IBufferGraph bufferGraph;
		readonly int endColumn, startColumn;
		readonly int linePartsIndex, linePartsLength;
		double top;
		readonly double width;
		readonly double textLeft;
		readonly double textWidth;
		readonly double virtualSpaceWidth;
		double deltaY;
		readonly double endOfLineWidth;
		TextViewLineChange change;
		Rect visibleArea;
		SnapshotSpan extentIncludingLineBreak;
		ITextSnapshot visualSnapshot;
		readonly int lineBreakLength;
		VisibilityState visibilityState;
		readonly bool isFirstTextViewLineForSnapshotLine;
		readonly bool isLastTextViewLineForSnapshotLine;
		LineTransform lineTransform;
		ReadOnlyCollection<TextLine> textLines;
		LinePartsCollection linePartsCollection;
		double realTopSpace, scaledTopSpace;
		double realBottomSpace;
		double realTextHeight, scaledTextHeight;
		double scaledHeight;
		double realBaseline;

		public double Bottom {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(WpfTextViewLine));
				return Top + Height;
			}
		}

		public double Height {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(WpfTextViewLine));
				return scaledHeight;
			}
		}

		public double Left {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(WpfTextViewLine));
				return TextLeft;
			}
		}

		public double Right {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(WpfTextViewLine));
				return Left + Width;
			}
		}

		public double Top {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(WpfTextViewLine));
				return top;
			}
		}

		public double Width {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(WpfTextViewLine));
				return width;
			}
		}

		public double TextBottom {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(WpfTextViewLine));
				return Top + scaledTopSpace + TextHeight;
			}
		}

		public double TextTop {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(WpfTextViewLine));
				return Top + scaledTopSpace;
			}
		}

		public double TextHeight {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(WpfTextViewLine));
				return scaledTextHeight;
			}
		}

		public double TextWidth {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(WpfTextViewLine));
				return textWidth;
			}
		}

		public double TextLeft {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(WpfTextViewLine));
				return textLeft;
			}
		}

		public double TextRight {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(WpfTextViewLine));
				return TextLeft + TextWidth;
			}
		}

		public double VirtualSpaceWidth {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(WpfTextViewLine));
				return virtualSpaceWidth;
			}
		}

		public double Baseline {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(WpfTextViewLine));
				return realBaseline * lineTransform.VerticalScale;
			}
		}

		public double DeltaY {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(WpfTextViewLine));
				return deltaY;
			}
		}

		public double EndOfLineWidth {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(WpfTextViewLine));
				return endOfLineWidth;
			}
		}

		public TextViewLineChange Change {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(WpfTextViewLine));
				return change;
			}
		}

		public object IdentityTag => this;
		public bool IsFirstTextViewLineForSnapshotLine => isFirstTextViewLineForSnapshotLine;
		public bool IsLastTextViewLineForSnapshotLine => isLastTextViewLineForSnapshotLine;
		public bool IsValid { get; private set; }

		public int Length {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(WpfTextViewLine));
				return ExtentIncludingLineBreak.Length - LineBreakLength;
			}
		}

		public int LengthIncludingLineBreak {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(WpfTextViewLine));
				return ExtentIncludingLineBreak.Length;
			}
		}

		public int LineBreakLength {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(WpfTextViewLine));
				return lineBreakLength;
			}
		}

		public LineTransform LineTransform {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(WpfTextViewLine));
				return lineTransform;
			}
		}

		public ITextSnapshot Snapshot => extentIncludingLineBreak.Snapshot;

		public SnapshotPoint Start {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(WpfTextViewLine));
				return extentIncludingLineBreak.Start;
			}
		}

		public SnapshotPoint End {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(WpfTextViewLine));
				return extentIncludingLineBreak.End - lineBreakLength;
			}
		}

		public SnapshotPoint EndIncludingLineBreak {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(WpfTextViewLine));
				return extentIncludingLineBreak.End;
			}
		}

		public SnapshotSpan Extent {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(WpfTextViewLine));
				return new SnapshotSpan(extentIncludingLineBreak.Snapshot, extentIncludingLineBreak.Start.Position, extentIncludingLineBreak.Length - lineBreakLength);
			}
		}

		public IMappingSpan ExtentAsMappingSpan {
			get {
				if (Snapshot != visualSnapshot)
					throw new NotSupportedException();
				return bufferGraph.CreateMappingSpan(Extent, SpanTrackingMode.EdgeInclusive);
			}
		}

		public SnapshotSpan ExtentIncludingLineBreak {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(WpfTextViewLine));
				return extentIncludingLineBreak;
			}
		}

		public IMappingSpan ExtentIncludingLineBreakAsMappingSpan {
			get {
				if (Snapshot != visualSnapshot)
					throw new NotSupportedException();
				return bufferGraph.CreateMappingSpan(ExtentIncludingLineBreak, SpanTrackingMode.EdgeInclusive);
			}
		}

		public LineTransform DefaultLineTransform {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(WpfTextViewLine));
				return new LineTransform(DEFAULT_TOP_SPACE, DEFAULT_BOTTOM_SPACE, DEFAULT_VERTICAL_SCALE, Right);
			}
		}

		public ReadOnlyCollection<TextLine> TextLines {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(WpfTextViewLine));
				return textLines;
			}
		}

		public VisibilityState VisibilityState {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(WpfTextViewLine));
				return visibilityState;
			}
		}

		public Rect VisibleArea {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(WpfTextViewLine));
				return visibleArea;
			}
		}

		public bool HasAdornments {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(WpfTextViewLine));
				foreach (var part in linePartsCollection.LineParts) {
					if (part.AdornmentElement != null)
						return true;
				}
				return false;
			}
		}

		bool IsLastVisualLine { get; }

		TextLine TextLine {
			get {
				Debug.Assert(textLines.Count == 1);
				return textLines[0];
			}
		}

		public WpfTextViewLine(IBufferGraph bufferGraph, LinePartsCollection linePartsCollection, int linePartsIndex, int linePartsLength, int startColumn, int endColumn, ITextSnapshotLine bufferLine, SnapshotSpan span, ITextSnapshot visualSnapshot, TextLine textLine, double indentation, double virtualSpaceWidth) {
			if (bufferGraph == null)
				throw new ArgumentNullException(nameof(bufferGraph));
			if (linePartsCollection == null)
				throw new ArgumentNullException(nameof(linePartsCollection));
			if (linePartsIndex < 0)
				throw new ArgumentOutOfRangeException(nameof(linePartsIndex));
			if (linePartsLength < 0)
				throw new ArgumentOutOfRangeException(nameof(linePartsLength));
			if (linePartsIndex + linePartsLength > linePartsCollection.LineParts.Count)
				throw new ArgumentOutOfRangeException(nameof(linePartsLength));
			if (bufferLine == null)
				throw new ArgumentNullException(nameof(bufferLine));
			if (span.Snapshot != bufferLine.Snapshot)
				throw new ArgumentException();
			if (visualSnapshot == null)
				throw new ArgumentNullException(nameof(visualSnapshot));
			if (textLine == null)
				throw new ArgumentNullException(nameof(textLine));

			IsValid = true;
			this.linePartsIndex = linePartsIndex;
			this.linePartsLength = linePartsLength;
			this.bufferGraph = bufferGraph;
			this.linePartsCollection = linePartsCollection;
			this.startColumn = startColumn;
			this.endColumn = endColumn;
			this.visualSnapshot = visualSnapshot;
			textLines = new ReadOnlyCollection<TextLine>(new[] { textLine });
			Debug.Assert(textLines.Count == 1);// Assumed by all code accessing TextLine prop

			realTopSpace = 0;
			realBottomSpace = 0;
			realBaseline = TextLine.Baseline;
			double baseLineHeight = TextLine.TextHeight - TextLine.Baseline;
			var lineParts = linePartsCollection.LineParts;
			for (int i = 0; i < linePartsLength; i++) {
				var adornmentElement = lineParts[linePartsIndex + i].AdornmentElement;
				if (adornmentElement == null)
					continue;
				double adornmentBaseLineHeight = adornmentElement.TextHeight - adornmentElement.Baseline;
				if (adornmentBaseLineHeight > baseLineHeight)
					baseLineHeight = adornmentBaseLineHeight;
				if (adornmentElement.Baseline > realBaseline)
					realBaseline = adornmentElement.Baseline;
				if (adornmentElement.TopSpace > realTopSpace)
					realTopSpace = adornmentElement.TopSpace;
				if (adornmentElement.BottomSpace > realBottomSpace)
					realBottomSpace = adornmentElement.BottomSpace;
			}
			realTextHeight = Math.Ceiling(baseLineHeight + realBaseline);

			isFirstTextViewLineForSnapshotLine = span.Start == bufferLine.Start;
			isLastTextViewLineForSnapshotLine = span.End == bufferLine.EndIncludingLineBreak;
			IsLastVisualLine = bufferLine.LineNumber + 1 == bufferLine.Snapshot.LineCount && IsLastTextViewLineForSnapshotLine;
			lineBreakLength = isLastTextViewLineForSnapshotLine ? bufferLine.LineBreakLength : 0;
			this.virtualSpaceWidth = virtualSpaceWidth;
			textLeft = indentation;
			textWidth = TextLine.WidthIncludingTrailingWhitespace;
			extentIncludingLineBreak = span;
			endOfLineWidth = Math.Floor(realTextHeight * 0.58333333333333337);// Same as VS
			width = textWidth + (lineBreakLength == 0 ? 0 : endOfLineWidth);
			change = TextViewLineChange.NewOrReformatted;
			SetLineTransform(DefaultLineTransform);
		}
		public const double DEFAULT_TOP_SPACE = 0.0;
		public const double DEFAULT_BOTTOM_SPACE = 1.0;
		public const double DEFAULT_VERTICAL_SCALE = 1.0;

		VisibilityState CalculateVisibilityState() {
			const double eps = 0.01;
			if (Top + eps >= visibleArea.Bottom)
				return VisibilityState.Hidden;
			if (Bottom - eps <= visibleArea.Top)
				return VisibilityState.Hidden;
			if (visibleArea.Top <= Top + eps && Bottom - eps <= visibleArea.Bottom)
				return VisibilityState.FullyVisible;
			return VisibilityState.PartiallyVisible;
		}

		public bool ContainsBufferPosition(SnapshotPoint bufferPosition) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(WpfTextViewLine));
			if (bufferPosition.Snapshot != Snapshot)
				throw new ArgumentException();
			if (!(ExtentIncludingLineBreak.Start <= bufferPosition))
				return false;
			if (IsLastVisualLine)
				return bufferPosition <= ExtentIncludingLineBreak.End;
			return bufferPosition < ExtentIncludingLineBreak.End ||
				// This could be a line with just an adornment which could have an empty span
				(ExtentIncludingLineBreak.Length == 0 && ExtentIncludingLineBreak.Start == bufferPosition);
		}

		public bool IntersectsBufferSpan(SnapshotSpan bufferSpan) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(WpfTextViewLine));
			if (bufferSpan.Snapshot != Snapshot)
				throw new ArgumentException();
			if (Start > bufferSpan.End)
				return false;
			if (bufferSpan.Start < EndIncludingLineBreak)
				return true;
			return LineBreakLength == 0 && bufferSpan.Start == EndIncludingLineBreak && bufferSpan.Start == Snapshot.Length;
		}

		public TF.TextBounds? GetAdornmentBounds(object identityTag) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(WpfTextViewLine));
			if (identityTag == null)
				throw new ArgumentNullException(nameof(identityTag));

			var lineParts = linePartsCollection.LineParts;
			int linePartsIndexLocal = linePartsIndex;
			int linePartsLengthLocal = linePartsLength;
			for (int i = 0; i < linePartsLengthLocal; i++) {
				var part = lineParts[linePartsIndexLocal + i];
				var adornment = part.AdornmentElement;
				if (adornment == null)
					continue;
				if (!identityTag.Equals(adornment.IdentityTag))
					continue;
				return GetTextBounds(part.Column);
			}

			return null;
		}

		public ReadOnlyCollection<object> GetAdornmentTags(object providerTag) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(WpfTextViewLine));
			if (providerTag == null)
				throw new ArgumentNullException(nameof(providerTag));

			var lineParts = linePartsCollection.LineParts;
			int linePartsIndexLocal = linePartsIndex;
			int linePartsLengthLocal = linePartsLength;
			List<object> list = null;
			for (int i = 0; i < linePartsLengthLocal; i++) {
				var part = lineParts[linePartsIndexLocal + i];
				var adornment = part.AdornmentElement;
				if (adornment == null)
					continue;
				if (!providerTag.Equals(adornment.ProviderTag))
					continue;
				if (list == null)
					list = new List<object>();
				list.Add(adornment.IdentityTag);
			}
			if (list == null)
				return emptyReadOnlyCollection;
			return new ReadOnlyCollection<object>(list);
		}
		static readonly ReadOnlyCollection<object> emptyReadOnlyCollection = new ReadOnlyCollection<object>(Array.Empty<object>());

		public SnapshotPoint? GetBufferPositionFromXCoordinate(double xCoordinate) =>
			GetBufferPositionFromXCoordinate(xCoordinate, false);
		public SnapshotPoint? GetBufferPositionFromXCoordinate(double xCoordinate, bool textOnly) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(WpfTextViewLine));
			if (xCoordinate < TextLeft)
				return null;
			if (xCoordinate >= TextLeft + Width)
				return null;
			if (xCoordinate >= TextRight)
				return End;

			Debug.Assert(TextLines.Count == 1);
			double extra = TextLeft;
			var column = TextLine.GetCharacterHitFromDistance(xCoordinate - extra).FirstCharacterIndex;
			return linePartsCollection.ConvertColumnToBufferPosition(column, includeHiddenPositions: !textOnly);
		}

		public VirtualSnapshotPoint GetVirtualBufferPositionFromXCoordinate(double xCoordinate) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(WpfTextViewLine));

			var pos = GetBufferPositionFromXCoordinate(xCoordinate);
			if (pos != null)
				return new VirtualSnapshotPoint(pos.Value);
			if (xCoordinate <= TextLeft)
				return new VirtualSnapshotPoint(ExtentIncludingLineBreak.Start);
			if (xCoordinate >= TextRight && IsLastTextViewLineForSnapshotLine)
				return new VirtualSnapshotPoint(ExtentIncludingLineBreak.End - LineBreakLength, (int)Math.Round((xCoordinate - TextRight) / VirtualSpaceWidth));
			return new VirtualSnapshotPoint(ExtentIncludingLineBreak.End - LineBreakLength);
		}

		public VirtualSnapshotPoint GetInsertionBufferPositionFromXCoordinate(double xCoordinate) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(WpfTextViewLine));

			var pos = GetBufferPositionFromXCoordinate(xCoordinate);
			if (pos != null) {
				if (pos.Value < End) {
					var bounds = GetExtendedCharacterBounds(pos.Value);
					// Get closest buffer position
					bool isOnLeftSide = xCoordinate < (bounds.Left + bounds.Right) / 2;
					if (isOnLeftSide == bounds.IsRightToLeft)
						pos = GetTextElementSpan(pos.Value).End;
				}
				return new VirtualSnapshotPoint(pos.Value);
			}
			if (xCoordinate <= TextLeft)
				return new VirtualSnapshotPoint(ExtentIncludingLineBreak.Start);
			if (xCoordinate >= TextRight && IsLastTextViewLineForSnapshotLine)
				return new VirtualSnapshotPoint(ExtentIncludingLineBreak.End - LineBreakLength, (int)Math.Round((xCoordinate - TextRight) / VirtualSpaceWidth));
			return new VirtualSnapshotPoint(ExtentIncludingLineBreak.End - LineBreakLength);
		}

		public TF.TextBounds GetExtendedCharacterBounds(SnapshotPoint bufferPosition) =>
			GetExtendedCharacterBounds(new VirtualSnapshotPoint(bufferPosition));
		public TF.TextBounds GetExtendedCharacterBounds(VirtualSnapshotPoint bufferPosition) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(WpfTextViewLine));
			if (bufferPosition.Position.Snapshot != Snapshot)
				throw new ArgumentException();
			if (bufferPosition.VirtualSpaces > 0) {
				if (IsLastTextViewLineForSnapshotLine)
					return new TF.TextBounds(TextRight + bufferPosition.VirtualSpaces * VirtualSpaceWidth, Top, VirtualSpaceWidth, Height, TextTop, TextHeight);
				return new TF.TextBounds(TextRight, Top, EndOfLineWidth, Height, TextTop, TextHeight);
			}

			var span = GetTextElementSpan(bufferPosition.Position);
			var part = linePartsCollection.GetLinePartFromBufferPosition(span.Start);
			if (part == null)
				return GetCharacterBounds(bufferPosition);
			var elem = part.Value.AdornmentElement;
			if (elem == null)
				return GetCharacterBounds(bufferPosition);
			return GetTextBounds(part.Value.Column);
		}

		public TF.TextBounds GetCharacterBounds(SnapshotPoint bufferPosition) =>
			GetCharacterBounds(new VirtualSnapshotPoint(bufferPosition));
		public TF.TextBounds GetCharacterBounds(VirtualSnapshotPoint bufferPosition) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(WpfTextViewLine));
			if (bufferPosition.Position.Snapshot != Snapshot)
				throw new ArgumentException();
			if (bufferPosition.VirtualSpaces > 0) {
				if (IsLastTextViewLineForSnapshotLine)
					return new TF.TextBounds(TextRight + bufferPosition.VirtualSpaces * VirtualSpaceWidth, Top, VirtualSpaceWidth, Height, TextTop, TextHeight);
				return new TF.TextBounds(TextRight, Top, EndOfLineWidth, Height, TextTop, TextHeight);
			}
			if (bufferPosition.Position >= End)
				return new TF.TextBounds(TextRight, Top, EndOfLineWidth, Height, TextTop, TextHeight);
			return GetFirstTextBounds(GetTextElementSpan(bufferPosition.Position).Start);
		}

		int GetFirstColumn(SnapshotPoint point) {
			if (point.Snapshot != Snapshot)
				throw new ArgumentException();
			return FilterColumn(linePartsCollection.ConvertBufferPositionToColumn(point));
		}

		int GetLastColumn(SnapshotPoint point) {
			if (point.Snapshot != Snapshot)
				throw new ArgumentException();
			int column = FilterColumn(linePartsCollection.ConvertBufferPositionToColumn(point));
			var part = linePartsCollection.GetLinePartFromColumn(column);
			if (part != null) {
				var lineParts = linePartsCollection.LineParts;
				int lineIndex = point - linePartsCollection.Span.Start;
				for (int i = part.Value.Index + 1; i < lineParts.Count; i++, column++) {
					var part2 = lineParts[i];
					if (!part2.BelongsTo(lineIndex))
						break;
				}
			}
			return FilterColumn(column);
		}

		int FilterColumn(int column) {
			if (column < startColumn)
				return startColumn;
			if (column > endColumn)
				return endColumn;
			return column;
		}

		TF.TextBounds GetFirstTextBounds(SnapshotPoint point) => GetTextBounds(GetFirstColumn(point));
		TF.TextBounds GetLastTextBounds(SnapshotPoint point) => GetTextBounds(GetLastColumn(point));

		TF.TextBounds GetTextBounds(int column) {
			column = FilterColumn(column);
			double start = TextLine.GetDistanceFromCharacterHit(new CharacterHit(column, 0));
			double end = TextLine.GetDistanceFromCharacterHit(new CharacterHit(column, 1));
			double extra = TextLeft;
			Debug.Assert(textLines.Count == 1);
			return new TF.TextBounds(extra + start, Top, end - start, Height, TextTop, TextHeight);
		}

		public TextRunProperties GetCharacterFormatting(SnapshotPoint bufferPosition) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(WpfTextViewLine));
			if (bufferPosition.Snapshot != Snapshot)
				throw new ArgumentException();
			if (!ContainsBufferPosition(bufferPosition))
				throw new ArgumentOutOfRangeException(nameof(bufferPosition));

			int column = GetFirstColumn(bufferPosition);
			TextSpan<TextRun> lastTextSpan = null;
			foreach (var textSpan in TextLine.GetTextRunSpans()) {
				lastTextSpan = textSpan;
				if (column < textSpan.Length)
					return textSpan.Value.Properties;
				column -= textSpan.Length;
			}

			return ((column == 0 && IsLastTextViewLineForSnapshotLine) || IsLastVisualLine) && lastTextSpan != null ? lastTextSpan.Value.Properties : null;
		}

		public Collection<TF.TextBounds> GetNormalizedTextBounds(SnapshotSpan bufferSpan) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(WpfTextViewLine));
			if (bufferSpan.Snapshot != Snapshot)
				throw new ArgumentException();
			var span = ExtentIncludingLineBreak.Intersection(bufferSpan);
			var list = new List<TF.TextBounds>();
			if (span == null)
				return new Collection<TF.TextBounds>(list);

			var startBounds = GetFirstTextBounds(span.Value.Start);
			var endBounds = GetLastTextBounds(span.Value.End);
			if (span.Value.End > End) {
				endBounds = new TF.TextBounds(
					endBounds.Trailing + EndOfLineWidth,
					endBounds.Top,
					0,
					endBounds.Height,
					endBounds.TextTop,
					endBounds.TextHeight);
			}

			list.Add(new TF.TextBounds(startBounds.Left, startBounds.Top, endBounds.Left - startBounds.Left, startBounds.Height, startBounds.TextTop, startBounds.TextHeight));

			return new Collection<TF.TextBounds>(list);
		}

		public SnapshotSpan GetTextElementSpan(SnapshotPoint bufferPosition) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(WpfTextViewLine));
			if (bufferPosition.Snapshot != Snapshot)
				throw new ArgumentException();
			if (!ContainsBufferPosition(bufferPosition))
				throw new ArgumentOutOfRangeException(nameof(bufferPosition));
			if (bufferPosition >= ExtentIncludingLineBreak.End - LineBreakLength)
				return new SnapshotSpan(ExtentIncludingLineBreak.End - LineBreakLength, LineBreakLength);

			int column = GetFirstColumn(bufferPosition);
			int lastColumn = GetLastColumn(bufferPosition);
			var charHit = TextLine.GetNextCaretCharacterHit(new CharacterHit(column, 0));
			var lastCharHit = TextLine.GetNextCaretCharacterHit(new CharacterHit(lastColumn, 0));
			var start = linePartsCollection.ConvertColumnToBufferPosition(charHit.FirstCharacterIndex);
			var end = linePartsCollection.ConvertColumnToBufferPosition(lastCharHit.FirstCharacterIndex + lastCharHit.TrailingLength);
			Debug.Assert(start <= end);
			if (start <= end)
				return new SnapshotSpan(start, end);
			return new SnapshotSpan(end, end);
		}

		public Visual GetOrCreateVisual() {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(WpfTextViewLine));
			if (drawingVisual == null) {
				drawingVisual = new DrawingVisual();
				double x = Left;
				var dc = drawingVisual.RenderOpen();
				foreach (var line in textLines) {
					line.Draw(dc, new Point(x, realBaseline - line.Baseline), InvertAxes.None);
					x += line.WidthIncludingTrailingWhitespace;
				}
				dc.Close();
				UpdateVisualTransform();
			}
			return drawingVisual;
		}
		DrawingVisual drawingVisual;

		public void RemoveVisual() {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(WpfTextViewLine));
			drawingVisual = null;
		}

		public void SetChange(TextViewLineChange change) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(WpfTextViewLine));
			this.change = change;
		}

		void UpdateVisualTransform() {
			if (drawingVisual == null)
				return;
			var t = new MatrixTransform(1, 0, 0, lineTransform.VerticalScale, 0, TextTop);
			t.Freeze();
			drawingVisual.Transform = t;
		}

		public void SetDeltaY(double deltaY) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(WpfTextViewLine));
			this.deltaY = deltaY;
		}

		public void SetLineTransform(LineTransform transform) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(WpfTextViewLine));
			var oldScaledTopSpace = scaledTopSpace;
			bool resetTransform = lineTransform.VerticalScale != transform.VerticalScale;
			lineTransform = transform;
			scaledTopSpace = Math.Ceiling(Math.Max(transform.TopSpace, realTopSpace) * transform.VerticalScale);
			scaledTextHeight = Math.Ceiling(realTextHeight * transform.VerticalScale);
			scaledHeight = scaledTextHeight + scaledTopSpace + Math.Ceiling(Math.Max(transform.BottomSpace, realBottomSpace) * transform.VerticalScale);
			if (resetTransform || scaledTopSpace != oldScaledTopSpace)
				UpdateVisualTransform();
		}

		public void SetSnapshot(ITextSnapshot visualSnapshot, ITextSnapshot editSnapshot) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(WpfTextViewLine));
			if (visualSnapshot == null)
				throw new ArgumentNullException(nameof(visualSnapshot));
			if (editSnapshot == null)
				throw new ArgumentNullException(nameof(editSnapshot));
			if (visualSnapshot != editSnapshot)
				throw new NotSupportedException();
			int oldLength = extentIncludingLineBreak.Length;
			extentIncludingLineBreak = extentIncludingLineBreak.TranslateTo(editSnapshot, SpanTrackingMode.EdgeExclusive);
			// This line should've been invalidated if there were any changes to it
			if (oldLength != extentIncludingLineBreak.Length)
				throw new InvalidOperationException();
			linePartsCollection.SetSnapshot(visualSnapshot, editSnapshot);
			this.visualSnapshot = visualSnapshot;
		}

		public void SetTop(double top) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(WpfTextViewLine));
			if (this.top != top) {
				this.top = top;
				UpdateVisualTransform();
			}
		}

		public void SetVisibleArea(Rect visibleArea) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(WpfTextViewLine));
			this.visibleArea = visibleArea;
			visibilityState = CalculateVisibilityState();
		}

		public void Dispose() {
			IsValid = false;
			foreach (var t in textLines)
				t.Dispose();
			bufferGraph = null;
			extentIncludingLineBreak = default(SnapshotSpan);
			visualSnapshot = null;
			linePartsCollection = null;
			textLines = null;
			drawingVisual = null;
		}
	}
}
