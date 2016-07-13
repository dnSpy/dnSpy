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
using System.Windows;
using System.Windows.Media;
using dnSpy.Contracts.Plugin;
using dnSpy.Contracts.Themes;

namespace dnSpy.Contracts.Hex {
	[ExportAutoLoaded(LoadType = AutoLoadedLoadType.BeforePlugins)]
	sealed class HexBoxThemeHelper : IAutoLoaded {
		[ImportingConstructor]
		HexBoxThemeHelper(IThemeManager themeManager) {
			themeManager.ThemeChanged += ThemeManager_ThemeChanged;
			UpdateColors(themeManager);
		}

		void ThemeManager_ThemeChanged(object sender, ThemeChangedEventArgs e) => UpdateColors((IThemeManager)sender);

		void UpdateColors(IThemeManager themeManager) {
			var theme = themeManager.Theme;

			var color = theme.GetColor(ColorType.HexText);
			Application.Current.Resources[GetBackgroundResourceKey(ColorType.HexText)] = GetBrush(color.Background);
			Application.Current.Resources[GetForegroundResourceKey(ColorType.HexText)] = GetBrush(color.Foreground);
			Application.Current.Resources[GetFontStyleResourceKey(ColorType.HexText)] = color.FontStyle ?? FontStyles.Normal;
			Application.Current.Resources[GetFontWeightResourceKey(ColorType.HexText)] = color.FontWeight ?? FontWeights.Normal;

			UpdateForeground(theme, ColorType.HexOffset);
			UpdateForeground(theme, ColorType.HexByte0);
			UpdateForeground(theme, ColorType.HexByte1);
			UpdateForeground(theme, ColorType.HexByteError);
			UpdateForeground(theme, ColorType.HexAscii);
			UpdateBackground(theme, ColorType.HexCaret);
			UpdateBackground(theme, ColorType.HexInactiveCaret);
			UpdateBackground(theme, ColorType.HexSelection);
		}

		static void UpdateForeground(ITheme theme, ColorType colorType) {
			var color = theme.GetTextColor(colorType);
			Application.Current.Resources[GetForegroundResourceKey(colorType)] = GetBrush(color.Foreground);
		}

		static void UpdateBackground(ITheme theme, ColorType colorType) {
			var color = theme.GetTextColor(colorType);
			Application.Current.Resources[GetBackgroundResourceKey(colorType)] = GetBrush(color.Background);
		}

		static Brush GetBrush(Brush b) => b ?? Brushes.Transparent;
		public static string GetBackgroundResourceKey(ColorType colorType) => string.Format("HB_{0}_Background", Enum.GetName(typeof(ColorType), colorType));
		public static string GetForegroundResourceKey(ColorType colorType) => string.Format("HB_{0}_Foreground", Enum.GetName(typeof(ColorType), colorType));
		public static string GetFontStyleResourceKey(ColorType colorType) => string.Format("HB_{0}_FontStyle", Enum.GetName(typeof(ColorType), colorType));
		public static string GetFontWeightResourceKey(ColorType colorType) => string.Format("HB_{0}_FontWeight", Enum.GetName(typeof(ColorType), colorType));
	}
}
