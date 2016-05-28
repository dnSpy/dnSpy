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
using dnSpy.Text.Formatting;
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
		public ITextViewLine ContainingTextViewLine => new WpfTextViewLine();//TODO:
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
				int virtualSpaces = 0;//TODO: Calculate VirtualSpaces
				return new VirtualSnapshotPoint(new SnapshotPoint(textView.TextSnapshot, caret.Offset), virtualSpaces);
			}
		}

		public event EventHandler<CaretPositionChangedEventArgs> PositionChanged;
		public CaretPosition Position => cachedCaretPosition;
		CaretPosition cachedCaretPosition;

		readonly ITextView textView;
		readonly DnSpyTextEditor dnSpyTextEditor;
		readonly Caret caret;

		public TextCaret(ITextView textView, DnSpyTextEditor dnSpyTextEditor) {
			this.textView = textView;
			this.dnSpyTextEditor = dnSpyTextEditor;
			this.caret = dnSpyTextEditor.TextArea.Caret;
			Affinity = PositionAffinity.Successor;
			caret.Position = new TextViewPosition(1, 1, 0);
			caret.PositionChanged += AvalonEdit_Caret_PositionChanged;

			// Update cached pos
			OnCaretPositionChanged();
		}

		void AvalonEdit_Caret_PositionChanged(object sender, EventArgs e) => OnCaretPositionChanged();
		void OnCaretPositionChanged() {
			var oldPos = cachedCaretPosition;
			cachedCaretPosition = new CaretPosition(BufferPosition, new MappingPoint(), Affinity);
			PositionChanged?.Invoke(this, new CaretPositionChangedEventArgs(textView, oldPos, Position));
		}

		public void EnsureVisible() => caret.BringCaretToView();

		public CaretPosition MoveTo(ITextViewLine textLine) {
			double preferredXCoordinate = 0;//TODO: Use captured horizontal position
			return MoveTo(textLine, preferredXCoordinate);
		}
		public CaretPosition MoveTo(ITextViewLine textLine, double xCoordinate) =>
			MoveTo(textLine, xCoordinate, true);
		public CaretPosition MoveTo(ITextViewLine textLine, double xCoordinate, bool captureHorizontalPosition) {
			throw new NotSupportedException();
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
			//TODO: Use captureHorizontalPosition
			var line = dnSpyTextEditor.TextArea.TextView.Document.GetLineByOffset(bufferPosition.Position.Position);
			Affinity = caretAffinity;
			caret.Position = new TextViewPosition(line.LineNumber, bufferPosition.Position.Position - (line.Offset - 1));
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
			if (cachedCaretPosition.BufferPosition.Position == textView.TextSnapshot.Length)
				return Position;
			//TODO: Handle UTF-16 surrogate pairs and combining character sequences
			return MoveTo(new SnapshotPoint(textView.TextSnapshot, cachedCaretPosition.BufferPosition.Position + 1));
		}

		public CaretPosition MoveToPreviousCaretPosition() {
			if (cachedCaretPosition.BufferPosition.Position == 0)
				return Position;
			//TODO: Handle UTF-16 surrogate pairs and combining character sequences
			return MoveTo(new SnapshotPoint(textView.TextSnapshot, cachedCaretPosition.BufferPosition.Position - 1));
		}

		public CaretPosition MoveToPreferredCoordinates() {
			return Position;//TODO:
		}
	}
}
