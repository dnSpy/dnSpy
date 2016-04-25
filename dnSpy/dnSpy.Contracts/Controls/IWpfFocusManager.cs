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

using System.Windows;

namespace dnSpy.Contracts.Controls {
	/// <summary>
	/// Focuses <see cref="UIElement"/>s
	/// </summary>
	public interface IWpfFocusManager {
		/// <summary>
		/// true if <see cref="Focus(IInputElement)"/> can be called
		/// </summary>
		bool CanFocus { get; }

		/// <summary>
		/// Gives the focus to <paramref name="element"/> by calling its <see cref="UIElement.Focus()"/>
		/// method unless some other code prevents it. Eg., a menu could be open which could prevent
		/// the focus from being stolen by <paramref name="element"/>. Export a <see cref="IWpfFocusChecker"/>
		/// class to prevent Focus() from being called.
		/// </summary>
		/// <param name="element">Element</param>
		void Focus(IInputElement element);
	}
}
