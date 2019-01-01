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
using System.Collections.Generic;
using System.Linq;
using dnSpy.Contracts.Settings.Fonts;

namespace dnSpy.Settings.Fonts {
	sealed class ThemeFontSettingsImpl : ThemeFontSettings {
		public override string Name { get; }
		public override FontType FontType { get; }
		public override FontSettings Active => active;

		internal IEnumerable<FontSettingsImpl> FontSettings => toSettings.Values.ToArray();

		readonly Dictionary<Guid, FontSettingsImpl> toSettings;
		readonly DefaultFontInfo defaultFontInfo;
		FontSettings active;

		public ThemeFontSettingsImpl(string name, FontType fontType, DefaultFontInfo defaultFontInfo) {
			Name = name ?? throw new ArgumentNullException(nameof(name));
			FontType = fontType;
			toSettings = new Dictionary<Guid, FontSettingsImpl>();
			this.defaultFontInfo = defaultFontInfo;
		}

		internal void Initialize(Guid activeThemeGuid) => active = GetSettings(activeThemeGuid);

		public override FontSettings GetSettings(Guid themeGuid) {
			if (toSettings.TryGetValue(themeGuid, out var settings))
				return settings;
			settings = new FontSettingsImpl(this, themeGuid, defaultFontInfo.FontFamily, defaultFontInfo.FontSize);
			toSettings.Add(themeGuid, settings);
			FontSettingsCreated?.Invoke(this, new FontSettingsCreatedEventArgs(settings));
			return settings;
		}

		internal void SetActive(Guid themeGuid) {
			var settings = GetSettings(themeGuid);
			if (Active == settings)
				return;
			active = settings;
			OnPropertyChanged(nameof(Active));
		}

		internal event EventHandler<FontSettingsCreatedEventArgs> FontSettingsCreated;
	}

	sealed class FontSettingsCreatedEventArgs : EventArgs {
		public FontSettings FontSettings { get; }
		public FontSettingsCreatedEventArgs(FontSettings fontSettings) => FontSettings = fontSettings ?? throw new ArgumentNullException(nameof(fontSettings));
	}
}
