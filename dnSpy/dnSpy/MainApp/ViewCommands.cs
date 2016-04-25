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

using System.ComponentModel.Composition;
using System.Windows.Input;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Plugin;
using dnSpy.Shared.Controls;
using dnSpy.Shared.Menus;

namespace dnSpy.MainApp {
	[ExportAutoLoaded]
	sealed class FullScreenInit : IAutoLoaded {
		static readonly RoutedCommand CollapseUnusedNodesCommand = new RoutedCommand("CollapseUnusedNodesCommand", typeof(FullScreenInit));

		[ImportingConstructor]
		FullScreenInit(IAppWindow appWindow, IFileTreeView fileTreeView) {
			var fullScreenCommand = new FullScreenCommand(appWindow);
			appWindow.MainWindowCommands.Add(MetroWindow.FullScreenCommand, (s, e) => fullScreenCommand.FullScreen(), (s, e) => e.CanExecute = true, ModifierKeys.Shift | ModifierKeys.Alt, Key.Enter);
			appWindow.MainWindowCommands.Add(CollapseUnusedNodesCommand, (s, e) => fileTreeView.TreeView.CollapseUnusedNodes(), (s, e) => e.CanExecute = true, ModifierKeys.Control | ModifierKeys.Shift, Key.W);
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_VIEW_GUID, Header = "res:FullScreenCommand", InputGestureText = "res:FullScreenKey", Icon = "FullScreen", Group = MenuConstants.GROUP_APP_MENU_VIEW_OPTS, Order = 20)]
	sealed class FullScreenCommand : MenuItemCommand {
		readonly MetroWindow window;

		[ImportingConstructor]
		public FullScreenCommand(IAppWindow appWindow)
			: base(MetroWindow.FullScreenCommand) {
			this.window = (MetroWindow)appWindow.MainWindow;
		}

		public override bool IsChecked(IMenuItemContext context) {
			return window.IsFullScreen;
		}

		public void FullScreen() {
			window.IsFullScreen = !window.IsFullScreen;
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_FILE_GUID, Header = "res:ExitAppCommand", Icon = "Close", InputGestureText = "res:ExitAppKey", Group = MenuConstants.GROUP_APP_MENU_FILE_EXIT, Order = 1000000)]
	sealed class MenuFileExitCommand : MenuItemCommand {
		public MenuFileExitCommand()
			: base(ApplicationCommands.Close) {
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_VIEW_GUID, Header = "res:CollapseTreeViewNodesCommand", InputGestureText = "res:ShortCutKeyCtrlShiftW", Icon = "OneLevelUp", Group = MenuConstants.GROUP_APP_MENU_VIEW_OPTS, Order = 30)]
	sealed class CollapseTreeViewCommand : MenuItemBase {
		readonly IFileTreeView fileTreeView;

		[ImportingConstructor]
		CollapseTreeViewCommand(IFileTreeView fileTreeView) {
			this.fileTreeView = fileTreeView;
		}

		public override void Execute(IMenuItemContext context) {
			fileTreeView.TreeView.CollapseUnusedNodes();
		}
	}
}
