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

namespace dnSpy.Text.Editor {
	static class WpfTextViewConstants {
		/// <summary>
		/// Extra visible horizontal space that should always be available. Used when calculating
		/// max horizontal scrolling distance or when moving caret horizontally.
		/// </summary>
		public const double EXTRA_HORIZONTAL_WIDTH = 200;

		/// <summary>
		/// Same as <see cref="EXTRA_HORIZONTAL_WIDTH"/> but used by the horizontal scroll bar
		/// </summary>
		public const double EXTRA_HORIZONTAL_SCROLLBAR_WIDTH = EXTRA_HORIZONTAL_WIDTH;
	}
}
