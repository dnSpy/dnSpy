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
using System.Linq;
using System.Reflection;
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Settings;

namespace dnSpy.Files.Tabs {
	sealed class FileList {
		public const string DEFAULT_NAME = "(Default)";
		const string FILELIST_NAME_ATTR = "name";
		const string FILE_SECTION = "File";

		public string Name {
			get { return name; }
		}
		readonly string name;

		public List<DnSpyFileInfo> Files {
			get { return files; }
		}
		readonly List<DnSpyFileInfo> files;

		public FileList(string name) {
			this.files = new List<DnSpyFileInfo>();
			this.name = name;
		}

		public FileList(DefaultFileList defaultList) {
			this.files = new List<DnSpyFileInfo>(defaultList.Files);
			this.name = defaultList.Name;
		}

		public static FileList Create(ISettingsSection section) {
			var fileList = new FileList(section.Attribute<string>(FILELIST_NAME_ATTR));
			foreach (var fileSect in section.SectionsWithName(FILE_SECTION)) {
				var info = DnSpyFileInfoSerializer.TryLoad(fileSect);
				if (info != null)
					fileList.Files.Add(info.Value);
			}
			return fileList;
		}

		public void Save(ISettingsSection section) {
			section.Attribute(FILELIST_NAME_ATTR, Name);
			foreach (var info in files)
				DnSpyFileInfoSerializer.Save(section.CreateSection(FILE_SECTION), info);
		}

		public void Update(IEnumerable<IDnSpyFile> files) {
			this.files.Clear();
			foreach (var f in files) {
				if (f.IsAutoLoaded)
					continue;
				var info = f.SerializedFile;
				if (info != null)
					this.files.Add(info.Value);
			}
		}

		void AddGacFile(string asmFullName) {
			Files.Add(DnSpyFileInfo.CreateGacFile(asmFullName));
		}

		void AddFile(Assembly asm) {
			Files.Add(DnSpyFileInfo.CreateFile(asm.Location));
		}

		public void AddDefaultFiles() {
			AddFile(typeof(int).Assembly);
			AddFile(typeof(Uri).Assembly);
			AddFile(typeof(Enumerable).Assembly);
			AddFile(typeof(System.Xml.XmlDocument).Assembly);
			AddFile(typeof(System.Windows.Markup.MarkupExtension).Assembly);
			AddFile(typeof(System.Windows.Rect).Assembly);
			AddFile(typeof(System.Windows.UIElement).Assembly);
			AddFile(typeof(System.Windows.FrameworkElement).Assembly);
			AddFile(typeof(dnlib.DotNet.ModuleDefMD).Assembly);
			AddFile(GetType().Assembly);
		}
	}
}
