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

namespace dnSpy.Contracts.Hex.Editor {
	/// <summary>
	/// Column line kind
	/// </summary>
	public enum HexColumnLineKind {
		/// <summary>
		/// No line is shown (it's disabled)
		/// </summary>
		None,

		/// <summary>
		/// Solid lines
		/// </summary>
		Solid,

		/// <summary>
		/// Dashed lines (dash 1px, gap 1px)
		/// </summary>
		Dashed_1_1,

		/// <summary>
		/// Dashed lines (dash 2px, gap 2px)
		/// </summary>
		Dashed_2_2,

		/// <summary>
		/// Dashed lines (dash 3px, gap 3px)
		/// </summary>
		Dashed_3_3,

		/// <summary>
		/// Dashed lines (dash 4px, gap 4px)
		/// </summary>
		Dashed_4_4,
	}
}
