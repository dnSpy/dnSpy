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
using VST = Microsoft.VisualStudio.Text;

namespace dnSpy.Contracts.Hex {
	/// <summary>
	/// Creates <see cref="HexBufferLine"/>s
	/// </summary>
	public abstract class HexBufferLineProvider {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexBufferLineProvider() { }

		/// <summary>
		/// Gets the buffer
		/// </summary>
		public abstract HexBuffer Buffer { get; }

		/// <summary>
		/// Gets the buffer span
		/// </summary>
		public abstract HexBufferSpan BufferSpan { get; }

		/// <summary>
		/// Gets the start position
		/// </summary>
		public HexBufferPoint BufferStart => BufferSpan.Start;

		/// <summary>
		/// Gets the end position
		/// </summary>
		public HexBufferPoint BufferEnd => BufferSpan.End;

		/// <summary>
		/// Number of lines. There's always at least one line.
		/// </summary>
		public abstract HexPosition LineCount { get; }

		/// <summary>
		/// Gets the span of the offset column. This is empty if the column isn't present.
		/// </summary>
		public abstract VST.Span OffsetSpan { get; }

		/// <summary>
		/// Gets the span of the values column. This is empty if the column isn't present.
		/// </summary>
		public abstract VST.Span ValuesSpan { get; }

		/// <summary>
		/// Gets the span of the ASCII column. This is empty if the column isn't present.
		/// </summary>
		public abstract VST.Span AsciiSpan { get; }

		/// <summary>
		/// Gets the span of a column. This is empty if the column isn't present.
		/// </summary>
		/// <param name="column">Column</param>
		/// <returns></returns>
		public VST.Span GetColumnSpan(HexColumnType column) {
			switch (column) {
			case HexColumnType.Offset:	return OffsetSpan;
			case HexColumnType.Values:	return ValuesSpan;
			case HexColumnType.Ascii:	return AsciiSpan;
			default: throw new ArgumentOutOfRangeException(nameof(column));
			}
		}

		/// <summary>
		/// Values group collection. It's empty if the values column isn't present.
		/// </summary>
		public abstract ReadOnlyCollection<HexGroupInformation> ValuesGroup { get; }

		/// <summary>
		/// ASCII group collection. It's empty if the ASCII column isn't present.
		/// </summary>
		public abstract ReadOnlyCollection<HexGroupInformation> AsciiGroup { get; }

		/// <summary>
		/// Number of characters per line
		/// </summary>
		public abstract int CharsPerLine { get; }

		/// <summary>
		/// Number of bytes per line or 0 to fit to width
		/// </summary>
		public abstract int BytesPerLine { get; }

		/// <summary>
		/// Number of bytes per group
		/// </summary>
		public abstract int GroupSizeInBytes { get; }

		/// <summary>
		/// true to show the offset
		/// </summary>
		public abstract bool ShowOffset { get; }

		/// <summary>
		/// true to use lower case hex
		/// </summary>
		public abstract bool OffsetLowerCaseHex { get; }

		/// <summary>
		/// Offset format
		/// </summary>
		public abstract HexOffsetFormat OffsetFormat { get; }

		/// <summary>
		/// First position to show
		/// </summary>
		public abstract HexPosition StartPosition { get; }

		/// <summary>
		/// End position
		/// </summary>
		public abstract HexPosition EndPosition { get; }

		/// <summary>
		/// Base position
		/// </summary>
		public abstract HexPosition BasePosition { get; }

		/// <summary>
		/// true if all visible positions are relative to <see cref="StartPosition"/>
		/// </summary>
		public abstract bool UseRelativePositions { get; }

		/// <summary>
		/// true to show the values
		/// </summary>
		public abstract bool ShowValues { get; }

		/// <summary>
		/// true to use lower case hex
		/// </summary>
		public abstract bool ValuesLowerCaseHex { get; }

		/// <summary>
		/// Number of bits of the offset to show
		/// </summary>
		public abstract int OffsetBitSize { get; }

		/// <summary>
		/// Values format
		/// </summary>
		public abstract HexValuesDisplayFormat ValuesFormat { get; }

		/// <summary>
		/// Number of bytes per value
		/// </summary>
		public abstract int BytesPerValue { get; }

		/// <summary>
		/// true to show ASCII chars
		/// </summary>
		public abstract bool ShowAscii { get; }

		/// <summary>
		/// Column order
		/// </summary>
		public abstract ReadOnlyCollection<HexColumnType> ColumnOrder { get; }

		/// <summary>
		/// Gets the number of characters per cell value. This value doesn't include any cell separators
		/// </summary>
		/// <returns></returns>
		public abstract int GetCharsPerCell(HexColumnType column);

		/// <summary>
		/// Gets the total number of characters per cell. This includes cell separators if any.
		/// </summary>
		/// <returns></returns>
		public abstract int GetCharsPerCellIncludingSeparator(HexColumnType column);

		/// <summary>
		/// Gets the buffer position of a line
		/// </summary>
		/// <param name="lineNumber">Line number</param>
		/// <returns></returns>
		public abstract HexBufferPoint GetBufferPositionFromLineNumber(HexPosition lineNumber);

		/// <summary>
		/// Returns a line
		/// </summary>
		/// <param name="lineNumber">Line number</param>
		/// <returns></returns>
		public abstract HexBufferLine GetLineFromLineNumber(HexPosition lineNumber);

		/// <summary>
		/// Creates a line
		/// </summary>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public abstract HexBufferLine GetLineFromPosition(HexBufferPoint position);

		/// <summary>
		/// Gets the line number
		/// </summary>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public abstract HexPosition GetLineNumberFromPosition(HexBufferPoint position);

		/// <summary>
		/// Converts a physical (stream) position to a logical position
		/// </summary>
		/// <param name="physicalPosition">Physical (stream) position</param>
		/// <returns></returns>
		public abstract HexPosition ToLogicalPosition(HexPosition physicalPosition);

		/// <summary>
		/// Converts a logical position to a physical (stream) position
		/// </summary>
		/// <param name="logicalPosition">Logical position</param>
		/// <returns></returns>
		public abstract HexPosition ToPhysicalPosition(HexPosition logicalPosition);

		/// <summary>
		/// Returns true if <paramref name="position"/> is a valid position that can be passed to
		/// methods expecting a (physical) position.
		/// </summary>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public bool IsValidPosition(HexBufferPoint position) {
			if (position.Buffer != Buffer)
				return false;
			return StartPosition == EndPosition ? StartPosition == position : StartPosition <= position && position < EndPosition;
		}

		/// <summary>
		/// Gets a buffer span within a cell
		/// </summary>
		/// <param name="cell">Cell</param>
		/// <param name="cellPosition">Position within the cell</param>
		/// <returns></returns>
		public abstract HexBufferSpan GetValueBufferSpan(HexCell cell, int cellPosition);
	}
}
