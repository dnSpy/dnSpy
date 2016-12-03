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

namespace dnSpy.Documents.Tabs {
	interface IDocumentTabServiceSettings : INotifyPropertyChanged {
		bool RestoreTabs { get; set; }
		bool DecompileFullType { get; set; }
	}

	class DocumentTabServiceSettings : ViewModelBase, IDocumentTabServiceSettings {
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

		public DocumentTabServiceSettings Clone() => CopyTo(new DocumentTabServiceSettings());
		public DocumentTabServiceSettings CopyTo(DocumentTabServiceSettings other) {
			other.RestoreTabs = RestoreTabs;
			other.DecompileFullType = DecompileFullType;
			return other;
		}
	}

	[Export, Export(typeof(IDocumentTabServiceSettings))]
	sealed class DocumentTabServiceSettingsImpl : DocumentTabServiceSettings {
		static readonly Guid SETTINGS_GUID = new Guid("1ACE15FD-D689-40DC-B1E7-6EEC25B3116F");

		readonly ISettingsService settingsService;

		[ImportingConstructor]
		DocumentTabServiceSettingsImpl(ISettingsService settingsService) {
			this.settingsService = settingsService;

			disableSave = true;
			var sect = settingsService.GetOrCreateSection(SETTINGS_GUID);
			RestoreTabs = sect.Attribute<bool?>(nameof(RestoreTabs)) ?? RestoreTabs;
			DecompileFullType = sect.Attribute<bool?>(nameof(DecompileFullType)) ?? DecompileFullType;
			disableSave = false;
		}
		readonly bool disableSave;

		protected override void OnModified() {
			if (disableSave)
				return;
			var sect = settingsService.RecreateSection(SETTINGS_GUID);
			sect.Attribute(nameof(RestoreTabs), RestoreTabs);
			sect.Attribute(nameof(DecompileFullType), DecompileFullType);
		}
	}
}
