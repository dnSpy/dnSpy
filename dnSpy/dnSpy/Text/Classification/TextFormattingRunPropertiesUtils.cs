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

using dnSpy.Contracts.Text.Formatting;

namespace dnSpy.Text.Classification {
	static class TextFormattingRunPropertiesUtils {
		public static TextFormattingRunProperties Merge(TextFormattingRunProperties lowPrio, TextFormattingRunProperties hiPrio) {
			var p = hiPrio;

			if (p.TypefaceEmpty && !lowPrio.TypefaceEmpty)
				p = p.SetTypeface(lowPrio.Typeface);
			if (p.BoldEmpty && !lowPrio.BoldEmpty)
				p = p.SetBold(lowPrio.Bold);
			if (p.ItalicEmpty && !lowPrio.ItalicEmpty)
				p = p.SetItalic(lowPrio.Italic);
			if (p.ForegroundBrushEmpty && !lowPrio.ForegroundBrushEmpty)
				p = p.SetForegroundBrush(lowPrio.ForegroundBrush);
			if (p.BackgroundBrushEmpty && !lowPrio.BackgroundBrushEmpty)
				p = p.SetBackgroundBrush(lowPrio.BackgroundBrush);
			if (p.ForegroundOpacityEmpty && !lowPrio.ForegroundOpacityEmpty)
				p = p.SetForegroundOpacity(lowPrio.ForegroundOpacity);
			if (p.BackgroundOpacityEmpty && !lowPrio.BackgroundOpacityEmpty)
				p = p.SetBackgroundOpacity(lowPrio.BackgroundOpacity);
			if (p.CultureInfoEmpty && !lowPrio.CultureInfoEmpty)
				p = p.SetCultureInfo(lowPrio.CultureInfo);
			if (p.FontHintingEmSizeEmpty && !lowPrio.FontHintingEmSizeEmpty)
				p = p.SetFontHintingEmSize(lowPrio.FontHintingEmSize);
			if (p.FontRenderingEmSizeEmpty && !lowPrio.FontRenderingEmSizeEmpty)
				p = p.SetFontRenderingEmSize(lowPrio.FontRenderingEmSize);
			if (p.TextDecorationsEmpty && !lowPrio.TextDecorationsEmpty)
				p = p.SetTextDecorations(lowPrio.TextDecorations);
			if (p.TextEffectsEmpty && !lowPrio.TextEffectsEmpty)
				p = p.SetTextEffects(lowPrio.TextEffects);

			return p;
		}
	}
}
