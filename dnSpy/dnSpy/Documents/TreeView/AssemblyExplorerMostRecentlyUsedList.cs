/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using dnSpy.Contracts.Settings;

namespace dnSpy.Documents.TreeView {
	abstract class AssemblyExplorerMostRecentlyUsedList {
		public abstract string[] RecentFiles { get; }
		public abstract void Add(string filename);
	}

	[Export(typeof(AssemblyExplorerMostRecentlyUsedList))]
	sealed class AssemblyExplorerMostRecentlyUsedListImpl : AssemblyExplorerMostRecentlyUsedList {
		static readonly Guid SETTINGS_GUID = new Guid("642B9276-3C9A-4EFE-9B3B-D62046824B18");
		const int MaxFileCount = 30;
		const string filenameSectionName = "file";
		const string filenameAttributeName = "name";

		readonly ISettingsSection fileSection;
		readonly List<string> fileList;

		[ImportingConstructor]
		AssemblyExplorerMostRecentlyUsedListImpl(ISettingsService settingsService) {
			fileList = new List<string>(MaxFileCount);

			fileSection = settingsService.GetOrCreateSection(SETTINGS_GUID);
			foreach (var fileSect in fileSection.SectionsWithName(filenameSectionName)) {
				if (fileList.Count == MaxFileCount)
					break;
				var name = fileSect.Attribute<string?>(filenameAttributeName);
				if (name is null)
					continue;
				fileList.Add(name);
			}
		}

		public override string[] RecentFiles => fileList.ToArray();

		public override void Add(string filename) {
			if (string.IsNullOrEmpty(filename))
				return;
			int index = GetIndexOf(filename);
			if (index >= 0)
				fileList.RemoveAt(index);
			if (fileList.Count == MaxFileCount)
				fileList.RemoveAt(fileList.Count - 1);
			fileList.Insert(0, filename);
			Save();
		}

		int GetIndexOf(string filename) {
			for (int i = 0; i < fileList.Count; i++) {
				if (StringComparer.OrdinalIgnoreCase.Equals(filename, fileList[i]))
					return i;
			}
			return -1;
		}

		void Save() {
			fileSection.RemoveSection(filenameSectionName);
			foreach (var file in fileList) {
				var fileSect = fileSection.CreateSection(filenameSectionName);
				fileSect.Attribute<string>(filenameAttributeName, file);
			}
		}
	}
}
