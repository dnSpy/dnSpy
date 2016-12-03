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

		public bool UseNewRenderer_DocumentTreeView {
			get { return useNewRenderer_DocumentTreeView; }
			set {
				if (useNewRenderer_DocumentTreeView != value) {
					useNewRenderer_DocumentTreeView = value;
					OnPropertyChanged(nameof(UseNewRenderer_DocumentTreeView));
					OnModified();
				}
			}
		}
		bool useNewRenderer_DocumentTreeView = false;
	}

	[Export, Export(typeof(IAppSettings))]
	sealed class AppSettingsImpl : AppSettings {
		static readonly Guid SETTINGS_GUID = new Guid("071CF92D-ACFA-46A1-8EEF-DFAC1D01E644");

		readonly ISettingsService settingsService;

		[ImportingConstructor]
		AppSettingsImpl(ISettingsService settingsService) {
			this.settingsService = settingsService;

			disableSave = true;
			var sect = settingsService.GetOrCreateSection(SETTINGS_GUID);
			UseNewRenderer_TextEditor = sect.Attribute<bool?>(nameof(UseNewRenderer_TextEditor)) ?? UseNewRenderer_TextEditor;
			UseNewRenderer_HexEditor = sect.Attribute<bool?>(nameof(UseNewRenderer_HexEditor)) ?? UseNewRenderer_HexEditor;
			UseNewRenderer_DocumentTreeView = sect.Attribute<bool?>(nameof(UseNewRenderer_DocumentTreeView)) ?? UseNewRenderer_DocumentTreeView;
			disableSave = false;
		}
		readonly bool disableSave;

		protected override void OnModified() {
			if (disableSave)
				return;
			var sect = settingsService.RecreateSection(SETTINGS_GUID);
			sect.Attribute(nameof(UseNewRenderer_TextEditor), UseNewRenderer_TextEditor);
			sect.Attribute(nameof(UseNewRenderer_HexEditor), UseNewRenderer_HexEditor);
			sect.Attribute(nameof(UseNewRenderer_DocumentTreeView), UseNewRenderer_DocumentTreeView);
		}
	}
}
