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

// from dnlib

using System.Collections.ObjectModel;

namespace dnSpy.Contracts.Hex.Files.DotNet {
	/// <summary>
	/// A MD table (eg. Method table)
	/// </summary>
	public sealed class MDTable {
		/// <summary>
		/// Gets the span
		/// </summary>
		public HexSpan Span { get; }

		/// <summary>
		/// Gets the table
		/// </summary>
		public Table Table { get; }

		/// <summary>
		/// Gets the name of this table
		/// </summary>
		public string Name => TableInfo.Name;

		/// <summary>
		/// Returns total number of rows
		/// </summary>
		public uint Rows { get; }

		/// <summary>
		/// Gets the total size in bytes of one row in this table
		/// </summary>
		public uint RowSize => (uint)TableInfo.RowSize;

		/// <summary>
		/// Returns all the columns
		/// </summary>
		public ReadOnlyCollection<ColumnInfo> Columns => TableInfo.Columns;

		/// <summary>
		/// Returns <c>true</c> if there are no valid rows
		/// </summary>
		public bool IsEmpty => Rows == 0;

		/// <summary>
		/// Returns info about this table
		/// </summary>
		public TableInfo TableInfo { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="position">Start position</param>
		/// <param name="table">The table</param>
		/// <param name="rows">Number of rows in this table</param>
		/// <param name="tableInfo">Info about this table</param>
		public MDTable(HexPosition position, Table table, uint rows, TableInfo tableInfo) {
			Table = table;
			Rows = rows;
			TableInfo = tableInfo;
			Span = new HexSpan(position, (ulong)rows * (uint)tableInfo.RowSize);
		}

		/// <summary>
		/// Checks whether the row <paramref name="rid"/> exists
		/// </summary>
		/// <param name="rid">Row ID</param>
		public bool IsValidRID(uint rid) => rid != 0 && rid <= Rows;

		/// <summary>
		/// Checks whether the row <paramref name="rid"/> does not exist
		/// </summary>
		/// <param name="rid">Row ID</param>
		public bool IsInvalidRID(uint rid) => rid == 0 || rid > Rows;
	}
}
