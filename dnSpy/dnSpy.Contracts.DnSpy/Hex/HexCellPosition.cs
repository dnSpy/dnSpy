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

namespace dnSpy.Contracts.Hex {
	/// <summary>
	/// A position within a cell
	/// </summary>
	public readonly struct HexCellPosition : IEquatable<HexCellPosition> {
		/// <summary>
		/// true if this is a default instance that hasn't been initialized
		/// </summary>
		public bool IsDefault => BufferPosition.IsDefault;

		/// <summary>
		/// Gets the column
		/// </summary>
		public HexColumnType Column { get; }

		/// <summary>
		/// Gets the buffer position
		/// </summary>
		public HexBufferPoint BufferPosition { get; }

		/// <summary>
		/// Gets the position within the cell
		/// </summary>
		public int CellPosition { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="column">Column</param>
		/// <param name="bufferPosition">Buffer position</param>
		/// <param name="cellPosition">Position within the cell</param>
		public HexCellPosition(HexColumnType column, HexBufferPoint bufferPosition, int cellPosition) {
			if (column != HexColumnType.Values && column != HexColumnType.Ascii)
				throw new ArgumentOutOfRangeException(nameof(column));
			if (bufferPosition.IsDefault)
				throw new ArgumentException();
			if (cellPosition < 0)
				throw new ArgumentOutOfRangeException(nameof(cellPosition));
			Column = column;
			BufferPosition = bufferPosition;
			CellPosition = cellPosition;
		}

		/// <summary>
		/// operator ==()
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static bool operator ==(HexCellPosition a, HexCellPosition b) => a.Equals(b);

		/// <summary>
		/// operator !=()
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static bool operator !=(HexCellPosition a, HexCellPosition b) => !a.Equals(b);

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="other">Other instance</param>
		/// <returns></returns>
		public bool Equals(HexCellPosition other) => Column == other.Column && BufferPosition == other.BufferPosition && CellPosition == other.CellPosition;

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="obj">Object</param>
		/// <returns></returns>
		public override bool Equals(object obj) => obj is HexCellPosition && Equals((HexCellPosition)obj);

		/// <summary>
		/// GetHashCode()
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() => (int)Column ^ BufferPosition.GetHashCode() ^ CellPosition.GetHashCode();

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() => "[" + Column.ToString() + "," + BufferPosition.ToString() + "," + CellPosition.ToString() + "]";
	}
}
