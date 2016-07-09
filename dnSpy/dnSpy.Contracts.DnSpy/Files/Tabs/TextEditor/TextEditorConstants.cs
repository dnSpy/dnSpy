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

namespace dnSpy.Contracts.Files.Tabs.TextEditor {
	/// <summary>
	/// Constants
	/// </summary>
	public static class TextEditorConstants {
		/// <summary>Z-order of breakpoints</summary>
		public static double ZORDER_BREAKPOINT = 1000;

		/// <summary>Z-order of return statements</summary>
		public static double ZORDER_RETURNSTATEMENT = 2000;

		/// <summary>Z-order of selected return statements</summary>
		public static double ZORDER_SELECTEDRETURNSTATEMENT = 3000;

		/// <summary>Z-order of current statement</summary>
		public static double ZORDER_CURRENTSTATEMENT = 4000;

		/// <summary>Z-order of search results</summary>
		public static double ZORDER_SEARCHRESULT = 5000;
	}
}
