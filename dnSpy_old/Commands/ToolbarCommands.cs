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

using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using dnSpy.Contracts.ToolBars;
using dnSpy.Shared.UI.ToolBars;
using ICSharpCode.ILSpy;

namespace dnSpy.Commands {
	[ExportToolBarObject(OwnerGuid = ToolBarConstants.APP_TB_GUID, Group = ToolBarConstants.GROUP_APP_TB_MAIN_MENU, Order = 0)]
	sealed class MainMenuToolbarCommand : ToolBarObjectBase {
		public override object GetUIObject(IToolBarItemContext context, IInputElement commandTarget) {
			return MainWindow.Instance.mainMenu;
		}
	}

	[ExportToolBarObject(OwnerGuid = ToolBarConstants.APP_TB_GUID, Group = ToolBarConstants.GROUP_APP_TB_MAIN_LANGUAGE, Order = 0)]
	sealed class LanguageComboBoxToolbarCommand : ToolBarObjectBase {
		public override object GetUIObject(IToolBarItemContext context, IInputElement commandTarget) {
			return MainWindow.Instance.languageComboBox;
		}
	}

	[ExportToolBarButton(OwnerGuid = ToolBarConstants.APP_TB_GUID, Icon = "FullScreen", ToolTip = "Full Screen", Header = "Full Screen", IsToggleButton = true, Group = ToolBarConstants.GROUP_APP_TB_MAIN_FULLSCREEN, Order = 0)]
	sealed class FullScreenToolbarCommand : ToolBarButtonBase, IToolBarToggleButton {
		public FullScreenToolbarCommand() {
			MainWindow.Instance.IsFullScreenChanged += (s, e) => MainWindow.Instance.UpdateToolbar();
		}

		public Binding GetBinding(IToolBarItemContext context) {
			return new Binding("IsFullScreen") {
				Source = MainWindow.Instance,
			};
		}

		public override void Execute(IToolBarItemContext context) {
		}

		public override bool IsVisible(IToolBarItemContext context) {
			return MainWindow.Instance.IsFullScreen;
		}
	}

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
}
