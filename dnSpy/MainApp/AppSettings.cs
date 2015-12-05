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

using System.ComponentModel.Composition;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Settings;
using dnSpy.Shared.UI.MVVM;

namespace dnSpy.MainApp {
	class AppSettings : ViewModelBase, IAppSettings {
		protected virtual void OnModified() {
		}

		public bool UseNewRenderer_TextEditor {
			get { return useNewRenderer_TextEditor; }
			set {
				if (useNewRenderer_TextEditor != value) {
					useNewRenderer_TextEditor = value;
					OnPropertyChanged("UseNewRenderer_TextEditor");
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
					OnPropertyChanged("UseNewRenderer_HexEditor");
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
					OnPropertyChanged("UseNewRenderer_FileTreeView");
					OnModified();
				}
			}
		}
		bool useNewRenderer_FileTreeView = false;
	}

	[Export, Export(typeof(IAppSettings)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class AppSettingsImpl : AppSettings {
		const string SETTINGS_NAME = "071CF92D-ACFA-46A1-8EEF-DFAC1D01E644";

		readonly ISettingsManager settingsManager;

		[ImportingConstructor]
		AppSettingsImpl(ISettingsManager settingsManager) {
			this.settingsManager = settingsManager;

			this.disableSave = true;
			var sect = settingsManager.GetOrCreateSection(SETTINGS_NAME);
			this.UseNewRenderer_TextEditor = sect.Attribute<bool?>("UseNewRenderer_TextEditor") ?? this.UseNewRenderer_TextEditor;
			this.UseNewRenderer_HexEditor = sect.Attribute<bool?>("UseNewRenderer_HexEditor") ?? this.UseNewRenderer_HexEditor;
			this.UseNewRenderer_FileTreeView = sect.Attribute<bool?>("UseNewRenderer_FileTreeView") ?? this.UseNewRenderer_FileTreeView;
			this.disableSave = false;
		}
		readonly bool disableSave;

		protected override void OnModified() {
			if (disableSave)
				return;
			var sect = settingsManager.RecreateSection(SETTINGS_NAME);
			sect.Attribute("UseNewRenderer_TextEditor", UseNewRenderer_TextEditor);
			sect.Attribute("UseNewRenderer_HexEditor", UseNewRenderer_HexEditor);
			sect.Attribute("UseNewRenderer_FileTreeView", UseNewRenderer_FileTreeView);
		}
	}
}
