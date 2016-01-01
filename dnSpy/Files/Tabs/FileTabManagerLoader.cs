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
using dnSpy.Contracts.App;
using dnSpy.Contracts.Settings;

namespace dnSpy.Files.Tabs {
	[ExportDnSpyLoader(Order = LoaderConstants.ORDER_FILETABMANAGER)]
	sealed class FileTabManagerLoader : IDnSpyLoader {
		static readonly Guid SETTINGS_GUID = new Guid("53863C11-DF95-43F2-8F86-5E9DFCCE6893");
		const string FILE_LISTS_SECTION = "FileLists";
		const string TABGROUPWINDOW_SECTION = "TabGroupWindow";

		readonly FileTabSerializer fileTabSerializer;
		readonly FileTabManager fileTabManager;
		readonly IFileListLoader fileListLoader;

		[ImportingConstructor]
		FileTabManagerLoader(FileTabSerializer fileTabSerializer, FileTabManager fileTabManager, IFileListLoader fileListLoader) {
			this.fileTabSerializer = fileTabSerializer;
			this.fileTabManager = fileTabManager;
			this.fileListLoader = fileListLoader;
		}

		public IEnumerable<object> Load(ISettingsManager settingsManager) {
			var section = settingsManager.GetOrCreateSection(SETTINGS_GUID);

			foreach (var o in fileListLoader.Load(section.GetOrCreateSection(FILE_LISTS_SECTION)))
				yield return o;

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

			foreach (var o in fileTabSerializer.Restore(tgws))
				yield return o;

			fileTabManager.OnTabsLoaded();
		}

		public void OnAppLoaded() {
		}

		public void Save(ISettingsManager settingsManager) {
			var section = settingsManager.RecreateSection(SETTINGS_GUID);
			fileListLoader.Save(section.GetOrCreateSection(FILE_LISTS_SECTION));

			if (fileTabManager.Settings.RestoreTabs) {
				foreach (var tgw in fileTabSerializer.SaveTabs())
					tgw.Save(section.CreateSection(TABGROUPWINDOW_SECTION));
			}
		}
	}
}
