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
using System.Windows;
using System.Windows.Controls;

namespace dnSpy.Contracts.Menus {
	/// <summary>
	/// Menu manager
	/// </summary>
	public interface IMenuManager {
		/// <summary>
		/// Initializes a context menu. Should be called when <paramref name="elem"/> has been created.
		/// </summary>
		/// <param name="elem">Element that needs a context menu</param>
		/// <param name="guid">Guid of <paramref name="elem"/></param>
		/// <param name="creator">A <see cref="IGuidObjectsCreator"/> instance or null</param>
		/// <param name="initCtxMenu">A <see cref="IContextMenuInitializer"/> instance or null</param>
		/// <param name="ctxMenuGuid">Guid of context menu, default is <see cref="MenuConstants.CTX_MENU_GUID"/></param>
		/// <returns></returns>
		IContextMenuCreator InitializeContextMenu(FrameworkElement elem, Guid guid, IGuidObjectsCreator creator = null, IContextMenuInitializer initCtxMenu = null, Guid? ctxMenuGuid = null);

		/// <summary>
		/// Initializes a context menu. Should be called when <paramref name="elem"/> has been created.
		/// </summary>
		/// <param name="elem">Element that needs a context menu</param>
		/// <param name="guid">Guid of <paramref name="elem"/></param>
		/// <param name="creator">A <see cref="IGuidObjectsCreator"/> instance or null</param>
		/// <param name="initCtxMenu">A <see cref="IContextMenuInitializer"/> instance or null</param>
		/// <param name="ctxMenuGuid">Guid of context menu, default is <see cref="MenuConstants.CTX_MENU_GUID"/></param>
		/// <returns></returns>
		IContextMenuCreator InitializeContextMenu(FrameworkElement elem, string guid, IGuidObjectsCreator creator = null, IContextMenuInitializer initCtxMenu = null, string ctxMenuGuid = null);

		/// <summary>
		/// Creates a <see cref="Menu"/>
		/// </summary>
		/// <param name="menuGuid">Guid of menu, eg. <see cref="MenuConstants.APP_MENU_GUID"/></param>
		/// <param name="commandTarget">Command target for menu items, eg. the owner window, or null</param>
		/// <returns></returns>
		Menu CreateMenu(Guid menuGuid, IInputElement commandTarget);
	}
}
