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
using System.Globalization;
using System.Text;

namespace dnSpy.Hex.Editor.Search {
	static class NumberParser {
		public static bool TryParseUnsigned(string s, ulong min, ulong max, out ulong value) {
			value = 0;
			bool isValid;
			s = s.Trim();
			if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ||
				s.StartsWith("&H", StringComparison.OrdinalIgnoreCase)) {
				var s2 = s.Substring(2);
				isValid = s2.Trim() == s2 && ulong.TryParse(s2, NumberStyles.HexNumber, null, out value);
			}
			else
				isValid = ulong.TryParse(s, NumberStyles.Integer, null, out value);
			if (!isValid)
				return false;
			if (value < min || value > max)
				return false;

			return true;
		}

		public static bool TryParseSigned(string s, long min, long max, out long value) {
			value = 0;
			bool isValid;
			s = s.Trim();
			bool isSigned = s.StartsWith("-", StringComparison.OrdinalIgnoreCase);
			if (isSigned)
				s = s.Substring(1);
			ulong value2 = 0;
			if (s.Trim() != s)
				isValid = false;
			else if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ||
					 s.StartsWith("&H", StringComparison.OrdinalIgnoreCase)) {
				var s2 = s.Substring(2);
				isValid = s2.Trim() == s2 && ulong.TryParse(s2, NumberStyles.HexNumber, null, out value2);
			}
			else
				isValid = ulong.TryParse(s, NumberStyles.Integer, null, out value2);
			if (!isValid)
				return false;
			if (isSigned) {
				if (value2 > (ulong)long.MaxValue + 1)
					return false;
				value = unchecked(-(long)value2);
			}
			else {
				if (value2 > (ulong)long.MaxValue)
					return false;
				value = (long)value2;
			}
			if (value < min || value > max)
				return false;

