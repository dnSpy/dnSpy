/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Settings.Fonts;
using dnSpy.Contracts.Settings.FontsAndColors;

namespace dnSpy.Settings.AppearanceCategory {
	[Export(typeof(FontAndColorOptionsProvider))]
	sealed class FontAndColorOptionsProviderImpl : FontAndColorOptionsProvider {
		readonly TextAppearanceCategoryService textAppearanceCategoryService;

		[ImportingConstructor]
		FontAndColorOptionsProviderImpl(TextAppearanceCategoryService textAppearanceCategoryService) => this.textAppearanceCategoryService = textAppearanceCategoryService;

		public override IEnumerable<FontAndColorOptions> GetFontAndColors() {
			foreach (var category in textAppearanceCategoryService.TextAppearanceCategories) {
				if (category.IsUserVisible)
					yield return new FontAndColorOptionsImpl(category);
			}
		}
	}

	sealed class FontAndColorOptionsImpl : FontAndColorOptions {
		public override string DisplayName => textAppearanceCategory.DisplayName;
		public override string Name => textAppearanceCategory.Category;
		public override FontOption FontOption { get; }

		readonly TextAppearanceCategory textAppearanceCategory;
		readonly FontSettings fontSettings;

		public FontAndColorOptionsImpl(TextAppearanceCategory textAppearanceCategory) {
			this.textAppearanceCategory = textAppearanceCategory;
			fontSettings = textAppearanceCategory.ThemeFontSettings.Active;
			FontOption = new FontOption(fontSettings.FontType) {
				FontFamily = fontSettings.FontFamily,
				FontSize = fontSettings.FontSize,
			};
		}

		public override void OnApply() {
			fontSettings.FontFamily = FontOption.FontFamily;
			fontSettings.FontSize = FontOption.FontSize;
		}
	}
}
