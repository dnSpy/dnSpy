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

using System.Windows.Media;

namespace dnSpy.Contracts.Text.Formatting {
	static class BrushUtils {
		public static bool Equals(Brush a, Brush b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (a.Opacity == 0 && b.Opacity == 0)
				return true;

			var sa = a as SolidColorBrush;
			var sb = b as SolidColorBrush;
			if (sa != null && sb != null) {
				if (sa.Color.A == 0 && sb.Color.A == 0)
					return true;
				return sa.Color.Equals(sb.Color);
			}

			return a.Equals(b);
		}

		public static int GetHashCode(Brush brush) {
			if (brush == null)
				return 0;
			if (brush.Opacity == 0)
				return -1;

			var sb = brush as SolidColorBrush;
			if (sb != null) {
				if (sb.Color.A == 0)
					return -1;
				return sb.Color.GetHashCode();
			}

			return brush.GetHashCode();
		}
	}
}
