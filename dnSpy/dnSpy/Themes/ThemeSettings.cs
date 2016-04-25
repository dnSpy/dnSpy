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
	[Export, PartCreationPolicy(CreationPolicy.Shared)]
	sealed class ThemeSettings {
		static readonly Guid SETTINGS_GUID = new Guid("34CF0AF5-D265-4393-BC68-9B8C9B8EA622");

		readonly ISettingsManager settingsManager;

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
		ThemeSettings(ISettingsManager settingsManager) {
			this.settingsManager = settingsManager;

			this.disableSave = true;
			var sect = settingsManager.GetOrCreateSection(SETTINGS_GUID);
			this.ThemeGuid = sect.Attribute<Guid?>("ThemeGuid");
			this.ShowAllThemes = sect.Attribute<bool?>("ShowAllThemes") ?? ShowAllThemes;
			this.disableSave = false;
		}
		readonly bool disableSave;

		void OnModified() {
			if (disableSave)
				return;
			var sect = settingsManager.RecreateSection(SETTINGS_GUID);
			sect.Attribute("ThemeGuid", ThemeGuid);
			sect.Attribute("ShowAllThemes", ShowAllThemes);
		}
	}
}
