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
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace dnSpy.Contracts.Controls {
	/// <summary>
	/// Font methods
	/// </summary>
	public static class FontUtilities {
		/// <summary>
		/// Default font size
		/// </summary>
		public static readonly double DEFAULT_FONT_SIZE = 10.0 * (96.0 / 72.0);
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

		static IEnumerable<string> GetDefaultLanguageFontsToCheck(bool onlyMonospacedFonts) {
			switch (CultureInfo.CurrentUICulture.ThreeLetterWindowsLanguageName) {
			case "CHS":
				yield return "NSimSun";
				break;

			case "CHT":
				yield return "MingLiU";
				yield return "MingLiU-ExtB";
				yield return "MingLiU_HKSCS";
				yield return "MingLiU_HKSCS-ExtB";
				break;

			case "JPN":
				yield return "MS Gothic";
				break;

			case "KOR":
				if (!onlyMonospacedFonts)
					yield return "DotumChe";
				break;

			default:
				yield return "Consolas";
				break;
			}
		}

		static IEnumerable<string> GetDefaultFontsToCheck(bool onlyMonospacedFonts) {
			foreach (var name in GetDefaultLanguageFontsToCheck(onlyMonospacedFonts))
				yield return name;
			foreach (var name in monospacedFontsToCheck)
				yield return name;
		}

		const string DefaultFontIfNoOtherFound = "Courier New";

		/// <summary>
		/// Returns the default monospaced font
		/// </summary>
		/// <returns></returns>
		public static string GetDefaultMonospacedFont() {
			foreach (var name in GetDefaultFontsToCheck(true)) {
				if (Exists(name))
					return name;
			}

			Debug.Fail("Couldn't find a default monospaced font");

			return DefaultFontIfNoOtherFound;
		}

		/// <summary>
		/// Returns the default text editor font (usually a monospaced font, but no guarantee)
		/// </summary>
		/// <returns></returns>
		public static string GetDefaultTextEditorFont() {
			foreach (var name in GetDefaultFontsToCheck(false)) {
				if (Exists(name))
					return name;
			}

			Debug.Fail("Couldn't find a default text editor font");

			return DefaultFontIfNoOtherFound;
		}

		static ICollection<FontFamily> SystemFontFamilies {
			get {
				try {
					// This can throw if an update was installed that removed support for the current
					// OS, see https://github.com/0xd4d/dnSpy/issues/692
					return Fonts.SystemFontFamilies;
				}
				catch {
					return Array.Empty<FontFamily>();
				}
			}
		}

		/// <summary>
		/// Checks whether a font exists
		/// </summary>
		/// <param name="name">Name</param>
		/// <returns></returns>
		public static bool Exists(string name) {
			foreach (var ff in SystemFontFamilies) {
				if (ff.Source.Equals(name, StringComparison.OrdinalIgnoreCase))
					return true;
			}
			return false;
		}

		/// <summary>
		/// Filters font size
		/// </summary>
		/// <param name="size">Size</param>
		/// <returns></returns>
		public static double FilterFontSize(double size) {
			if (size < MIN_FONT_SIZE)
				return MIN_FONT_SIZE;
			if (size > MAX_FONT_SIZE)
				return MAX_FONT_SIZE;
			return size;
		}

		/// <summary>
		/// Checks whether <paramref name="ff"/> is a symbol font
		/// </summary>
		/// <param name="ff">Font</param>
		/// <returns></returns>
		public static bool IsSymbol(FontFamily ff) {
			foreach (var tf in ff.GetTypefaces()) {
				if (!tf.TryGetGlyphTypeface(out var gtf))
					return true;
				if (gtf.Symbol)
					return true;
			}
			return false;
		}

		/// <summary>
		/// Gets all monospaced fonts
		/// </summary>
		/// <returns></returns>
		public static FontFamily[] GetMonospacedFonts() => SystemFontFamilies.Where(a => IsMonospacedFont(a)).OrderBy(a => a.Source.ToUpperInvariant()).ToArray();

		/// <summary>
		/// Checks whether <paramref name="ff"/> is a monospaced font. It currently only checks
		/// chars 0x20-0x7E whether they have the same width and height.
		/// </summary>
		/// <param name="ff">Font</param>
		/// <returns></returns>
		public static bool IsMonospacedFont(FontFamily ff) {
			if (ff.Source.Equals(GLOBAL_MONOSPACE_FONT, StringComparison.OrdinalIgnoreCase))
				return true;

			foreach (var tf in ff.GetTypefaces()) {
				if (tf.Weight != FontWeights.Normal)
					continue;
				if (tf.Style != FontStyles.Normal)
					continue;

				if (!tf.TryGetGlyphTypeface(out var gtf))
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
				if (!gtf.CharacterToGlyphMap.TryGetValue(c, out ushort glyphIndex))
					return false;
				if (!gtf.AdvanceWidths.TryGetValue(glyphIndex, out double w))
					return false;
				if (width is null)
					width = w;
				else if (width.Value != w)
					return false;

				if (!gtf.AdvanceHeights.TryGetValue(glyphIndex, out double h))
					return false;
				if (height is null)
					height = h;
				else if (height.Value != h)
					return false;
			}
			return true;
		}
	}
}
