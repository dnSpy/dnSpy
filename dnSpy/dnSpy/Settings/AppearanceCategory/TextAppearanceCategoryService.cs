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
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Settings.AppearanceCategory;
using dnSpy.Contracts.Settings.Fonts;
using dnSpy.Contracts.Themes;

namespace dnSpy.Settings.AppearanceCategory {
	[Export(typeof(ITextAppearanceCategoryService))]
	[Export(typeof(TextAppearanceCategoryService))]
	sealed class TextAppearanceCategoryService : ITextAppearanceCategoryService {
		readonly Dictionary<string, TextAppearanceCategory> categoryToTextAppearanceCategoryDefinition;

		internal TextAppearanceCategory[] TextAppearanceCategories => categoryToTextAppearanceCategoryDefinition.Values.ToArray();

		[ImportingConstructor]
		TextAppearanceCategoryService(IThemeService themeService, ThemeFontSettingsService themeFontSettingsService, [ImportMany] TextAppearanceCategoryDefinition[] textAppearanceCategoryDefinitions) {
			themeService.ThemeChangedHighPriority += ThemeService_ThemeChangedHighPriority;
			categoryToTextAppearanceCategoryDefinition = new Dictionary<string, TextAppearanceCategory>(textAppearanceCategoryDefinitions.Length, StringComparer.Ordinal);
			foreach (var def in textAppearanceCategoryDefinitions) {
				Debug.Assert(!categoryToTextAppearanceCategoryDefinition.ContainsKey(def.Category));
				categoryToTextAppearanceCategoryDefinition[def.Category] = new TextAppearanceCategory(def, themeFontSettingsService.GetSettings(def.Category));
			}
		}

		void ThemeService_ThemeChangedHighPriority(object sender, ThemeChangedEventArgs e) {
			foreach (var settings in categoryToTextAppearanceCategoryDefinition.Values)
				settings.ClearCache();
			foreach (var settings in categoryToTextAppearanceCategoryDefinition.Values)
				settings.OnThemeChanged();
		}

		public ITextAppearanceCategory GetSettings(string category) {
			if (category == null)
				throw new ArgumentNullException(nameof(category));
			TextAppearanceCategory settings;
			if (!categoryToTextAppearanceCategoryDefinition.TryGetValue(category, out settings))
				throw new ArgumentOutOfRangeException(nameof(category));
			return settings;
		}
	}
}
