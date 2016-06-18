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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media.TextFormatting;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Formatting;
using ICSharpCode.AvalonEdit.Rendering;

namespace dnSpy.Text.Formatting {
	sealed class WpfTextViewLine : IWpfTextViewLine {
		readonly double height;
		readonly double top;
		readonly double width;
		readonly double textHeight;
		readonly double textLeft;
		readonly double textWidth;
		readonly double virtualSpaceWidth;
		readonly double deltaY;
		readonly double endOfLineWidth;
		readonly TextViewLineChange change;
		Rect visibleArea;
		readonly SnapshotSpan extentIncludingLineBreak;
		readonly int lineBreakLength;
		VisibilityState visibilityState;
		readonly bool isFirstTextViewLineForSnapshotLine;
		readonly bool isLastTextViewLineForSnapshotLine;
		readonly LineTransform lineTransform;
		readonly int lineStartOffset;
		bool isValid;

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
				return height;
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
		internal double GetTop() => top;

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
				return Top + lineTransform.TopSpace + TextHeight;
			}
		}

		public double TextTop {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(WpfTextViewLine));
				return Top + lineTransform.TopSpace;
			}
		}

		public double TextHeight {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(WpfTextViewLine));
				return textHeight;
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
				return TextLine.Baseline;
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
		public bool IsValid => isValid;

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
				throw new NotImplementedException();//TODO:
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
				throw new NotImplementedException();//TODO:
			}
		}

		public LineTransform DefaultLineTransform {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(WpfTextViewLine));
				return new LineTransform(0, 1, 1, Right);
			}
		}

		public ReadOnlyCollection<TextLine> TextLines {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(WpfTextViewLine));
				return textLines;
			}
		}
		ReadOnlyCollection<TextLine> textLines;

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
			internal set {
				visibleArea = value;
				visibilityState = CalculateVisibilityState();
			}
		}

		internal void SetIsInvalid() {
			isValid = false;
			VisualLine = null;
			textLines = null;
		}

		bool IsLastVisualLine { get; }
		internal VisualLine VisualLine { get; private set; }

		internal TextLine TextLine {
			get {
				Debug.Assert(textLines.Count == 1);
				return textLines[0];
			}
		}

		public WpfTextViewLine(ITextSnapshot snapshot, VisualLine visualLine, TextLineInfo info, double top, double deltaY, TextViewLineChange change, Rect visibleArea, double virtualSpaceWidth) {
			if (snapshot == null)
				throw new ArgumentNullException(nameof(snapshot));
			if (visualLine == null)
				throw new ArgumentNullException(nameof(visualLine));
			if (info == null)
				throw new ArgumentNullException(nameof(info));

			this.lineStartOffset = visualLine.FirstDocumentLine.Offset;
			var startOffset = lineStartOffset + info.StartOffset;
			var endOffset = lineStartOffset + info.EndOffset;
			if (startOffset < visualLine.FirstDocumentLine.Offset)
				throw new ArgumentException();
			if (endOffset > visualLine.FirstDocumentLine.EndOffset + visualLine.FirstDocumentLine.DelimiterLength)
				throw new ArgumentException();
			if (endOffset > snapshot.Length)
				throw new ArgumentException();
			isValid = true;
			VisualLine = visualLine;
			IsLastVisualLine = VisualLine.TextLines.IndexOf(info.TextLine) == VisualLine.TextLines.Count - 1 &&
						VisualLine.LastDocumentLine.LineNumber == VisualLine.Document.LineCount;

			// Only one line must be used
			Debug.Assert(visualLine.FirstDocumentLine == visualLine.LastDocumentLine);
			if (visualLine.FirstDocumentLine != visualLine.LastDocumentLine)
				throw new InvalidOperationException();
			this.isFirstTextViewLineForSnapshotLine = startOffset == visualLine.FirstDocumentLine.Offset;
			this.isLastTextViewLineForSnapshotLine = endOffset == visualLine.FirstDocumentLine.EndOffset + visualLine.FirstDocumentLine.DelimiterLength;

			this.lineBreakLength = info.LineBreakLength;
			this.extentIncludingLineBreak = new SnapshotSpan(snapshot, Span.FromBounds(startOffset, endOffset));
			this.textLines = new ReadOnlyCollection<TextLine>(new TextLine[] { info.TextLine });
			this.height = info.TextLine.Height;
			this.textHeight = info.TextLine.TextHeight;
			this.textLeft = info.Indentation;
			this.virtualSpaceWidth = virtualSpaceWidth;
			this.top = top;
			this.deltaY = deltaY;
			this.change = change;
			this.visibleArea = visibleArea;
			this.textWidth = info.TextLine.WidthIncludingTrailingWhitespace - info.Indentation;
			this.endOfLineWidth = Math.Floor(this.textHeight * 0.58333333333333337);// Same as VS
			this.width = this.textWidth + (info.TextLine.NewlineLength == 0 ? 0 : this.endOfLineWidth);
			this.lineTransform = new LineTransform(0, 0, 1, Right);

			this.visibilityState = CalculateVisibilityState();
		}

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
			return bufferPosition < ExtentIncludingLineBreak.End;
		}

		public bool IntersectsBufferSpan(SnapshotSpan bufferSpan) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(WpfTextViewLine));
			if (bufferSpan.Snapshot != Snapshot)
				throw new ArgumentException();
			if (IsLastVisualLine)
				return ExtentIncludingLineBreak.IntersectsWith(bufferSpan);
			return ExtentIncludingLineBreak.OverlapsWith(bufferSpan);
		}

		public Contracts.Text.Formatting.TextBounds? GetAdornmentBounds(object identityTag) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(WpfTextViewLine));
			throw new NotImplementedException();//TODO:
		}

		public ReadOnlyCollection<object> GetAdornmentTags(object providerTag) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(WpfTextViewLine));
			if (providerTag == null)
				throw new ArgumentNullException(nameof(providerTag));
			throw new NotImplementedException();//TODO:
		}

		public SnapshotPoint? GetBufferPositionFromXCoordinate(double xCoordinate) =>
			GetBufferPositionFromXCoordinate(xCoordinate, false);
		public SnapshotPoint? GetBufferPositionFromXCoordinate(double xCoordinate, bool textOnly) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(WpfTextViewLine));
			if (!(TextLeft <= xCoordinate && xCoordinate < TextRight))
				return null;

			//TODO: Use textOnly

			var charHit = TextLine.GetCharacterHitFromDistance(xCoordinate);
			return new SnapshotPoint(Snapshot, lineStartOffset + VisualLine.GetRelativeOffset(charHit.FirstCharacterIndex + charHit.TrailingLength));
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
					//TODO: Handle RTL text
				}
				return new VirtualSnapshotPoint(pos.Value);
			}
			if (xCoordinate <= TextLeft)
				return new VirtualSnapshotPoint(ExtentIncludingLineBreak.Start);
			if (xCoordinate >= TextRight && IsLastTextViewLineForSnapshotLine)
				return new VirtualSnapshotPoint(ExtentIncludingLineBreak.End - LineBreakLength, (int)Math.Round((xCoordinate - TextRight) / VirtualSpaceWidth));
			return new VirtualSnapshotPoint(ExtentIncludingLineBreak.End - LineBreakLength);
		}

		public Contracts.Text.Formatting.TextBounds GetExtendedCharacterBounds(SnapshotPoint bufferPosition) =>
			GetExtendedCharacterBounds(new VirtualSnapshotPoint(bufferPosition));
		public Contracts.Text.Formatting.TextBounds GetExtendedCharacterBounds(VirtualSnapshotPoint bufferPosition) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(WpfTextViewLine));
			if (bufferPosition.Position.Snapshot != Snapshot)
				throw new ArgumentException();
			if (bufferPosition.VirtualSpaces > 0) {
				if (IsLastTextViewLineForSnapshotLine)
					return new Contracts.Text.Formatting.TextBounds(TextRight + bufferPosition.VirtualSpaces * VirtualSpaceWidth, Top, VirtualSpaceWidth, Height, TextTop, TextHeight);
				return new Contracts.Text.Formatting.TextBounds(TextRight, Top, EndOfLineWidth, Height, TextTop, TextHeight);
			}
			throw new NotImplementedException();//TODO:
		}

		public Contracts.Text.Formatting.TextBounds GetCharacterBounds(SnapshotPoint bufferPosition) =>
			GetCharacterBounds(new VirtualSnapshotPoint(bufferPosition));
		public Contracts.Text.Formatting.TextBounds GetCharacterBounds(VirtualSnapshotPoint bufferPosition) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(WpfTextViewLine));
			if (bufferPosition.Position.Snapshot != Snapshot)
				throw new ArgumentException();
			if (bufferPosition.VirtualSpaces > 0) {
				if (IsLastTextViewLineForSnapshotLine)
					return new Contracts.Text.Formatting.TextBounds(TextRight + bufferPosition.VirtualSpaces * VirtualSpaceWidth, Top, VirtualSpaceWidth, Height, TextTop, TextHeight);
				return new Contracts.Text.Formatting.TextBounds(TextRight, Top, EndOfLineWidth, Height, TextTop, TextHeight);
			}
			throw new NotImplementedException();//TODO:
		}

		public TextRunProperties GetCharacterFormatting(SnapshotPoint bufferPosition) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(WpfTextViewLine));
			if (bufferPosition.Snapshot != Snapshot)
				throw new ArgumentException();
			if (!ContainsBufferPosition(bufferPosition))
				throw new ArgumentOutOfRangeException(nameof(bufferPosition));
			int column = bufferPosition - ExtentIncludingLineBreak.Start;
			Debug.Assert(column >= 0);

			TextSpan<TextRun> lastTextSpan = null;
			foreach (var textSpan in TextLine.GetTextRunSpans()) {
				lastTextSpan = textSpan;
				if (column < textSpan.Length)
					return textSpan.Value.Properties;
				column -= textSpan.Length;
			}

			return ((column == 0 && IsLastTextViewLineForSnapshotLine) || IsLastVisualLine) && lastTextSpan != null ? lastTextSpan.Value.Properties : null;
		}

		public Collection<Contracts.Text.Formatting.TextBounds> GetNormalizedTextBounds(SnapshotSpan bufferSpan) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(WpfTextViewLine));
			throw new NotImplementedException();//TODO:
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

			int visualColumn = VisualLine.GetVisualColumn(bufferPosition.Position - lineStartOffset);
			Debug.Assert(visualColumn >= 0);

			var charHit = TextLine.GetNextCaretCharacterHit(new CharacterHit(visualColumn, 0));
			return new SnapshotSpan(Snapshot, lineStartOffset + VisualLine.GetRelativeOffset(charHit.FirstCharacterIndex), charHit.TrailingLength);
		}
	}
}
