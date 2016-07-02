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
using System.Windows;
using System.Windows.Media;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Contracts.Themes;

namespace dnSpy.Text.Classification {
	sealed class DefaultTextEditorFormatDefinition : TextEditorFormatDefinition {
		const ColorType colorType = ColorType.DefaultText;
		readonly ITextEditorSettings textEditorSettings;

		public DefaultTextEditorFormatDefinition(ITextEditorSettings textEditorSettings) {
			this.textEditorSettings = textEditorSettings;
		}

		// Round to an integer so the IFormattedLine property sizes (Height etc) are integers
		public override double? GetFontRenderingEmSize(ITheme theme) => Math.Round(textEditorSettings.FontSize);
		public override Brush GetForeground(ITheme theme) => theme.GetColor(colorType).Foreground;
		public override Brush GetWindowBackground(ITheme theme) => theme.GetColor(colorType).Background;

		public override Typeface GetTypeface(ITheme theme) {
			var tc = theme.GetColor(ColorType.Text);
			return new Typeface(textEditorSettings.FontFamily, tc.FontStyle ?? FontStyles.Normal, tc.FontWeight ?? FontWeights.Normal, FontStretches.Normal, ClassificationFontUtils.DefaultFallbackFontFamily);
		}

		public override bool? GetIsBold(ITheme theme) {
			var tc = theme.GetColor(colorType);
			if (tc.FontWeight == null)
				return null;
			return tc.FontWeight.Value == FontWeights.Bold;
		}

		public override bool? GetIsItalic(ITheme theme) {
			var tc = theme.GetColor(colorType);
			if (tc.FontStyle == null)
				return null;
			return tc.FontStyle.Value == FontStyles.Italic;
		}
	}
}
