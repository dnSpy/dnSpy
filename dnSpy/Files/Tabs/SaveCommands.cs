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
using System.ComponentModel.Composition;
using System.Windows.Input;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Plugin;
using dnSpy.Contracts.Tabs;
using dnSpy.Shared.UI.Menus;

namespace dnSpy.Files.Tabs {
	[ExportAutoLoaded]
	sealed class SaveCommandInit : IAutoLoaded {
		[ImportingConstructor]
		SaveCommandInit(ISaveManager saveManager, IWpfCommandManager wpfCommandManager, IFileTabManager fileTabManager) {
			var cmds = wpfCommandManager.GetCommands(CommandConstants.GUID_MAINWINDOW);
			cmds.Add(ApplicationCommands.Save, (s, e) => saveManager.Save(fileTabManager.ActiveTab), (s, e) => e.CanExecute = saveManager.CanSave(fileTabManager.ActiveTab));
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_FILE_GUID, Icon = "Save", Group = MenuConstants.GROUP_APP_MENU_FILE_SAVE, Order = 0)]
	sealed class MenuSaveCommand : MenuItemCommand {
		readonly ISaveManager saveManager;
		readonly IFileTabManager fileTabManager;

		[ImportingConstructor]
		MenuSaveCommand(ISaveManager saveManager, IFileTabManager fileTabManager)
			: base(ApplicationCommands.Save) {
			this.saveManager = saveManager;
			this.fileTabManager = fileTabManager;
		}

		public override string GetHeader(IMenuItemContext context) {
			return saveManager.GetMenuHeader(fileTabManager.ActiveTab);
		}
	}

	[ExportMenuItem(InputGestureText = "Ctrl+S", Icon = "Save", Group = MenuConstants.GROUP_CTX_TABS_CLOSE, Order = 0)]
	sealed class SaveTabCtxMenuCommand : MenuItemCommand {
		readonly ISaveManager saveManager;
		readonly IFileTabManager fileTabManager;

		[ImportingConstructor]
		SaveTabCtxMenuCommand(ISaveManager saveManager, IFileTabManager fileTabManager)
			: base(ApplicationCommands.Save) {
			this.saveManager = saveManager;
			this.fileTabManager = fileTabManager;
		}

		public override bool IsVisible(IMenuItemContext context) {
			return GetTabGroup(context) != null;
		}

		public override string GetHeader(IMenuItemContext context) {
			return saveManager.GetMenuHeader(GetFileTab(context));
		}

		ITabGroup GetTabGroup(IMenuItemContext context) {
			if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_FILES_TABCONTROL_GUID))
				return null;
			var g = context.FindByType<ITabGroup>();
			return g != null && fileTabManager.Owns(g) ? g : null;
		}

		IFileTab GetFileTab(IMenuItemContext context) {
			var g = GetTabGroup(context);
			return g == null ? null : fileTabManager.TryGetFileTab(g.ActiveTabContent);
		}
	}
}
