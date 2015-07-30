/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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

using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

namespace dnSpy.HexEditor {
	sealed class HexTextRunProperties : TextRunProperties {
		public Brush _BackgroundBrush;
		public CultureInfo _CultureInfo;
		public double _FontHintingEmSize;
		public double _FontRenderingEmSize;
		public Brush _ForegroundBrush;
		public TextDecorationCollection _TextDecorations;
		public TextEffectCollection _TextEffects;
		public Typeface _Typeface;

		public override Brush BackgroundBrush {
			get { return _BackgroundBrush; }
		}

		public override CultureInfo CultureInfo {
			get { return _CultureInfo; }
		}

		public override double FontHintingEmSize {
			get { return _FontHintingEmSize; }
		}

		public override double FontRenderingEmSize {
			get { return _FontRenderingEmSize; }
		}

		public override Brush ForegroundBrush {
			get { return _ForegroundBrush; }
		}

		public override TextDecorationCollection TextDecorations {
			get { return _TextDecorations; }
		}

		public override TextEffectCollection TextEffects {
			get { return _TextEffects; }
		}

		public override Typeface Typeface {
			get { return _Typeface; }
		}

		public HexTextRunProperties() {
		}

		public HexTextRunProperties(TextRunProperties other) {
			this._BackgroundBrush = other.BackgroundBrush;
			this._CultureInfo = other.CultureInfo;
			this._FontHintingEmSize = other.FontHintingEmSize;
			this._FontRenderingEmSize = other.FontRenderingEmSize;
			this._ForegroundBrush = other.ForegroundBrush;
			this._TextDecorations = other.TextDecorations;
			this._TextEffects = other.TextEffects;
			this._Typeface = other.Typeface;
		}

		public static bool Equals(TextRunProperties a, TextRunProperties b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;

			if (a.FontHintingEmSize != b.FontHintingEmSize)
				return false;
			if (a.FontRenderingEmSize != b.FontRenderingEmSize)
				return false;
			if (a.TextDecorations != b.TextDecorations)	// We don't use it so this is enough
				return false;
			if (a.TextEffects != b.TextEffects) // We don't use it so this is enough
				return false;
			if (!a.CultureInfo.Equals(b.CultureInfo))
				return false;
			if (!a.Typeface.Equals(b.Typeface))
				return false;
			if (!Equals(a.BackgroundBrush, b.BackgroundBrush))
				return false;
			if (!Equals(a.ForegroundBrush, b.ForegroundBrush))
				return false;
			if (a.BaselineAlignment != b.BaselineAlignment)
				return false;
			if (!Equals(a.NumberSubstitution, b.NumberSubstitution))
				return false;
			if (!Equals(a.TypographyProperties, b.TypographyProperties))
				return false;

			return true;
		}

		static bool Equals(Brush a, Brush b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (a.Opacity == 0 && b.Opacity == 0)
				return true;

			var sa = a as SolidColorBrush;
			var sb = b as SolidColorBrush;
			if (sa != null && sb != null) {
				var ca = sa.Color;
				var cb = sb.Color;
				return ca == cb || (ca.A == 0 && cb.A == 0);
			}

			return a.Equals(b);
		}

		static bool Equals(NumberSubstitution a, NumberSubstitution b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			return a.Equals(b);
		}

		static bool Equals(TextRunTypographyProperties a, TextRunTypographyProperties b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			return a.Equals(b);
		}
	}
}
