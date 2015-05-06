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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace ICSharpCode.ILSpy.AsmEditor
{
	static class NumberVMUtils
	{
		public static byte[] ParseByteArray(string s)
		{
			s = s.Replace(" ", string.Empty);
			s = s.Replace("\t", string.Empty);
			s = s.Replace("\r", string.Empty);
			s = s.Replace("\n", string.Empty);
			if (s.Length % 2 != 0)
				throw new FormatException("A hex string must contain an even number of hex digits");
			var bytes = new byte[s.Length / 2];
			for (int i = 0; i < s.Length; i += 2) {
				int upper = TryParseHexChar(s[i]);
				int lower = TryParseHexChar(s[i + 1]);
				if (upper < 0 || lower < 0)
					throw new FormatException("A hex string must contain only hex digits: 0-9 and A-F");
				bytes[i / 2] = (byte)((upper << 4) | lower);
			}
			return bytes;
		}

		static int TryParseHexChar(char c)
		{
			if ('0' <= c && c <= '9')
				return c - '0';
			if ('a' <= c && c <= 'f')
				return c - 'a' + 10;
			if ('A' <= c && c <= 'F')
				return c - 'A' + 10;
			return -1;
		}

		public static string ByteArrayToString(IList<byte> value, bool upper = true)
		{
			if (value == null)
				return string.Empty;
			var chars = new char[value.Count * 2];
			for (int i = 0, j = 0; i < value.Count; i++) {
				byte b = value[i];
				chars[j++] = ToHexChar(b >> 4, upper);
				chars[j++] = ToHexChar(b & 0x0F, upper);
			}
			return new string(chars);
		}

		static char ToHexChar(int val, bool upper)
		{
			if (0 <= val && val <= 9)
				return (char)(val + (int)'0');
			return (char)(val - 10 + (upper ? (int)'A' : (int)'a'));
		}

		const string INVALID_TOSTRING_VALUE = "<invalid value>";
		public static string ToString(ulong value, ulong min, ulong max, bool useDecimal)
		{
			if (value < min || value > max)
				return INVALID_TOSTRING_VALUE;
			if (value <= 9 || useDecimal)
				return value.ToString();
			return string.Format("0x{0:X}", value);
		}

		public static string ToString(long value, long min, long max, bool useDecimal)
		{
			if (value < min || value > max)
				return INVALID_TOSTRING_VALUE;
			if (-9 <= value && value <= 9 || useDecimal)
				return value.ToString();
			if (value < 0)
				return string.Format("-0x{0:X}", -value);
			return string.Format("0x{0:X}", value);
		}

		public static string ToString(float value)
		{
			return value.ToString();
		}

		public static string ToString(double value)
		{
			return value.ToString();
		}

		public static string ToString(bool value)
		{
			return value.ToString();
		}

		public static string ToString(char value)
		{
			var sb = new StringBuilder(8);
			sb.Append('\'');
			switch (value) {
			case '\a': sb.Append(@"\a"); break;
			case '\b': sb.Append(@"\b"); break;
			case '\f': sb.Append(@"\f"); break;
			case '\n': sb.Append(@"\n"); break;
			case '\r': sb.Append(@"\r"); break;
			case '\t': sb.Append(@"\t"); break;
			case '\v': sb.Append(@"\v"); break;
			case '\\': sb.Append(@"\\"); break;
			case '\0': sb.Append(@"\0"); break;
			case '\'': sb.Append(@"\'"); break;
			default:
				if (char.IsControl(value))
					sb.Append(string.Format(@"\u{0:X4}", (ushort)value));
				else
					sb.Append(value);
				break;
			}
			sb.Append('\'');
			return sb.ToString();
		}

		public static string ToString(string s, bool canHaveNull)
		{
			if (s == null)
				return canHaveNull ? "null" : string.Empty;
			var sb = new StringBuilder(s.Length + 10);
			sb.Append('"');
			foreach (var c in s) {
				switch (c) {
				case '\a': sb.Append(@"\a"); break;
				case '\b': sb.Append(@"\b"); break;
				case '\f': sb.Append(@"\f"); break;
				case '\n': sb.Append(@"\n"); break;
				case '\r': sb.Append(@"\r"); break;
				case '\t': sb.Append(@"\t"); break;
				case '\v': sb.Append(@"\v"); break;
				case '\\': sb.Append(@"\\"); break;
				case '\0': sb.Append(@"\0"); break;
				case '"':  sb.Append("\\\""); break;
				default:
					if (char.IsControl(c))
						sb.Append(string.Format(@"\u{0:X4}", (ushort)c));
					else
						sb.Append(c);
					break;
				}
			}
			sb.Append('"');
			return sb.ToString();
		}

		static string TryParseUnsigned(string s, ulong min, ulong max, out ulong value)
		{
			value = 0;
			bool isValid;
			s = s.Trim();
			if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) {
				var s2 = s.Substring(2);
				isValid = s2.Trim() == s2 && ulong.TryParse(s2, NumberStyles.HexNumber, null, out value);
			}
			else
				isValid = ulong.TryParse(s, NumberStyles.Integer, null, out value);
			if (!isValid) {
				if (s.StartsWith("-"))
					return "Only non-negative integers are allowed";
				return "The value is not an unsigned hexadecimal or decimal integer";
			}
			if (value < min || value > max) {
				if (min == 0)
					return string.Format("Value must be between {0} and {1} (0x{1:X}) inclusive", min, max);
				return string.Format("Value must be between {0} (0x{0:X}) and {1} (0x{1:X}) inclusive", min, max);
			}

			return null;
		}

		static ulong ParseUnsigned(string s, ulong min, ulong max)
		{
			ulong value;
			var err = TryParseUnsigned(s, min, max, out value);
			if (err != null)
				throw new FormatException(err);
			return value;
		}

		public static float ParseSingle(string s)
		{
			float value;
			if (float.TryParse(s, out value))
				return value;
			throw new FormatException("Value must be a 32-bit floating point number");
		}

		public static double ParseDouble(string s)
		{
			double value;
			if (double.TryParse(s, out value))
				return value;
			throw new FormatException("Value must be a 64-bit floating point number");
		}

		public static bool ParseBoolean(string s)
		{
			bool value;
			if (bool.TryParse(s, out value))
				return value;
			throw new FormatException("Value must be a boolean value (True or False)");
		}

		public static char ParseChar(string s)
		{
			int index = 0;
			char c = ParseChar(s, ref index);
			SkipSpaces(s, ref index);
			if (index != s.Length)
				ThrowParseCharError();
			return c;
		}

		static void ThrowParseCharError()
		{
			throw new FormatException("A character must be enclosed in single quotes (').");
		}

		static char ParseChar(string s, ref int index)
		{
			SkipSpaces(s, ref index);
			if (index >= s.Length || s[index] != '\'')
				ThrowParseCharError();

			index++;
			char c = s[index++];
			if (c == '\\') {
				if (index >= s.Length)
					ThrowParseCharError();
				c = s[index++];
				switch (c) {
				case 'a': c = '\a'; break;
				case 'b': c = '\b'; break;
				case 'f': c = '\f'; break;
				case 'n': c = '\n'; break;
				case 'r': c = '\r'; break;
				case 't': c = '\t'; break;
				case 'v': c = '\v'; break;
				case '\\':c = '\\'; break;
				case '0': c = '\0'; break;
				case '"': c = '"'; break;
				case '\'':c = '\''; break;
				case 'x':
				case 'u':
					if (index >= s.Length)
						ThrowParseCharError();
					char surrogate;
					int ch = ParseHex(s, ref index, c == 'x' ? -1 : 4, out surrogate);
					if (ch < 0)
						ThrowParseCharError();
					c = (char)ch;
					break;

				default:
					throw new FormatException(string.Format("Unknown character escape sequence: \\{0}", c));
				}
			}
			if (index >= s.Length)
				ThrowParseCharError();
			if (s[index] != '\'')
				ThrowParseCharError();
			index++;

			return c;
		}

		public static string ParseString(string s, bool canHaveNull)
		{
			int index = 0;
			var res = ParseString(s, canHaveNull, ref index);
			SkipSpaces(s, ref index);
			if (index != s.Length)
				ThrowParseStringError(canHaveNull);
			return res;
		}

		static void ThrowParseStringError(bool canHaveNull)
		{
			throw new FormatException(canHaveNull ?
				"A string must contain the value 'null' or must be enclosed in double quotes (\")" :
				"A string must be enclosed in double quotes (\")");
		}

		static string ParseString(string s, bool canHaveNull, ref int index)
		{
			SkipSpaces(s, ref index);
			if (canHaveNull && s.Substring(index).StartsWith("null")) {
				index += 4;
				return null;
			}
			if (index + 2 > s.Length || s[index] != '"')
				ThrowParseStringError(canHaveNull);
			var sb = new StringBuilder(s.Length - index - 2);
			while (true) {
				index++;
				if (index >= s.Length)
					ThrowParseStringError(canHaveNull);
				char c = s[index];
				if (c == '"') {
					index++;
					break;
				}
				if (c == '\\') {
					index++;
					if (index >= s.Length)
						ThrowParseStringError(canHaveNull);
					c = s[index];
					switch (c) {
					case 'a': sb.Append('\a'); break;
					case 'b': sb.Append('\b'); break;
					case 'f': sb.Append('\f'); break;
					case 'n': sb.Append('\n'); break;
					case 'r': sb.Append('\r'); break;
					case 't': sb.Append('\t'); break;
					case 'v': sb.Append('\v'); break;
					case '\\':sb.Append('\\'); break;
					case '0': sb.Append('\0'); break;
					case '"': sb.Append('"'); break;
					case '\'':sb.Append('\''); break;
					case 'x':
					case 'u':
					case 'U':
						index++;
						if (index >= s.Length)
							ThrowParseStringError(canHaveNull);
						char surrogate;
						int ch = ParseHex(s, ref index, c == 'x' ? -1 : c == 'u' ? 4 : 8, out surrogate);
						if (ch < 0)
							ThrowParseStringError(canHaveNull);
						if (c == 'U' && surrogate != 0)
							sb.Append(surrogate);
						sb.Append((char)ch);
						index--;
						break;

					default:
						throw new FormatException(string.Format("Unknown string escape sequence: \\{0}", c));
					}
				}
				else
					sb.Append(c);
			}

			return sb.ToString();
		}

		static void SkipSpaces(string s, ref int index)
		{
			while (index < s.Length && char.IsWhiteSpace(s[index]))
				index++;
		}

		static int ParseHex(string s, ref int index, int hexChars, out char surrogate)
		{
			surrogate = (char)0;
			if (index >= s.Length)
				return -1;
			int val = 0;
			int i;
			int max = hexChars < 0 ? 4 : hexChars;
			for (i = 0; i < max; i++, index++) {
				if (index >= s.Length)
					break;
				int v = TryParseHexChar(s[index]);
				if (v < 0)
					break;
				val = (val << 4) | v;
			}
			if (hexChars >= 0 && hexChars != i)
				return -1;
			else if (hexChars < 0 && i == 0)
				return -1;
			if (hexChars == 8) {
				if (val >= 0x00110000)
					return -1;
				if (val >= 0x00010000) {
					val -= 0x00010000;
					surrogate = (char)(0xD800 + (val >> 10));
					val = 0xDC00 + (val & 0x3FF);
				}
			}
			return val;
		}

		public static byte ParseByte(string s, byte min, byte max)
		{
			return (byte)ParseUnsigned(s, min, max);
		}

		public static ushort ParseUInt16(string s, ushort min, ushort max)
		{
			return (ushort)ParseUnsigned(s, min, max);
		}

		public static uint ParseUInt32(string s, uint min, uint max)
		{
			return (uint)ParseUnsigned(s, min, max);
		}

		public static ulong ParseUInt64(string s, ulong min, ulong max)
		{
			return ParseUnsigned(s, min, max);
		}

		static string TryParseSigned(string s, long min, long max, out long value)
		{
			value = 0;
			bool isValid;
			s = s.Trim();
			bool isSigned = s.StartsWith("-", StringComparison.OrdinalIgnoreCase);
			if (isSigned)
				s = s.Substring(1);
			ulong value2 = 0;
			if (s.Trim() != s)
				isValid = false;
			else if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) {
				var s2 = s.Substring(2);
				isValid = s2.Trim() == s2 && ulong.TryParse(s2, NumberStyles.HexNumber, null, out value2);
			}
			else
				isValid = ulong.TryParse(s, NumberStyles.Integer, null, out value2);
			if (!isValid)
				return "The value is not a hexadecimal or decimal integer";
			if (isSigned) {
				if (value2 > (ulong)long.MaxValue + 1)
					return "The value is too small";
				value = unchecked(-(long)value2);
			}
			else {
				if (value2 > (ulong)long.MaxValue)
					return "The value is too big";
				value = (long)value2;
			}
			if (value < min || value > max) {
				if (min == 0)
					return string.Format("Value must be between {0} and {1} (0x{1:X}) inclusive", min, max);
				return string.Format("Value must be between {0} (0x{0:X}) and {1} (0x{1:X}) inclusive", min, max);
			}

			return null;
		}

		static long ParseSigned(string s, long min, long max)
		{
			long value;
			var err = TryParseSigned(s, min, max, out value);
			if (err != null)
				throw new FormatException(err);
			return value;
		}

		public static sbyte ParseSByte(string s, sbyte min, sbyte max)
		{
			return (sbyte)ParseSigned(s, min, max);
		}

		public static short ParseInt16(string s, short min, short max)
		{
			return (short)ParseSigned(s, min, max);
		}

		public static int ParseInt32(string s, int min, int max)
		{
			return (int)ParseSigned(s, min, max);
		}

		public static long ParseInt64(string s, long min, long max)
		{
			return (long)ParseSigned(s, min, max);
		}

		static string ToString<T>(IList<T> list, Func<T,string> toString)
		{
			if (list == null)
				return string.Empty;
			var sb = new StringBuilder();
			for (int i = 0; i < list.Count; i++) {
				if (i != 0)
					sb.Append(", ");
				sb.Append(toString(list[i]));
			}
			return sb.ToString();
		}

		public static string ToString(IList<bool> values)
		{
			return ToString(values, v => ToString(v));
		}

		public static string ToString(IList<char> values)
		{
			return ToString(values, v => ToString(v));
		}

		public static string ToString(IList<byte> values, byte min, byte max, bool useDecimal)
		{
			return ToString(values, v => ToString(v, min, max, useDecimal));
		}

		public static string ToString(IList<ushort> values, ushort min, ushort max, bool useDecimal)
		{
			return ToString(values, v => ToString(v, min, max, useDecimal));
		}

		public static string ToString(IList<uint> values, uint min, uint max, bool useDecimal)
		{
			return ToString(values, v => ToString(v, min, max, useDecimal));
		}

		public static string ToString(IList<ulong> values, ulong min, ulong max, bool useDecimal)
		{
			return ToString(values, v => ToString(v, min, max, useDecimal));
		}

		public static string ToString(IList<sbyte> values, sbyte min, sbyte max, bool useDecimal)
		{
			return ToString(values, v => ToString(v, min, max, useDecimal));
		}

		public static string ToString(IList<short> values, short min, short max, bool useDecimal)
		{
			return ToString(values, v => ToString(v, min, max, useDecimal));
		}

		public static string ToString(IList<int> values, int min, int max, bool useDecimal)
		{
			return ToString(values, v => ToString(v, min, max, useDecimal));
		}

		public static string ToString(IList<long> values, long min, long max, bool useDecimal)
		{
			return ToString(values, v => ToString(v, min, max, useDecimal));
		}

		public static string ToString(IList<float> values)
		{
			return ToString(values, v => ToString(v));
		}

		public static string ToString(IList<double> values)
		{
			return ToString(values, v => ToString(v));
		}

		public static string ToString(IList<string> values, bool canHaveNull)
		{
			return ToString(values, v => ToString(v, canHaveNull));
		}

		static T[] ParseList<T>(string s, Func<string, T> parseValue)
		{
			var list = new List<T>();

			s = s.Trim();
			if (s == string.Empty)
				return list.ToArray();

			foreach (var elem in s.Split(',')) {
				var value = elem.Trim();
				if (value == string.Empty)
					throw new FormatException("Value in list can't be empty");
				list.Add(parseValue(value));
			}

			return list.ToArray();
		}

		delegate T ParseListCallBack<T, U>(U data, string s, ref int index);

		static T[] ParseList<T, U>(string s, ParseListCallBack<T, U> parseValue, U data)
		{
			var list = new List<T>();

			if (s.Trim() == string.Empty)
				return list.ToArray();

			int index = 0;
			while (true) {
				int oldIndex = index;
				list.Add(parseValue(data, s, ref index));
				Debug.Assert(oldIndex < index);
				if (oldIndex >= index)
					throw new InvalidOperationException();
				SkipSpaces(s, ref index);
				if (index >= s.Length)
					break;
				if (s[index] != ',')
					throw new FormatException("List elements must be separated with commas");
				index++;
			}

			return list.ToArray();
		}

		public static bool[] ParseBooleanList(string s)
		{
			return ParseList(s, v => ParseBoolean(v));
		}

		public static char[] ParseCharList(string s)
		{
			return ParseList(s, ParseCharPart, 0);
		}

		static char ParseCharPart(int data, string s, ref int index)
		{
			return ParseChar(s, ref index);
		}

		public static byte[] ParseByteList(string s, byte min, byte max)
		{
			return ParseList(s, v => ParseByte(v, min, max));
		}

		public static ushort[] ParseUInt16List(string s, ushort min, ushort max)
		{
			return ParseList(s, v => ParseUInt16(v, min, max));
		}

		public static uint[] ParseUInt32List(string s, uint min, uint max)
		{
			return ParseList(s, v => ParseUInt32(v, min, max));
		}

		public static ulong[] ParseUInt64List(string s, ulong min, ulong max)
		{
			return ParseList(s, v => ParseUInt64(v, min, max));
		}

		public static sbyte[] ParseSByteList(string s, sbyte min, sbyte max)
		{
			return ParseList(s, v => ParseSByte(v, min, max));
		}

		public static short[] ParseInt16List(string s, short min, short max)
		{
			return ParseList(s, v => ParseInt16(v, min, max));
		}

		public static int[] ParseInt32List(string s, int min, int max)
		{
			return ParseList(s, v => ParseInt32(v, min, max));
		}

		public static long[] ParseInt64List(string s, long min, long max)
		{
			return ParseList(s, v => ParseInt64(v, min, max));
		}

		public static float[] ParseSingleList(string s)
		{
			return ParseList(s, v => ParseSingle(v));
		}

		public static double[] ParseDoubleList(string s)
		{
			return ParseList(s, v => ParseDouble(v));
		}

		public static string[] ParseStringList(string s, bool canHaveNull)
		{
			return ParseList(s, ParseStringPart, canHaveNull);
		}

		static string ParseStringPart(bool canHaveNull, string s, ref int index)
		{
			return ParseString(s, (bool)canHaveNull, ref index);
		}
	}
}
