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
using dnSpy.Contracts.Settings;

namespace dnSpy.Themes {
	[Export]
	sealed class ThemeSettings {
		static readonly Guid SETTINGS_GUID = new Guid("34CF0AF5-D265-4393-BC68-9B8C9B8EA622");

		readonly ISettingsService settingsService;

		public Guid? ThemeGuid {
			get { return themeGuid; }
			set {
				if (themeGuid != value) {
					themeGuid = value;
					OnModified();
				}
			}
		}
		Guid? themeGuid;

		public bool ShowAllThemes {
			get { return showAllThemes; }
			set {
				if (showAllThemes != value) {
					showAllThemes = value;
					OnModified();
				}
			}
		}
		bool showAllThemes = false;

		[ImportingConstructor]
		ThemeSettings(ISettingsService settingsService) {
			this.settingsService = settingsService;

			this.disableSave = true;
			var sect = settingsService.GetOrCreateSection(SETTINGS_GUID);
			this.ThemeGuid = sect.Attribute<Guid?>(nameof(ThemeGuid));
			this.ShowAllThemes = sect.Attribute<bool?>(nameof(ShowAllThemes)) ?? ShowAllThemes;
			this.disableSave = false;
		}
		readonly bool disableSave;

		void OnModified() {
			if (disableSave)
				return;
			var sect = settingsService.RecreateSection(SETTINGS_GUID);
			sect.Attribute(nameof(ThemeGuid), ThemeGuid);
			sect.Attribute(nameof(ShowAllThemes), ShowAllThemes);
		}
	}
}
