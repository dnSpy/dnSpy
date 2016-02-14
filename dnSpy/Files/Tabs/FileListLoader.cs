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
using System.Linq;
using System.Windows.Threading;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Settings;

namespace dnSpy.Files.Tabs {
	interface IFileListLoader {
		IEnumerable<object> Load(ISettingsSection section, bool loadFiles);
		void Save(ISettingsSection section);
		bool CanLoad { get; }
		bool Load(FileList fileList, IDnSpyFileLoader dnSpyFileLoader = null);
		bool CanReload { get; }
		bool Reload(IDnSpyFileLoader dnSpyFileLoader = null);
		void SaveCurrentFilesToList();
	}

	[Export, Export(typeof(IFileListLoader)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class FileListLoader : IFileListLoader {
		readonly FileListManager fileListManager;
		readonly FileTabManager fileTabManager;
		readonly FileTabSerializer fileTabSerializer;
		readonly Lazy<IFileListListener, IFileListListenerMetadata>[] listeners;

		[ImportingConstructor]
		FileListLoader(IAppWindow appWindow, FileListManager fileListManager, FileTabManager fileTabManager, FileTabSerializer fileTabSerializer, [ImportMany] IEnumerable<Lazy<IFileListListener, IFileListListenerMetadata>> mefListeners) {
			this.fileListManager = fileListManager;
			this.fileTabManager = fileTabManager;
			this.fileTabSerializer = fileTabSerializer;
			this.listeners = mefListeners.OrderBy(a => a.Metadata.Order).ToArray();
			appWindow.MainWindowClosed += AppWindow_MainWindowClosed;
		}

		void AppWindow_MainWindowClosed(object sender, EventArgs e) {
			SaveCurrentFilesToList();
		}

		struct Disable_SaveCurrentFilesToList : IDisposable {
			readonly FileListLoader fileListLoader;
			readonly bool oldValue;

			public Disable_SaveCurrentFilesToList(FileListLoader fileListLoader) {
				this.fileListLoader = fileListLoader;
				this.oldValue = fileListLoader.disable_SaveCurrentFilesToList;
				fileListLoader.disable_SaveCurrentFilesToList = true;
			}

			public void Dispose() {
				fileListLoader.disable_SaveCurrentFilesToList = oldValue;
				fileListLoader.SaveCurrentFilesToList();
			}
		}

		Disable_SaveCurrentFilesToList DisableSaveToList() {
			return new Disable_SaveCurrentFilesToList(this);
		}

		public void SaveCurrentFilesToList() {
			if (disable_SaveCurrentFilesToList)
				return;
			fileListManager.SelectedFileList.Update(fileTabManager.FileTreeView.TreeView.Root.DataChildren.OfType<IDnSpyFileNode>().Select(a => a.DnSpyFile));
		}
		bool disable_SaveCurrentFilesToList;

		public IEnumerable<object> Load(ISettingsSection section, bool loadFiles) {
			var disable = DisableSaveToList();
			fileListManager.Load(section);
			yield return null;

			if (loadFiles) {
				foreach (var f in fileListManager.SelectedFileList.Files) {
					if (!(f.Type == FileConstants.FILETYPE_FILE && string.IsNullOrEmpty(f.Name)))
						fileTabManager.FileTreeView.FileManager.TryGetOrCreate(f);
					yield return null;
				}
			}
			disable.Dispose();
		}

		public void Save(ISettingsSection section) {
			SaveCurrentFilesToList();
			fileListManager.Save(section);
		}

		bool CheckCanLoad(bool isReload) {
			foreach (var listener in listeners) {
				if (!listener.Value.CheckCanLoad(isReload))
					return false;
			}
			return true;
		}

		void NotifyBeforeLoad(bool isReload) {
			foreach (var listener in listeners)
				listener.Value.BeforeLoad(isReload);
		}

		void NotifyAfterLoad(bool isReload) {
			foreach (var listener in listeners)
				listener.Value.AfterLoad(isReload);
		}

		public bool CanLoad {
			get { return !disableLoadAndReload && listeners.All(a => a.Value.CanLoad); }
		}

		public bool Load(FileList fileList, IDnSpyFileLoader dnSpyFileLoader) {
			const bool isReload = false;
			if (dnSpyFileLoader == null)
				dnSpyFileLoader = new DefaultDnSpyFileLoader(fileTabManager.FileTreeView.FileManager);
			if (!CanLoad)
				return false;
			if (!CheckCanLoad(isReload))
				return false;
			if (fileList != fileListManager.SelectedFileList)
				SaveCurrentFilesToList();

			NotifyBeforeLoad(isReload);
			using (DisableSaveToList()) {
				fileTabManager.CloseAll();
				fileTabManager.FileTreeView.FileManager.Clear();
				dnSpyFileLoader.Load(fileList.Files.Select(a => new FileToLoad(a)));
			}
			NotifyAfterLoad(isReload);

			Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => {
				GC.Collect();
				GC.WaitForPendingFinalizers();
			}));
			return true;
		}

		public bool CanReload {
			get { return !disableLoadAndReload && listeners.All(a => a.Value.CanReload); }
		}

		public bool Reload(IDnSpyFileLoader dnSpyFileLoader) {
			const bool isReload = true;
			if (dnSpyFileLoader == null)
				dnSpyFileLoader = new DefaultDnSpyFileLoader(fileTabManager.FileTreeView.FileManager);
			if (!CanReload)
				return false;
			if (!CheckCanLoad(isReload))
				return false;
			SaveCurrentFilesToList();

			NotifyBeforeLoad(isReload);
			var tgws = fileTabSerializer.SaveTabs();
			using (DisableSaveToList())
			using (fileTabManager.OnReloadAll()) {
				fileTabManager.CloseAll();
				fileTabManager.FileTreeView.FileManager.Clear();
				var files = fileListManager.SelectedFileList.Files.Select(a => new FileToLoad(a)).ToList();
				foreach (var tgw in tgws) {
					foreach (var g in tgw.TabGroups) {
						foreach (var t in g.Tabs) {
							foreach (var f in t.AutoLoadedFiles)
								files.Add(new FileToLoad(f, true));
						}
					}
				}
				dnSpyFileLoader.Load(files);
			}
			NotifyAfterLoad(isReload);

			// The files in the TV is loaded with a delay so make sure we delay before restoring
			// or the code that tries to find the nodes might fail to find them.
			disableLoadAndReload = true;
			Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => {
				GC.Collect();
				GC.WaitForPendingFinalizers();
				foreach (var o in fileTabSerializer.Restore(tgws)) {
				}
				disableLoadAndReload = false;
			}));
			return true;
		}
		bool disableLoadAndReload;
	}
}
