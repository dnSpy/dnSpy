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

using dnSpy.Shared.UI.MVVM;

namespace dnSpy.Files.Tabs.Dialogs {
	sealed class FileListVM : ViewModelBase {
		public object NameObject { get { return this; } }
		public object FileCountObject { get { return this; } }

		public string Name {
			get { return Filter(fileList.Name); }
		}

		public int FileCount {
			get { return fileList.Files.Count; }
		}

		public OpenFileListVM Owner {
			get { return owner; }
		}

		public bool IsDefault {
			get { return fileList.IsDefault; }
		}

		public FileList FileList {
			get { return fileList; }
		}
		readonly FileList fileList;

		public bool IsExistingList {
			get { return isExistingList; }
		}
		readonly bool isExistingList;

		public bool IsUserList {
			get { return isUserList; }
		}
		readonly bool isUserList;

		readonly OpenFileListVM owner;

		public FileListVM(OpenFileListVM owner, FileList fileList, bool isExistingList, bool isUserList) {
			this.owner = owner;
			this.fileList = fileList;
			this.isExistingList = isExistingList;
			this.isUserList = isUserList;
		}

		static string Filter(string s) {
			if (s == null)
				return string.Empty;
			const int MAX = 512;
			if (s.Length > MAX)
				s = s.Substring(0, MAX);
			return s;
		}
	}
}
