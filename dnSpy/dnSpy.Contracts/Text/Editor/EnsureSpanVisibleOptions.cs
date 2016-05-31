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

namespace dnSpy.Contracts.Text.Editor {
	/// <summary>
	/// Ensure span visible options
	/// </summary>
	[Flags]
	public enum EnsureSpanVisibleOptions {
		/// <summary>
		/// Ensure that the end of the span is visible if it is impossible to display the entire span. If none of the text in the span is currently visible, center the span in the view.
		/// </summary>
		None				= 0,

		/// <summary>
		/// Ensure that the start of the span is visible if it is impossible to display the entire span.
		/// </summary>
		ShowStart			= 1,

		/// <summary>
		/// Do the minimum amount of scrolling to display the span in the view.
		/// </summary>
		MinimumScroll		= 2,

		/// <summary>
		/// Always center the span in the view.
		/// </summary>
		AlwaysCenter		= 4,
	}
}
