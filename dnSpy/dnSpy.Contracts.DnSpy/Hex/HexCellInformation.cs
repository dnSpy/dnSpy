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
using Microsoft.VisualStudio.Text;

namespace dnSpy.Contracts.Hex {
	/// <summary>
	/// Cell information
	/// </summary>
	public sealed class HexCellInformation {
		/// <summary>
		/// true if there's data in the cell
		/// </summary>
		public bool HasData { get; }

		/// <summary>
		/// Index
		/// </summary>
		public int Index { get; }

		/// <summary>
		/// Gets the hex span
		/// </summary>
		public HexBufferSpan HexSpan { get; }

		/// <summary>
		/// Span of the text
		/// </summary>
		public Span TextSpan { get; }

		/// <summary>
		/// Span of the cell, some of the span could be whitespace
		/// </summary>
		public Span CellSpan { get; }

		/// <summary>
		/// Span of the cell separator
		/// </summary>
		public Span SeparatorSpan { get; }

		/// <summary>
		/// Includes the whole cell and separator span
		/// </summary>
		public Span FullSpan { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="index">Cell index</param>
		/// <param name="hexSpan">Hex span</param>
		/// <param name="textSpan">Span of the text. This span doesn't include any whitespace before and after the text.</param>
		/// <param name="cellSpan">Span of the cell, some of the span could be whitespace</param>
		/// <param name="separatorSpan">Span of the cell separator</param>
		/// <param name="fullSpan">Includes the whole cell and separator span</param>
		public HexCellInformation(int index, HexBufferSpan hexSpan, Span textSpan, Span cellSpan, Span separatorSpan, Span fullSpan) {
			HasData = true;
			Index = index;
			HexSpan = hexSpan;
			TextSpan = textSpan;
			CellSpan = cellSpan;
			SeparatorSpan = separatorSpan;
			FullSpan = fullSpan;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="index"></param>
		public HexCellInformation(int index) {
			HasData = false;
			Index = index;
		}

		/// <summary>
		/// Gets a text span
		/// </summary>
		/// <param name="flags">Flags</param>
		/// <returns></returns>
		public Span GetSpan(HexCellSpanFlags flags) {
			if ((flags & HexCellSpanFlags.Cell) == 0) {
				if ((flags & HexCellSpanFlags.Separator) != 0)
					throw new ArgumentOutOfRangeException(nameof(flags));
				return TextSpan;
			}
			if ((flags & HexCellSpanFlags.Separator) != 0)
				return FullSpan;
			return CellSpan;
		}
	}
}
