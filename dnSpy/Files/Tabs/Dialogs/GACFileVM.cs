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
using System.Diagnostics;
using System.IO;
using dnlib.DotNet;
using dnSpy.Shared.UI.Files;
using dnSpy.Shared.UI.MVVM;

namespace dnSpy.Files.Tabs.Dialogs {
	sealed class GACFileVM : ViewModelBase {
		public object NameObject { get { return this; } }
		public object VersionObject { get { return this; } }

		public string Name {
			get { return gacFileInfo.Assembly.Name; }
		}

		public Version Version {
			get { return gacFileInfo.Assembly.Version; }
		}

		public bool IsExe {
			get { return gacFileInfo.Path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase); }
		}

		public string CreatedBy {
			get {
				if (createdBy == null)
					CalculateInfo();
				return createdBy;
			}
		}
		string createdBy;

		public string FileVersion {
			get {
				if (fileVersion == null)
					CalculateInfo();
				return fileVersion;
			}
		}
		string fileVersion;

		public string Path {
			get { return gacFileInfo.Path; }
		}

		public bool IsDuplicate {
			get { return isDuplicate; }
			set {
				if (isDuplicate != value) {
					isDuplicate = value;
					OnPropertyChanged("IsDuplicate");
				}
			}
		}
		bool isDuplicate;

		public IAssembly Assembly {
			get { return gacFileInfo.Assembly; }
		}

		public OpenFromGACVM Owner {
			get { return owner; }
		}
		readonly OpenFromGACVM owner;

		readonly GacFileInfo gacFileInfo;

		public GACFileVM(OpenFromGACVM owner, GacFileInfo gacFileInfo) {
			this.owner = owner;
			this.gacFileInfo = gacFileInfo;
		}

		string CalculateCreatedByFromAttribute() {
			if (!File.Exists(gacFileInfo.Path))
				return string.Empty;
			try {
				using (var mod = ModuleDefMD.Load(gacFileInfo.Path))
					return GetCreatedBy(mod) ?? string.Empty;
			}
			catch (BadImageFormatException) {
			}
			catch (IOException) {
			}
			return string.Empty;
		}

		string GetCreatedBy(ModuleDef mod) {
			var asm = mod.Assembly;
			if (asm == null)
				return null;
			var ca = asm.CustomAttributes.Find("System.Reflection.AssemblyCompanyAttribute");
			if (ca == null)
				return null;
			if (ca.ConstructorArguments.Count != 1)
				return null;
			var arg = ca.ConstructorArguments[0];
			var s = arg.Value as UTF8String;
			if (UTF8String.IsNull(s))
				return null;
			return s;
		}

		void CalculateInfo() {
			createdBy = string.Empty;
			fileVersion = string.Empty;
			if (!File.Exists(gacFileInfo.Path))
				return;
			var info = FileVersionInfo.GetVersionInfo(gacFileInfo.Path);
			fileVersion = Filter(info.FileVersion ?? string.Empty);
			createdBy = Filter(info.CompanyName);
			if (string.IsNullOrWhiteSpace(createdBy))
				createdBy = CalculateCreatedByFromAttribute() ?? string.Empty;
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
