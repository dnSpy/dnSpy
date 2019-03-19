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

using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Windows;
using dnSpy.AsmEditor.Properties;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.Settings.AppearanceCategory;
using dnSpy.Contracts.Settings.Fonts;

namespace dnSpy.AsmEditor {
	[ExportExtension]
	sealed class TheExtension : IExtension {
		[ImportingConstructor]
		TheExtension(ThemeFontSettingsService themeFontSettingsService) {
			var themeFontSettings = themeFontSettingsService.GetSettings(AppearanceCategoryConstants.TextEditor);
			themeFontSettings.PropertyChanged += ThemeFontSettings_PropertyChanged;
			Initialize(themeFontSettings.Active);
		}

		void ThemeFontSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			var themeFontSettings = (ThemeFontSettings)sender;
			if (e.PropertyName == nameof(themeFontSettings.Active))
				Initialize(themeFontSettings.Active);
		}

		void Initialize(FontSettings fontSettings) {
			if (prevFontSettings == fontSettings)
				return;
			if (prevFontSettings != null)
				prevFontSettings.PropertyChanged -= FontSettings_PropertyChanged;
			prevFontSettings = fontSettings;
			fontSettings.PropertyChanged += FontSettings_PropertyChanged;
			UpdateFont(fontSettings);
		}
		FontSettings prevFontSettings;

		void UpdateFont(FontSettings fontSettings) =>
			Application.Current.Resources["TextEditorFontFamily"] = fontSettings.FontFamily;

		void FontSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			var fontSettings = (FontSettings)sender;
			if (e.PropertyName == nameof(fontSettings.FontFamily))
				UpdateFont(fontSettings);
		}

		public IEnumerable<string> MergedResourceDictionaries {
			get {
				yield return "Themes/wpf.styles.templates.xaml";
				yield return "Hex/Nodes/wpf.styles.templates.xaml";
			}
		}

		public ExtensionInfo ExtensionInfo {
			get {
				return new ExtensionInfo {
					ShortDescription = dnSpy_AsmEditor_Resources.Plugin_ShortDescription,
				};
			}
		}

		public void OnEvent(ExtensionEvent @event, object obj) {
		}
	}
}
