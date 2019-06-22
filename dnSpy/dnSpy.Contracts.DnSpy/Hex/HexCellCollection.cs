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
using System.Collections.Generic;
using System.Diagnostics;
using VST = Microsoft.VisualStudio.Text;

namespace dnSpy.Contracts.Hex {
	/// <summary>
	/// Hex cell collection
	/// </summary>
	public readonly struct HexCellCollection {
		/// <summary>
		/// true if this is a default instance that hasn't been initialized
		/// </summary>
		public bool IsDefault => cells is null;

		/// <summary>
		/// Gets the empty collection
		/// </summary>
		public static readonly HexCellCollection Empty = new HexCellCollection(Array.Empty<HexCell>());

		readonly HexCell[] cells;
		readonly int validStart, validEnd;

		/// <summary>
		/// Gets the number of elements in this collection
		/// </summary>
		public int Count => cells.Length;

		/// <summary>
		/// Gets the span of cells in the collection that have data (<see cref="HexCell.HasData"/> is true)
		/// </summary>
		public VST.Span HasDataSpan => VST.Span.FromBounds(validStart, validEnd);

		/// <summary>
		/// Gets a cell
		/// </summary>
		/// <param name="index">Index</param>
		/// <returns></returns>
		public HexCell this[int index] => cells[index];

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="cells">All cells</param>
		public HexCellCollection(HexCell[] cells) {
			if (cells is null)
				throw new ArgumentNullException(nameof(cells));
			for (int i = 0; i < cells.Length; i++) {
				if (cells[i].HasData) {
					validStart = i;
					int j = cells.Length - 1;
					while (!cells[j].HasData)
						j--;
					validEnd = j + 1;
					goto done;
				}
			}
			validStart = 0;
			validEnd = 0;
done:;
#if DEBUG
			for (int i = 0; i < cells.Length; i++) {
				if (cells[i].Index != i)
					throw new ArgumentException();
				if (cells[i].HasData != (validStart <= i && i < validEnd))
					throw new ArgumentException();
			}
			for (int i = validStart + 1; i < validEnd; i++) {
				if (cells[i - 1].BufferEnd != cells[i].BufferStart)
					throw new ArgumentException();
				if (cells[i - 1].BufferSpan.Length != cells[i].BufferSpan.Length && i + 1 != validEnd)
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
		public HexCell? GetCell(HexBufferPoint point) {
			int index = GetStartIndex(point);
			if (validStart <= index && index < validEnd)
				return cells[index];
			return null;
		}

		/// <summary>
		/// Gets all cells, including unused cells
		/// </summary>
		/// <returns></returns>
		public IEnumerable<HexCell> GetCells() {
			foreach (var cell in cells)
				yield return cell;
		}

		/// <summary>
		/// Gets all visible cells
		/// </summary>
		/// <returns></returns>
		public IEnumerable<HexCell> GetVisibleCells() {
			for (int i = validStart; i < validEnd; i++)
				yield return cells[i];
		}

		/// <summary>
		/// Gets all cells that are contained in <paramref name="span"/>. The returned cells
		/// are ordered by index.
		/// </summary>
		/// <param name="span">Span</param>
		/// <returns></returns>
		public IEnumerable<HexCell> GetCells(HexBufferSpan span) {
			int index = GetStartIndex(span.Start);
			if (index >= validStart) {
				while (index < validEnd) {
					var cell = cells[index];
					Debug.Assert(cell.HasData);
					if (span.End <= cell.BufferStart)
						break;
					yield return cell;
					index++;
				}
			}
		}

		int GetStartIndex(HexBufferPoint position) {
			var cellsLocal = cells;
			int lo = validStart, hi = validEnd;
			if (lo >= hi)
				return -1;
			var cellFirst = cellsLocal[lo];
			var cellLast = cellsLocal[hi - 1];
			if (position < cellFirst.BufferStart || position >= cellLast.BufferEnd)
				return -1;
			return lo + (int)((position - cellFirst.BufferStart).ToUInt64() / cellFirst.BufferSpan.Length.ToUInt64());
		}
	}
}
