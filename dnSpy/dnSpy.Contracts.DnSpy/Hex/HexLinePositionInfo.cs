/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
	/// Line position info
	/// </summary>
	public struct HexLinePositionInfo {
		/// <summary>
		/// Gets the type
		/// </summary>
		public HexLinePositionInfoType Type { get; }

		/// <summary>
		/// true if it's a position within the offset column
		/// </summary>
		public bool IsOffset => Type == HexLinePositionInfoType.Offset;

		/// <summary>
		/// true if it's a position within a value cell
		/// </summary>
		public bool IsValueCell => Type == HexLinePositionInfoType.ValueCell;

		/// <summary>
		/// true if it's a position within an ASCII cell
		/// </summary>
		public bool IsAsciiCell => Type == HexLinePositionInfoType.AsciiCell;

		/// <summary>
		/// true if it's a value cell separator position
		/// </summary>
		public bool IsValueCellSeparator => Type == HexLinePositionInfoType.ValueCellSeparator;

		/// <summary>
		/// true if it's a position between two columns
		/// </summary>
		public bool IsColumnSeparator => Type == HexLinePositionInfoType.ColumnSeparator;

		/// <summary>
		/// true if it's a position greater than or equal to the line length
		/// </summary>
		public bool IsVirtualSpace => Type == HexLinePositionInfoType.VirtualSpace;

		/// <summary>
		/// Gets the line position
		/// </summary>
		public int Position { get; }

		/// <summary>
		/// Gets the position within the cell, offset column
		/// </summary>
		public int CellPosition { get; }

		/// <summary>
		/// Gets the cell if any
		/// </summary>
		public HexCell Cell { get; }

		HexLinePositionInfo(HexLinePositionInfoType type, int position, int cellPosition) {
			Type = type;
			Position = position;
			CellPosition = cellPosition;
			Cell = null;
		}

		HexLinePositionInfo(HexLinePositionInfoType type, int position, HexCell cell) {
			Type = type;
			Position = position;
			CellPosition = position - cell.CellSpan.Start;
			if (CellPosition < 0)
				throw new ArgumentOutOfRangeException(nameof(position));
			Cell = cell;
		}

		/// <summary>
		/// Creates a position within the offset column
		/// </summary>
		/// <param name="linePosition">Line position</param>
		/// <param name="offsetIndex">Offset character index</param>
		/// <returns></returns>
		public static HexLinePositionInfo CreateOffset(int linePosition, int offsetIndex) {
			if (linePosition < 0)
				throw new ArgumentOutOfRangeException(nameof(linePosition));
			if (offsetIndex < 0)
				throw new ArgumentOutOfRangeException(nameof(offsetIndex));
			if (linePosition < offsetIndex)
				throw new ArgumentOutOfRangeException(nameof(linePosition));
			return new HexLinePositionInfo(HexLinePositionInfoType.Offset, linePosition, offsetIndex);
		}

		/// <summary>
		/// Creates a position within a value cell
		/// </summary>
		/// <param name="linePosition">Line position</param>
		/// <param name="cell">Cell</param>
		/// <returns></returns>
		public static HexLinePositionInfo CreateValue(int linePosition, HexCell cell) {
			if (linePosition < 0)
				throw new ArgumentOutOfRangeException(nameof(linePosition));
			if (cell == null)
				throw new ArgumentNullException(nameof(cell));
			return new HexLinePositionInfo(HexLinePositionInfoType.ValueCell, linePosition, cell);
		}

		/// <summary>
		/// Creates a value cell separator position
		/// </summary>
		/// <param name="linePosition">Line position</param>
		/// <param name="cell">Cell</param>
		/// <returns></returns>
		public static HexLinePositionInfo CreateValueCellSeparator(int linePosition, HexCell cell) {
			if (linePosition < 0)
				throw new ArgumentOutOfRangeException(nameof(linePosition));
			if (cell == null)
				throw new ArgumentNullException(nameof(cell));
			return new HexLinePositionInfo(HexLinePositionInfoType.ValueCellSeparator, linePosition, cell);
		}

		/// <summary>
		/// Creates a position within an ASCII cell
		/// </summary>
		/// <param name="linePosition">Line position</param>
		/// <param name="cell">Cell</param>
		/// <returns></returns>
		public static HexLinePositionInfo CreateAscii(int linePosition, HexCell cell) {
			if (linePosition < 0)
				throw new ArgumentOutOfRangeException(nameof(linePosition));
			if (cell == null)
				throw new ArgumentNullException(nameof(cell));
			return new HexLinePositionInfo(HexLinePositionInfoType.AsciiCell, linePosition, cell);
		}

		/// <summary>
		/// Creates a column separator position
		/// </summary>
		/// <param name="linePosition">Line position</param>
		/// <returns></returns>
		public static HexLinePositionInfo CreateColumnSeparator(int linePosition) {
			if (linePosition < 0)
				throw new ArgumentOutOfRangeException(nameof(linePosition));
			return new HexLinePositionInfo(HexLinePositionInfoType.ColumnSeparator, linePosition, 0);
		}

		/// <summary>
		/// Creates a virtual space position
		/// </summary>
		/// <param name="linePosition">Line position</param>
		/// <param name="lineLength">Length of line</param>
		/// <returns></returns>
		public static HexLinePositionInfo CreateVirtualSpace(int linePosition, int lineLength) {
			if (linePosition < 0)
				throw new ArgumentOutOfRangeException(nameof(linePosition));
			if (lineLength < 0)
				throw new ArgumentOutOfRangeException(nameof(lineLength));
			if (linePosition < lineLength)
				throw new ArgumentOutOfRangeException(nameof(linePosition));
			return new HexLinePositionInfo(HexLinePositionInfoType.VirtualSpace, linePosition, linePosition - lineLength);
		}
	}
}
