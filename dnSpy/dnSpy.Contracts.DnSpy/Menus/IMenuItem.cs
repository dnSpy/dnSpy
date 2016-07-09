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

using dnSpy.Contracts.Images;

namespace dnSpy.Contracts.Menus {
	/// <summary>
	/// A menu item command. See also <see cref="IMenuItem2"/>
	/// </summary>
	public interface IMenuItem {
		/// <summary>
		/// Returns true if the menu item is visible in the menu
		/// </summary>
		/// <param name="context">Context</param>
		/// <returns></returns>
		bool IsVisible(IMenuItemContext context);

		/// <summary>
		/// Returns true if the menu item is enabled and its <see cref="Execute(IMenuItemContext)"/>
		/// method can be called.
		/// </summary>
		/// <param name="context">Context</param>
		/// <returns></returns>
		bool IsEnabled(IMenuItemContext context);

		/// <summary>
		/// Executes the command
		/// </summary>
		/// <param name="context">Context</param>
		void Execute(IMenuItemContext context);
	}

	/// <summary>
	/// Extends <see cref="IMenuItem"/> with optional methods
	/// </summary>
	public interface IMenuItem2 {
		/// <summary>
		/// Gets the menu item header or null if the default header from the attribute should be
		/// used.
		/// </summary>
		/// <param name="context">Context</param>
		/// <returns></returns>
		string GetHeader(IMenuItemContext context);

		/// <summary>
		/// Gets the menu item input gesture text or null if the default input gesture text from the
		/// attribute should be used.
		/// </summary>
		/// <param name="context">Context</param>
		/// <returns></returns>
		string GetInputGestureText(IMenuItemContext context);

		/// <summary>
		/// Gets the menu item icon or null if the default icon from the attribute should be used.
		/// </summary>
		/// <param name="context">Context</param>
		/// <returns></returns>
		ImageReference? GetIcon(IMenuItemContext context);

		/// <summary>
		/// Returns true if the menu item is checked
		/// </summary>
		/// <param name="context">Context</param>
		/// <returns></returns>
		bool IsChecked(IMenuItemContext context);
	}
}
