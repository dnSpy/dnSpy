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

using System.Runtime.InteropServices;
using System.Text;
using System.Windows;

namespace dnSpy.AsmEditor.Hex {
	static class ClipboardUtils {
		public static byte[] GetData(bool canBeEmpty) {
			string s;
			try {
				s = Clipboard.GetText();
			}
			catch (ExternalException) { return null; }
			if (s == null)
				return null;
			if (!canBeEmpty && s.Length == 0)
				return null;
			return HexStringToByteArray(s);
		}

		static byte[] HexStringToByteArray(string s) {
			if (s == null)
				return null;
			if (s.Length % 2 != 0)
				return null;

			var bytes = new byte[s.Length / 2];
			for (int i = 0; i < s.Length; i += 2) {
				int hi = s[i] == '?' ? 0 : TryParseHexChar(s[i]);
				int lo = s[i + 1] == '?' ? 0 : TryParseHexChar(s[i + 1]);
				if (hi < 0 || lo < 0)
					return null;
				bytes[i / 2] = (byte)((hi << 4) | lo);
			}

			return bytes;
		}

		static int TryParseHexChar(char c) {
			if ('0' <= c && c <= '9')
				return c - '0';
			if ('a' <= c && c <= 'f')
				return c - 'a' + 10;
			if ('A' <= c && c <= 'F')
				return c - 'A' + 10;
			return -1;
		}

		public static string ToHexString(byte[] data) {
			if (data == null || data.Length == 0)
				return string.Empty;

			var sb = new StringBuilder(data.Length * 2);
			foreach (var b in data)
				sb.Append(string.Format("{0:X2}", b));
			return sb.ToString();
		}

		public static void SetText(string text) {
			try {
				Clipboard.SetText(text);
			}
			catch (ExternalException) { }
		}
	}
}
