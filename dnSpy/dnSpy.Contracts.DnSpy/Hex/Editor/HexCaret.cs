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
using dnSpy.Contracts.Hex.Formatting;

namespace dnSpy.Contracts.Hex.Editor {
	/// <summary>
	/// Hex caret
	/// </summary>
	public abstract class HexCaret {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexCaret() { }

		/// <summary>
		/// true if the caret in the values column is present
		/// </summary>
		public abstract bool IsValuesCaretPresent { get; }

		/// <summary>
		/// true if the caret in the ASCII column is present
		/// </summary>
		public abstract bool IsAsciiCaretPresent { get; }

		/// <summary>
		/// Gets the position of the top edge of the caret in the values column
		/// </summary>
		public abstract double ValuesTop { get; }

		/// <summary>
		/// Gets the position of the bottom edge of the caret in the values column
		/// </summary>
		public abstract double ValuesBottom { get; }

		/// <summary>
		/// Gets the position of the left edge of the caret in the values column
		/// </summary>
		public abstract double ValuesLeft { get; }

		/// <summary>
		/// Gets the position of the right edge of the caret in the values column
		/// </summary>
		public abstract double ValuesRight { get; }

		/// <summary>
		/// Gets the width of the caret in the values column
		/// </summary>
		public abstract double ValuesWidth { get; }

		/// <summary>
		/// Gets the height of the caret in the values column
		/// </summary>
		public abstract double ValuesHeight { get; }

		/// <summary>
		/// Gets the position of the top edge of the caret in the ASCII column
		/// </summary>
		public abstract double AsciiTop { get; }

		/// <summary>
		/// Gets the position of the bottom edge of the caret in the ASCII column
		/// </summary>
		public abstract double AsciiBottom { get; }

		/// <summary>
		/// Gets the position of the left edge of the caret in the ASCII column
		/// </summary>
		public abstract double AsciiLeft { get; }

		/// <summary>
		/// Gets the position of the right edge of the caret in the ASCII column
		/// </summary>
		public abstract double AsciiRight { get; }

		/// <summary>
		/// Gets the width of the caret in the ASCII column
		/// </summary>
		public abstract double AsciiWidth { get; }

		/// <summary>
		/// Gets the height of the caret in the ASCII column
		/// </summary>
		public abstract double AsciiHeight { get; }

		/// <summary>
		/// true if the caret is hidden, false if it's visible
		/// </summary>
		public abstract bool IsHidden { get; set; }

		/// <summary>
		/// Gets the containing hex view line
		/// </summary>
		public abstract HexViewLine ContainingHexViewLine { get; }

		/// <summary>
		/// true if it's overwrite mode
		/// </summary>
		public abstract bool OverwriteMode { get; }

		/// <summary>
		/// Gets the position
		/// </summary>
		public abstract HexCaretPosition Position { get; }

		/// <summary>
		/// Raised after the position is changed by calling one of the MoveTo methods
		/// </summary>
		public abstract event EventHandler<HexCaretPositionChangedEventArgs> PositionChanged;

		/// <summary>
		/// Brings the caret into view
		/// </summary>
		public abstract void EnsureVisible();

		/// <summary>
		/// Toggles the active column
		/// </summary>
		/// <returns></returns>
		public abstract HexCaretPosition ToggleActiveColumn();

		/// <summary>
		/// Moves the caret to a new position
		/// </summary>
		/// <param name="column">Column</param>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public HexCaretPosition MoveTo(HexColumnType column, HexBufferPoint position) =>
			MoveTo(column, position, true);

		/// <summary>
		/// Moves the caret to a new position
		/// </summary>
		/// <param name="column">Column</param>
		/// <param name="position">Position</param>
		/// <param name="captureHorizontalPosition">true to capture the horizontal position</param>
		/// <returns></returns>
		public abstract HexCaretPosition MoveTo(HexColumnType column, HexBufferPoint position, bool captureHorizontalPosition);

		/// <summary>
		/// Moves the caret to a new position
		/// </summary>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public HexCaretPosition MoveTo(HexCellPosition position) =>
			MoveTo(position, true);

		/// <summary>
		/// Moves the caret to a new position
		/// </summary>
		/// <param name="position">Position</param>
		/// <param name="captureHorizontalPosition">true to capture the horizontal position</param>
		/// <returns></returns>
		public abstract HexCaretPosition MoveTo(HexCellPosition position, bool captureHorizontalPosition);

		/// <summary>
		/// Moves the caret to a new position
		/// </summary>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public HexCaretPosition MoveTo(HexColumnPosition position) =>
			MoveTo(position, true);

		/// <summary>
		/// Moves the caret to a new position
		/// </summary>
		/// <param name="position">Position</param>
		/// <param name="captureHorizontalPosition">true to capture the horizontal position</param>
		/// <returns></returns>
		public abstract HexCaretPosition MoveTo(HexColumnPosition position, bool captureHorizontalPosition);

		/// <summary>
		/// Moves the caret to a new position
		/// </summary>
		/// <param name="hexLine">Line</param>
		/// <returns></returns>
		public abstract HexCaretPosition MoveTo(HexViewLine hexLine);

		/// <summary>
		/// Moves the caret to a new position
		/// </summary>
		/// <param name="hexLine">Line</param>
		/// <param name="xCoordinate">X coordinate</param>
		/// <returns></returns>
		public abstract HexCaretPosition MoveTo(HexViewLine hexLine, double xCoordinate);

		/// <summary>
		/// Moves the caret to a new position
		/// </summary>
		/// <param name="hexLine">Line</param>
		/// <param name="xCoordinate">X coordinate</param>
		/// <param name="captureHorizontalPosition">true to capture the horizontal position</param>
		/// <returns></returns>
		public abstract HexCaretPosition MoveTo(HexViewLine hexLine, double xCoordinate, bool captureHorizontalPosition);

		/// <summary>
		/// Moves the caret to the previous position
		/// </summary>
		/// <returns></returns>
		public abstract HexCaretPosition MoveToPreviousCaretPosition();

		/// <summary>
		/// Moves the caret to the next position
		/// </summary>
		/// <returns></returns>
		public abstract HexCaretPosition MoveToNextCaretPosition();

		/// <summary>
		/// Moves the caret to the preferred x and y coordinates
		/// </summary>
		/// <returns></returns>
		public abstract HexCaretPosition MoveToPreferredCoordinates();
	}

	/// <summary>
	/// Caret position changed event args
	/// </summary>
	public sealed class HexCaretPositionChangedEventArgs : EventArgs {
		/// <summary>
		/// Gets the hex view
		/// </summary>
		public HexView HexView { get; }

		/// <summary>
		/// Gets the old position
		/// </summary>
		public HexCaretPosition OldPosition { get; }

		/// <summary>
		/// Gets the new position
		/// </summary>
		public HexCaretPosition NewPosition { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="hexView">Hex view</param>
		/// <param name="oldPosition">Old position</param>
		/// <param name="newPosition">New position</param>
		public HexCaretPositionChangedEventArgs(HexView hexView, HexCaretPosition oldPosition, HexCaretPosition newPosition) {
			if (hexView == null)
				throw new ArgumentNullException(nameof(hexView));
			if (oldPosition.IsDefault)
				throw new ArgumentException();
			if (newPosition.IsDefault)
				throw new ArgumentException();
			HexView = hexView;
			OldPosition = oldPosition;
			NewPosition = newPosition;
		}
	}
}
