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
using System.Text;

namespace dnSpy.Roslyn.Shared.Debugger.Formatters {
	static class FormatSpecifiersUtils {
		// https://docs.microsoft.com/en-us/visualstudio/debugger/format-specifiers-in-csharp
		static bool IsFormatSpecifier(string s) => s == "d" || s == "h" || s == "nq";

		public static (string expression, string[] formatSpecifiers) GetFormatSpecifiers(StringBuilder sb) {
			int pos = sb.Length - 1;
			if (pos < 0)
				return (string.Empty, Array.Empty<string>());
			int lastComma = sb.Length;
			var list = ListCache<string>.AllocList();
			for (;;) {
				var commaPos = ReverseIndexOf(sb, ',', pos);
				if (commaPos < 0)
					break;
				var fs = sb.ToString(commaPos + 1, lastComma - (commaPos + 1)).Trim();
				if (!IsFormatSpecifier(fs))
					break;
				list.Add(fs);
				lastComma = commaPos;
				pos = commaPos - 1;
			}
			var expression = sb.ToString(0, lastComma);
			list.Reverse();
			var formatSpecifiers = ListCache<string>.FreeAndToArray(ref list);
			return (expression, formatSpecifiers);
		}

		static int ReverseIndexOf(StringBuilder sb, char c, int pos) {
			while (pos >= 0) {
				if (sb[pos] == c)
					return pos;
				pos--;
			}
			return -1;
		}
	}
}
