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

using System.Windows.Media;

namespace dnSpy.Text.WPF {
	static class BrushComparer {
		public static bool Equals(Brush? a, Brush? b) {
			if (a == b)
				return true;
			if (a is null || b is null)
				return false;

			if (a.Opacity == 0 && b.Opacity == 0)
				return true;

			var sa = a as SolidColorBrush;
			var sb = b as SolidColorBrush;
			if (sa is not null && sb is not null) {
				if (sa.Color.A == 0 && sb.Color.A == 0)
					return true;
				return sa.Color.A == sb.Color.A && sa.Color.R == sb.Color.R && sa.Color.G == sb.Color.G && sa.Color.B == sb.Color.B;
			}

			return a.Equals(b);
		}
	}
}
