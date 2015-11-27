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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using dnSpy.Contracts.Settings;

namespace dnSpy.Files.Tabs {
	[Export]
	sealed class FileListManager {
		const string DEFAULT_NAME = "(Default)";
		const string CURRENT_LIST_NAME = "name";
		const string FILE_LIST_SECTION_NAME = "FileList";

		public FileList SelectedFileList {
			get {
				Debug.Assert(hasLoaded);
				if (fileLists.Count == 0)
					CreateDefaultList();
				if ((uint)selectedIndex >= (uint)fileLists.Count)
					selectedIndex = 0;
				return fileLists[selectedIndex];
			}
		}
		int selectedIndex;

		readonly List<FileList> fileLists;

		FileListManager() {
			this.fileLists = new List<FileList>();
			this.selectedIndex = -1;
			this.hasLoaded = false;
		}

		void CreateDefaultList() {
			var fl = new FileList(DEFAULT_NAME);
			fl.AddDefaultFiles();
			fileLists.Add(fl);
		}

		int IndexOf(string name) {
			for (int i = 0; i < fileLists.Count; i++) {
				if (StringComparer.Ordinal.Equals(fileLists[i].Name, name))
					return i;
			}
			return -1;
		}

		public void SelectList(string name) {
			selectedIndex = IndexOf(name);
		}

		public void Load(ISettingsSection section) {
			var listName = section.Attribute<string>(CURRENT_LIST_NAME);
			var names = new HashSet<string>(StringComparer.Ordinal);
			foreach (var listSection in section.SectionsWithName(FILE_LIST_SECTION_NAME)) {
				var fileList = FileList.Create(listSection);
				if (names.Contains(fileList.Name))
					continue;
				fileLists.Add(fileList);
			}
			this.hasLoaded = true;

			SelectList(listName);
		}
		bool hasLoaded;

		public void Save(ISettingsSection section) {
			section.Attribute(CURRENT_LIST_NAME, SelectedFileList.Name);
			foreach (var fileList in fileLists)
				fileList.Save(section.CreateSection(FILE_LIST_SECTION_NAME));
		}
	}
}
