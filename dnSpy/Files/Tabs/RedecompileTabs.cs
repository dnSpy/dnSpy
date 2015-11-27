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
using System.Windows.Threading;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.Plugin;

namespace dnSpy.Files.Tabs {
	[ExportAutoLoaded]
	sealed class RedecompileTabs : IAutoLoaded {
		readonly IFileTabManager fileTabManager;
		readonly IAppWindow appWindow;

		[ImportingConstructor]
		RedecompileTabs(IFileTabManager fileTabManager, ILanguageManager languageManager, IAppWindow appWindow) {
			this.fileTabManager = fileTabManager;
			this.appWindow = appWindow;
			languageManager.LanguageChanged += LanguageManager_LanguageChanged;
		}

		void LanguageManager_LanguageChanged(object sender, EventArgs e) {
			if (!appWindow.AppLoaded)
				return;
			Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => {
				var tab = fileTabManager.ActiveTab;
				if (tab != null)
					fileTabManager.CheckRefresh(new IFileTab[] { tab });
			}));
		}
	}
}
