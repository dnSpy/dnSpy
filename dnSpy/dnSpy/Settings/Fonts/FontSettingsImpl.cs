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

using System;
using System.Windows.Media;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Settings.Fonts;

namespace dnSpy.Settings.Fonts {
	sealed class FontSettingsImpl : FontSettings {
		public override ThemeFontSettings ThemeFontSettings { get; }
		public override Guid ThemeGuid { get; }

		public override FontFamily FontFamily {
			get { return fontFamily; }
			set {
				if (value == null)
					throw new ArgumentNullException(nameof(value));
				if (fontFamily.Source != value.Source) {
					fontFamily = value;
					OnPropertyChanged(nameof(FontFamily));
				}
			}
		}
		FontFamily fontFamily;

		public override double FontSize {
			get { return fontSize; }
			set {
				var newValue = FontUtilities.FilterFontSize(value);
				if (fontSize != newValue) {
					fontSize = newValue;
					OnPropertyChanged(nameof(FontSize));
				}
			}
		}
		double fontSize;

		public FontSettingsImpl(ThemeFontSettings owner, Guid themeGuid, FontFamily fontFamily, double fontSize) {
			ThemeFontSettings = owner ?? throw new ArgumentNullException(nameof(owner));
			ThemeGuid = themeGuid;
			this.fontFamily = fontFamily ?? throw new ArgumentNullException(nameof(fontFamily));
			FontSize = fontSize;
		}
	}
}
