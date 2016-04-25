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
using dnSpy.Contracts.App;
using dnSpy.Contracts.Menus;
using dnSpy.Properties;
using dnSpy.Shared.Menus;

namespace dnSpy.MainApp {
	static class AboutHelpers {
		public const string BASE_URL = @"https://github.com/0xd4d/dnSpy/";
		public const string BUILD_URL = @"https://ci.appveyor.com/project/0xd4d/dnspy/build/artifacts";

		public static void OpenWebPage(string url, IMessageBoxManager messageBoxManager) {
			try {
				Process.Start(url);
			}
			catch {
				messageBoxManager.Show(dnSpy_Resources.CouldNotStartBrowser);
			}
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_HELP_GUID, Header = "res:About_LatestRelease", Group = MenuConstants.GROUP_APP_MENU_HELP_LINKS, Order = 0)]
	sealed class OpenReleasesUrlCommand : MenuItemBase {
		readonly IMessageBoxManager messageBoxManager;

		[ImportingConstructor]
		OpenReleasesUrlCommand(IMessageBoxManager messageBoxManager) {
			this.messageBoxManager = messageBoxManager;
		}

		public override void Execute(IMenuItemContext context) {
			AboutHelpers.OpenWebPage(AboutHelpers.BASE_URL + @"releases", messageBoxManager);
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_HELP_GUID, Header = "res:About_LatestBuild", Group = MenuConstants.GROUP_APP_MENU_HELP_LINKS, Order = 10)]
	sealed class OpenLatestBuildUrlCommand : MenuItemBase {
		readonly IMessageBoxManager messageBoxManager;

		[ImportingConstructor]
		OpenLatestBuildUrlCommand(IMessageBoxManager messageBoxManager) {
			this.messageBoxManager = messageBoxManager;
		}

		public override void Execute(IMenuItemContext context) {
			AboutHelpers.OpenWebPage(AboutHelpers.BUILD_URL, messageBoxManager);
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_HELP_GUID, Header = "res:About_Issues", Group = MenuConstants.GROUP_APP_MENU_HELP_LINKS, Order = 20)]
	sealed class OpenIssuesUrlCommand : MenuItemBase {
		readonly IMessageBoxManager messageBoxManager;

		[ImportingConstructor]
		OpenIssuesUrlCommand(IMessageBoxManager messageBoxManager) {
			this.messageBoxManager = messageBoxManager;
		}

		public override void Execute(IMenuItemContext context) {
			AboutHelpers.OpenWebPage(AboutHelpers.BASE_URL + @"issues", messageBoxManager);
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_HELP_GUID, Header = "res:About_Wiki", Group = MenuConstants.GROUP_APP_MENU_HELP_LINKS, Order = 30)]
	sealed class OpenWikiUrlCommand : MenuItemBase {
		readonly IMessageBoxManager messageBoxManager;

		[ImportingConstructor]
		OpenWikiUrlCommand(IMessageBoxManager messageBoxManager) {
			this.messageBoxManager = messageBoxManager;
		}

		public override void Execute(IMenuItemContext context) {
			AboutHelpers.OpenWebPage(AboutHelpers.BASE_URL + @"wiki", messageBoxManager);
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_HELP_GUID, Header = "res:About_SourceCode", Group = MenuConstants.GROUP_APP_MENU_HELP_LINKS, Order = 40)]
	sealed class OpenSourceCodeUrlCommand : MenuItemBase {
		readonly IMessageBoxManager messageBoxManager;

		[ImportingConstructor]
		OpenSourceCodeUrlCommand(IMessageBoxManager messageBoxManager) {
			this.messageBoxManager = messageBoxManager;
		}

		public override void Execute(IMenuItemContext context) {
			AboutHelpers.OpenWebPage(AboutHelpers.BASE_URL, messageBoxManager);
		}
	}
}
