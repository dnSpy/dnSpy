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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Input;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Plugin;
using dnSpy.Contracts.ToolWindows;
using dnSpy.Contracts.ToolWindows.App;
using dnSpy.Contracts.TreeView;
using dnSpy.Shared.UI.Menus;

namespace dnSpy.MainApp {
	[Export(typeof(IMainToolWindowContentCreator))]
	sealed class FileTreeViewWindowContentCreator : IMainToolWindowContentCreator {
		readonly IFileTreeView fileTreeView;

		FileTreeViewWindowContent FileTreeViewWindowContent {
			get { return fileTreeViewWindowContent ?? (fileTreeViewWindowContent = new FileTreeViewWindowContent(fileTreeView.TreeView)); }
		}
		FileTreeViewWindowContent fileTreeViewWindowContent;

		[ImportingConstructor]
		FileTreeViewWindowContentCreator(IFileTreeView fileTreeView) {
			this.fileTreeView = fileTreeView;
		}

		public IEnumerable<ToolWindowContentInfo> ContentInfos {
			get { yield return new ToolWindowContentInfo(FileTreeViewWindowContent.THE_GUID, AppToolWindowLocation.Left, AppToolWindowConstants.DEFAULT_CONTENT_ORDER_LEFT_FILES, true); }
		}

		public IToolWindowContent GetOrCreate(Guid guid) {
			if (guid == FileTreeViewWindowContent.THE_GUID)
				return FileTreeViewWindowContent;
			return null;
		}
	}

	sealed class FileTreeViewWindowContent : IToolWindowContent, IFocusable {
		public static readonly Guid THE_GUID = new Guid("5495EE9F-1EF2-45F3-A320-22A89BFDF731");

		public IInputElement FocusedElement {
			get { return null; }
		}

		public Guid Guid {
			get { return THE_GUID; }
		}

		public string Title {
			get { return "Assembly Explorer"; }
		}

		public object ToolTip {
			get { return null; }
		}

		public object UIObject {
			get { return treeView.UIObject; }
		}

		public bool CanFocus {
			get { return true; }
		}

		readonly ITreeView treeView;

		public FileTreeViewWindowContent(ITreeView treeView) {
			this.treeView = treeView;
		}

		public void OnVisibilityChanged(ToolWindowContentVisibilityEvent visEvent) {
		}

		public void Focus() {
			treeView.Focus();
		}
	}

	[ExportAutoLoaded]
	sealed class ShowFileTreeViewCommandLoader : IAutoLoaded {
		public static readonly RoutedCommand ShowFileTreeViewRoutedCommand = new RoutedCommand("ShowFileTreeViewRoutedCommand", typeof(ShowFileTreeViewCommandLoader));

		[ImportingConstructor]
		ShowFileTreeViewCommandLoader(IWpfCommandManager wpfCommandManager, IMainToolWindowManager mainToolWindowManager) {
			wpfCommandManager.GetCommands(CommandConstants.GUID_MAINWINDOW).Add(
				ShowFileTreeViewRoutedCommand,
				(s, e) => mainToolWindowManager.Add(FileTreeViewWindowContent.THE_GUID, AppToolWindowLocation.Left),
				(s, e) => e.CanExecute = true,
				ModifierKeys.Control | ModifierKeys.Alt, Key.L);
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_VIEW_GUID, Header = "Assembly Ex_plorer", InputGestureText = "Ctrl+Alt+L", Icon = "Assembly", Group = MenuConstants.GROUP_APP_MENU_VIEW_WINDOWS, Order = 10)]
	sealed class ShowFileTreeViewCommand : MenuItemCommand {
		ShowFileTreeViewCommand()
			: base(ShowFileTreeViewCommandLoader.ShowFileTreeViewRoutedCommand) {
		}
	}
}
