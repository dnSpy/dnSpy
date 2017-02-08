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
using System.IO;
using System.Linq;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.UI;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Debugger.Dialogs.DebugProgram;

namespace dnSpy.Debugger.DbgUI {
	[Export(typeof(StartDebuggingOptionsProvider))]
	sealed class StartDebuggingOptionsProvider {
		readonly IAppWindow appWindow;
		readonly IDocumentTabService documentTabService;
		readonly Lazy<StartDebuggingOptionsPageProvider>[] startDebuggingOptionsPageProviders;

		[ImportingConstructor]
		StartDebuggingOptionsProvider(IAppWindow appWindow, IDocumentTabService documentTabService, [ImportMany] IEnumerable<Lazy<StartDebuggingOptionsPageProvider>> startDebuggingOptionsPageProviders) {
			this.appWindow = appWindow;
			this.documentTabService = documentTabService;
			this.startDebuggingOptionsPageProviders = startDebuggingOptionsPageProviders.ToArray();
		}

		StartDebuggingOptionsPage[] GetStartDebuggingOptionsPages(StartDebuggingOptionsPageContext context) {
			var list = new List<StartDebuggingOptionsPage>();
			foreach (var provider in startDebuggingOptionsPageProviders)
				list.AddRange(provider.Value.Create(context));
			return list.OrderBy(a => a.DisplayOrder).ToArray();
		}

		string GetCurrentFilename() {
			var filename = (documentTabService.ActiveTab?.Content.Nodes ?? Array.Empty<DocumentTreeNodeData>()).FirstOrDefault().GetDocumentNode()?.Document.Filename ?? string.Empty;
			if (File.Exists(filename))
				return filename;
			return string.Empty;
		}

		public StartDebuggingOptions GetStartDebuggingOptions() {
			var context = new StartDebuggingOptionsPageContext(GetCurrentFilename());
			var pages = GetStartDebuggingOptionsPages(context);
			if (pages.Length == 0)
				return null;

			var dlg = new DebugProgramDlg();
			var vm = new DebugProgramVM(pages);
			dlg.DataContext = vm;
			dlg.Owner = appWindow.MainWindow;
			var res = dlg.ShowDialog();
			vm.Close();
			if (res != true)
				return null;
			return vm.StartDebuggingOptions;
		}
	}
}
