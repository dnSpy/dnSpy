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

namespace dnSpy.Contracts.Hex {
	/// <summary>
	/// Span selection flags
	/// </summary>
	[Flags]
	public enum HexSpanSelectionFlags {
		/// <summary>
		/// No bit is set
		/// </summary>
		None					= 0,

		/// <summary>
		/// Offset column
		/// </summary>
		Offset					= 0x00000001,

		/// <summary>
		/// Values column
		/// </summary>
		Values					= 0x00000002,

		/// <summary>
		/// ASCII column
		/// </summary>
		Ascii					= 0x00000004,

		/// <summary>
		/// Cells in group #0
		/// </summary>
		Group0					= 0x00000008,

		/// <summary>
		/// Cells in group #1
		/// </summary>
		Group1					= 0x00000010,

		/// <summary>
		/// Select the full cell instead of only the text
		/// </summary>
		Cell					= 0x00000020,

		/// <summary>
		/// Include the cell separator, if any
		/// </summary>
		Separator				= 0x00000040,

		/// <summary>
		/// One value at a time
		/// </summary>
		OneValue				= 0x00000080,

		/// <summary>
		/// Select all cells in the values/ASCII column
		/// </summary>
		AllCells				= 0x00000100,

		/// <summary>
		/// Select all visible cells in the values/ASCII column
		/// </summary>
		AllVisibleCells			= 0x00000200,
	}
}
