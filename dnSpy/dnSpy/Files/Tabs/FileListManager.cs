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
using System.Diagnostics;
using dnSpy.Contracts.Settings;

namespace dnSpy.Files.Tabs {
	[Export]
	sealed class FileListManager {
		const string CURRENT_LIST_ATTR = "name";
		const string FILE_LIST_SECTION = "FileList";

		public FileList SelectedFileList {
			get {
				Debug.Assert(hasLoaded);
				if (fileLists.Count == 0)
					CreateDefaultList();
				if ((uint)selectedIndex >= (uint)fileLists.Count)
					selectedIndex = 0;
				return fileLists[selectedIndex];
			}
			set {
				if (value == null)
					throw new ArgumentNullException(nameof(value));
				int index = fileLists.IndexOf(value);
				if (index < 0)
					throw new InvalidOperationException();
				selectedIndex = index;
			}
		}
		int selectedIndex;

		public FileList[] FileLists => fileLists.ToArray();
		readonly List<FileList> fileLists;

		FileListManager() {
			this.fileLists = new List<FileList>();
			this.selectedIndex = -1;
			this.hasLoaded = false;
		}

		void CreateDefaultList() {
			var fl = new FileList(FileList.DEFAULT_NAME);
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

		void SelectList(string name) => selectedIndex = IndexOf(name);

		public void Load(ISettingsSection section) {
			var listName = section.Attribute<string>(CURRENT_LIST_ATTR);
			var names = new HashSet<string>(StringComparer.Ordinal);
			foreach (var listSection in section.SectionsWithName(FILE_LIST_SECTION)) {
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
			section.Attribute(CURRENT_LIST_ATTR, SelectedFileList.Name);
			foreach (var fileList in fileLists)
				fileList.Save(section.CreateSection(FILE_LIST_SECTION));
		}

		public bool Remove(FileList fileList) {
			if (fileList == SelectedFileList)
				return false;
			var selected = SelectedFileList;
			fileLists.Remove(fileList);
			selectedIndex = fileLists.IndexOf(selected);
			Debug.Assert(selectedIndex >= 0);
			Debug.Assert(SelectedFileList == selected);
			return true;
		}

		public void Add(FileList fileList) {
			if (fileLists.Contains(fileList))
				return;
			fileLists.Add(fileList);
		}
	}
}
