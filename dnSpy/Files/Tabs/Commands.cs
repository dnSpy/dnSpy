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
using System.IO;
using System.Windows.Input;
using System.Windows.Threading;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Plugin;
using dnSpy.Contracts.ToolBars;
using dnSpy.Shared.UI.Menus;
using dnSpy.Shared.UI.ToolBars;
using Microsoft.Win32;

namespace dnSpy.Files.Tabs {
	[ExportAutoLoaded]
	sealed class OpenFileInit : IAutoLoaded {
		readonly IFileTreeView fileTreeView;

		[ImportingConstructor]
		OpenFileInit(IWpfCommandManager wpfCommandManager, IFileTreeView fileTreeView) {
			this.fileTreeView = fileTreeView;
			var cmds = wpfCommandManager.GetCommands(CommandConstants.GUID_MAINWINDOW);
			cmds.Add(ApplicationCommands.Open, (s, e) => { Open(); e.Handled = true; }, (s, e) => e.CanExecute = true);
		}

		static readonly string DotNetAssemblyOrModuleFilter = ".NET Executables (*.exe, *.dll, *.netmodule, *.winmd)|*.exe;*.dll;*.netmodule;*.winmd|All files (*.*)|*.*";

		void Open() {
			var openDlg = new OpenFileDialog {
				Filter = DotNetAssemblyOrModuleFilter,
				RestoreDirectory = true,
				Multiselect = true,
			};
			if (openDlg.ShowDialog() != true)
				return;
			IDnSpyFile file = null;
			foreach (var filename in openDlg.FileNames) {
				if (File.Exists(filename))
					file = fileTreeView.FileManager.TryGetOrCreate(DnSpyFileInfo.CreateFile(filename)) ?? file;
			}
			if (file != null) {
				Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => {
					var node = fileTreeView.FindNode(file);
					fileTreeView.TreeView.SelectItems(new IFileTreeNodeData[] { node });
				}));
			}
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_FILE_GUID, Header = "_Open...", InputGestureText = "Ctrl+O", Icon = "Open", Group = MenuConstants.GROUP_APP_MENU_FILE_OPEN, Order = 0)]
	sealed class MenuFileOpenCommand : MenuItemCommand {
		public MenuFileOpenCommand()
			: base(ApplicationCommands.Open) {
		}
	}

	[ExportToolBarButton(OwnerGuid = ToolBarConstants.APP_TB_GUID, Icon = "Open", ToolTip = "Open (Ctrl+O)", Group = ToolBarConstants.GROUP_APP_TB_MAIN_OPEN, Order = 0)]
	sealed class ToolbarFileOpenCommand : ToolBarButtonCommand {
		public ToolbarFileOpenCommand()
			: base(ApplicationCommands.Open) {
		}
	}
}
