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
using System.Windows;
using System.Windows.Media;
using dnSpy.Contracts.Themes;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Contracts.Text.Classification {
	/// <summary>
	/// Theme classification definition
	/// </summary>
	abstract class ThemeClassificationFormatDefinition : ClassificationFormatDefinition, IThemeFormatDefinition {
		readonly TextColor textColor;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="textColor">Color</param>
		protected ThemeClassificationFormatDefinition(TextColor textColor) => this.textColor = textColor;

		/// <summary>
		/// Creates a new <see cref="ResourceDictionary"/>
		/// </summary>
		/// <param name="theme">Theme</param>
		/// <returns></returns>
		public ResourceDictionary CreateResourceDictionary(ITheme theme) {
			if (theme is null)
				throw new ArgumentNullException(nameof(theme));

			var res = CreateResourceDictionary();

			var isBold = GetIsBold(theme);
			if (!(isBold is null))
				res.Add(IsBoldId, isBold.Value);

			var isItalic = GetIsItalic(theme);
			if (!(isItalic is null))
				res.Add(IsItalicId, isItalic.Value);

			var fg = GetForeground(theme);
			if (!(fg is null)) {
				res[ForegroundBrushId] = fg;
				if (fg.Opacity != 1)
					res[ForegroundOpacityId] = fg.Opacity;
			}

			var bg = GetBackground(theme);
			if (!(bg is null)) {
				res[BackgroundBrushId] = bg;
				if (bg.Opacity != 1)
					res[BackgroundOpacityId] = bg.Opacity;
			}

			return res;
		}

		Brush? GetForeground(ITheme theme) => theme.GetExplicitColor(textColor.ToColorType()).Foreground;
		Brush? GetBackground(ITheme theme) => theme.GetExplicitColor(textColor.ToColorType()).Background;

		bool? GetIsBold(ITheme theme) {
			var tc = theme.GetExplicitColor(textColor.ToColorType());
			if (tc.FontWeight is null)
				return null;
			return tc.FontWeight.Value == FontWeights.Bold;
		}

		bool? GetIsItalic(ITheme theme) {
			var tc = theme.GetExplicitColor(textColor.ToColorType());
			if (tc.FontStyle is null)
				return null;
			return tc.FontStyle.Value == FontStyles.Italic;
		}
	}
}
