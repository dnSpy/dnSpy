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
	/// Text view line change
	/// </summary>
	public enum TextViewLineChange {
		/// <summary>
		/// No change type is specified
		/// </summary>
		None,

		/// <summary>
		/// The line is new or reformatted
		/// </summary>
		NewOrReformatted,

		/// <summary>
		/// The text has not changed, but some change has caused the y-coordinate to change. For example, a line was inserted above this line, or the user scrolled the view up or down.
		/// </summary>
		Translated,
	}
}
