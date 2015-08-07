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

using System.Windows;

namespace dnSpy.HexEditor {
	static class ClipboardUtils {
		public static byte[] GetData() {
			var s = Clipboard.GetText();
			if (s == null)
				return null;
			if (s.Length == 0)
				return null;
			return HexStringToByteArray(s);
		}

		static byte[] HexStringToByteArray(string s) {
			if (s == null)
				return null;

			s = s.Replace(" ", string.Empty);
			s = s.Replace("\t", string.Empty);
			s = s.Replace("\r", string.Empty);
			s = s.Replace("\n", string.Empty);
			if (s.Length % 2 != 0)
				return null;

			var bytes = new byte[s.Length / 2];
			for (int i = 0; i < s.Length; i += 2) {
				int upper = s[i] == '?' ? 0 : TryParseHexChar(s[i]);
				int lower = s[i + 1] == '?' ? 0 : TryParseHexChar(s[i + 1]);
				if (upper < 0 || lower < 0)
					return null;
				bytes[i / 2] = (byte)((upper << 4) | lower);
			}

			return bytes;
		}

		public static int TryParseHexChar(char c) {
			if ('0' <= c && c <= '9')
				return c - '0';
			if ('a' <= c && c <= 'f')
				return c - 'a' + 10;
			if ('A' <= c && c <= 'F')
				return c - 'A' + 10;
			return -1;
		}
	}
}
