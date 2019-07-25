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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Settings;
using dnSpy.Contracts.Settings.Fonts;

namespace dnSpy.Settings.Fonts {
	abstract class ThemeFontSettingsSerializer {
		public abstract ThemeFontSettingsData[] Deserialize();
		public abstract void Remove(string name);
		public abstract void Serialize(FontSettings fontSettings);
	}

	[Export(typeof(ThemeFontSettingsSerializer))]
	sealed class ThemeFontSettingsSerializerImpl : ThemeFontSettingsSerializer {
		static readonly Guid SETTINGS_GUID = new Guid("B744AE6D-24E0-47A3-ACF6-388ECCB1C65A");

		const string ThemeFontSettingsSection = "ThemeFontSettings";
		const string ThemeFontSettingsAttrName = "name";
		const string FontSettingsSection = "FontSettings";
		const string FontSettingsAttrThemeGuid = "theme-guid";
		const string FontSettingsAttrFontFamily = "font-family";
		const string FontSettingsAttrFontSize = "font-size";

		readonly ISettingsSection rootSection;
		readonly Dictionary<string, ISettingsSection> toThemeFontSettingsSection;
		readonly Dictionary<FontSettingsKey, ISettingsSection> toFontSettingsSection;

		readonly struct FontSettingsKey : IEquatable<FontSettingsKey> {
			readonly string name;
			readonly Guid themeGuid;
			public FontSettingsKey(string name, Guid themeGuid) {
				this.name = name;
				this.themeGuid = themeGuid;
			}
			public bool Equals(FontSettingsKey other) => StringComparer.Ordinal.Equals(name, other.name) && themeGuid == other.themeGuid;
			public override bool Equals(object? obj) => obj is FontSettingsKey && Equals((FontSettingsKey)obj);
			public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(name) ^ themeGuid.GetHashCode();
		}

		[ImportingConstructor]
		ThemeFontSettingsSerializerImpl(ISettingsService settingsService) {
			toThemeFontSettingsSection = new Dictionary<string, ISettingsSection>(StringComparer.Ordinal);
			toFontSettingsSection = new Dictionary<FontSettingsKey, ISettingsSection>(EqualityComparer<FontSettingsKey>.Default);
			rootSection = settingsService.GetOrCreateSection(SETTINGS_GUID);
		}

		public override ThemeFontSettingsData[] Deserialize() {
			var list = new List<ThemeFontSettingsData>();
			foreach (var tfsSection in rootSection.SectionsWithName(ThemeFontSettingsSection)) {
				var name = tfsSection.Attribute<string>(ThemeFontSettingsAttrName);
				if (name is null)
					continue;
				if (!(TryGetThemeFontSettingsSection(name) is null)) {
					rootSection.RemoveSection(tfsSection);
					continue;
				}
				list.Add(DeserializeThemeFontSettingsData(tfsSection, name));
			}
			return list.ToArray();
		}

		public override void Remove(string name) {
			if (name is null)
				throw new ArgumentNullException(nameof(name));
			var section = TryGetThemeFontSettingsSection(name);
			if (!(section is null))
				rootSection.RemoveSection(section);
		}

		ThemeFontSettingsData DeserializeThemeFontSettingsData(ISettingsSection tfsSection, string name) {
			var list = new List<FontSettingsData>();
			foreach (var fontSection in tfsSection.SectionsWithName(FontSettingsSection)) {
				var themeGuid = fontSection.Attribute<Guid?>(FontSettingsAttrThemeGuid);
				var fontFamily = fontSection.Attribute<string>(FontSettingsAttrFontFamily);
				var fontSize = fontSection.Attribute<double?>(FontSettingsAttrFontSize);
				if (themeGuid is null || string.IsNullOrWhiteSpace(fontFamily) || fontSize is null || !(TryGetThemeFontSettingsSection(name, themeGuid.Value) is null)) {
					tfsSection.RemoveSection(fontSection);
					continue;
				}
				toFontSettingsSection[new FontSettingsKey(name, themeGuid.Value)] = fontSection;
				list.Add(new FontSettingsData(themeGuid.Value, fontFamily, fontSize.Value));
			}
			toThemeFontSettingsSection[name] = tfsSection;
			return new ThemeFontSettingsData(name, list.ToArray());
		}

		public override void Serialize(FontSettings fontSettings) {
			if (fontSettings is null)
				throw new ArgumentNullException(nameof(fontSettings));
			var section = GetThemeFontSettingsSection(fontSettings.ThemeFontSettings.Name, fontSettings.ThemeGuid);
			section.Attribute(FontSettingsAttrFontFamily, fontSettings.FontFamily.Source);
			section.Attribute(FontSettingsAttrFontSize, fontSettings.FontSize);
		}

		ISettingsSection? TryGetThemeFontSettingsSection(string name) {
			toThemeFontSettingsSection.TryGetValue(name, out var section);
			return section;
		}

		ISettingsSection? TryGetThemeFontSettingsSection(string name, Guid themeGuid) {
			toFontSettingsSection.TryGetValue(new FontSettingsKey(name, themeGuid), out var section);
			return section;
		}

		ISettingsSection GetThemeFontSettingsSection(string name) {
			var section = TryGetThemeFontSettingsSection(name);
			if (!(section is null))
				return section;
			section = rootSection.CreateSection(ThemeFontSettingsSection);
			section.Attribute(ThemeFontSettingsAttrName, name);
			toThemeFontSettingsSection.Add(name, section);
			return section;
		}

		ISettingsSection GetThemeFontSettingsSection(string name, Guid themeGuid) {
			var tfsSection = GetThemeFontSettingsSection(name);
			var section = TryGetThemeFontSettingsSection(name, themeGuid);
			if (!(section is null))
				return section;
			section = tfsSection.CreateSection(FontSettingsSection);
			section.Attribute(FontSettingsAttrThemeGuid, themeGuid);
			toFontSettingsSection.Add(new FontSettingsKey(name, themeGuid), section);
			return section;
		}
	}

	sealed class ThemeFontSettingsData {
		public string Name { get; }
		public FontSettingsData[] FontSettings { get; }
		public ThemeFontSettingsData(string name, FontSettingsData[] fontSettings) {
			Name = name;
			FontSettings = fontSettings;
		}
	}

	sealed class FontSettingsData {
		public Guid ThemeGuid { get; }
		public string FontFamily { get; }
		public double FontSize { get; }
		public FontSettingsData(Guid themeGuid, string fontFamily, double fontSize) {
			ThemeGuid = themeGuid;
			FontFamily = fontFamily;
			FontSize = fontSize;
		}
	}
}
