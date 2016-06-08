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
		public double Left => textView.ViewportLeft + caret.CalculateCaretRectangle().Left;
		public double Right => textView.ViewportLeft + caret.CalculateCaretRectangle().Right;
		public double Top => textView.ViewportTop + caret.CalculateCaretRectangle().Top;
		public double Bottom => textView.ViewportTop + caret.CalculateCaretRectangle().Bottom;
		public double Width => caret.CalculateCaretRectangle().Width;
		public double Height => caret.CalculateCaretRectangle().Height;
		public bool InVirtualSpace => Position.VirtualSpaces > 0;
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
		readonly ISmartIndentationService smartIndentationService;
		readonly Caret caret;
		double preferredXCoordinate;

		public TextCaret(ITextView textView, DnSpyTextEditor dnSpyTextEditor, ISmartIndentationService smartIndentationService) {
			this.textView = textView;
			this.dnSpyTextEditor = dnSpyTextEditor;
			this.smartIndentationService = smartIndentationService;
			this.preferredXCoordinate = 0;
			this.__preferredYCoordinate = 0;
			this.caret = dnSpyTextEditor.TextArea.Caret;
			Affinity = PositionAffinity.Successor;
			caret.SetPosition(new TextViewPosition(1, 1, 0), true);
			caret.DesiredXPos = double.NaN;
			caret.PositionChanged += AvalonEdit_Caret_PositionChanged;
			textView.TextBuffer.ChangedHighPriority += TextBuffer_ChangedHighPriority;
			textView.TextBuffer.ContentTypeChanged += TextBuffer_ContentTypeChanged;
			textView.Options.OptionChanged += Options_OptionChanged;
			dnSpyTextEditor.TextArea.TextView.VisualLinesChanged += AvalonEdit_TextView_VisualLinesChanged;

			// Update cached pos
			OnCaretPositionChanged();
		}

		void Options_OptionChanged(object sender, EditorOptionChangedEventArgs e) {
			if (e.OptionId == DefaultTextViewOptions.UseVirtualSpaceId.Name) {
				if (Position.VirtualSpaces > 0 && textView.Selection.Mode != TextSelectionMode.Box && !textView.Options.GetOptionValue(DefaultTextViewOptions.UseVirtualSpaceId))
					MoveTo(Position.BufferPosition);
			}
		}

		void AvalonEdit_TextView_VisualLinesChanged(object sender, EventArgs e) {
			// Needed because VisualLengths could've changed, eg. when toggling show-whitespace option.
			caret.SetPosition(Utils.ToTextViewPosition(dnSpyTextEditor, cachedCaretPosition.VirtualBufferPosition, cachedCaretPosition.Affinity == PositionAffinity.Predecessor), invalidateVisualColumn: false);
			caret.DesiredXPos = double.NaN;
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
					xCoordinate = indentation * wpfView.FormattedLineSource.ColumnWidth;
					filterPos = false;
				}
			}

			var bufferPosition = textLine.GetInsertionBufferPositionFromXCoordinate(xCoordinate);
			Affinity = textLine.IsLastTextViewLineForSnapshotLine || bufferPosition.Position != textLine.End ? PositionAffinity.Successor : PositionAffinity.Predecessor;
			if (filterPos)
				bufferPosition = FilterColumn(bufferPosition);
			caret.SetPosition(Utils.ToTextViewPosition(dnSpyTextEditor, bufferPosition, Affinity == PositionAffinity.Predecessor), invalidateVisualColumn: false);
			caret.DesiredXPos = double.NaN;
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
			caret.SetPosition(Utils.ToTextViewPosition(dnSpyTextEditor, bufferPosition, Affinity == PositionAffinity.Predecessor), invalidateVisualColumn: false);
			caret.DesiredXPos = double.NaN;
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
			if (Position.VirtualSpaces == 0 && newPos.Position != 0 && Position.BufferPosition.Position != Position.BufferPosition.Snapshot.Length) {
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
			if (textView.TextViewLines.Count == 0)
				return null;
			var line = ContainingTextViewLine;
			if (line.IsVisible())
				return line;
			return textView.TextViewLines.FirstVisibleLine;
		}

		void SavePreferredYCoordinate() {
			var line = GetVisibleCaretLine();
			if (line != null)
				__preferredYCoordinate = line.Top + (line.Bottom - line.Top) / 2 - textView.ViewportTop;
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
	}
}
