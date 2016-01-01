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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Settings;
using dnSpy.Shared.UI.MVVM;

namespace dnSpy.Files {
	class FileManagerSettings : ViewModelBase, IFileManagerSettings {
		protected virtual void OnModified() {
		}

		public bool UseMemoryMappedIO {
			get { return useMemoryMappedIO; }
			set {
				if (useMemoryMappedIO != value) {
					useMemoryMappedIO = value;
					OnPropertyChanged("UseMemoryMappedIO");
					OnModified();
				}
			}
		}
		bool useMemoryMappedIO = true;

		public bool LoadPDBFiles {
			get { return loadPDBFiles; }
			set {
				if (loadPDBFiles != value) {
					loadPDBFiles = value;
					OnPropertyChanged("LoadPDBFiles");
					OnModified();
				}
			}
		}
		bool loadPDBFiles = true;

		public bool UseGAC {
			get { return useGAC; }
			set {
				if (useGAC != value) {
					useGAC = value;
					OnPropertyChanged("UseGAC");
					OnModified();
				}
			}
		}
		bool useGAC = true;
	}

	[Export, Export(typeof(IFileManagerSettings)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class FileManagerSettingsImpl : FileManagerSettings {
		static readonly Guid SETTINGS_GUID = new Guid("3643CE93-84D5-455A-9183-94B58BC80942");

		readonly ISettingsManager settingsManager;

		[ImportingConstructor]
		FileManagerSettingsImpl(ISettingsManager settingsManager) {
			this.settingsManager = settingsManager;

			this.disableSave = true;
			var sect = settingsManager.GetOrCreateSection(SETTINGS_GUID);
			this.UseMemoryMappedIO = sect.Attribute<bool?>("UseMemoryMappedIO") ?? this.UseMemoryMappedIO;
			this.LoadPDBFiles = sect.Attribute<bool?>("LoadPDBFiles") ?? this.LoadPDBFiles;
			this.UseGAC = sect.Attribute<bool?>("UseGAC") ?? this.UseGAC;
			this.disableSave = false;
		}
		readonly bool disableSave;

		protected override void OnModified() {
			if (disableSave)
				return;
			var sect = settingsManager.RecreateSection(SETTINGS_GUID);
			sect.Attribute("UseMemoryMappedIO", UseMemoryMappedIO);
			sect.Attribute("LoadPDBFiles", LoadPDBFiles);
			sect.Attribute("UseGAC", UseGAC);
		}
	}
}
