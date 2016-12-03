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
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Settings;

namespace dnSpy.Output {
	interface IOutputServiceSettings : INotifyPropertyChanged {
		Guid SelectedGuid { get; }
	}

	class OutputServiceSettings : ViewModelBase, IOutputServiceSettings {
		protected virtual void OnModified() { }

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

	[Export, Export(typeof(IOutputServiceSettings))]
	sealed class OutputServiceSettingsImpl : OutputServiceSettings {
		static readonly Guid SETTINGS_GUID = new Guid("64414B81-EF07-4DA1-9D21-1F625A6E0080");

		readonly ISettingsService settingsService;

		[ImportingConstructor]
		OutputServiceSettingsImpl(ISettingsService settingsService) {
			this.settingsService = settingsService;

			disableSave = true;
			var sect = settingsService.GetOrCreateSection(SETTINGS_GUID);
			SelectedGuid = sect.Attribute<Guid?>(nameof(SelectedGuid)) ?? SelectedGuid;
			disableSave = false;
		}
		readonly bool disableSave;

		protected override void OnModified() {
			if (disableSave)
				return;
			var sect = settingsService.RecreateSection(SETTINGS_GUID);
			sect.Attribute(nameof(SelectedGuid), SelectedGuid);
		}
	}
}
