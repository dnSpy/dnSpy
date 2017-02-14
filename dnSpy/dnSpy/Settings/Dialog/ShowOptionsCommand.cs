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

using System;
using System.ComponentModel.Composition;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Settings.Dialog;

namespace dnSpy.Settings.Dialog {
	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_VIEW_GUID, Header = "res:OptionsCommand", Icon = DsImagesAttribute.Settings, Group = MenuConstants.GROUP_APP_MENU_VIEW_OPTSDLG, Order = 1000000)]
	sealed class ShowOptionsCommand : MenuItemBase {
		readonly Lazy<IAppSettingsService> appSettingsService;

		[ImportingConstructor]
		ShowOptionsCommand(Lazy<IAppSettingsService> appSettingsService) => this.appSettingsService = appSettingsService;

		public override void Execute(IMenuItemContext context) => appSettingsService.Value.Show();
	}
}
