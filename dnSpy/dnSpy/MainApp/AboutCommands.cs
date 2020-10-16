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

using System.ComponentModel.Composition;
using System.Diagnostics;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Menus;
using dnSpy.Properties;

namespace dnSpy.MainApp {
	static class AboutHelpers {
		public const string BASE_URL = @"https://github.com/dnSpy/dnSpy/";
		public const string BUILD_URL = @"https://github.com/dnSpy/dnSpy/actions";

		public static void OpenWebPage(string url, IMessageBoxService messageBoxService) {
			try {
				Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
			}
			catch {
				messageBoxService.Show(dnSpy_Resources.CouldNotStartBrowser);
			}
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_HELP_GUID, Header = "res:About_LatestRelease", Group = MenuConstants.GROUP_APP_MENU_HELP_LINKS, Order = 0)]
	sealed class OpenReleasesUrlCommand : MenuItemBase {
		readonly IMessageBoxService messageBoxService;

		[ImportingConstructor]
		OpenReleasesUrlCommand(IMessageBoxService messageBoxService) => this.messageBoxService = messageBoxService;

		public override void Execute(IMenuItemContext context) =>
			AboutHelpers.OpenWebPage(AboutHelpers.BASE_URL + @"releases", messageBoxService);
	}

	//[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_HELP_GUID, Header = "res:About_LatestBuild", Group = MenuConstants.GROUP_APP_MENU_HELP_LINKS, Order = 10)]
	sealed class OpenLatestBuildUrlCommand : MenuItemBase {
		readonly IMessageBoxService messageBoxService;

		[ImportingConstructor]
		OpenLatestBuildUrlCommand(IMessageBoxService messageBoxService) => this.messageBoxService = messageBoxService;

		public override void Execute(IMenuItemContext context) =>
			AboutHelpers.OpenWebPage(AboutHelpers.BUILD_URL, messageBoxService);
	}

	//[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_HELP_GUID, Header = "res:About_Issues", Group = MenuConstants.GROUP_APP_MENU_HELP_LINKS, Order = 20)]
	sealed class OpenIssuesUrlCommand : MenuItemBase {
		readonly IMessageBoxService messageBoxService;

		[ImportingConstructor]
		OpenIssuesUrlCommand(IMessageBoxService messageBoxService) => this.messageBoxService = messageBoxService;

		public override void Execute(IMenuItemContext context) =>
			AboutHelpers.OpenWebPage(AboutHelpers.BASE_URL + @"issues", messageBoxService);
	}

	//[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_HELP_GUID, Header = "res:About_Wiki", Group = MenuConstants.GROUP_APP_MENU_HELP_LINKS, Order = 30)]
	sealed class OpenWikiUrlCommand : MenuItemBase {
		readonly IMessageBoxService messageBoxService;

		[ImportingConstructor]
		OpenWikiUrlCommand(IMessageBoxService messageBoxService) => this.messageBoxService = messageBoxService;

		public override void Execute(IMenuItemContext context) =>
			AboutHelpers.OpenWebPage(AboutHelpers.BASE_URL + @"wiki", messageBoxService);
	}

	//[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_HELP_GUID, Header = "res:About_SourceCode", Group = MenuConstants.GROUP_APP_MENU_HELP_LINKS, Order = 40)]
	sealed class OpenSourceCodeUrlCommand : MenuItemBase {
		readonly IMessageBoxService messageBoxService;

		[ImportingConstructor]
		OpenSourceCodeUrlCommand(IMessageBoxService messageBoxService) => this.messageBoxService = messageBoxService;

		public override void Execute(IMenuItemContext context) =>
			AboutHelpers.OpenWebPage(AboutHelpers.BASE_URL, messageBoxService);
	}
}
