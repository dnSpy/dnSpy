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
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Settings;
using dnSpy.Shared.MVVM;

namespace dnSpy.Files.Tabs {
	class FileTabManagerSettings : ViewModelBase, IFileTabManagerSettings {
		protected virtual void OnModified() { }

		public bool RestoreTabs {
			get { return restoreTabs; }
			set {
				if (restoreTabs != value) {
					restoreTabs = value;
					OnPropertyChanged(nameof(RestoreTabs));
					OnModified();
				}
			}
		}
		bool restoreTabs = true;

		public bool DecompileFullType {
			get { return decompileFullType; }
			set {
				if (decompileFullType != value) {
					decompileFullType = value;
					OnPropertyChanged(nameof(DecompileFullType));
					OnModified();
				}
			}
		}
		bool decompileFullType = true;

		public FileTabManagerSettings Clone() => CopyTo(new FileTabManagerSettings());
		public FileTabManagerSettings CopyTo(FileTabManagerSettings other) {
			other.RestoreTabs = this.RestoreTabs;
			other.DecompileFullType = this.DecompileFullType;
			return other;
		}
	}

	[Export, Export(typeof(IFileTabManagerSettings)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class FileTabManagerSettingsImpl : FileTabManagerSettings {
		static readonly Guid SETTINGS_GUID = new Guid("1ACE15FD-D689-40DC-B1E7-6EEC25B3116F");

		readonly ISettingsManager settingsManager;

		[ImportingConstructor]
		FileTabManagerSettingsImpl(ISettingsManager settingsManager) {
			this.settingsManager = settingsManager;

			this.disableSave = true;
			var sect = settingsManager.GetOrCreateSection(SETTINGS_GUID);
			this.RestoreTabs = sect.Attribute<bool?>(nameof(RestoreTabs)) ?? this.RestoreTabs;
			this.DecompileFullType = sect.Attribute<bool?>(nameof(DecompileFullType)) ?? this.DecompileFullType;
			this.disableSave = false;
		}
		readonly bool disableSave;

		protected override void OnModified() {
			if (disableSave)
				return;
			var sect = settingsManager.RecreateSection(SETTINGS_GUID);
			sect.Attribute(nameof(RestoreTabs), RestoreTabs);
			sect.Attribute(nameof(DecompileFullType), DecompileFullType);
		}
	}
}
