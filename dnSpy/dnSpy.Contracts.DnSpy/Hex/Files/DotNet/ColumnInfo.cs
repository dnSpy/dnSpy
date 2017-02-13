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

// from dnlib

using System;

namespace dnSpy.Contracts.Hex.Files.DotNet {
	/// <summary>
	/// Info about one column in a MD table
	/// </summary>
	public sealed class ColumnInfo {
		/// <summary>
		/// Gets the column index
		/// </summary>
		public int Index { get; }

		/// <summary>
		/// Returns the column offset within the table row
		/// </summary>
		public int Offset { get; }

		/// <summary>
		/// Returns the column size
		/// </summary>
		public int Size { get; }

		/// <summary>
		/// Returns the column name
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Returns the ColumnSize enum value
		/// </summary>
		public ColumnSize ColumnSize { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="index">Column index</param>
		/// <param name="name">The column name</param>
		/// <param name="columnSize">Column size</param>
		/// <param name="offset">Offset of column</param>
		/// <param name="size">Size of column</param>
		public ColumnInfo(int index, string name, ColumnSize columnSize, int offset = 0, int size = 0) {
			if (index < 0)
				throw new ArgumentOutOfRangeException(nameof(index));
			if (offset < 0)
				throw new ArgumentOutOfRangeException(nameof(offset));
			if (size < 0)
				throw new ArgumentOutOfRangeException(nameof(size));
			Index = index;
			Name = name ?? throw new ArgumentNullException(nameof(name));
			ColumnSize = columnSize;
			Offset = offset;
			Size = size;
		}
	}
}
