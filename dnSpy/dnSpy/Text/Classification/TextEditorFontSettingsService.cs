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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Contracts.Themes;
using dnSpy.Text.MEF;

namespace dnSpy.Text.Classification {
	[Export(typeof(ITextEditorFontSettingsService))]
	sealed class TextEditorFontSettingsService : ITextEditorFontSettingsService {
		readonly Dictionary<string, TextEditorFontSettings> toTextEditorFontSettings;
		readonly ITextEditorFontSettings defaultTextEditorFontSettings;

		[ImportingConstructor]
		TextEditorFontSettingsService(IThemeService themeService, ITextEditorSettings textEditorSettings, [ImportMany] IEnumerable<Lazy<TextEditorFormatDefinition, ITextEditorFormatDefinitionMetadata>> textEditorFormatDefinitions) {
			themeService.ThemeChangedHighPriority += ThemeService_ThemeChangedHighPriority;
			var provider = new TextEditorFontSettingsDictionaryProvider(textEditorSettings, textEditorFormatDefinitions);
			toTextEditorFontSettings = provider.Result;
			defaultTextEditorFontSettings = provider.DefaultSettings;
		}

		void ThemeService_ThemeChangedHighPriority(object sender, ThemeChangedEventArgs e) {
			foreach (var settings in toTextEditorFontSettings.Values)
				settings.ClearCache();
			foreach (var settings in toTextEditorFontSettings.Values)
				settings.OnThemeChanged();
		}

		public ITextEditorFontSettings GetSettings(string category) {
			if (category == null)
				throw new ArgumentNullException(nameof(category));
			TextEditorFontSettings settings;
			toTextEditorFontSettings.TryGetValue(category, out settings);
			return settings ?? defaultTextEditorFontSettings;
		}
	}
}
