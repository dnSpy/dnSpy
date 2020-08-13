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
using System.Diagnostics;
using System.IO;
using dnlib.DotNet;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Utilities;

namespace dnSpy.Documents.Tabs.Dialogs {
	sealed class GACFileVM : ViewModelBase {
		public object NameObject => this;
		public object VersionObject => this;
		public string Name => gacFileInfo.Assembly.Name;
		public Version Version => gacFileInfo.Assembly.Version;
		public bool IsExe => gacFileInfo.Path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);
		public string Path => gacFileInfo.Path;
		public IAssembly Assembly => gacFileInfo.Assembly;
		public OpenFromGACVM Owner { get; }

		public string VersionString => versionString ??= (Version ?? new Version(0, 0, 0, 0)).ToString();
		string? versionString;

		public string CreatedBy {
			get {
				if (createdBy is null)
					CalculateInfo();
				Debug2.Assert(!(createdBy is null));
				return createdBy;
			}
		}
		string? createdBy;

		public string FileVersion {
			get {
				if (fileVersion is null)
					CalculateInfo();
				Debug2.Assert(!(fileVersion is null));
				return fileVersion;
			}
		}
		string? fileVersion;

		public bool IsDuplicate {
			get => isDuplicate;
			set {
				if (isDuplicate != value) {
					isDuplicate = value;
					OnPropertyChanged(nameof(IsDuplicate));
				}
			}
		}
		bool isDuplicate;

		readonly GacFileInfo gacFileInfo;

		public GACFileVM(OpenFromGACVM owner, GacFileInfo gacFileInfo) {
			Owner = owner;
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

		string? GetCreatedBy(ModuleDef mod) {
			var asm = mod.Assembly;
			if (asm is null)
				return null;
			var ca = asm.CustomAttributes.Find("System.Reflection.AssemblyCompanyAttribute");
			if (ca is null)
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

		static string Filter(string? s) {
			if (s is null)
				return string.Empty;
			const int MAX = 512;
			if (s.Length > MAX)
				s = s.Substring(0, MAX);
			return s;
		}
	}
}
