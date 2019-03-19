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

namespace dnSpy.Contracts.Hex {
	/// <summary>
	/// <see cref="HexLinePositionInfo"/> type
	/// </summary>
	public enum HexLinePositionInfoType {
		/// <summary>
		/// Offset column
		/// </summary>
		Offset,

		/// <summary>
		/// Value cell
		/// </summary>
		ValueCell,

		/// <summary>
		/// Value cell separator
		/// </summary>
		ValueCellSeparator,

		/// <summary>
		/// ASCII cell
		/// </summary>
		AsciiCell,

		/// <summary>
		/// Column separator
		/// </summary>
		ColumnSeparator,

		/// <summary>
		/// Virtual space (a position greater than or equal to the line length)
		/// </summary>
		VirtualSpace,
	}
}
