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

using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Plugin;
using dnSpy.Contracts.ToolBars;
using dnSpy.Shared.UI.MVVM;
using dnSpy.Shared.UI.ToolBars;

namespace dnSpy.Files.Tabs {
	[ExportToolBarButton(OwnerGuid = ToolBarConstants.APP_TB_GUID, ToolTip = "Navigate Backward (Backspace)", Icon = "Backward", Group = ToolBarConstants.GROUP_APP_TB_MAIN_NAVIGATION, Order = 0)]
	sealed class BrowseBackCommand : ToolBarButtonCommand {
		public BrowseBackCommand()
			: base(NavigationCommands.BrowseBack) {
		}
	}

	[ExportToolBarButton(OwnerGuid = ToolBarConstants.APP_TB_GUID, ToolTip = "Navigate Forward (Alt+Right)", Icon = "Forward", Group = ToolBarConstants.GROUP_APP_TB_MAIN_NAVIGATION, Order = 10)]
	sealed class BrowseForwardCommand : ToolBarButtonCommand {
		public BrowseForwardCommand()
			: base(NavigationCommands.BrowseForward) {
		}
	}

	[ExportAutoLoaded]
	sealed class NavigationCommandInstaller : IAutoLoaded {
		readonly IFileTabManager fileTabManager;

		[ImportingConstructor]
		NavigationCommandInstaller(IFileTabManager fileTabManager, IWpfCommandManager wpfCommandManager) {
			this.fileTabManager = fileTabManager;
			Debug.Assert(Application.Current != null && Application.Current.MainWindow != null);
			var cmds = wpfCommandManager.GetCommands(CommandConstants.GUID_MAINWINDOW);
			cmds.Add(NavigationCommands.BrowseBack, new RelayCommand(a => BrowseBack(), a => CanBrowseBack));
			cmds.Add(NavigationCommands.BrowseForward, new RelayCommand(a => BrowseForward(), a => CanBrowseForward));
		}

		bool CanBrowseBack {
			get {
				var tab = fileTabManager.ActiveTab;
				return tab != null && tab.CanNavigateBackward;
			}
		}

		void BrowseBack() {
			if (!CanBrowseBack)
				return;
			fileTabManager.ActiveTab.NavigateBackward();
		}

		bool CanBrowseForward {
			get {
				var tab = fileTabManager.ActiveTab;
				return tab != null && tab.CanNavigateForward;
			}
		}

		void BrowseForward() {
			if (!CanBrowseForward)
				return;
			fileTabManager.ActiveTab.NavigateForward();
		}
	}
}
