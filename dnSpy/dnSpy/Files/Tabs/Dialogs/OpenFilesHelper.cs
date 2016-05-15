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
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Files.TreeView;

namespace dnSpy.Files.Tabs.Dialogs {
	static class OpenFilesHelper {
		internal static IDnSpyFile[] OpenFiles(IFileTreeView fileTreeView, Window ownerWindow, IEnumerable<string> filenames, bool selectFile = true) {
			var fileLoader = new FileLoader(fileTreeView.FileManager, ownerWindow);
			var loadedFiles = fileLoader.Load(filenames.Select(a => new FileToLoad(DnSpyFileInfo.CreateFile(a))));
			var file = loadedFiles.Length == 0 ? null : loadedFiles[loadedFiles.Length - 1];
			if (selectFile && file != null) {
				Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => {
					var node = fileTreeView.FindNode(file);
					if (node != null)
						fileTreeView.TreeView.SelectItems(new IFileTreeNodeData[] { node });
				}));
			}
			return loadedFiles;
		}
	}
}
