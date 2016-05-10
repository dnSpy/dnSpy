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
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.Settings;
using dnSpy.Shared.MVVM;

namespace dnSpy.Languages {
	interface ILanguageManagerSettings : INotifyPropertyChanged {
		Guid LanguageGuid { get; }
	}

	class LanguageManagerSettings : ViewModelBase, ILanguageManagerSettings {
		protected virtual void OnModified() { }

		public Guid LanguageGuid {
			get { return languageGuid; }
			set {
				if (languageGuid != value) {
					languageGuid = value;
					OnPropertyChanged(nameof(LanguageGuid));
					OnModified();
				}
			}
		}
		Guid languageGuid = LanguageConstants.LANGUAGE_CSHARP;
	}

	[Export, Export(typeof(ILanguageManagerSettings))]
	sealed class LanguageManagerSettingsImpl : LanguageManagerSettings {
		static readonly Guid SETTINGS_GUID = new Guid("6A7E565D-DC09-4AAE-A7C8-E86A835FCBFC");

		readonly ISettingsManager settingsManager;

		[ImportingConstructor]
		LanguageManagerSettingsImpl(ISettingsManager settingsManager) {
			this.settingsManager = settingsManager;

			this.disableSave = true;
			var sect = settingsManager.GetOrCreateSection(SETTINGS_GUID);
			this.LanguageGuid = sect.Attribute<Guid?>(nameof(LanguageGuid)) ?? this.LanguageGuid;
			this.disableSave = false;
		}
		readonly bool disableSave;

		protected override void OnModified() {
			if (disableSave)
				return;
			var sect = settingsManager.RecreateSection(SETTINGS_GUID);
			sect.Attribute(nameof(LanguageGuid), LanguageGuid);
		}
	}
}
