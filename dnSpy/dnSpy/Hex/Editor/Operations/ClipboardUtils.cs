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
using System.Runtime.InteropServices;
using System.Windows;
using dnSpy.Contracts.Hex;

namespace dnSpy.Hex.Editor.Operations {
	static class ClipboardUtils {
		public static byte[] GetData() {
			string s;
			try {
				s = Clipboard.GetText();
			}
			catch (ExternalException) { return null; }
			if (s == null)
				return null;
			if (s.Length == 0)
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

		public static string ToHexString(HexBufferSpan span, bool upper) {
			if (span.IsDefault)
				return null;
			if (span.Length > int.MaxValue / 2)
				return null;
			int totalByteLength = (int)span.Length.ToUInt64();
			int totalCharLength = totalByteLength * 2;
			var charArray = new char[totalCharLength];
			int charArrayIndex = 0;
			var buffer = new byte[Math.Min(0x1000, totalByteLength)];
			for (int pos = 0; pos < totalByteLength;) {
				int bytesRead = Math.Min(totalByteLength - pos, buffer.Length);
				span.Buffer.ReadBytes(span.Start + pos, buffer, 0, bytesRead);
				pos += bytesRead;
				for (int i = 0; i < bytesRead; i++) {
					var b = buffer[i];
					charArray[charArrayIndex++] = NibbleToHex(b >> 4, upper);
					charArray[charArrayIndex++] = NibbleToHex(b & 0x0F, upper);
				}
			}
			return new string(charArray);
		}

		static char NibbleToHex(int v, bool upper) {
			Debug.Assert(0 <= v && v <= 15);
			if (v <= 9)
				return (char)('0' + v);
			return (char)((upper ? 'A' - 10 : 'a' - 10) + v);
		}

		public static string GetText() {
			try {
				var s = Clipboard.GetText();
				return s.Length == 0 ? null : s;
			}
			catch (ExternalException) { return null; }
		}
	}
}