			return true;
		}

		public static byte[] TryParseByteArray(string text) {
			int byteLength = GetByteLength(text);
			if (byteLength <= 0)
				return null;

			var bytes = new byte[byteLength];
			int bytesIndex = 0;
			for (int i = 0; i < text.Length;) {
				i = SkipWhitespace(text, i);
				if (i >= text.Length)
					break;
				int b = 0;
				var c = text[i++];
				var v = HexToBin(c);
				if (v < 0)
					return null;
				b |= v << 4;

				i = SkipWhitespace(text, i);
				if (i < text.Length) {
					c = text[i++];
					v = HexToBin(c);
					if (v < 0)
						return null;
					b |= v;
				}
				bytes[bytesIndex] = (byte)b;
				bytesIndex++;
			}
			if (bytesIndex != bytes.Length)
				return null;
			return bytes;
		}

		static int SkipWhitespace(string pattern, int index) {
			while (index < pattern.Length) {
				if (!char.IsWhiteSpace(pattern[index]))
					break;
				index++;
			}
			return index;
		}

		static int GetByteLength(string pattern) {
			int nibbles = 0;
			foreach (var c in pattern) {
				if (char.IsWhiteSpace(c))
					continue;
				if (HexToBin(c) < 0)
					return -1;
				nibbles++;
			}
			return (nibbles + 1) / 2;
		}

		static int HexToBin(char c) {
			if ('0' <= c && c <= '9')
				return c - '0';
			if ('a' <= c && c <= 'f')
				return c - 'a' + 10;
			if ('A' <= c && c <= 'F')
				return c - 'A' + 10;
			return -1;
		}
	}

	static class DataParser {
		public static byte[] TryParseData(string text, HexDataKind dataKind, bool isBigEndian) {
			switch (dataKind) {
			case HexDataKind.Bytes:			return TryParseBytes(text);
			case HexDataKind.Utf8String:	return TryParseUtf8String(text);
			case HexDataKind.Utf16String:	return TryParseUtf16String(text, isBigEndian);
			case HexDataKind.Byte:			return TryParseByte(text);
			case HexDataKind.SByte:			return TryParseSByte(text);
			case HexDataKind.Int16:			return TryParseInt16(text, isBigEndian);
			case HexDataKind.UInt16:		return TryParseUInt16(text, isBigEndian);
			case HexDataKind.Int32:			return TryParseInt32(text, isBigEndian);
			case HexDataKind.UInt32:		return TryParseUInt32(text, isBigEndian);
			case HexDataKind.Int64:			return TryParseInt64(text, isBigEndian);
			case HexDataKind.UInt64:		return TryParseUInt64(text, isBigEndian);
			case HexDataKind.Single:		return TryParseSingle(text, isBigEndian);
			case HexDataKind.Double:		return TryParseDouble(text, isBigEndian);
			default:						return null;
			}
		}

		static byte[] TryParseBytes(string text) => NumberParser.TryParseByteArray(text);
		static byte[] TryParseUtf8String(string text) => Encoding.UTF8.GetBytes(text);
		static byte[] TryParseUtf16String(string text, bool isBigEndian) =>
			isBigEndian ? Encoding.BigEndianUnicode.GetBytes(text) : Encoding.Unicode.GetBytes(text);

		static byte[] TryParseByte(string text) {
			ulong value;
			if (!NumberParser.TryParseUnsigned(text, byte.MinValue, byte.MaxValue, out value))
				return null;
			return new byte[1] { (byte)value };
		}

		static byte[] TryParseSByte(string text) {
			long value;
			if (!NumberParser.TryParseSigned(text, sbyte.MinValue, sbyte.MaxValue, out value))
				return null;
			return new byte[1] { (byte)value };
		}

		static byte[] TryParseInt16(string text, bool isBigEndian) {
			long value;
			if (!NumberParser.TryParseSigned(text, short.MinValue, short.MaxValue, out value))
				return null;
			return GetBytes((ushort)value, isBigEndian);
		}

		static byte[] TryParseUInt16(string text, bool isBigEndian) {
			ulong value;
			if (!NumberParser.TryParseUnsigned(text, ushort.MinValue, ushort.MaxValue, out value))
				return null;
			return GetBytes((ushort)value, isBigEndian);
		}

		static byte[] TryParseInt32(string text, bool isBigEndian) {
			long value;
			if (!NumberParser.TryParseSigned(text, int.MinValue, int.MaxValue, out value))
				return null;
			return GetBytes((uint)value, isBigEndian);
		}

		static byte[] TryParseUInt32(string text, bool isBigEndian) {
			ulong value;
			if (!NumberParser.TryParseUnsigned(text, uint.MinValue, uint.MaxValue, out value))
				return null;
			return GetBytes((uint)value, isBigEndian);
		}

		static byte[] TryParseInt64(string text, bool isBigEndian) {
			long value;
			if (!NumberParser.TryParseSigned(text, long.MinValue, long.MaxValue, out value))
				return null;
			return GetBytes((ulong)value, isBigEndian);
		}

		static byte[] TryParseUInt64(string text, bool isBigEndian) {
			ulong value;
			if (!NumberParser.TryParseUnsigned(text, ulong.MinValue, ulong.MaxValue, out value))
				return null;
			return GetBytes((ulong)value, isBigEndian);
		}

		static byte[] TryParseSingle(string text, bool isBigEndian) {
			float value;
			if (!float.TryParse(text, out value))
				return null;
			return GetBytes(value, isBigEndian);
		}

		static byte[] TryParseDouble(string text, bool isBigEndian) {
			double value;
			if (!double.TryParse(text, out value))
				return null;
			return GetBytes(value, isBigEndian);
		}

		static byte[] GetBytes(ushort value, bool isBigEndian) {
			if (isBigEndian) {
				return new byte[2] {
					(byte)(value >> 8),
					(byte)value,
				};
			}
			return BitConverter.GetBytes(value);
		}

		static byte[] GetBytes(uint value, bool isBigEndian) {
			if (isBigEndian) {
				return new byte[4] {
					(byte)(value >> 24),
					(byte)(value >> 16),
					(byte)(value >> 8),
					(byte)value,
				};
			}
			return BitConverter.GetBytes(value);
		}

		static byte[] GetBytes(ulong value, bool isBigEndian) {
			if (isBigEndian) {
				return new byte[8] {
					(byte)(value >> 56),
					(byte)(value >> 48),
					(byte)(value >> 40),
					(byte)(value >> 32),
					(byte)(value >> 24),
					(byte)(value >> 16),
					(byte)(value >> 8),
					(byte)value,
				};
			}
			return BitConverter.GetBytes(value);
		}

		static byte[] GetBytes(float value, bool isBigEndian) {
			var bytes = BitConverter.GetBytes(value);
			if (isBigEndian)
				return GetBytes(BitConverter.ToUInt32(bytes, 0), isBigEndian);
			return bytes;
		}

		static byte[] GetBytes(double value, bool isBigEndian) {
			var bytes = BitConverter.GetBytes(value);
			if (isBigEndian)
				return GetBytes(BitConverter.ToUInt64(bytes, 0), isBigEndian);
			return bytes;
		}
	}
}
