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
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Settings.Fonts;
using dnSpy.Contracts.Themes;

namespace dnSpy.Settings.Fonts {
	[Export(typeof(ThemeFontSettingsService))]
	sealed class ThemeFontSettingsServiceImpl : ThemeFontSettingsService {
		readonly IThemeService themeService;
		readonly ThemeFontSettingsSerializer themeFontSettingsSerializer;
		readonly Dictionary<string, IThemeFontSettingsDefinitionMetadata> toMetadata;
		readonly Dictionary<string, ThemeFontSettingsImpl> toSettings;

		[ImportingConstructor]
		ThemeFontSettingsServiceImpl(IThemeService themeService, ThemeFontSettingsSerializer themeFontSettingsSerializer, [ImportMany] Lazy<ThemeFontSettingsDefinition, IThemeFontSettingsDefinitionMetadata>[] themeFontSettingsDefinitions) {
			this.themeService = themeService;
			this.themeFontSettingsSerializer = themeFontSettingsSerializer;
			toMetadata = new Dictionary<string, IThemeFontSettingsDefinitionMetadata>(themeFontSettingsDefinitions.Length, StringComparer.Ordinal);
			toSettings = new Dictionary<string, ThemeFontSettingsImpl>(StringComparer.Ordinal);
			foreach (var lz in themeFontSettingsDefinitions) {
				Debug.Assert(!toMetadata.ContainsKey(lz.Metadata.Name));
				toMetadata[lz.Metadata.Name] = lz.Metadata;
			}
			themeService.ThemeChangedHighPriority += ThemeService_ThemeChangedHighPriority;

			foreach (var data in themeFontSettingsSerializer.Deserialize()) {
				var themeSettings = TryGetSettings(data.Name);
				if (themeSettings == null) {
					themeFontSettingsSerializer.Remove(data.Name);
					continue;
				}
				foreach (var fs in data.FontSettings) {
					var fontSettings = themeSettings.GetSettings(fs.ThemeGuid);
					if (fontSettings == null)
						continue;
					fontSettings.FontFamily = new FontFamily(fs.FontFamily);
					fontSettings.FontSize = fs.FontSize;
				}
			}

			canSerialize = true;
		}
		readonly bool canSerialize;

		void FontSetting_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (!canSerialize)
				return;
			var fontSettings = (FontSettings)sender;
			if (e.PropertyName == nameof(fontSettings.FontFamily) || e.PropertyName == nameof(fontSettings.FontSize))
				themeFontSettingsSerializer.Serialize(fontSettings);
		}

		void ThemeService_ThemeChangedHighPriority(object sender, ThemeChangedEventArgs e) {
			foreach (var settings in toSettings.Values.ToArray())
				settings.SetActive(themeService.Theme.Guid);
		}

		public override ThemeFontSettings GetSettings(string name) {
			var settings = TryGetSettings(name);
			if (settings == null)
				throw new ArgumentOutOfRangeException(nameof(name));
			return settings;
		}

		ThemeFontSettingsImpl TryGetSettings(string name) {
			if (name == null)
				throw new ArgumentNullException(nameof(name));
			if (toSettings.TryGetValue(name, out var settings))
				return settings;
			if (!toMetadata.TryGetValue(name, out var md))
				return null;
			settings = new ThemeFontSettingsImpl(name, md.FontType, GetDefaultFontInfo(md.FontType));
			toSettings.Add(name, settings);
			settings.FontSettingsCreated += Settings_FontSettingsCreated;
			settings.Initialize(themeService.Theme.Guid);
			return settings;
		}

		void Settings_FontSettingsCreated(object sender, FontSettingsCreatedEventArgs e) =>
			e.FontSettings.PropertyChanged += FontSetting_PropertyChanged;

		DefaultFontInfo TextEditorDefaultFontInfo {
			get {
				if (textEditorDefaultFontInfo.FontFamily == null)
					textEditorDefaultFontInfo = new DefaultFontInfo(new FontFamily(FontUtilities.GetDefaultTextEditorFont()), FontUtilities.DEFAULT_FONT_SIZE);
				return textEditorDefaultFontInfo;
			}
		}
		DefaultFontInfo textEditorDefaultFontInfo;

		DefaultFontInfo HexEditorDefaultFontInfo {
			get {
				if (hexEditorDefaultFontInfo.FontFamily == null)
					hexEditorDefaultFontInfo = new DefaultFontInfo(new FontFamily(FontUtilities.GetDefaultMonospacedFont()), FontUtilities.DEFAULT_FONT_SIZE);
				return hexEditorDefaultFontInfo;
			}
		}
		DefaultFontInfo hexEditorDefaultFontInfo;

		DefaultFontInfo MonospacedDefaultFontInfo {
			get {
				if (uiDefaultFontInfo.FontFamily == null)
					monospacedDefaultFontInfo = new DefaultFontInfo(new FontFamily(FontUtilities.GetDefaultMonospacedFont()), FontUtilities.DEFAULT_FONT_SIZE);
				return monospacedDefaultFontInfo;
			}
		}
		DefaultFontInfo monospacedDefaultFontInfo;

		DefaultFontInfo UIDefaultFontInfo {
			get {
				if (uiDefaultFontInfo.FontFamily == null)
					uiDefaultFontInfo = new DefaultFontInfo(SystemFonts.MessageFontFamily, SystemFonts.MessageFontSize);
				return uiDefaultFontInfo;
			}
		}
		DefaultFontInfo uiDefaultFontInfo;

		DefaultFontInfo GetDefaultFontInfo(FontType fontType) {
			switch (fontType) {
			case FontType.TextEditor:		return TextEditorDefaultFontInfo;
			case FontType.HexEditor:		return HexEditorDefaultFontInfo;
			case FontType.Monospaced:		return MonospacedDefaultFontInfo;
			case FontType.UI:				return UIDefaultFontInfo;
			default:
				throw new ArgumentOutOfRangeException(nameof(fontType));
			}
		}
	}
}
