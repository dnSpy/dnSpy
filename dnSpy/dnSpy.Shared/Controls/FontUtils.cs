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
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace dnSpy.Shared.Controls {
	public static class FontUtils {
		public static readonly double DEFAULT_FONT_SIZE = 10.0 * 4 / 3;
		const double MIN_FONT_SIZE = 1;
		const double MAX_FONT_SIZE = 200;

		const string GLOBAL_MONOSPACE_FONT = "Global Monospace";

		static readonly string[] monospacedFontsToCheck = new string[] {
			"Consolas",
			"Lucida Console",
			"Courier New",
			"Courier",
			GLOBAL_MONOSPACE_FONT,
		};

		public static string GetDefaultMonospacedFont() {
			foreach (var name in monospacedFontsToCheck) {
				if (Exists(name))
					return name;
			}

			Debug.Fail("Couldn't find a default monospace font");

			return "Courier New";
		}

		public static bool Exists(string name) {
			foreach (var ff in Fonts.SystemFontFamilies) {
				if (ff.Source.Equals(name, StringComparison.OrdinalIgnoreCase))
					return true;
			}
			return false;
		}

		public static double FilterFontSize(double size) {
			if (size < MIN_FONT_SIZE)
				return MIN_FONT_SIZE;
			if (size > MAX_FONT_SIZE)
				return MAX_FONT_SIZE;
			return size;
		}

		public static bool IsSymbol(FontFamily ff) {
			foreach (var tf in ff.GetTypefaces()) {
				GlyphTypeface gtf;
				if (!tf.TryGetGlyphTypeface(out gtf))
					return true;
				if (gtf.Symbol)
					return true;
			}
			return false;
		}

		public static FontFamily[] GetMonospacedFonts() => Fonts.SystemFontFamilies.Where(a => IsMonospacedFont(a)).OrderBy(a => a.Source.ToUpperInvariant()).ToArray();

		// Checks chars 0x20-0x7E (the only ones used by the hex editor) whether they have the same
		// width and height. There's probably a better way of doing this...
		public static bool IsMonospacedFont(FontFamily ff) {
			if (ff.Source.Equals(GLOBAL_MONOSPACE_FONT, StringComparison.OrdinalIgnoreCase))
				return true;

			foreach (var tf in ff.GetTypefaces()) {
				if (tf.Weight != FontWeights.Normal)
					continue;
				if (tf.Style != FontStyles.Normal)
					continue;

				GlyphTypeface gtf;
				if (!tf.TryGetGlyphTypeface(out gtf))
					return false;
				if (gtf.Symbol)
					return false;
				if (!CheckSameSize(gtf))
					return false;

				return true;
			}

			return false;
		}

		static bool CheckSameSize(GlyphTypeface gtf) {
			double? width = null, height = null;
			for (char c = ' '; c <= (char)0x7E; c++) {
				ushort glyphIndex;
				if (!gtf.CharacterToGlyphMap.TryGetValue(c, out glyphIndex))
					return false;
				double w;
				if (!gtf.AdvanceWidths.TryGetValue(glyphIndex, out w))
					return false;
				if (width == null)
					width = w;
				else if (width.Value != w)
					return false;

				double h;
				if (!gtf.AdvanceHeights.TryGetValue(glyphIndex, out h))
					return false;
				if (height == null)
					height = h;
				else if (height.Value != h)
					return false;
			}
			return true;
		}
	}
}
