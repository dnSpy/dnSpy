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

namespace dnSpy.Contracts.Text.Formatting {
	/// <summary>
	/// Visibility state
	/// </summary>
	public enum VisibilityState {
		/// <summary>
		/// The line is unattached, that is, it was not formatted as part of a layout in the text view.
		/// </summary>
		Unattached,

		/// <summary>
		/// The line is hidden, that is, not visible inside the view. Lines are also hidden when their bottom edge is even with the top of the view or their top edge is even with the bottom of the view.
		/// </summary>
		Hidden,

		/// <summary>
		/// The line is partially visible, that is, some portion of the line extends above the top of the view or below the bottom of the view.
		/// </summary>
		PartiallyVisible,

		/// <summary>
		/// The line is fully visible.
		/// </summary>
		FullyVisible,
	}
}
