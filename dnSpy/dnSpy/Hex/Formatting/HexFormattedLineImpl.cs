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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Formatting;
using VST = Microsoft.VisualStudio.Text;
using VSTF = Microsoft.VisualStudio.Text.Formatting;

namespace dnSpy.Hex.Formatting {
	sealed class HexFormattedLineImpl : HexFormattedLine {
		readonly int endColumn, startColumn;
		readonly int linePartsIndex, linePartsLength;
		double top;
		readonly double width;
		readonly double textLeft;
		readonly double textWidth;
		readonly double virtualSpaceWidth;
		double deltaY;
		readonly double endOfLineWidth;
		VSTF.TextViewLineChange change;
		Rect visibleArea;
		VST.Span lineExtent;
		VSTF.VisibilityState visibilityState;
		VSTF.LineTransform lineTransform;
		ReadOnlyCollection<TextLine> textLines;
		HexLinePartsCollection linePartsCollection;
		double realTopSpace, scaledTopSpace;
		double realBottomSpace;
		double realTextHeight, scaledTextHeight;
		double scaledHeight;
		double realBaseline;
		HexBufferLine bufferLine;

		public override HexBufferLine BufferLine {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(HexFormattedLineImpl));
				return bufferLine;
			}
		}

		public override double Bottom {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(HexFormattedLineImpl));
				return Top + Height;
			}
		}

		public override double Height {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(HexFormattedLineImpl));
				return scaledHeight;
			}
		}

		public override double Left {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(HexFormattedLineImpl));
				return TextLeft;
			}
		}

		public override double Right {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(HexFormattedLineImpl));
				return Left + Width;
			}
		}

		public override double Top {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(HexFormattedLineImpl));
				return top;
			}
		}

		public override double Width {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(HexFormattedLineImpl));
				return width;
			}
		}

		public override double TextBottom {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(HexFormattedLineImpl));
				return Top + scaledTopSpace + TextHeight;
			}
		}

		public override double TextTop {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(HexFormattedLineImpl));
				return Top + scaledTopSpace;
			}
		}

		public override double TextHeight {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(HexFormattedLineImpl));
				return scaledTextHeight;
			}
		}

		public override double TextWidth {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(HexFormattedLineImpl));
				return textWidth;
			}
		}

		public override double TextLeft {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(HexFormattedLineImpl));
				return textLeft;
			}
		}

		public override double TextRight {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(HexFormattedLineImpl));
				return TextLeft + TextWidth;
			}
		}

		public override double VirtualSpaceWidth {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(HexFormattedLineImpl));
				return virtualSpaceWidth;
			}
		}

		public override double Baseline {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(HexFormattedLineImpl));
				return realBaseline * lineTransform.VerticalScale;
			}
		}

		public override double DeltaY {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(HexFormattedLineImpl));
				return deltaY;
			}
		}

		public override double EndOfLineWidth {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(HexFormattedLineImpl));
				return endOfLineWidth;
			}
		}

		public override VSTF.TextViewLineChange Change {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(HexFormattedLineImpl));
				return change;
			}
		}

		public override object IdentityTag => this;
		public override bool IsValid => isValid;
		bool isValid;

		public override VSTF.LineTransform LineTransform {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(HexFormattedLineImpl));
				return lineTransform;
			}
		}

		public override VSTF.LineTransform DefaultLineTransform {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(HexFormattedLineImpl));
				return new VSTF.LineTransform(DEFAULT_TOP_SPACE, DEFAULT_BOTTOM_SPACE, DEFAULT_VERTICAL_SCALE, Right);
			}
		}

		public override ReadOnlyCollection<TextLine> TextLines {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(HexFormattedLineImpl));
				return textLines;
			}
		}

		public override VSTF.VisibilityState VisibilityState {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(HexFormattedLineImpl));
				return visibilityState;
			}
		}

		public override Rect VisibleArea {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(HexFormattedLineImpl));
				return visibleArea;
			}
		}

		public override bool HasAdornments {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(HexFormattedLineImpl));
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

		public HexFormattedLineImpl(HexLinePartsCollection linePartsCollection, int linePartsIndex, int linePartsLength, int startColumn, int endColumn, HexBufferLine bufferLine, VST.Span lineSpan, TextLine textLine, double indentation, double virtualSpaceWidth) {
			if (linePartsCollection == null)
				throw new ArgumentNullException(nameof(linePartsCollection));
			if (linePartsIndex < 0)
				throw new ArgumentOutOfRangeException(nameof(linePartsIndex));
			if (linePartsLength < 0)
				throw new ArgumentOutOfRangeException(nameof(linePartsLength));
			if (linePartsIndex + linePartsLength > linePartsCollection.LineParts.Count)
				throw new ArgumentOutOfRangeException(nameof(linePartsLength));
			if (textLine == null)
				throw new ArgumentNullException(nameof(textLine));

			this.bufferLine = bufferLine ?? throw new ArgumentNullException(nameof(bufferLine));
			isValid = true;
			this.linePartsIndex = linePartsIndex;
			this.linePartsLength = linePartsLength;
			this.linePartsCollection = linePartsCollection;
			this.startColumn = startColumn;
			this.endColumn = endColumn;
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

			IsLastVisualLine = bufferLine.LineNumber + 1 == bufferLine.LineProvider.LineCount;
			this.virtualSpaceWidth = virtualSpaceWidth;
			textLeft = indentation;
			textWidth = TextLine.WidthIncludingTrailingWhitespace;
			lineExtent = lineSpan;
			endOfLineWidth = Math.Floor(realTextHeight * 0.58333333333333337);// Same as VS
			width = textWidth;
			change = VSTF.TextViewLineChange.NewOrReformatted;
			SetLineTransform(DefaultLineTransform);
		}
		public const double DEFAULT_TOP_SPACE = 0.0;
		public const double DEFAULT_BOTTOM_SPACE = 1.0;
		public const double DEFAULT_VERTICAL_SCALE = 1.0;

		VSTF.VisibilityState CalculateVisibilityState() {
			const double eps = 0.01;
			if (Top + eps >= visibleArea.Bottom)
				return VSTF.VisibilityState.Hidden;
			if (Bottom - eps <= visibleArea.Top)
				return VSTF.VisibilityState.Hidden;
			if (visibleArea.Top <= Top + eps && Bottom - eps <= visibleArea.Bottom)
				return VSTF.VisibilityState.FullyVisible;
			return VSTF.VisibilityState.PartiallyVisible;
		}

		public override bool ContainsBufferPosition(HexBufferPoint bufferPosition) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(HexFormattedLineImpl));
			if (bufferPosition.Buffer != Buffer)
				throw new ArgumentException();
			if (!(BufferStart <= bufferPosition))
				return false;
			if (IsLastVisualLine)
				return bufferPosition <= BufferEnd;
			return bufferPosition < BufferEnd;
		}

		public override bool IntersectsBufferSpan(HexBufferSpan bufferSpan) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(HexFormattedLineImpl));
			if (bufferSpan.Buffer != Buffer)
				throw new ArgumentException();
			if (BufferStart > bufferSpan.End)
				return false;
			if (bufferSpan.Start < BufferEnd)
				return true;
			return IsLastVisualLine && bufferSpan.Start == BufferEnd;
		}

		public override VSTF.TextBounds? GetAdornmentBounds(object identityTag) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(HexFormattedLineImpl));
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

		public override ReadOnlyCollection<object> GetAdornmentTags(object providerTag) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(HexFormattedLineImpl));
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

		public override int? GetLinePositionFromXCoordinate(double xCoordinate) =>
			GetLinePositionFromXCoordinate(xCoordinate, false);
		public override int? GetLinePositionFromXCoordinate(double xCoordinate, bool textOnly) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(HexFormattedLineImpl));
			if (xCoordinate < TextLeft)
				return null;
			if (xCoordinate >= TextLeft + Width)
				return null;
			if (xCoordinate >= TextRight)
				return TextSpan.End;

			Debug.Assert(TextLines.Count == 1);
			double extra = TextLeft;
			var column = TextLine.GetCharacterHitFromDistance(xCoordinate - extra).FirstCharacterIndex;
			return linePartsCollection.ConvertColumnToLinePosition(column, includeHiddenPositions: !textOnly);
		}

		public override int GetVirtualLinePositionFromXCoordinate(double xCoordinate) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(HexFormattedLineImpl));

			var pos = GetLinePositionFromXCoordinate(xCoordinate);
			if (pos != null)
				return pos.Value;
			if (xCoordinate <= TextLeft)
				return TextSpan.Start;
			if (xCoordinate >= TextRight)
				return TextSpan.End + (int)Math.Round((xCoordinate - TextRight) / VirtualSpaceWidth);
			return TextSpan.End;
		}

		public override int GetInsertionLinePositionFromXCoordinate(double xCoordinate) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(HexFormattedLineImpl));

			var pos = GetLinePositionFromXCoordinate(xCoordinate);
			if (pos != null) {
				if (pos.Value < TextSpan.End) {
					var bounds = GetExtendedCharacterBounds(pos.Value);
					// Get closest buffer position
					bool isOnLeftSide = xCoordinate < (bounds.Left + bounds.Right) / 2;
					if (isOnLeftSide == bounds.IsRightToLeft)
						pos = GetTextElementSpan(pos.Value).End;
				}
				return pos.Value;
			}
			if (xCoordinate <= TextLeft)
				return TextSpan.Start;
			if (xCoordinate >= TextRight)
				return TextSpan.End + (int)Math.Round((xCoordinate - TextRight) / VirtualSpaceWidth);
			return TextSpan.End;
		}

		public override VSTF.TextBounds GetExtendedCharacterBounds(int linePosition) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(HexFormattedLineImpl));
			if (linePosition < 0)
				throw new ArgumentOutOfRangeException(nameof(linePosition));
			int virtualSpaces = linePosition - TextSpan.End;
			if (virtualSpaces > 0)
				return new VSTF.TextBounds(TextRight + virtualSpaces * VirtualSpaceWidth, Top, VirtualSpaceWidth, Height, TextTop, TextHeight);

			var span = GetTextElementSpan(linePosition);
			var part = linePartsCollection.GetLinePartFromLinePosition(span.Start);
			if (part == null)
				return GetCharacterBounds(linePosition);
			var elem = part.Value.AdornmentElement;
			if (elem == null)
				return GetCharacterBounds(linePosition);
			return GetTextBounds(part.Value.Column);
		}

		public override VSTF.TextBounds GetCharacterBounds(int linePosition) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(HexFormattedLineImpl));
			if (linePosition < 0)
				throw new ArgumentOutOfRangeException(nameof(linePosition));
			int virtualSpaces = linePosition - TextSpan.End;
			if (virtualSpaces > 0)
				return new VSTF.TextBounds(TextRight + virtualSpaces * VirtualSpaceWidth, Top, VirtualSpaceWidth, Height, TextTop, TextHeight);
			if (virtualSpaces == 0)
				return new VSTF.TextBounds(TextRight, Top, EndOfLineWidth, Height, TextTop, TextHeight);
			return GetFirstTextBounds(GetTextElementSpan(linePosition).Start);
		}

		int GetFirstColumn(int position) =>
			FilterColumn(linePartsCollection.ConvertLinePositionToColumn(position));

		int GetLastColumn(int position) {
			int column = FilterColumn(linePartsCollection.ConvertLinePositionToColumn(position));
			var part = linePartsCollection.GetLinePartFromColumn(column);
			if (part != null) {
				var lineParts = linePartsCollection.LineParts;
				int lineIndex = position - linePartsCollection.Span.Start;
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

		VSTF.TextBounds GetFirstTextBounds(int position) => GetTextBounds(GetFirstColumn(position));
		VSTF.TextBounds GetLastTextBounds(int position) => GetTextBounds(GetLastColumn(position));

		VSTF.TextBounds GetTextBounds(int column) {
			column = FilterColumn(column);
			double start = TextLine.GetDistanceFromCharacterHit(new CharacterHit(column, 0));
			double end = TextLine.GetDistanceFromCharacterHit(new CharacterHit(column, 1));
			double extra = TextLeft;
			Debug.Assert(textLines.Count == 1);
			return new VSTF.TextBounds(extra + start, Top, end - start, Height, TextTop, TextHeight);
		}

		public override TextRunProperties GetCharacterFormatting(int linePosition) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(HexFormattedLineImpl));
			if (linePosition < 0)
				throw new ArgumentOutOfRangeException(nameof(linePosition));

			int column = GetFirstColumn(linePosition);
			TextSpan<TextRun> lastTextSpan = null;
			foreach (var textSpan in TextLine.GetTextRunSpans()) {
				lastTextSpan = textSpan;
				if (column < textSpan.Length)
					return textSpan.Value.Properties;
				column -= textSpan.Length;
			}

			return (column == 0 || IsLastVisualLine) && lastTextSpan != null ? lastTextSpan.Value.Properties : null;
		}

		public override Collection<VSTF.TextBounds> GetNormalizedTextBounds(VST.Span lineSpan) {
			var list = new List<VSTF.TextBounds>();
			var bounds = TryGetNormalizedTextBounds(lineSpan);
			if (bounds != null)
				list.Add(bounds.Value);
			return new Collection<VSTF.TextBounds>(list);
		}

		VSTF.TextBounds? TryGetNormalizedTextBounds(VST.Span lineSpan) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(HexFormattedLineImpl));
			var span = lineExtent.Intersection(lineSpan);
			if (span == null)
				return null;

			var startBounds = GetFirstTextBounds(span.Value.Start);
			var endBounds = GetLastTextBounds(span.Value.End);
			if (span.Value.End > TextSpan.End) {
				endBounds = new VSTF.TextBounds(
					endBounds.Trailing + EndOfLineWidth,
					endBounds.Top,
					0,
					endBounds.Height,
					endBounds.TextTop,
					endBounds.TextHeight);
			}

			return new VSTF.TextBounds(startBounds.Left, startBounds.Top, endBounds.Left - startBounds.Left, startBounds.Height, startBounds.TextTop, startBounds.TextHeight);
		}

		public override Collection<VSTF.TextBounds> GetNormalizedTextBounds(HexBufferSpan bufferPosition, HexSpanSelectionFlags flags) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(HexFormattedLineImpl));
			var pos = BufferSpan.Intersection(bufferPosition);
			var list = new List<VSTF.TextBounds>();
			if (pos == null)
				return new Collection<VSTF.TextBounds>(list);

			foreach (var info in bufferLine.GetSpans(pos.Value, flags)) {
				var valuesSpan = TryGetNormalizedTextBounds(info.TextSpan);
				if (valuesSpan != null)
					list.Add(valuesSpan.Value);
			}

			return new Collection<VSTF.TextBounds>(list);
		}

		public override VST.Span GetTextElementSpan(int linePosition) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(HexFormattedLineImpl));
			if (linePosition < 0)
				throw new ArgumentOutOfRangeException(nameof(linePosition));

			int column = GetFirstColumn(linePosition);
			int lastColumn = GetLastColumn(linePosition);
			var charHit = TextLine.GetNextCaretCharacterHit(new CharacterHit(column, 0));
			var lastCharHit = TextLine.GetNextCaretCharacterHit(new CharacterHit(lastColumn, 0));
			var start = linePartsCollection.ConvertColumnToLinePosition(charHit.FirstCharacterIndex);
			var end = linePartsCollection.ConvertColumnToLinePosition(lastCharHit.FirstCharacterIndex + lastCharHit.TrailingLength);
			Debug.Assert(start <= end);
			if (start <= end)
				return VST.Span.FromBounds(start, end);
			return VST.Span.FromBounds(end, end);
		}

		public override Visual GetOrCreateVisual() {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(HexFormattedLineImpl));
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

		public override void RemoveVisual() {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(HexFormattedLineImpl));
			drawingVisual = null;
		}

		public override void SetChange(VSTF.TextViewLineChange change) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(HexFormattedLineImpl));
			this.change = change;
		}

		void UpdateVisualTransform() {
			if (drawingVisual == null)
				return;
			var t = new MatrixTransform(1, 0, 0, lineTransform.VerticalScale, 0, TextTop);
			t.Freeze();
			drawingVisual.Transform = t;
		}

		public override void SetDeltaY(double deltaY) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(HexFormattedLineImpl));
			this.deltaY = deltaY;
		}

		public override void SetLineTransform(VSTF.LineTransform transform) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(HexFormattedLineImpl));
			var oldScaledTopSpace = scaledTopSpace;
			bool resetTransform = lineTransform.VerticalScale != transform.VerticalScale;
			lineTransform = transform;
			scaledTopSpace = Math.Ceiling(Math.Max(transform.TopSpace, realTopSpace) * transform.VerticalScale);
			scaledTextHeight = Math.Ceiling(realTextHeight * transform.VerticalScale);
			scaledHeight = scaledTextHeight + scaledTopSpace + Math.Ceiling(Math.Max(transform.BottomSpace, realBottomSpace) * transform.VerticalScale);
			if (resetTransform || scaledTopSpace != oldScaledTopSpace)
				UpdateVisualTransform();
		}

		public override void SetTop(double top) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(HexFormattedLineImpl));
			if (this.top != top) {
				this.top = top;
				UpdateVisualTransform();
			}
		}

		public override void SetVisibleArea(Rect visibleArea) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(HexFormattedLineImpl));
			this.visibleArea = visibleArea;
			visibilityState = CalculateVisibilityState();
		}

		protected override void DisposeCore() {
			isValid = false;
			foreach (var t in textLines)
				t.Dispose();
			bufferLine = null;
			linePartsCollection = null;
			textLines = null;
			drawingVisual = null;
		}
	}
}
