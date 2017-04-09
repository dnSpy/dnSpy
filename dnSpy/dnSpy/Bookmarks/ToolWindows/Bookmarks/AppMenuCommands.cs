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

using System.ComponentModel.Composition;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.ToolWindows.App;

namespace dnSpy.Bookmarks.ToolWindows.Bookmarks {
	static class AppMenuCommands {
		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_VIEW_GUID, Header = "res:BookmarksCommand", Icon = DsImagesAttribute.BookmarkMainMenuItem, Guid = MenuConstants.APP_MENU_VIEW_BOOKMARKS_GUID, Group = MenuConstants.GROUP_APP_MENU_VIEW_WINDOWS, Order = 40)]
		sealed class BookmarksSubMenuCommand : MenuItemBase {
			public override void Execute(IMenuItemContext context) { }
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_VIEW_BOOKMARKS_GUID, Header = "res:BookmarksWindowCommand", Icon = DsImagesAttribute.BookmarkMainMenuItem, InputGestureText = "res:ShortCutKeyCtrlK_CtrlW", Group = MenuConstants.GROUP_APP_MENU_BOOKMARKS_WINDOWS, Order = 0)]
		sealed class BookmarksWindowCommand : MenuItemBase {
			readonly IDsToolWindowService toolWindowService;
			[ImportingConstructor]
			public BookmarksWindowCommand(IDsToolWindowService toolWindowService) => this.toolWindowService = toolWindowService;
			public override void Execute(IMenuItemContext context) => toolWindowService.Show(BookmarksToolWindowContent.THE_GUID);
		}
	}
}
