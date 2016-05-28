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
using dnSpy.Contracts.Text.Formatting;

namespace dnSpy.Contracts.Text.Editor {
	/// <summary>
	/// Text caret
	/// </summary>
	public interface ITextCaret {
		/// <summary>
		/// Raised when the position has changed
		/// </summary>
		event EventHandler<CaretPositionChangedEventArgs> PositionChanged;

		/// <summary>
		/// Left position of caret
		/// </summary>
		double Left { get; }

		/// <summary>
		/// Right position of caret
		/// </summary>
		double Right { get; }

		/// <summary>
		/// Top position of caret
		/// </summary>
		double Top { get; }

		/// <summary>
		/// Bottom position of caret
		/// </summary>
		double Bottom { get; }

		/// <summary>
		/// Width of caret
		/// </summary>
		double Width { get; }

		/// <summary>
		/// Height of caret
		/// </summary>
		double Height { get; }

		/// <summary>
		/// true if the caret is in the virtual space (after the end of the line)
		/// </summary>
		bool InVirtualSpace { get; }

		/// <summary>
		/// Hides or shows the caret
		/// </summary>
		bool IsHidden { get; set; }

		/// <summary>
		/// true if it's in overwrite mode
		/// </summary>
		bool OverwriteMode { get; }

		/// <summary>
		/// Gets the position
		/// </summary>
		CaretPosition Position { get; }

		/// <summary>
		/// Containing line if the if the line is visible in the view
		/// </summary>
		ITextViewLine ContainingTextViewLine { get; }

		/// <summary>
		/// Scrolls the view until the caret becomes visible
		/// </summary>
		void EnsureVisible();

		/// <summary>
		/// Moves the caret
		/// </summary>
		/// <param name="textLine">Line</param>
		/// <returns></returns>
		CaretPosition MoveTo(ITextViewLine textLine);

		/// <summary>
		/// Moves the caret
		/// </summary>
		/// <param name="textLine">Line</param>
		/// <param name="xCoordinate">X coordinate of the caret</param>
		/// <returns></returns>
		CaretPosition MoveTo(ITextViewLine textLine, double xCoordinate);

		/// <summary>
		/// Moves the caret
		/// </summary>
		/// <param name="textLine">Line</param>
		/// <param name="xCoordinate">X coordinate of the caret</param>
		/// <param name="captureHorizontalPosition">true to capture the caret's horizontal position for subsequent
		/// moves up or down, false to retain the previously captured position</param>
		/// <returns></returns>
		CaretPosition MoveTo(ITextViewLine textLine, double xCoordinate, bool captureHorizontalPosition);

		/// <summary>
		/// Moves the caret
		/// </summary>
		/// <param name="bufferPosition">Buffer position</param>
		/// <returns></returns>
		CaretPosition MoveTo(SnapshotPoint bufferPosition);

		/// <summary>
		/// Moves the caret
		/// </summary>
		/// <param name="bufferPosition">Buffer position</param>
		/// <param name="caretAffinity">Caret affinity</param>
		/// <returns></returns>
		CaretPosition MoveTo(SnapshotPoint bufferPosition, PositionAffinity caretAffinity);

		/// <summary>
		/// Moves the caret
		/// </summary>
		/// <param name="bufferPosition">Buffer position</param>
		/// <param name="caretAffinity">Caret affinity</param>
		/// <param name="captureHorizontalPosition">true to capture the caret's horizontal position for subsequent
		/// moves up or down, false to retain the previously captured position</param>
		/// <returns></returns>
		CaretPosition MoveTo(SnapshotPoint bufferPosition, PositionAffinity caretAffinity, bool captureHorizontalPosition);

		/// <summary>
		/// Moves the caret
		/// </summary>
		/// <param name="bufferPosition">Buffer position</param>
		/// <returns></returns>
		CaretPosition MoveTo(VirtualSnapshotPoint bufferPosition);

		/// <summary>
		/// Moves the caret
		/// </summary>
		/// <param name="bufferPosition">Buffer position</param>
		/// <param name="caretAffinity">Caret affinity</param>
		/// <returns></returns>
		CaretPosition MoveTo(VirtualSnapshotPoint bufferPosition, PositionAffinity caretAffinity);

		/// <summary>
		/// Moves the caret
		/// </summary>
		/// <param name="bufferPosition">Buffer position</param>
		/// <param name="caretAffinity">Caret affinity</param>
		/// <param name="captureHorizontalPosition">true to capture the caret's horizontal position for subsequent
		/// moves up or down, false to retain the previously captured position</param>
		/// <returns></returns>
		CaretPosition MoveTo(VirtualSnapshotPoint bufferPosition, PositionAffinity caretAffinity, bool captureHorizontalPosition);

		/// <summary>
		/// Moves the caret
		/// </summary>
		/// <param name="line">Line number, 0-based</param>
		/// <returns></returns>
		CaretPosition MoveTo(int line);

		/// <summary>
		/// Moves the caret
		/// </summary>
		/// <param name="line">Line, 0-based</param>
		/// <param name="column">Column, 0-based</param>
		/// <returns></returns>
		CaretPosition MoveTo(int line, int column);

		/// <summary>
		/// Moves the caret
		/// </summary>
		/// <param name="line">Line, 0-based</param>
		/// <param name="column">Column, 0-based</param>
		/// <param name="caretAffinity">Caret affinity</param>
		/// <returns></returns>
		CaretPosition MoveTo(int line, int column, PositionAffinity caretAffinity);

		/// <summary>
		/// Moves the caret
		/// </summary>
		/// <param name="line">Line, 0-based</param>
		/// <param name="column">Column, 0-based</param>
		/// <param name="caretAffinity">Caret affinity</param>
		/// <param name="captureHorizontalPosition">true to capture the caret's horizontal position for subsequent
		/// moves up or down, false to retain the previously captured position</param>
		/// <returns></returns>
		CaretPosition MoveTo(int line, int column, PositionAffinity caretAffinity, bool captureHorizontalPosition);

		/// <summary>
		/// Moves the caret to the next valid <see cref="CaretPosition"/>. This method handles UTF-16 surrogate pairs and combining character sequences.
		/// </summary>
		/// <returns></returns>
		CaretPosition MoveToNextCaretPosition();

		/// <summary>
		/// Moves the caret to the previous valid <see cref="CaretPosition"/>. This method handles UTF-16 surrogate pairs and combining character sequences.
		/// </summary>
		/// <returns></returns>
		CaretPosition MoveToPreviousCaretPosition();

		/// <summary>
		/// Moves the caret to the preferred x and y coordinates
		/// </summary>
		/// <returns></returns>
		CaretPosition MoveToPreferredCoordinates();
	}
}
