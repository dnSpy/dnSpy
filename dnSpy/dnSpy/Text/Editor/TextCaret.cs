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
using System.Linq;
using System.Windows.Threading;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Contracts.Text.Formatting;

namespace dnSpy.Text.Editor {
	sealed class TextCaret : ITextCaret {
		public double Left => textCaretLayer.Left;
		public double Right => textCaretLayer.Right;
		public double Top => textCaretLayer.Top;
		public double Bottom => textCaretLayer.Bottom;
		public double Width => textCaretLayer.Width;
		public double Height => textCaretLayer.Height;
		public bool InVirtualSpace => Position.VirtualSpaces > 0;
		public bool OverwriteMode => textCaretLayer.OverwriteMode;
		public ITextViewLine ContainingTextViewLine => GetLine(Position.BufferPosition, Affinity);
		PositionAffinity Affinity { get; set; }

		public bool IsHidden {
			get { return textCaretLayer.IsHidden; }
			set { textCaretLayer.IsHidden = value; }
		}

		public event EventHandler<CaretPositionChangedEventArgs> PositionChanged;
		public CaretPosition Position => currentPosition;
		CaretPosition currentPosition;

		readonly IWpfTextView textView;
		readonly ISmartIndentationService smartIndentationService;
		readonly TextCaretLayer textCaretLayer;
		double preferredXCoordinate;

		public TextCaret(IWpfTextView textView, IAdornmentLayer caretLayer, ISmartIndentationService smartIndentationService, IClassificationFormatMap classificationFormatMap) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			if (caretLayer == null)
				throw new ArgumentNullException(nameof(caretLayer));
			if (smartIndentationService == null)
				throw new ArgumentNullException(nameof(smartIndentationService));
			this.textView = textView;
			this.smartIndentationService = smartIndentationService;
			this.preferredXCoordinate = 0;
			this.__preferredYCoordinate = 0;
			Affinity = PositionAffinity.Successor;
			var bufferPos = new VirtualSnapshotPoint(textView.TextSnapshot, 0);
			this.currentPosition = new CaretPosition(bufferPos, new MappingPoint(bufferPos.Position, PointTrackingMode.Negative), Affinity);
			textView.TextBuffer.ChangedHighPriority += TextBuffer_ChangedHighPriority;
			textView.TextBuffer.ContentTypeChanged += TextBuffer_ContentTypeChanged;
			textView.Options.OptionChanged += Options_OptionChanged;
			this.textCaretLayer = new TextCaretLayer(this, caretLayer, classificationFormatMap);
		}

		void Options_OptionChanged(object sender, EditorOptionChangedEventArgs e) {
			if (e.OptionId == DefaultTextViewOptions.UseVirtualSpaceId.Name) {
				if (Position.VirtualSpaces > 0 && textView.Selection.Mode != TextSelectionMode.Box && !textView.Options.GetOptionValue(DefaultTextViewOptions.UseVirtualSpaceId))
					MoveTo(Position.BufferPosition);
			}
			else if (e.OptionId == DefaultTextViewOptions.OverwriteModeId.Name)
				textCaretLayer.OverwriteMode = textView.Options.GetOptionValue(DefaultTextViewOptions.OverwriteModeId);
		}

		void TextBuffer_ContentTypeChanged(object sender, ContentTypeChangedEventArgs e) {
			// The value is cached, make sure it uses the latest snapshot
			OnCaretPositionChanged();
		}

