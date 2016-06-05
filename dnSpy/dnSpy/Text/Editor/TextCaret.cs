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
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Contracts.Text.Formatting;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Editing;

namespace dnSpy.Text.Editor {
	sealed class TextCaret : ITextCaret {
		public double Left => caret.CaretRect.Left;
		public double Right => caret.CaretRect.Right;
		public double Top => caret.CaretRect.Top;
		public double Bottom => caret.CaretRect.Bottom;
		public double Width => caret.CaretRect.Width;
		public double Height => caret.CaretRect.Height;
		public bool InVirtualSpace => caret.IsInVirtualSpace;
		public bool OverwriteMode => caret.OverstrikeMode;
		public ITextViewLine ContainingTextViewLine => GetLine(Position.BufferPosition, Affinity);
		PositionAffinity Affinity { get; set; }

		public bool IsHidden {
			get { return isHidden; }
			set {
				if (isHidden == value)
					return;
				isHidden = value;
				if (isHidden)
					caret.Hide();
				else
					caret.Show();
			}
		}
		bool isHidden;

		VirtualSnapshotPoint BufferPosition {
			get {
				int virtualSpaces = Utils.GetVirtualSpaces(dnSpyTextEditor, caret.Offset, caret.VisualColumn);
				return new VirtualSnapshotPoint(new SnapshotPoint(textView.TextSnapshot, caret.Offset), virtualSpaces);
			}
		}

		public event EventHandler<CaretPositionChangedEventArgs> PositionChanged;
		public CaretPosition Position => cachedCaretPosition;
		CaretPosition cachedCaretPosition;

		readonly ITextView textView;
		readonly DnSpyTextEditor dnSpyTextEditor;
		readonly Caret caret;
		double preferredXCoordinate;

		public TextCaret(ITextView textView, DnSpyTextEditor dnSpyTextEditor) {
			this.textView = textView;
			this.dnSpyTextEditor = dnSpyTextEditor;
			this.preferredXCoordinate = 0;
			this.caret = dnSpyTextEditor.TextArea.Caret;
			Affinity = PositionAffinity.Successor;
			caret.Position = new TextViewPosition(1, 1, 0);
			caret.PositionChanged += AvalonEdit_Caret_PositionChanged;
			textView.TextBuffer.ChangedHighPriority += TextBuffer_ChangedHighPriority;
			textView.TextBuffer.ContentTypeChanged += TextBuffer_ContentTypeChanged;

			// Update cached pos
			OnCaretPositionChanged();
		}

		void TextBuffer_ContentTypeChanged(object sender, ContentTypeChangedEventArgs e) {
			// The value is cached, make sure it uses the latest snapshot
			OnCaretPositionChanged();
		}

		void TextBuffer_ChangedHighPriority(object sender, TextContentChangedEventArgs e) {
			// The value is cached, make sure it uses the latest snapshot
			OnCaretPositionChanged();
		}

		// Compares two caret positions, ignoring the snapshot
		static bool CaretEquals(CaretPosition a, CaretPosition b) =>
			a.Affinity == b.Affinity &&
			a.VirtualSpaces == b.VirtualSpaces &&
			a.BufferPosition.Position == b.BufferPosition.Position;

		void AvalonEdit_Caret_PositionChanged(object sender, EventArgs e) => OnCaretPositionChanged();
		void OnCaretPositionChanged() {
			var oldPos = cachedCaretPosition;
			cachedCaretPosition = new CaretPosition(BufferPosition, new MappingPoint(), Affinity);
			if (!CaretEquals(oldPos, cachedCaretPosition))
				PositionChanged?.Invoke(this, new CaretPositionChangedEventArgs(textView, oldPos, Position));
		}
		internal void OnVisualLinesCreated() => OnCaretPositionChanged();

		public void EnsureVisible() => caret.BringCaretToView();

		public CaretPosition MoveTo(ITextViewLine textLine) =>
			MoveTo(textLine, preferredXCoordinate, true);
		public CaretPosition MoveTo(ITextViewLine textLine, double xCoordinate) =>
			MoveTo(textLine, xCoordinate, true);
		public CaretPosition MoveTo(ITextViewLine textLine, double xCoordinate, bool captureHorizontalPosition) {
			if (textLine == null)
				throw new ArgumentNullException(nameof(textLine));
			if (captureHorizontalPosition) {
				//TODO: Update preferredXCoordinate with new value
			}
			throw new NotSupportedException();//TODO:
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
			if (captureHorizontalPosition) {
				//TODO: Update preferredXCoordinate with new value
			}
			var line = dnSpyTextEditor.TextArea.TextView.Document.GetLineByOffset(bufferPosition.Position.Position);
			Affinity = caretAffinity;
			caret.Position = new TextViewPosition(line.LineNumber, bufferPosition.Position.Position - (line.Offset - 1), line.Length + bufferPosition.VirtualSpaces >= 0 ? line.Length + bufferPosition.VirtualSpaces : int.MaxValue);
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
			var l = dnSpyTextEditor.TextArea.TextView.Document.GetLineByNumber(line + 1);
			if (column >= l.Length)
				column = l.Length;
			return MoveTo(new SnapshotPoint(textView.TextSnapshot, l.Offset + column), caretAffinity, captureHorizontalPosition);
		}

		public CaretPosition MoveToNextCaretPosition() {
			if (textView.Options.GetOptionValue(DefaultTextViewOptions.UseVirtualSpaceId)) {
				bool useVirtSpaces;
				if (Position.VirtualSpaces > 0)
					useVirtSpaces = true;
				else {
					var docLine = dnSpyTextEditor.TextArea.TextView.Document.GetLineByNumber(caret.Line);
					useVirtSpaces = Position.BufferPosition >= docLine.EndOffset;
				}
				if (useVirtSpaces) {
					if (Position.VirtualSpaces != int.MaxValue)
						return MoveTo(new VirtualSnapshotPoint(Position.BufferPosition, Position.VirtualSpaces + 1));
					return Position;
				}
			}
			if (Position.BufferPosition.Position == Position.BufferPosition.Snapshot.Length)
				return Position;

			var line = ContainingTextViewLine;
			var span = line.GetTextElementSpan(Position.BufferPosition);
			return MoveTo(new SnapshotPoint(textView.TextSnapshot, span.End));
		}

		public CaretPosition MoveToPreviousCaretPosition() {
			if (Position.VirtualSpaces > 0)
				return MoveTo(new VirtualSnapshotPoint(Position.BufferPosition, Position.VirtualSpaces - 1));
			if (Position.BufferPosition.Position == 0)
				return Position;

			var currentLine = ContainingTextViewLine;
			var span = currentLine.GetTextElementSpan(Position.BufferPosition);
			var newPos = span.Start;
			if (newPos.Position != 0 && Position.BufferPosition.Position != Position.BufferPosition.Snapshot.Length) {
				newPos -= 1;
				var line = textView.GetTextViewLineContainingBufferPosition(newPos);
				if (line.IsLastTextViewLineForSnapshotLine && newPos > line.End)
					newPos = line.End;
				newPos = line.GetTextElementSpan(newPos).Start;
			}
			if (textView.Options.GetOptionValue(DefaultTextViewOptions.UseVirtualSpaceId)) {
				var line = textView.GetTextViewLineContainingBufferPosition(newPos);
				if (line != currentLine)
					newPos = currentLine.Start;
			}
			return MoveTo(newPos);
		}

		public CaretPosition MoveToPreferredCoordinates() {
			return Position;//TODO:
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
	}
}
