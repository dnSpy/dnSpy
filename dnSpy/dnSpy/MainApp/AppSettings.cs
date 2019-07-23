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
using System.ComponentModel;
using System.ComponentModel.Composition;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Settings;

namespace dnSpy.MainApp {
	class AppSettings : ViewModelBase {
		public bool AllowMoreThanOneInstance {
			get => allowMoreThanOneInstance;
			set {
				if (allowMoreThanOneInstance != value) {
					allowMoreThanOneInstance = value;
					OnPropertyChanged(nameof(AllowMoreThanOneInstance));
				}
			}
		}
		bool allowMoreThanOneInstance = true;
	}

	[Export]
	sealed class AppSettingsImpl : AppSettings {
		static readonly Guid SETTINGS_GUID = new Guid("071CF92D-ACFA-46A1-8EEF-DFAC1D01E644");

		readonly ISettingsService settingsService;

		[ImportingConstructor]
		AppSettingsImpl(ISettingsService settingsService) {
			this.settingsService = settingsService;

			var sect = settingsService.GetOrCreateSection(SETTINGS_GUID);
			AllowMoreThanOneInstance = sect.Attribute<bool?>(nameof(AllowMoreThanOneInstance)) ?? AllowMoreThanOneInstance;
			PropertyChanged += AppSettingsImpl_PropertyChanged;
		}

		void AppSettingsImpl_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
			var sect = settingsService.RecreateSection(SETTINGS_GUID);
			sect.Attribute(nameof(AllowMoreThanOneInstance), AllowMoreThanOneInstance);
		}
	}
}
