/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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

namespace dnSpy.Contracts.Themes {
	/// <summary>
	/// A theme
	/// </summary>
	public interface ITheme {
		/// <summary>Guid</summary>
		Guid Guid { get; }

		/// <summary>Name of theme that can be used in a MenuItem</summary>
		string MenuName { get; }

		/// <summary>true if this is a high-contrast theme</summary>
		bool IsHighContrast { get; }

		/// <summary>Theme order. Can be used by a UI class to sort the themes before showing them
		/// to the user</summary>
		double Order { get; }

		/// <summary>
		/// Gets the inherited color
		/// </summary>
		/// <param name="colorType">Color</param>
		/// <returns></returns>
		IThemeColor GetColor(ColorType colorType);

		/// <summary>
		/// Gets the inherited color that can be used by a text editor (default colors are null)
		/// </summary>
		/// <param name="colorType">Color</param>
		/// <returns></returns>
		IThemeColor GetTextColor(ColorType colorType);
	}
}
