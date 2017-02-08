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
using System.Collections.ObjectModel;

namespace dnSpy.Contracts.Hex.Files.DotNet {
	/// <summary>
	/// Info about one MD table
	/// </summary>
	public sealed class TableInfo {
		/// <summary>
		/// Returns the table type
		/// </summary>
		public Table Table { get; }

		/// <summary>
		/// Returns the total size of a row in bytes
		/// </summary>
		public int RowSize { get; }

		/// <summary>
		/// Returns all the columns
		/// </summary>
		public ReadOnlyCollection<ColumnInfo> Columns { get; }

		/// <summary>
		/// Returns the name of the table
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="table">Table type</param>
		/// <param name="name">Table name</param>
		/// <param name="columns">All columns</param>
		/// <param name="rowSize">Row size</param>
		public TableInfo(Table table, string name, ColumnInfo[] columns, int rowSize = 0) {
			if (name == null)
				throw new ArgumentNullException(nameof(name));
			if (columns == null)
				throw new ArgumentNullException(nameof(columns));
			Table = table;
			Name = name;
			Columns = new ReadOnlyCollection<ColumnInfo>(columns);
			RowSize = rowSize;
		}
	}
}
