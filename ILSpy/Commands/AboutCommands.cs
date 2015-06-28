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
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace ICSharpCode.ILSpy.Commands
{
	static class AboutHelpers
	{
		public static void OpenWebPage(string url)
		{
			try {
				Process.Start(url);
			}
			catch {
				MainWindow.Instance.ShowMessageBox("Could not start browser");
			}
		}
	}

	[ExportMainMenuCommand(Menu = "_Help", MenuHeader = "_Latest Release", MenuOrder = 99900)]
	sealed class OpenReleasesUrlCommand : SimpleCommand
	{
		public override void Execute(object parameter)
		{
			AboutHelpers.OpenWebPage(@"https://github.com/0xd4d/dnSpy/releases");
		}
	}

	[ExportMainMenuCommand(Menu = "_Help", MenuHeader = "_Source Code", MenuOrder = 99910)]
	sealed class OpenSourceCodeUrlCommand : SimpleCommand
	{
		public override void Execute(object parameter)
		{
			AboutHelpers.OpenWebPage(@"https://github.com/0xd4d/dnSpy");
		}
	}

	[ExportMainMenuCommand(Menu = "_Help", MenuHeader = "_Wiki", MenuOrder = 99920)]
	sealed class OpenWikiUrlCommand : SimpleCommand
	{
		public override void Execute(object parameter)
		{
			AboutHelpers.OpenWebPage(@"https://github.com/0xd4d/dnSpy/wiki");
		}
	}

	[ExportMainMenuCommand(Menu = "_Help", MenuHeader = "_Issues", MenuOrder = 99930)]
	sealed class OpenIssuesUrlCommand : SimpleCommand
	{
		public override void Execute(object parameter)
		{
			AboutHelpers.OpenWebPage(@"https://github.com/0xd4d/dnSpy/issues");
		}
	}
}
