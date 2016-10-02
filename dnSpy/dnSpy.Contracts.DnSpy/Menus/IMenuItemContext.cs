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
using System.Collections.Generic;

namespace dnSpy.Contracts.Menus {
	/// <summary>
	/// <see cref="IMenuItem"/> context
	/// </summary>
	public interface IMenuItemContext {
		/// <summary>
		/// Gets the guid of the top-level menu, eg. <see cref="MenuConstants.CTX_MENU_GUID"/> or
		/// <see cref="MenuConstants.APP_MENU_GUID"/>
		/// </summary>
		Guid MenuGuid { get; }

		/// <summary>
		/// true if it was opened from the keyboard, else mouse. If it's the main menu (and not
		/// a context menu), this will always be true.
		/// </summary>
		bool OpenedFromKeyboard { get; }

		/// <summary>
		/// Creator object
		/// </summary>
		GuidObject CreatorObject { get; }

		/// <summary>
		/// All objects. <see cref="CreatorObject"/> is always the first one
		/// </summary>
		IEnumerable<GuidObject> GuidObjects { get; }

		/// <summary>
		/// Gets or creates user state that can be saved in the context to prevent re-generating the
		/// same state when various <see cref="IMenuItem"/> methods get called.
		/// </summary>
		/// <typeparam name="T">State type</typeparam>
		/// <param name="key">Key, eg. a guid or a static key in some base command class</param>
		/// <param name="createState">Delegate that creates a new value if it hasn't been created yet</param>
		/// <returns></returns>
		T GetOrCreateState<T>(object key, Func<T> createState) where T : class;

		/// <summary>
		/// Finds the first object of a certain type. Returns default({T}) if none was found
		/// </summary>
		/// <typeparam name="T">Type</typeparam>
		/// <returns></returns>
		T Find<T>();
	}
}
