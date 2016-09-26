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
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Extension;

namespace dnSpy.Documents.Tabs {
	[ExportAutoLoaded(LoadType = AutoLoadedLoadType.BeforeExtensions)]
	sealed class RedecompileTabs : IAutoLoaded {
		readonly IDocumentTabService documentTabService;
		readonly IAppWindow appWindow;

		[ImportingConstructor]
		RedecompileTabs(IDocumentTabService documentTabService, IDecompilerService decompilerService, IAppWindow appWindow) {
			this.documentTabService = documentTabService;
			this.appWindow = appWindow;
			decompilerService.DecompilerChanged += DecompilerManager_DecompilerChanged;
		}

		void DecompilerManager_DecompilerChanged(object sender, EventArgs e) {
			if (!appWindow.AppLoaded)
				return;
			var tab = documentTabService.ActiveTab;
			var decompilerContent = tab?.Content as IDecompilerTabContent;
			if (decompilerContent == null)
				return;
			var decompilerService = (IDecompilerService)sender;
			if (decompilerContent.Decompiler == decompilerService.Decompiler)
				return;
			decompilerContent.Decompiler = decompilerService.Decompiler;
			documentTabService.Refresh(new IDocumentTab[] { tab });
		}
	}
}
