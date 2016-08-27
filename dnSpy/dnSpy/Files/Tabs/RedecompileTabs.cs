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
using System.ComponentModel.Composition;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.Files.Tabs;

namespace dnSpy.Files.Tabs {
	[ExportAutoLoaded(LoadType = AutoLoadedLoadType.BeforeExtensions)]
	sealed class RedecompileTabs : IAutoLoaded {
		readonly IFileTabManager fileTabManager;
		readonly IAppWindow appWindow;

		[ImportingConstructor]
		RedecompileTabs(IFileTabManager fileTabManager, IDecompilerManager decompilerManager, IAppWindow appWindow) {
			this.fileTabManager = fileTabManager;
			this.appWindow = appWindow;
			decompilerManager.DecompilerChanged += DecompilerManager_DecompilerChanged;
		}

		void DecompilerManager_DecompilerChanged(object sender, EventArgs e) {
			if (!appWindow.AppLoaded)
				return;
			var tab = fileTabManager.ActiveTab;
			var decompilerContent = tab?.Content as IDecompilerTabContent;
			if (decompilerContent == null)
				return;
			var decompilerManager = (IDecompilerManager)sender;
			if (decompilerContent.Decompiler == decompilerManager.Decompiler)
				return;
			decompilerContent.Decompiler = decompilerManager.Decompiler;
			fileTabManager.Refresh(new IFileTab[] { tab });
		}
	}
}
