/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using System.ComponentModel.Composition;
using dnSpy.Bookmarks.TextEditor;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.ToolWindows.App;
using dnSpy.Properties;

namespace dnSpy.Bookmarks.Commands {
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
			public override void Execute(IMenuItemContext context) => toolWindowService.Show(ToolWindows.Bookmarks.BookmarksToolWindowContent.THE_GUID);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_VIEW_BOOKMARKS_GUID, Header = "res:ToggleBookmarkCommand", Icon = DsImagesAttribute.Bookmark, InputGestureText = "res:ShortCutKeyCtrlK_CtrlK", Group = MenuConstants.GROUP_APP_MENU_BOOKMARKS_COMMANDS1, Order = 0)]
		sealed class ToggleBookmarkCommand : MenuItemBase {
			readonly Lazy<MainMenuOperations> mainMenuOperations;
			[ImportingConstructor]
			public ToggleBookmarkCommand(Lazy<MainMenuOperations> mainMenuOperations) => this.mainMenuOperations = mainMenuOperations;
			public override void Execute(IMenuItemContext context) => mainMenuOperations.Value.ToggleBookmark();
			public override bool IsEnabled(IMenuItemContext context) => mainMenuOperations.Value.CanToggleBookmark;
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_VIEW_BOOKMARKS_GUID, Icon = DsImagesAttribute.BookmarkDisabled, Group = MenuConstants.GROUP_APP_MENU_BOOKMARKS_COMMANDS1, Order = 10)]
		sealed class EnableAllBookmarksCommand : MenuItemBase {
			readonly Lazy<MainMenuOperations> mainMenuOperations;
			[ImportingConstructor]
			public EnableAllBookmarksCommand(Lazy<MainMenuOperations> mainMenuOperations) => this.mainMenuOperations = mainMenuOperations;
			public override void Execute(IMenuItemContext context) => mainMenuOperations.Value.EnableAllBookmarks();
			public override bool IsEnabled(IMenuItemContext context) => mainMenuOperations.Value.CanEnableAllBookmarks;
			public override string GetHeader(IMenuItemContext context) {
				switch (mainMenuOperations.Value.GetEnableAllBookmarksKind()) {
				case EnableAllBookmarksKind.None:
				case EnableAllBookmarksKind.Enable:		return dnSpy_Resources.EnableAllBookmarksCommand;
				case EnableAllBookmarksKind.Disable:	return dnSpy_Resources.DisableAllBookmarksCommand;
				default:								return null;
				}
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_VIEW_BOOKMARKS_GUID, Header = "res:EnableBookmarkCommand", Icon = DsImagesAttribute.Bookmark, Group = MenuConstants.GROUP_APP_MENU_BOOKMARKS_COMMANDS1, Order = 20)]
		sealed class EnableBookmarkCommand : MenuItemBase {
			readonly Lazy<MainMenuOperations> mainMenuOperations;
			[ImportingConstructor]
			public EnableBookmarkCommand(Lazy<MainMenuOperations> mainMenuOperations) => this.mainMenuOperations = mainMenuOperations;
			public override void Execute(IMenuItemContext context) => mainMenuOperations.Value.EnableBookmark();
			public override bool IsEnabled(IMenuItemContext context) => mainMenuOperations.Value.CanEnableBookmark;
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_VIEW_BOOKMARKS_GUID, Header = "res:GoToPreviousBookmarkCommand", Icon = DsImagesAttribute.PreviousBookmark, InputGestureText = "res:ShortCutKeyCtrlK_CtrlP", Group = MenuConstants.GROUP_APP_MENU_BOOKMARKS_COMMANDS1, Order = 30)]
		sealed class GoToPreviousBookmarkCommand : MenuItemBase {
			readonly Lazy<MainMenuOperations> mainMenuOperations;
			[ImportingConstructor]
			public GoToPreviousBookmarkCommand(Lazy<MainMenuOperations> mainMenuOperations) => this.mainMenuOperations = mainMenuOperations;
			public override void Execute(IMenuItemContext context) => mainMenuOperations.Value.SelectPreviousBookmark();
			public override bool IsEnabled(IMenuItemContext context) => mainMenuOperations.Value.CanSelectPreviousBookmark;
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_VIEW_BOOKMARKS_GUID, Header = "res:GoToNextBookmarkCommand", Icon = DsImagesAttribute.NextBookmark, InputGestureText = "res:ShortCutKeyCtrlK_CtrlN", Group = MenuConstants.GROUP_APP_MENU_BOOKMARKS_COMMANDS1, Order = 40)]
		sealed class GoToNextBookmarkCommand : MenuItemBase {
			readonly Lazy<MainMenuOperations> mainMenuOperations;
			[ImportingConstructor]
			public GoToNextBookmarkCommand(Lazy<MainMenuOperations> mainMenuOperations) => this.mainMenuOperations = mainMenuOperations;
			public override void Execute(IMenuItemContext context) => mainMenuOperations.Value.SelectNextBookmark();
			public override bool IsEnabled(IMenuItemContext context) => mainMenuOperations.Value.CanSelectNextBookmark;
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_VIEW_BOOKMARKS_GUID, Header = "res:ClearBookmarksCommand", Icon = DsImagesAttribute.ClearBookmark, InputGestureText = "res:ShortCutKeyCtrlK_CtrlL", Group = MenuConstants.GROUP_APP_MENU_BOOKMARKS_COMMANDS1, Order = 50)]
		sealed class ClearBookmarksCommand : MenuItemBase {
			readonly Lazy<MainMenuOperations> mainMenuOperations;
			[ImportingConstructor]
			public ClearBookmarksCommand(Lazy<MainMenuOperations> mainMenuOperations) => this.mainMenuOperations = mainMenuOperations;
			public override void Execute(IMenuItemContext context) => mainMenuOperations.Value.ClearBookmarks();
			public override bool IsEnabled(IMenuItemContext context) => mainMenuOperations.Value.CanClearBookmarks;
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_VIEW_BOOKMARKS_GUID, Header = "res:GoToPreviousBookmarkWithSameLabelCommand", Icon = DsImagesAttribute.PreviousBookmarkInFolder, Group = MenuConstants.GROUP_APP_MENU_BOOKMARKS_COMMANDS2, Order = 0)]
		sealed class GoToPreviousBookmarkWithSameLabelCommand : MenuItemBase {
			readonly Lazy<MainMenuOperations> mainMenuOperations;
			[ImportingConstructor]
			public GoToPreviousBookmarkWithSameLabelCommand(Lazy<MainMenuOperations> mainMenuOperations) => this.mainMenuOperations = mainMenuOperations;
			public override void Execute(IMenuItemContext context) => mainMenuOperations.Value.SelectPreviousBookmarkWithSameLabel();
			public override bool IsEnabled(IMenuItemContext context) => mainMenuOperations.Value.CanSelectPreviousBookmarkWithSameLabel;
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_VIEW_BOOKMARKS_GUID, Header = "res:GoToNextBookmarkWithSameLabelCommand", Icon = DsImagesAttribute.NextBookmarkInFolder, Group = MenuConstants.GROUP_APP_MENU_BOOKMARKS_COMMANDS2, Order = 10)]
		sealed class GoToNextBookmarkWithSameLabelCommand : MenuItemBase {
			readonly Lazy<MainMenuOperations> mainMenuOperations;
			[ImportingConstructor]
			public GoToNextBookmarkWithSameLabelCommand(Lazy<MainMenuOperations> mainMenuOperations) => this.mainMenuOperations = mainMenuOperations;
			public override void Execute(IMenuItemContext context) => mainMenuOperations.Value.SelectNextBookmarkWithSameLabel();
			public override bool IsEnabled(IMenuItemContext context) => mainMenuOperations.Value.CanSelectNextBookmarkWithSameLabel;
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_VIEW_BOOKMARKS_GUID, Header = "res:GoToPreviousBookmarkInDocumentCommand", Icon = DsImagesAttribute.PreviousBookmarkInFile, Group = MenuConstants.GROUP_APP_MENU_BOOKMARKS_COMMANDS3, Order = 0)]
		sealed class GoToPreviousBookmarkInDocumentCommand : MenuItemBase {
			readonly Lazy<MainMenuOperations> mainMenuOperations;
			[ImportingConstructor]
			public GoToPreviousBookmarkInDocumentCommand(Lazy<MainMenuOperations> mainMenuOperations) => this.mainMenuOperations = mainMenuOperations;
			public override void Execute(IMenuItemContext context) => mainMenuOperations.Value.SelectPreviousBookmarkInDocument();
			public override bool IsEnabled(IMenuItemContext context) => mainMenuOperations.Value.CanSelectPreviousBookmarkInDocument;
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_VIEW_BOOKMARKS_GUID, Header = "res:GoToNextBookmarkInDocumentCommand", Icon = DsImagesAttribute.NextBookmarkInFile, Group = MenuConstants.GROUP_APP_MENU_BOOKMARKS_COMMANDS3, Order = 10)]
		sealed class GoToNextBookmarkInDocumentCommand : MenuItemBase {
			readonly Lazy<MainMenuOperations> mainMenuOperations;
			[ImportingConstructor]
			public GoToNextBookmarkInDocumentCommand(Lazy<MainMenuOperations> mainMenuOperations) => this.mainMenuOperations = mainMenuOperations;
			public override void Execute(IMenuItemContext context) => mainMenuOperations.Value.SelectNextBookmarkInDocument();
			public override bool IsEnabled(IMenuItemContext context) => mainMenuOperations.Value.CanSelectNextBookmarkInDocument;
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_VIEW_BOOKMARKS_GUID, Header = "res:ClearAllBookmarksInDocumentCommand", Group = MenuConstants.GROUP_APP_MENU_BOOKMARKS_COMMANDS3, Order = 20)]
		sealed class ClearAllBookmarksInDocumentCommand : MenuItemBase {
			readonly Lazy<MainMenuOperations> mainMenuOperations;
			[ImportingConstructor]
			public ClearAllBookmarksInDocumentCommand(Lazy<MainMenuOperations> mainMenuOperations) => this.mainMenuOperations = mainMenuOperations;
			public override void Execute(IMenuItemContext context) => mainMenuOperations.Value.ClearAllBookmarksInDocument();
			public override bool IsEnabled(IMenuItemContext context) => mainMenuOperations.Value.CanClearAllBookmarksInDocument;
		}
	}
}
