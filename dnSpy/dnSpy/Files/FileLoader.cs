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

using System.Collections.Generic;
using System.Linq;
using System.Windows;
using dnSpy.Contracts.Files;
using dnSpy.Contracts.MVVM.Dialogs;

namespace dnSpy.Files {
	sealed class FileLoader : IProgressTask, IDnSpyFileLoader {
		readonly IFileManager fileManager;
		readonly Window ownerWindow;
		readonly HashSet<IDnSpyFile> hash;
		readonly List<IDnSpyFile> loadedFiles;
		FileToLoad[] filesToLoad;

		public bool IsIndeterminate => false;
		public double ProgressMinimum => 0;
		public double ProgressMaximum { get; set; }

		public FileLoader(IFileManager fileManager, Window ownerWindow) {
			this.fileManager = fileManager;
			this.ownerWindow = ownerWindow;
			this.loadedFiles = new List<IDnSpyFile>();
			this.hash = new HashSet<IDnSpyFile>();
		}

		public IDnSpyFile[] Load(IEnumerable<FileToLoad> files) {
			filesToLoad = files.ToArray();
			ProgressMaximum = filesToLoad.Length;

			const int MAX_NUM_FILES_NO_DLG_BOX = 10;
			if (filesToLoad.Length <= MAX_NUM_FILES_NO_DLG_BOX) {
				foreach (var f in filesToLoad)
					Load(f);
			}
			else
				ProgressDlg.Show(this, "dnSpy", ownerWindow);

			return loadedFiles.ToArray();
		}

		void Load(FileToLoad f) {
			if (f.Info.Type == FileConstants.FILETYPE_FILE && string.IsNullOrEmpty(f.Info.Name))
				return;
			var file = fileManager.TryGetOrCreate(f.Info, f.IsAutoLoaded);
			if (file != null && !hash.Contains(file)) {
				loadedFiles.Add(file);
				hash.Add(file);
			}
		}

		public void Execute(IProgress progress) {
			for (int i = 0; i < filesToLoad.Length; i++) {
				progress.ThrowIfCancellationRequested();
				var f = filesToLoad[i];
				progress.SetTotalProgress(i);
				progress.SetDescription(GetDescription(f.Info));
				Load(f);
			}

			progress.SetTotalProgress(filesToLoad.Length);
		}

		string GetDescription(DnSpyFileInfo info) {
			if (info.Type == FileConstants.FILETYPE_REFASM) {
				int index = info.Name.LastIndexOf(FileConstants.REFERENCE_ASSEMBLY_SEPARATOR);
				if (index >= 0)
					return info.Name.Substring(0, index);
			}
			return info.Name;
		}
	}
}
