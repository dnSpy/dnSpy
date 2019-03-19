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

using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace dnSpy.Settings {
	static class XmlUtils {
		const char ESCAPE_CHAR = '©';

		public static string EscapeAttributeValue(string s) {
			if (s == null)
				return null;
			var sb = new StringBuilder(s.Length);
			foreach (var c in s) {
				if (c < ' ' || c == ESCAPE_CHAR)
					sb.Append($"{ESCAPE_CHAR}{(int)c:X4}");
				else
					sb.Append(c);
			}
			return sb.ToString();
		}

		public static string UnescapeAttributeValue(string s) {
			if (s == null)
				return null;
			if (s.IndexOf(ESCAPE_CHAR) < 0)
				return s;
			var parts = s.Split(new char[] { ESCAPE_CHAR });
			var sb = new StringBuilder(s.Length);
			for (int i = 0; i < parts.Length; i++) {
				var p = parts[i];
				if (i == 0 || p.Length < 4)
					sb.Append(p);
				else {
					var hex = p.Substring(0, 4);
					var rest = p.Substring(4);
					int val = ParseHex(hex);
					if (val >= 0)
						sb.Append((char)val);
					else
						sb.Append(hex);
					sb.Append(rest);
				}
			}
			return sb.ToString();
		}

		static int ParseHex(string hex) {
			if (hex.Length != 4)
				return -1;
			if (!IsHex(hex[0]) || !IsHex(hex[1]) || !IsHex(hex[2]) || !IsHex(hex[3]))
				return -1;
			bool b = int.TryParse(hex, NumberStyles.HexNumber, null, out int val);
			Debug.Assert(b);
			if (b)
				return val;
			return -1;
		}

		static bool IsHex(char c) => ('0' <= c && c <= '9') || ('A' <= c && c <= 'F') || ('a' <= c && c <= 'f');

		public static string FilterAttributeName(string s) {
			if (s == null || s.Length == 0)
				return null;

			// Only allow a sub set of the valid names. Fix it if this is a problem.

			if (!IsValidFirstXmlAttrChar(s[0]))
				return null;
			for (int i = 1; i < s.Length; i++) {
				if (!IsValidXmlAttrChar(s[i]))
					return null;
			}

			return s;
		}

		static bool IsValidFirstXmlAttrChar(char c) =>
			c == '-' || c == '_' || c == '.' ||
			('A' <= c && c <= 'Z') ||
			('a' <= c && c <= 'z');

		static bool IsValidXmlAttrChar(char c) =>
			c == '-' || c == '_' || c == '.' ||
			('0' <= c && c <= '9') ||
			('A' <= c && c <= 'Z') ||
			('a' <= c && c <= 'z');
	}
}
