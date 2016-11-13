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
using System.Collections.Generic;

namespace dnSpy.Contracts.Hex.Editor {
	/// <summary>
	/// Hex cell collection
	/// </summary>
	public struct HexCellInformationCollection {
		/// <summary>
		/// Gets the empty collection
		/// </summary>
		public static readonly HexCellInformationCollection Empty = new HexCellInformationCollection(Array.Empty<HexCellInformation>());

		readonly HexCellInformation[] cells;

		/// <summary>
		/// Gets the number of elements in this collection
		/// </summary>
		public int Count => cells.Length;

		/// <summary>
		/// Gets a cell
		/// </summary>
		/// <param name="index">Index</param>
		/// <returns></returns>
		public HexCellInformation this[int index] => cells[index];

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="cells">All cells</param>
		public HexCellInformationCollection(HexCellInformation[] cells) {
			if (cells == null)
				throw new ArgumentNullException(nameof(cells));
#if DEBUG
			for (int i = 0; i < cells.Length; i++) {
				if (cells[i].Index != i)
					throw new ArgumentException();
			}
#endif
			this.cells = cells;
		}

		/// <summary>
		/// Gets the cell that contains <paramref name="point"/>
		/// </summary>
		/// <param name="point">Point</param>
		/// <returns></returns>
		public HexCellInformation GetCell(HexBufferPoint point) {
			foreach (var cell in cells) {
				if (cell.HasData && cell.HexSpan.Contains(point))
					return cell;
			}
			return null;
		}

		/// <summary>
		/// Gets all cells that are contained in <paramref name="span"/>. The returned cells
		/// are ordered by index.
		/// </summary>
		/// <param name="span">Span</param>
		/// <returns></returns>
		public IEnumerable<HexCellInformation> GetCells(HexBufferSpan span) {
			foreach (var cell in cells) {
				if (cell.HasData && cell.HexSpan.Contains(span))
					yield return cell;
			}
		}
	}
}
