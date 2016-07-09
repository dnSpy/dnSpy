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
using dnSpy.Contracts.App;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Settings;

namespace dnSpy.MainApp {
	class AppSettings : ViewModelBase, IAppSettings {
		protected virtual void OnModified() { }

		public bool UseNewRenderer_TextEditor {
			get { return useNewRenderer_TextEditor; }
			set {
				if (useNewRenderer_TextEditor != value) {
					useNewRenderer_TextEditor = value;
					OnPropertyChanged(nameof(UseNewRenderer_TextEditor));
					OnModified();
				}
			}
		}
		bool useNewRenderer_TextEditor = false;

		public bool UseNewRenderer_HexEditor {
			get { return useNewRenderer_HexEditor; }
			set {
				if (useNewRenderer_HexEditor != value) {
					useNewRenderer_HexEditor = value;
					OnPropertyChanged(nameof(UseNewRenderer_HexEditor));
					OnModified();
				}
			}
		}
		bool useNewRenderer_HexEditor = false;

		public bool UseNewRenderer_FileTreeView {
			get { return useNewRenderer_FileTreeView; }
			set {
				if (useNewRenderer_FileTreeView != value) {
					useNewRenderer_FileTreeView = value;
					OnPropertyChanged(nameof(UseNewRenderer_FileTreeView));
					OnModified();
				}
			}
		}
		bool useNewRenderer_FileTreeView = false;
	}

	[Export, Export(typeof(IAppSettings))]
	sealed class AppSettingsImpl : AppSettings {
		static readonly Guid SETTINGS_GUID = new Guid("071CF92D-ACFA-46A1-8EEF-DFAC1D01E644");

		readonly ISettingsManager settingsManager;

		[ImportingConstructor]
		AppSettingsImpl(ISettingsManager settingsManager) {
			this.settingsManager = settingsManager;

			this.disableSave = true;
			var sect = settingsManager.GetOrCreateSection(SETTINGS_GUID);
			this.UseNewRenderer_TextEditor = sect.Attribute<bool?>(nameof(UseNewRenderer_TextEditor)) ?? this.UseNewRenderer_TextEditor;
			this.UseNewRenderer_HexEditor = sect.Attribute<bool?>(nameof(UseNewRenderer_HexEditor)) ?? this.UseNewRenderer_HexEditor;
			this.UseNewRenderer_FileTreeView = sect.Attribute<bool?>(nameof(UseNewRenderer_FileTreeView)) ?? this.UseNewRenderer_FileTreeView;
			this.disableSave = false;
		}
		readonly bool disableSave;

		protected override void OnModified() {
			if (disableSave)
				return;
			var sect = settingsManager.RecreateSection(SETTINGS_GUID);
			sect.Attribute(nameof(UseNewRenderer_TextEditor), UseNewRenderer_TextEditor);
			sect.Attribute(nameof(UseNewRenderer_HexEditor), UseNewRenderer_HexEditor);
			sect.Attribute(nameof(UseNewRenderer_FileTreeView), UseNewRenderer_FileTreeView);
		}
	}
}
