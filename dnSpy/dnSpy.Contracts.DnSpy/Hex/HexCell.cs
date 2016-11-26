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
using VST = Microsoft.VisualStudio.Text;

namespace dnSpy.Contracts.Hex {
	/// <summary>
	/// Cell information
	/// </summary>
	public sealed class HexCell {
		/// <summary>
		/// true if there's data in the cell even if there's no memory there; false if it's a blank cell
		/// </summary>
		public bool HasData { get; }

		/// <summary>
		/// Index in <see cref="HexCellCollection"/>
		/// </summary>
		public int Index { get; }

		/// <summary>
		/// Group index
		/// </summary>
		public int GroupIndex { get; }

		/// <summary>
		/// Gets the buffer span. It's valid if <see cref="HasData"/> is true
		/// </summary>
		public HexBufferSpan BufferSpan { get; }

		/// <summary>
		/// Gets the start position. It's valid if <see cref="HasData"/> is true
		/// </summary>
		public HexBufferPoint BufferStart => BufferSpan.Start;

		/// <summary>
		/// Gets the end position. It's valid if <see cref="HasData"/> is true
		/// </summary>
		public HexBufferPoint BufferEnd => BufferSpan.End;

		/// <summary>
		/// Span of the text
		/// </summary>
		public VST.Span TextSpan { get; }

		/// <summary>
		/// Span of the cell, some of the span could be whitespace
		/// </summary>
		public VST.Span CellSpan { get; }

		/// <summary>
		/// Span of the cell separator
		/// </summary>
		public VST.Span SeparatorSpan { get; }

		/// <summary>
		/// Includes the whole cell and separator span
		/// </summary>
		public VST.Span FullSpan { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="index">Cell index</param>
		/// <param name="groupIndex">Group index</param>
		/// <param name="bufferSpan">Buffer span or the default value if there's no data</param>
		/// <param name="textSpan">Span of the text. This span doesn't include any whitespace before and after the text.</param>
		/// <param name="cellSpan">Span of the cell, some of the span could be whitespace</param>
		/// <param name="separatorSpan">Span of the cell separator</param>
		/// <param name="fullSpan">Includes the whole cell and separator span</param>
		public HexCell(int index, int groupIndex, HexBufferSpan bufferSpan, VST.Span textSpan, VST.Span cellSpan, VST.Span separatorSpan, VST.Span fullSpan) {
			if (index < 0)
				throw new ArgumentOutOfRangeException(nameof(index));
			if (groupIndex < 0 || groupIndex > 1)
				throw new ArgumentOutOfRangeException(nameof(groupIndex));
			if (cellSpan.Length == 0)
				throw new ArgumentOutOfRangeException(nameof(cellSpan));
			if (!fullSpan.Contains(cellSpan))
				throw new ArgumentOutOfRangeException(nameof(cellSpan));
			HasData = !bufferSpan.IsDefault;
			Index = index;
			GroupIndex = groupIndex;
			BufferSpan = bufferSpan;
			TextSpan = textSpan;
			CellSpan = cellSpan;
			SeparatorSpan = separatorSpan;
			FullSpan = fullSpan;
		}

		/// <summary>
		/// Gets a text span
		/// </summary>
		/// <param name="flags">Flags, only <see cref="HexSpanSelectionFlags.Cell"/> and
		/// <see cref="HexSpanSelectionFlags.Separator"/> are checked</param>
		/// <returns></returns>
		public VST.Span GetSpan(HexSpanSelectionFlags flags) {
			if ((flags & HexSpanSelectionFlags.Cell) == 0) {
				if ((flags & HexSpanSelectionFlags.Separator) != 0)
					throw new ArgumentOutOfRangeException(nameof(flags));
				return TextSpan;
			}
			if ((flags & HexSpanSelectionFlags.Separator) != 0)
				return FullSpan;
			return CellSpan;
		}
	}
}
