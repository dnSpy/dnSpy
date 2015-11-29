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

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Settings;

namespace dnSpy.Files.Tabs {
	[ExportDnSpyLoader(Order = LoaderConstants.ORDER_FILETABMANAGER)]
	sealed class FileTabManagerLoader : IDnSpyLoader {
		const string SETTINGS_NAME = "53863C11-DF95-43F2-8F86-5E9DFCCE6893";
		const string FILE_LISTS_SECTION = "FileLists";
		const string TABGROUPWINDOW_SECTION = "TabGroupWindow";

		readonly IFileTabContentFactoryManager fileTabContentFactoryManager;
		readonly FileTabManager fileTabManager;
		readonly FileListManager fileListManager;

		[ImportingConstructor]
		FileTabManagerLoader(IFileTabContentFactoryManager fileTabContentFactoryManager, FileTabManager fileTabManager, FileListManager fileListManager) {
			this.fileTabContentFactoryManager = fileTabContentFactoryManager;
			this.fileTabManager = fileTabManager;
			this.fileListManager = fileListManager;
		}

		public IEnumerable<object> Load(ISettingsManager settingsManager) {
			var section = settingsManager.GetOrCreateSection(SETTINGS_NAME);

			fileListManager.Load(section.GetOrCreateSection(FILE_LISTS_SECTION));
			yield return null;

			foreach (var f in fileListManager.SelectedFileList.Files) {
				fileTabManager.FileTreeView.FileManager.TryGetOrCreate(f);
				yield return null;
			}

			var tgws = new List<SerializedTabGroupWindow>();
			var tgwsHash = new HashSet<string>();
			foreach (var tgwSection in section.SectionsWithName(TABGROUPWINDOW_SECTION)) {
				var tgw = SerializedTabGroupWindow.Load(tgwSection);
				yield return null;
				if (tgwsHash.Contains(tgw.Name))
					continue;
				tgws.Add(tgw);
			}

			// The files are added to the treeview with a slight delay. Make sure the files have
			// been added to the TV or the node lookup code will fail to find the nodes it needs.
			yield return LoaderConstants.Delay;

			var mainTgw = tgws.FirstOrDefault(a => a.Name == SerializedTabGroupWindow.MAIN_NAME);
			if (mainTgw != null) {
				foreach (var o in Load(mainTgw))
					yield return o;
				yield return null;
			}

			fileTabManager.OnTabsLoaded();
		}

		IEnumerable<object> Load(SerializedTabGroupWindow tgw) {
			bool addedAutoLoadedAssembly = false;
			foreach (var f in GetAutoLoadedAssemblies(tgw)) {
				addedAutoLoadedAssembly = true;
				fileTabManager.FileTreeView.FileManager.TryGetOrCreate(f, true);
				yield return null;
			}
			if (addedAutoLoadedAssembly)
				yield return LoaderConstants.Delay;

			foreach (var o in tgw.Restore(fileTabManager, fileTabContentFactoryManager, fileTabManager.TabGroupManager))
				yield return o;
		}

		IEnumerable<DnSpyFileInfo> GetAutoLoadedAssemblies(SerializedTabGroupWindow tgw) {
			foreach (var g in tgw.TabGroups) {
				foreach (var t in g.Tabs) {
					foreach (var f in t.AutoLoadedFiles)
						yield return f;
				}
			}
		}

		public void OnAppLoaded() {
		}

		public void Save(ISettingsManager settingsManager) {
			var section = settingsManager.RecreateSection(SETTINGS_NAME);
			fileListManager.SelectedFileList.Update(fileTabManager.FileTreeView.FileManager.GetFiles());
			fileListManager.Save(section.GetOrCreateSection(FILE_LISTS_SECTION));

			if (fileTabManager.Settings.RestoreTabs) {
				var tgw = SerializedTabGroupWindow.Create(fileTabContentFactoryManager, fileTabManager.TabGroupManager, SerializedTabGroupWindow.MAIN_NAME);
				tgw.Save(section.CreateSection(TABGROUPWINDOW_SECTION));
			}
		}
	}
}
