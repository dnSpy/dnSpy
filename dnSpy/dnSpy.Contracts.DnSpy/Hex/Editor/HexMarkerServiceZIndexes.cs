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

namespace dnSpy.Contracts.Hex.Editor {
	/// <summary>
	/// Hex marker service Z-indexes. Markers with a negative z-index are placed in a
	/// marker layer below most other layers.
	/// </summary>
	public static class HexMarkerServiceZIndexes {
		/// <summary>
		/// Current value highlighter
		/// </summary>
		public const int CurrentValue = 0;

		/// <summary>
		/// Find match
		/// </summary>
		public const int FindMatch = 5000;

		/// <summary>
		/// ToolTip field #0
		/// </summary>
		public const int ToolTipField0 = 6000;

		/// <summary>
		/// ToolTip field #1
		/// </summary>
		public const int ToolTipField1 = 6001;

		/// <summary>
		/// ToolTip current field
		/// </summary>
		public const int ToolTipCurrentField = 7000;
	}
}
