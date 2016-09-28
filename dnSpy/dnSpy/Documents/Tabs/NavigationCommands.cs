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
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.ToolBars;

namespace dnSpy.Documents.Tabs {
	[ExportToolBarButton(OwnerGuid = ToolBarConstants.APP_TB_GUID, ToolTip = "res:NavigateBackCommand", Icon = DsImagesAttribute.Backwards, Group = ToolBarConstants.GROUP_APP_TB_MAIN_NAVIGATION, Order = 0)]
	sealed class BrowseBackCommand : ToolBarButtonCommand {
		public BrowseBackCommand()
			: base(NavigationCommands.BrowseBack) {
		}
	}

	[ExportToolBarButton(OwnerGuid = ToolBarConstants.APP_TB_GUID, ToolTip = "res:NavigateForwardCommand", Icon = DsImagesAttribute.Forwards, Group = ToolBarConstants.GROUP_APP_TB_MAIN_NAVIGATION, Order = 10)]
	sealed class BrowseForwardCommand : ToolBarButtonCommand {
		public BrowseForwardCommand()
			: base(NavigationCommands.BrowseForward) {
		}
	}

	[ExportAutoLoaded]
	sealed class NavigationCommandInstaller : IAutoLoaded {
		readonly IDocumentTabService documentTabService;

		[ImportingConstructor]
		NavigationCommandInstaller(IDocumentTabService documentTabService, IAppWindow appWindow) {
			this.documentTabService = documentTabService;
			Debug.Assert(Application.Current != null && Application.Current.MainWindow != null);
			var cmds = appWindow.MainWindowCommands;
			cmds.Add(NavigationCommands.BrowseBack, new RelayCommand(a => BrowseBack(), a => CanBrowseBack));
			cmds.Add(NavigationCommands.BrowseForward, new RelayCommand(a => BrowseForward(), a => CanBrowseForward));
		}

		bool CanBrowseBack => documentTabService.ActiveTab?.CanNavigateBackward == true;

		void BrowseBack() {
			if (!CanBrowseBack)
				return;
			documentTabService.ActiveTab.NavigateBackward();
		}

		bool CanBrowseForward => documentTabService.ActiveTab?.CanNavigateForward == true;

		void BrowseForward() {
			if (!CanBrowseForward)
				return;
			documentTabService.ActiveTab.NavigateForward();
		}
	}
}
