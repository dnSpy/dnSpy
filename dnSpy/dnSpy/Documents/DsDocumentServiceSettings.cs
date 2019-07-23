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

namespace dnSpy.Documents {
	interface IDsDocumentServiceSettings : INotifyPropertyChanged {
		bool UseMemoryMappedIO { get; set; }
	}

	class DsDocumentServiceSettings : ViewModelBase, IDsDocumentServiceSettings {
		public bool UseMemoryMappedIO {
			get => useMemoryMappedIO;
			set {
				if (useMemoryMappedIO != value) {
					useMemoryMappedIO = value;
					OnPropertyChanged(nameof(UseMemoryMappedIO));
				}
			}
		}
		bool useMemoryMappedIO = false;
	}

	[Export, Export(typeof(IDsDocumentServiceSettings))]
	sealed class DsDocumentServiceSettingsImpl : DsDocumentServiceSettings {
		static readonly Guid SETTINGS_GUID = new Guid("3643CE93-84D5-455A-9183-94B58BC80942");

		readonly ISettingsService settingsService;

		[ImportingConstructor]
		DsDocumentServiceSettingsImpl(ISettingsService settingsService) {
			this.settingsService = settingsService;

			var sect = settingsService.GetOrCreateSection(SETTINGS_GUID);
			UseMemoryMappedIO = sect.Attribute<bool?>(nameof(UseMemoryMappedIO)) ?? UseMemoryMappedIO;
			PropertyChanged += DsDocumentServiceSettingsImpl_PropertyChanged;
		}

		void DsDocumentServiceSettingsImpl_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
			var sect = settingsService.RecreateSection(SETTINGS_GUID);
			sect.Attribute(nameof(UseMemoryMappedIO), UseMemoryMappedIO);
		}
	}
}
