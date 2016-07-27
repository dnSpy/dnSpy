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

namespace dnSpy.Contracts.Text.Editor {
	/// <summary>
	/// <see cref="IGlyphTextMarkerService"/> Z-indexes
	/// </summary>
	public static class GlyphTextMarkerServiceZIndexes {
		/// <summary>
		/// Z-index of bookmarks
		/// </summary>
		public const int Bookmark = 1;

		/// <summary>
		/// (Debugger) Z-index of breakpoints
		/// </summary>
		public const int Breakpoint = 2;

		/// <summary>
		/// (Debugger) Z-index of current statement
		/// </summary>
		public const int CurrentStatement = 3;

		/// <summary>
		/// (Debugger) Z-index of return statement
		/// </summary>
		public const int ReturnStatement = 4;
	}
}
