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
using dnSpy.Contracts.Text.Editor;
using dnSpy.Shared.MVVM;

namespace dnSpy.Output {
	interface IOutputManagerSettings : INotifyPropertyChanged {
		WordWrapStyles WordWrapStyle { get; }
		bool ShowLineNumbers { get; }
		bool ShowTimestamps { get; }
		Guid SelectedGuid { get; }
	}

	class OutputManagerSettings : ViewModelBase, IOutputManagerSettings {
		protected virtual void OnModified() { }

		public WordWrapStyles WordWrapStyle {
			get { return wordWrapStyle; }
			set {
				if (wordWrapStyle != value) {
					wordWrapStyle = value;
					OnPropertyChanged(nameof(WordWrapStyle));
					OnModified();
				}
			}
		}
		WordWrapStyles wordWrapStyle = WordWrapStyles.DefaultDisabled;

		public bool ShowLineNumbers {
			get { return showLineNumbers; }
			set {
				if (showLineNumbers != value) {
					showLineNumbers = value;
					OnPropertyChanged(nameof(ShowLineNumbers));
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
					OnPropertyChanged(nameof(ShowTimestamps));
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
					OnPropertyChanged(nameof(SelectedGuid));
					OnModified();
				}
			}
		}
		Guid selectedGuid = Guid.Empty;
	}

	[Export, Export(typeof(IOutputManagerSettings))]
	sealed class OutputManagerSettingsImpl : OutputManagerSettings {
		static readonly Guid SETTINGS_GUID = new Guid("64414B81-EF07-4DA1-9D21-1F625A6E0080");

		readonly ISettingsManager settingsManager;

		[ImportingConstructor]
		OutputManagerSettingsImpl(ISettingsManager settingsManager) {
			this.settingsManager = settingsManager;

			this.disableSave = true;
			var sect = settingsManager.GetOrCreateSection(SETTINGS_GUID);
			this.WordWrapStyle = sect.Attribute<WordWrapStyles?>(nameof(WordWrapStyle)) ?? this.WordWrapStyle;
			this.ShowLineNumbers = sect.Attribute<bool?>(nameof(ShowLineNumbers)) ?? this.ShowLineNumbers;
			this.ShowTimestamps = sect.Attribute<bool?>(nameof(ShowTimestamps)) ?? this.ShowTimestamps;
			this.SelectedGuid= sect.Attribute<Guid?>(nameof(SelectedGuid)) ?? this.SelectedGuid;
			this.disableSave = false;
		}
		readonly bool disableSave;

		protected override void OnModified() {
			if (disableSave)
				return;
			var sect = settingsManager.RecreateSection(SETTINGS_GUID);
			sect.Attribute(nameof(WordWrapStyle), WordWrapStyle);
			sect.Attribute(nameof(ShowLineNumbers), ShowLineNumbers);
			sect.Attribute(nameof(ShowTimestamps), ShowTimestamps);
			sect.Attribute(nameof(SelectedGuid), SelectedGuid);
		}
	}
}
