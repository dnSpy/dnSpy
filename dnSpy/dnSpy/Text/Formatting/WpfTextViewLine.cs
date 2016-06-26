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
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Formatting;
using CF = dnSpy.Contracts.Text.Formatting;

namespace dnSpy.Text.Formatting {
	sealed class WpfTextViewLine : IFormattedLine {
		readonly double height;
		double top;
		readonly double width;
		readonly double textHeight;
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
		readonly LinePartsCollection linePartsCollection;

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
				return new MappingSpan(Extent, SpanTrackingMode.EdgeInclusive);
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
				return new MappingSpan(ExtentIncludingLineBreak, SpanTrackingMode.EdgeInclusive);
			}
		}

		public LineTransform DefaultLineTransform {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(WpfTextViewLine));
				return new LineTransform(DEFAULT_TOP_SPACE, DEFAULT_BOTTOM_SPACE, 1, Right);
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

		bool IsLastVisualLine { get; }

		TextLine TextLine {
			get {
				Debug.Assert(textLines.Count == 1);
				return textLines[0];
			}
		}

		public WpfTextViewLine(LinePartsCollection linePartsCollection, ITextSnapshotLine bufferLine, SnapshotSpan span, ITextSnapshot visualSnapshot, TextLine textLine, double indentation, double virtualSpaceWidth) {
			if (linePartsCollection == null)
				throw new ArgumentNullException(nameof(linePartsCollection));
			if (bufferLine == null)
				throw new ArgumentNullException(nameof(bufferLine));
			if (span.Snapshot != bufferLine.Snapshot)
				throw new ArgumentException();
			if (visualSnapshot == null)
				throw new ArgumentNullException(nameof(visualSnapshot));
			if (textLine == null)
				throw new ArgumentNullException(nameof(textLine));

			this.IsValid = true;
			this.linePartsCollection = linePartsCollection;
			this.visualSnapshot = visualSnapshot;
			this.textLines = new ReadOnlyCollection<TextLine>(new[] { textLine });
			Debug.Assert(textLines.Count == 1);// Assumed by all code accessing TextLine prop
			this.isFirstTextViewLineForSnapshotLine = span.Start == bufferLine.Start;
			this.isLastTextViewLineForSnapshotLine = span.End == bufferLine.EndIncludingLineBreak;
			IsLastVisualLine = bufferLine.LineNumber + 1 == bufferLine.Snapshot.LineCount && IsLastTextViewLineForSnapshotLine;
			this.lineBreakLength = isLastTextViewLineForSnapshotLine ? bufferLine.LineBreakLength : 0;
			this.virtualSpaceWidth = virtualSpaceWidth;
			this.textLeft = indentation;
			this.textWidth = Math.Max(0, textLine.WidthIncludingTrailingWhitespace - indentation);
			this.extentIncludingLineBreak = span;
			this.endOfLineWidth = Math.Floor(this.textHeight * 0.58333333333333337);// Same as VS
			this.width = this.textWidth + (this.lineBreakLength == 0 ? 0 : this.endOfLineWidth);
			this.lineTransform = new LineTransform(DEFAULT_TOP_SPACE, DEFAULT_BOTTOM_SPACE, 1, Right);
			this.height = textLine.Height + lineTransform.BottomSpace;
			this.textHeight = textLine.TextHeight;
			this.change = TextViewLineChange.NewOrReformatted;
		}
		public const double DEFAULT_TOP_SPACE = 0.0;
		public const double DEFAULT_BOTTOM_SPACE = 1.0;

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

		public CF.TextBounds? GetAdornmentBounds(object identityTag) {
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
			return linePartsCollection.ConvertColumnToBufferPosition(charHit.FirstCharacterIndex + charHit.TrailingLength);
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

		public CF.TextBounds GetExtendedCharacterBounds(SnapshotPoint bufferPosition) =>
			GetExtendedCharacterBounds(new VirtualSnapshotPoint(bufferPosition));
		public CF.TextBounds GetExtendedCharacterBounds(VirtualSnapshotPoint bufferPosition) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(WpfTextViewLine));
			if (bufferPosition.Position.Snapshot != Snapshot)
				throw new ArgumentException();
			if (bufferPosition.VirtualSpaces > 0) {
				if (IsLastTextViewLineForSnapshotLine)
					return new CF.TextBounds(TextRight + bufferPosition.VirtualSpaces * VirtualSpaceWidth, Top, VirtualSpaceWidth, Height, TextTop, TextHeight);
				return new CF.TextBounds(TextRight, Top, EndOfLineWidth, Height, TextTop, TextHeight);
			}
			throw new NotImplementedException();//TODO:
		}

		public CF.TextBounds GetCharacterBounds(SnapshotPoint bufferPosition) =>
			GetCharacterBounds(new VirtualSnapshotPoint(bufferPosition));
		public CF.TextBounds GetCharacterBounds(VirtualSnapshotPoint bufferPosition) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(WpfTextViewLine));
			if (bufferPosition.Position.Snapshot != Snapshot)
				throw new ArgumentException();
			if (bufferPosition.VirtualSpaces > 0) {
				if (IsLastTextViewLineForSnapshotLine)
					return new CF.TextBounds(TextRight + bufferPosition.VirtualSpaces * VirtualSpaceWidth, Top, VirtualSpaceWidth, Height, TextTop, TextHeight);
				return new CF.TextBounds(TextRight, Top, EndOfLineWidth, Height, TextTop, TextHeight);
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

		public Collection<CF.TextBounds> GetNormalizedTextBounds(SnapshotSpan bufferSpan) {
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

			int column = linePartsCollection.ConvertBufferPositionToColumn(bufferPosition);
			var charHit = TextLine.GetNextCaretCharacterHit(new CharacterHit(column, 0));
			return new SnapshotSpan(linePartsCollection.ConvertColumnToBufferPosition(charHit.FirstCharacterIndex), charHit.TrailingLength);
		}

		public Visual GetOrCreateVisual() {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(WpfTextViewLine));
			if (drawingVisual == null) {
				drawingVisual = new DrawingVisual();
				double x = Left;
				var dc = drawingVisual.RenderOpen();
				foreach (var line in textLines) {
					line.Draw(dc, new Point(x, Baseline - line.Baseline), InvertAxes.None);
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
			var t = new TranslateTransform(0, TextTop);
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
			lineTransform = transform;
			UpdateVisualTransform();
			throw new NotImplementedException();//TODO:
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
			bool topChanged = this.top != top;
			this.top = top;
			if (topChanged || drawingVisual?.Transform == null)
				UpdateVisualTransform();
		}

		public void SetVisibleArea(Rect visibleArea) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(WpfTextViewLine));
			this.visibleArea = visibleArea;
			this.visibilityState = CalculateVisibilityState();
		}

		public void Dispose() {
			IsValid = false;
			foreach (var t in textLines)
				t.Dispose();
			textLines = null;
			drawingVisual = null;
		}
	}
}
