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
using System.ComponentModel;
using System.ComponentModel.Composition;
using dnSpy.Contracts.Settings;
using dnSpy.Shared.MVVM;

namespace dnSpy.Output {
	interface IOutputManagerSettings : INotifyPropertyChanged {
		bool WordWrap { get; }
		bool ShowLineNumbers { get; }
		bool ShowTimestamps { get; }
		Guid SelectedGuid { get; }
	}

	class OutputManagerSettings : ViewModelBase, IOutputManagerSettings {
		protected virtual void OnModified() {
		}

		public bool WordWrap {
			get { return wordWrap; }
			set {
				if (wordWrap != value) {
					wordWrap = value;
					OnPropertyChanged("WordWrap");
					OnModified();
				}
			}
		}
		bool wordWrap = false;

		public bool ShowLineNumbers {
			get { return showLineNumbers; }
			set {
				if (showLineNumbers != value) {
					showLineNumbers = value;
					OnPropertyChanged("ShowLineNumbers");
					OnModified();
				}
			}
		}
		bool showLineNumbers = true;

		public bool ShowTimestamps {
			get { return showTimestamps; }
			set {
				if (showTimestamps != value) {
					showTimestamps = value;
					OnPropertyChanged("ShowTimestamps");
					OnModified();
				}
			}
		}
		bool showTimestamps = true;

		public Guid SelectedGuid {
			get { return selectedGuid; }
			set {
				if (selectedGuid != value) {
					selectedGuid = value;
					OnPropertyChanged("SelectedGuid");
					OnModified();
				}
			}
		}
		Guid selectedGuid = Guid.Empty;
	}

	[Export, Export(typeof(IOutputManagerSettings)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class OutputManagerSettingsImpl : OutputManagerSettings {
		static readonly Guid SETTINGS_GUID = new Guid("64414B81-EF07-4DA1-9D21-1F625A6E0080");

		readonly ISettingsManager settingsManager;

		[ImportingConstructor]
		OutputManagerSettingsImpl(ISettingsManager settingsManager) {
			this.settingsManager = settingsManager;

			this.disableSave = true;
			var sect = settingsManager.GetOrCreateSection(SETTINGS_GUID);
			this.WordWrap = sect.Attribute<bool?>("WordWrap") ?? this.WordWrap;
			this.ShowLineNumbers = sect.Attribute<bool?>("ShowLineNumbers") ?? this.ShowLineNumbers;
			this.ShowTimestamps = sect.Attribute<bool?>("ShowTimestamps") ?? this.ShowTimestamps;
			this.SelectedGuid= sect.Attribute<Guid?>("SelectedGuid") ?? this.SelectedGuid;
			this.disableSave = false;
		}
		readonly bool disableSave;

		protected override void OnModified() {
			if (disableSave)
				return;
			var sect = settingsManager.RecreateSection(SETTINGS_GUID);
			sect.Attribute("WordWrap", WordWrap);
			sect.Attribute("ShowLineNumbers", ShowLineNumbers);
			sect.Attribute("ShowTimestamps", ShowTimestamps);
			sect.Attribute("SelectedGuid", SelectedGuid);
		}
	}
}
