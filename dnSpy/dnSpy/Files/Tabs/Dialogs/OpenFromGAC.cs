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
using System.Linq;
using System.Windows;
using dnlib.DotNet;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Files.TreeView;

namespace dnSpy.Files.Tabs.Dialogs {
	[Export(typeof(IOpenFromGAC))]
	sealed class OpenFromGAC : IOpenFromGAC {
		readonly IAppWindow appWindow;
		readonly IFileTreeView fileTreeView;

		[ImportingConstructor]
		OpenFromGAC(IAppWindow appWindow, IFileTreeView fileTreeView) {
			this.appWindow = appWindow;
			this.fileTreeView = fileTreeView;
		}

		public string[] GetPaths(Window ownerWindow) {
			var win = new OpenFromGACDlg();
			const bool syntaxHighlight = true;
			var vm = new OpenFromGACVM(syntaxHighlight);
			win.DataContext = vm;
			win.Owner = ownerWindow ?? appWindow.MainWindow;
			if (win.ShowDialog() != true)
				return Array.Empty<string>();
			return win.SelectedItems.Select(a => a.Path).ToArray();
		}

		public ModuleDef[] OpenAssemblies(bool selectAssembly, Window ownerWindow) =>
			OpenFilesHelper.OpenFiles(fileTreeView, appWindow.MainWindow, GetPaths(ownerWindow), selectAssembly).Select(a => a.ModuleDef).Where(a => a != null).ToArray();
	}
}
