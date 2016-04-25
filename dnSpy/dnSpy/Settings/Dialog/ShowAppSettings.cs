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

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Settings.Dialog;
using dnSpy.Shared.Menus;

namespace dnSpy.Settings.Dialog {
	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_VIEW_GUID, Header = "res:OptionsCommand", Icon = "Settings", Group = MenuConstants.GROUP_APP_MENU_VIEW_OPTSDLG, Order = 1000000)]
	sealed class ShowOptionsCommand : MenuItemBase {
		readonly IAppWindow appWindow;
		readonly Lazy<IAppSettingsTabCreator>[] tabCreators;
		readonly Lazy<IAppSettingsModifiedListener, IAppSettingsModifiedListenerMetadata>[] listeners;

		[ImportingConstructor]
		ShowOptionsCommand(IAppWindow appWindow, [ImportMany] IEnumerable<Lazy<IAppSettingsTabCreator>> mefTabCreators, [ImportMany] IEnumerable<Lazy<IAppSettingsModifiedListener, IAppSettingsModifiedListenerMetadata>> mefListeners) {
			this.appWindow = appWindow;
			this.tabCreators = mefTabCreators.ToArray();
			this.listeners = mefListeners.OrderBy(a => a.Metadata.Order).ToArray();
		}

		public override void Execute(IMenuItemContext context) {
			var tabs = tabCreators.SelectMany(a => a.Value.Create()).OrderBy(a => a.Order).ToArray();
			var dlg = new AppSettingsDlg(tabs);
			dlg.Owner = appWindow.MainWindow;
			bool saveSettings = dlg.ShowDialog() == true;
			var appRefreshSettings = new AppRefreshSettings();
			foreach (var tab in tabs)
				tab.OnClosed(saveSettings, appRefreshSettings);
			if (saveSettings) {
				foreach (var listener in listeners)
					listener.Value.OnSettingsModified(appRefreshSettings);
			}
		}
	}
}
