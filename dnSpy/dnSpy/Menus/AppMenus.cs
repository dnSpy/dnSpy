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

using dnSpy.Contracts.Menus;

namespace dnSpy.Menus {
	[ExportMenu(OwnerGuid = MenuConstants.APP_MENU_GUID, Guid = MenuConstants.APP_MENU_FILE_GUID, Order = MenuConstants.ORDER_APP_MENU_FILE, Header = "res:Menu_File")]
	sealed class FileMenu : IMenu { }

	[ExportMenu(OwnerGuid = MenuConstants.APP_MENU_GUID, Guid = MenuConstants.APP_MENU_EDIT_GUID, Order = MenuConstants.ORDER_APP_MENU_EDIT, Header = "res:Menu_Edit")]
	sealed class EditMenu : IMenu { }

	[ExportMenu(OwnerGuid = MenuConstants.APP_MENU_GUID, Guid = MenuConstants.APP_MENU_VIEW_GUID, Order = MenuConstants.ORDER_APP_MENU_VIEW, Header = "res:Menu_View")]
	sealed class ViewMenu : IMenu { }

	[ExportMenu(OwnerGuid = MenuConstants.APP_MENU_GUID, Guid = MenuConstants.APP_MENU_WINDOW_GUID, Order = MenuConstants.ORDER_APP_MENU_WINDOW, Header = "res:Menu_Windows")]
	sealed class WindowMenu : IMenu { }

	[ExportMenu(OwnerGuid = MenuConstants.APP_MENU_GUID, Guid = MenuConstants.APP_MENU_HELP_GUID, Order = MenuConstants.ORDER_APP_MENU_HELP, Header = "res:Menu_Help")]
	sealed class HelpMenu : IMenu { }
}