		void TextBuffer_ChangedHighPriority(object sender, TextContentChangedEventArgs e) {
			// The value is cached, make sure it uses the latest snapshot
			OnCaretPositionChanged();
			if (textView.Options.GetOptionValue(DefaultTextViewOptions.AutoScrollId)) {
				// Delay this so we don't cause extra events to be raised inside the Changed event
				textView.VisualElement.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(AutoScrollCaret));
			}
		}

		void AutoScrollCaret() {
			if (textView.IsClosed)
				return;
			if (!textView.Options.GetOptionValue(DefaultTextViewOptions.AutoScrollId))
				return;
			var line = ContainingTextViewLine;
			if (line.IsLastDocumentLine()) {
				MoveTo(line.End);
				EnsureVisible();
			}
		}

		void OnCaretPositionChanged() => SetPosition(currentPosition.VirtualBufferPosition.TranslateTo(textView.TextSnapshot, Affinity == PositionAffinity.Predecessor ? PointTrackingMode.Negative : PointTrackingMode.Positive));
		void SetPosition(VirtualSnapshotPoint bufferPosition) {
			var oldPos = currentPosition;
			var bufPos = bufferPosition;
			currentPosition = new CaretPosition(bufPos, new MappingPoint(bufPos.Position, PointTrackingMode.Negative), Affinity);
			if (!CaretEquals(oldPos, currentPosition))
				PositionChanged?.Invoke(this, new CaretPositionChangedEventArgs(textView, oldPos, Position));
		}

		// Compares two caret positions, ignoring the snapshot
		static bool CaretEquals(CaretPosition a, CaretPosition b) =>
			a.Affinity == b.Affinity &&
			a.VirtualSpaces == b.VirtualSpaces &&
			a.BufferPosition.Position == b.BufferPosition.Position;

		public void EnsureVisible() {
			var line = this.ContainingTextViewLine;
			if (line.VisibilityState != VisibilityState.FullyVisible) {
				ViewRelativePosition relativeTo;
				var firstVisibleLine = textView.TextViewLines?.FirstVisibleLine;
				if (firstVisibleLine == null || !firstVisibleLine.IsVisible())
					relativeTo = ViewRelativePosition.Top;
				else if (line.Start.Position <= firstVisibleLine.Start.Position)
					relativeTo = ViewRelativePosition.Top;
				else
					relativeTo = ViewRelativePosition.Bottom;
				textView.DisplayTextLineContainingBufferPosition(line.Start, 0, relativeTo);
			}

			double left = textCaretLayer.Left;
			double right = textCaretLayer.Right;

			const double EXTRA_SCROLL_WIDTH = 200;
			double availWidth = Math.Max(0, textView.ViewportWidth - textCaretLayer.Width);
			double extraScroll;
			if (availWidth >= EXTRA_SCROLL_WIDTH)
				extraScroll = EXTRA_SCROLL_WIDTH;
			else
				extraScroll = availWidth / 2;
			if (left < textView.ViewportLeft)
				textView.ViewportLeft = left - extraScroll;
			else if (right > textView.ViewportRight)
				textView.ViewportLeft = right + extraScroll - textView.ViewportWidth;
		}

		bool CanAutoIndent(ITextViewLine line) {
			if (line.Start != line.End)
				return false;
			if (textView.Options.GetOptionValue(DefaultTextViewOptions.UseVirtualSpaceId))
				return false;
			if (textView.Selection.Mode != TextSelectionMode.Stream)
				return false;

			return true;
		}

		VirtualSnapshotPoint FilterColumn(VirtualSnapshotPoint pos) {
			if (!pos.IsInVirtualSpace)
				return pos;
			if (textView.Options.GetOptionValue(DefaultTextViewOptions.UseVirtualSpaceId))
				return pos;
			if (textView.Selection.Mode != TextSelectionMode.Stream)
				return pos;
			return new VirtualSnapshotPoint(pos.Position);
		}

		public CaretPosition MoveTo(ITextViewLine textLine) =>
			MoveTo(textLine, preferredXCoordinate, false, true, true);
		public CaretPosition MoveTo(ITextViewLine textLine, double xCoordinate) =>
			MoveTo(textLine, xCoordinate, true, true, true);
		public CaretPosition MoveTo(ITextViewLine textLine, double xCoordinate, bool captureHorizontalPosition) =>
			MoveTo(textLine, xCoordinate, captureHorizontalPosition, true, true);
		CaretPosition MoveTo(ITextViewLine textLine, double xCoordinate, bool captureHorizontalPosition, bool captureVerticalPosition, bool canAutoIndent) {
			if (textLine == null)
				throw new ArgumentNullException(nameof(textLine));

			bool filterPos = true;
			// Don't auto indent if it's at column 0
			if (canAutoIndent && CanAutoIndent(textLine) && xCoordinate > textLine.TextRight) {
				var wpfView = textView as IWpfTextView;
				if (wpfView != null) {
					int indentation = IndentHelper.GetDesiredIndentation(textView, smartIndentationService, textLine.Start.GetContainingLine()) ?? 0;
					var textBounds = textLine.GetExtendedCharacterBounds(new VirtualSnapshotPoint(textLine.Start, indentation));
					xCoordinate = textBounds.Leading;
					filterPos = false;
				}
			}

			var bufferPosition = textLine.GetInsertionBufferPositionFromXCoordinate(xCoordinate);
			Affinity = textLine.IsLastTextViewLineForSnapshotLine || bufferPosition.Position != textLine.End ? PositionAffinity.Successor : PositionAffinity.Predecessor;
			if (filterPos)
				bufferPosition = FilterColumn(bufferPosition);
			SetPosition(bufferPosition);
			if (captureHorizontalPosition)
				preferredXCoordinate = Left;
			if (captureVerticalPosition)
				SavePreferredYCoordinate();
			return Position;
		}

		public CaretPosition MoveTo(SnapshotPoint bufferPosition) =>
			MoveTo(new VirtualSnapshotPoint(bufferPosition));
		public CaretPosition MoveTo(SnapshotPoint bufferPosition, PositionAffinity caretAffinity) =>
			MoveTo(new VirtualSnapshotPoint(bufferPosition), caretAffinity);
		public CaretPosition MoveTo(SnapshotPoint bufferPosition, PositionAffinity caretAffinity, bool captureHorizontalPosition) =>
			MoveTo(new VirtualSnapshotPoint(bufferPosition), caretAffinity, captureHorizontalPosition);

		public CaretPosition MoveTo(VirtualSnapshotPoint bufferPosition) =>
			MoveTo(bufferPosition, PositionAffinity.Successor);
		public CaretPosition MoveTo(VirtualSnapshotPoint bufferPosition, PositionAffinity caretAffinity) =>
			MoveTo(bufferPosition, caretAffinity, true);
		public CaretPosition MoveTo(VirtualSnapshotPoint bufferPosition, PositionAffinity caretAffinity, bool captureHorizontalPosition) {
			if (bufferPosition.Position.Snapshot != textView.TextSnapshot)
				throw new ArgumentException();

			Affinity = caretAffinity;
			// Don't call FilterColumn() or pressing END on an empty line won't indent it to a virtual column
			//bufferPosition = FilterColumn(bufferPosition);
			SetPosition(bufferPosition);
			if (captureHorizontalPosition)
				preferredXCoordinate = Left;
			SavePreferredYCoordinate();
			return Position;
		}

		public CaretPosition MoveTo(int line) => MoveTo(line, 0);
		public CaretPosition MoveTo(int line, int column) =>
			MoveTo(line, column, PositionAffinity.Successor);
		public CaretPosition MoveTo(int line, int column, PositionAffinity caretAffinity) =>
			MoveTo(line, column, caretAffinity, true);
		public CaretPosition MoveTo(int line, int column, PositionAffinity caretAffinity, bool captureHorizontalPosition) {
			if (line < 0)
				throw new ArgumentOutOfRangeException(nameof(line));
			if (column < 0)
				throw new ArgumentOutOfRangeException(nameof(column));
			if (line >= textView.TextSnapshot.LineCount)
				line = textView.TextSnapshot.LineCount - 1;
			var snapshotLine = textView.TextSnapshot.GetLineFromLineNumber(line);
			if (column >= snapshotLine.Length)
				column = snapshotLine.Length;
			return MoveTo(snapshotLine.Start + column, caretAffinity, captureHorizontalPosition);
		}

		public CaretPosition MoveToNextCaretPosition() {
			if (textView.Options.GetOptionValue(DefaultTextViewOptions.UseVirtualSpaceId)) {
				bool useVirtSpaces;
				if (Position.VirtualSpaces > 0)
					useVirtSpaces = true;
				else {
					var snapshotLine = Position.BufferPosition.GetContainingLine();
					useVirtSpaces = Position.BufferPosition >= snapshotLine.End;
				}
				if (useVirtSpaces) {
					if (Position.VirtualSpaces != int.MaxValue)
						return MoveTo(new VirtualSnapshotPoint(Position.BufferPosition, Position.VirtualSpaces + 1));
					return Position;
				}
			}
			if (Position.BufferPosition.Position == Position.BufferPosition.Snapshot.Length)
				return Position;

			var line = textView.GetTextViewLineContainingBufferPosition(Position.BufferPosition);
			var span = line.GetTextElementSpan(Position.BufferPosition);
			return MoveTo(new SnapshotPoint(textView.TextSnapshot, span.End));
		}

		public CaretPosition MoveToPreviousCaretPosition() {
			if (Position.VirtualSpaces > 0 && textView.Options.GetOptionValue(DefaultTextViewOptions.UseVirtualSpaceId))
				return MoveTo(new VirtualSnapshotPoint(Position.BufferPosition, Position.VirtualSpaces - 1));
			if (Position.BufferPosition.Position == 0)
				return Position;

			var currentLine = textView.GetTextViewLineContainingBufferPosition(Position.BufferPosition);
			var span = currentLine.GetTextElementSpan(Position.BufferPosition);
			var newPos = span.Start;
			if (Position.VirtualSpaces == 0 && newPos.Position != 0) {
				newPos -= 1;
				var line = textView.GetTextViewLineContainingBufferPosition(newPos);
				if (line.IsLastTextViewLineForSnapshotLine && newPos > line.End)
					newPos = line.End;
				newPos = line.GetTextElementSpan(newPos).Start;
			}
			if (textView.Options.GetOptionValue(DefaultTextViewOptions.UseVirtualSpaceId)) {
				var line = textView.GetTextViewLineContainingBufferPosition(newPos);
				if (line.ExtentIncludingLineBreak != currentLine.ExtentIncludingLineBreak)
					newPos = currentLine.Start;
			}
			return MoveTo(newPos);
		}

		double PreferredYCoordinate {
			get { return Math.Min(__preferredYCoordinate, textView.ViewportHeight) + textView.ViewportTop; }
		}
		double __preferredYCoordinate;

		ITextViewLine GetVisibleCaretLine() {
			if (textView.TextViewLines == null)
				return null;
			var line = ContainingTextViewLine;
			if (line.IsVisible())
				return line;
			// Don't use FirstVisibleLine since it will return a hidden line if it fails to find a visible line
			return textView.TextViewLines.FirstOrDefault(a => a.IsVisible());
		}

		void SavePreferredYCoordinate() {
			var line = GetVisibleCaretLine();
			if (line != null)
				__preferredYCoordinate = (line.Top + line.Bottom) / 2 - textView.ViewportTop;
			else
				__preferredYCoordinate = 0;
		}

		public CaretPosition MoveToPreferredCoordinates() {
			var textLine = textView.GetVisibleTextViewLineContainingYCoordinate(PreferredYCoordinate);
			if (textLine == null)
				textLine = textView.GetVisibleTextViewLineContainingYCoordinate(textView.ViewportBottom - 0.01);
			if (textLine == null)
				textLine = textView.TextViewLines.LastVisibleLine;
			return MoveTo(textLine, preferredXCoordinate, false, false, true);
		}

		ITextViewLine GetLine(SnapshotPoint bufferPosition, PositionAffinity affinity) {
			var line = textView.GetTextViewLineContainingBufferPosition(bufferPosition);
			if (line == null)
				return null;
			if (affinity == PositionAffinity.Successor)
				return line;
			if (line.Start.Position == 0 || line.Start != bufferPosition)
				return line;
			if (bufferPosition.GetContainingLine().Start == bufferPosition)
				return line;
			return textView.GetTextViewLineContainingBufferPosition(bufferPosition - 1);
		}

		public void Dispose() {
			textView.TextBuffer.ChangedHighPriority -= TextBuffer_ChangedHighPriority;
			textView.TextBuffer.ContentTypeChanged -= TextBuffer_ContentTypeChanged;
			textView.Options.OptionChanged -= Options_OptionChanged;
			textCaretLayer.Dispose();
		}
	}
}
