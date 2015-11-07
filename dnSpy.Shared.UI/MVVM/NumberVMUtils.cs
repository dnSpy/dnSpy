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

namespace dnSpy.Shared.UI.MVVM {
	public static class NumberVMUtils {
		public static byte[] ParseByteArray(string s, out string error) {
			s = s.Replace(" ", string.Empty);
			s = s.Replace("\t", string.Empty);
			s = s.Replace("\r", string.Empty);
			s = s.Replace("\n", string.Empty);
			if (s.Length % 2 != 0) {
				error = "A hex string must contain an even number of hex digits";
				return null;
			}
			var bytes = new byte[s.Length / 2];
			for (int i = 0; i < s.Length; i += 2) {
				int upper = TryParseHexChar(s[i]);
				int lower = TryParseHexChar(s[i + 1]);
				if (upper < 0 || lower < 0) {
					error = "A hex string must contain only hex digits: 0-9 and A-F";
					return null;
				}
				bytes[i / 2] = (byte)((upper << 4) | lower);
			}
			error = null;
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

		public static string ByteArrayToString(IList<byte> value, bool upper = true) {
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

		static NumberVMUtils() {
			for (ulong i = 0; i <= 20; i++)
				AddNumber(i);
			ulong n = 10;
			while (true) {
				AddNumber(n - 1);
				AddNumber(n);
				AddNumber(n + 1);

				ulong a = unchecked(n * 10);
				if (a < n)
					break;
				n = a;
			}
		}

		static void AddNumber(ulong n) {
			decimalUInt64.Add(n);
			if (n <= long.MaxValue)
				decimalInt64.Add((long)n);
			if (n <= (ulong)long.MaxValue + 1)
				decimalInt64.Add(unchecked(-(long)n));
		}

		static readonly HashSet<long> decimalInt64 = new HashSet<long>();
		static readonly HashSet<ulong> decimalUInt64 = new HashSet<ulong>();

		static char ToHexChar(int val, bool upper) {
			if (0 <= val && val <= 9)
				return (char)(val + (int)'0');
			return (char)(val - 10 + (upper ? (int)'A' : (int)'a'));
		}

		const string INVALID_TOSTRING_VALUE = "<invalid value>";
		public static string ToString(ulong value, ulong min, ulong max, bool? useDecimal) {
			if (value < min || value > max)
				return INVALID_TOSTRING_VALUE;
			if (useDecimal == null) {
				if (decimalUInt64.Contains(value))
					return value.ToString();
			}
			else if (useDecimal.Value)
				return value.ToString();
			else if (value <= 9)
				return value.ToString();
			return string.Format("0x{0:X}", value);
		}

		public static string ToString(long value, long min, long max, bool? useDecimal) {
			if (value < min || value > max)
				return INVALID_TOSTRING_VALUE;
			if (useDecimal == null) {
				if (decimalInt64.Contains(value))
					return value.ToString();
			}
			else if (useDecimal.Value)
				return value.ToString();
			else if (-9 <= value && value <= 9)
				return value.ToString();
			if (value < 0)
				return string.Format("-0x{0:X}", -value);
			return string.Format("0x{0:X}", value);
		}

		public static string ToString(long value) {
			return ToString(value, long.MinValue, long.MaxValue, false);
		}

		public static string ToString(ulong value) {
			return ToString(value, ulong.MinValue, ulong.MaxValue, false);
		}

		public static string ToString(float value) {
			return value.ToString();
		}

		public static string ToString(double value) {
			return value.ToString();
		}

		public static string ToString(decimal value) {
			return value.ToString();
		}

		public static string ToString(DateTime value) {
			return value.ToString();
		}

		public static string ToString(TimeSpan value) {
			return value.ToString();
		}

		public static string ToString(bool value) {
			return value.ToString();
		}

		public static string ToString(char value) {
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

		public static string ToString(string s, bool canHaveNull) {
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

		static string TryParseUnsigned(string s, ulong min, ulong max, out ulong value) {
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

		static ulong ParseUnsigned(string s, ulong min, ulong max, out string error) {
			ulong value;
			error = TryParseUnsigned(s, min, max, out value);
			if (error != null)
				return 0;
			return value;
		}

		public static float ParseSingle(string s, out string error) {
			float value;
			if (float.TryParse(s, out value)) {
				error = null;
				return value;
			}
			error = "Value must be a 32-bit floating point number";
			return 0;
		}

		public static double ParseDouble(string s, out string error) {
			double value;
			if (double.TryParse(s, out value)) {
				error = null;
				return value;
			}
			error = "Value must be a 64-bit floating point number";
			return 0;
		}

		public static decimal ParseDecimal(string s, out string error) {
			decimal value;
			if (decimal.TryParse(s, out value)) {
				error = null;
				return value;
			}
			error = "Value must be a Decimal";
			return 0;
		}

		public static DateTime ParseDateTime(string s, out string error) {
			DateTime value;
			if (DateTime.TryParse(s, out value)) {
				error = null;
				return value;
			}
			error = "Value must be a DateTime";
			return DateTime.MinValue;
		}

		public static TimeSpan ParseTimeSpan(string s, out string error) {
			TimeSpan value;
			if (TimeSpan.TryParse(s, out value)) {
				error = null;
				return value;
			}
			error = "Value must be a TimeSpan";
			return TimeSpan.Zero;
		}

		public static bool ParseBoolean(string s, out string error) {
			bool value;
			if (bool.TryParse(s, out value)) {
				error = null;
				return value;
			}
			error = "Value must be a boolean value (True or False)";
			return false;
		}

		public static char ParseChar(string s, out string error) {
			int index = 0;
			char c = ParseChar(s, ref index, out error);
			if (error != null)
				return (char)0;
			SkipSpaces(s, ref index);
			if (index != s.Length)
				return SetParseCharError(out error);
			return c;
		}

		static char SetParseCharError(out string error) {
			error = "A character must be enclosed in single quotes (')";
			return (char)0;
		}

		static char ParseChar(string s, ref int index, out string error) {
			SkipSpaces(s, ref index);
			if (index >= s.Length || s[index] != '\'')
				return SetParseCharError(out error);

			index++;
			if (index >= s.Length)
				return SetParseCharError(out error);
			char c = s[index++];
			if (c == '\\') {
				if (index >= s.Length)
					return SetParseCharError(out error);
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
						return SetParseCharError(out error);
					char surrogate;
					int ch = ParseHex(s, ref index, c == 'x' ? -1 : 4, out surrogate);
					if (ch < 0)
						return SetParseCharError(out error);
					c = (char)ch;
					break;

				default:
					error = string.Format("Unknown character escape sequence: \\{0}", c);
					return (char)0;
				}
			}
			if (index >= s.Length)
				return SetParseCharError(out error);
			if (s[index] != '\'')
				return SetParseCharError(out error);
			index++;

			error = null;
			return c;
		}

		public static string ParseString(string s, bool canHaveNull, out string error) {
			int index = 0;
			var res = ParseString(s, canHaveNull, ref index, out error);
			if (error != null)
				return null;
			SkipSpaces(s, ref index);
			if (index != s.Length)
				return SetParseStringError(canHaveNull, out error);
			return res;
		}

		static string SetParseStringError(bool canHaveNull, out string error) {
			error = canHaveNull ?
				"A string must contain the value 'null' or must be enclosed in double quotes (\")" :
				"A string must be enclosed in double quotes (\")";
			return null;
		}

		static string ParseString(string s, bool canHaveNull, ref int index, out string error) {
			SkipSpaces(s, ref index);
			if (canHaveNull && s.Substring(index).StartsWith("null")) {
				index += 4;
				error = null;
				return null;
			}
			if (index + 2 > s.Length || s[index] != '"')
				return SetParseStringError(canHaveNull, out error);
			var sb = new StringBuilder(s.Length - index - 2);
			while (true) {
				index++;
				if (index >= s.Length)
					return SetParseStringError(canHaveNull, out error);
				char c = s[index];
				if (c == '"') {
					index++;
					break;
				}
				if (c == '\\') {
					index++;
					if (index >= s.Length)
						return SetParseStringError(canHaveNull, out error);
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
							return SetParseStringError(canHaveNull, out error);
						char surrogate;
						int ch = ParseHex(s, ref index, c == 'x' ? -1 : c == 'u' ? 4 : 8, out surrogate);
						if (ch < 0)
							return SetParseStringError(canHaveNull, out error);
						if (c == 'U' && surrogate != 0)
							sb.Append(surrogate);
						sb.Append((char)ch);
						index--;
						break;

					default:
						error = string.Format("Unknown string escape sequence: \\{0}", c);
						return null;
					}
				}
				else
					sb.Append(c);
			}

			error = null;
			return sb.ToString();
		}

		static void SkipSpaces(string s, ref int index) {
			while (index < s.Length && char.IsWhiteSpace(s[index]))
				index++;
		}

		static int ParseHex(string s, ref int index, int hexChars, out char surrogate) {
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

		public static byte ParseByte(string s, byte min, byte max, out string error) {
			return (byte)ParseUnsigned(s, min, max, out error);
		}

		public static ushort ParseUInt16(string s, ushort min, ushort max, out string error) {
			return (ushort)ParseUnsigned(s, min, max, out error);
		}

		public static uint ParseUInt32(string s, uint min, uint max, out string error) {
			return (uint)ParseUnsigned(s, min, max, out error);
		}

		public static ulong ParseUInt64(string s, ulong min, ulong max, out string error) {
			return ParseUnsigned(s, min, max, out error);
		}

		static string TryParseSigned(string s, long min, long max, object minObject, out long value) {
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
				return string.Format("Value must be between {0} ({2}0x{0:X}) and {1} (0x{1:X}) inclusive", minObject, max, min < 0 ? "-" : string.Empty);
			}

			return null;
		}

		static long ParseSigned(string s, long min, long max, object minObject, out string error) {
			long value;
			error = TryParseSigned(s, min, max, minObject, out value);
			if (error != null)
				return 0;
			return value;
		}

		public static sbyte ParseSByte(string s, sbyte min, sbyte max, out string error) {
			return (sbyte)ParseSigned(s, min, max, min, out error);
		}

		public static short ParseInt16(string s, short min, short max, out string error) {
			return (short)ParseSigned(s, min, max, min, out error);
		}

		public static int ParseInt32(string s, int min, int max, out string error) {
			return (int)ParseSigned(s, min, max, min, out error);
		}

		public static long ParseInt64(string s, long min, long max, out string error) {
			return (long)ParseSigned(s, min, max, min, out error);
		}

		static string ToString<T>(IList<T> list, Func<T, string> toString) {
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

		public static string ToString(IList<bool> values) {
			return ToString(values, v => ToString(v));
		}

		public static string ToString(IList<char> values) {
			return ToString(values, v => ToString(v));
		}

		public static string ToString(IList<byte> values, byte min, byte max, bool? useDecimal) {
			return ToString(values, v => ToString(v, min, max, useDecimal));
		}

		public static string ToString(IList<ushort> values, ushort min, ushort max, bool? useDecimal) {
			return ToString(values, v => ToString(v, min, max, useDecimal));
		}

		public static string ToString(IList<uint> values, uint min, uint max, bool? useDecimal) {
			return ToString(values, v => ToString(v, min, max, useDecimal));
		}

		public static string ToString(IList<ulong> values, ulong min, ulong max, bool? useDecimal) {
			return ToString(values, v => ToString(v, min, max, useDecimal));
		}

		public static string ToString(IList<sbyte> values, sbyte min, sbyte max, bool? useDecimal) {
			return ToString(values, v => ToString(v, min, max, useDecimal));
		}

		public static string ToString(IList<short> values, short min, short max, bool? useDecimal) {
			return ToString(values, v => ToString(v, min, max, useDecimal));
		}

		public static string ToString(IList<int> values, int min, int max, bool? useDecimal) {
			return ToString(values, v => ToString(v, min, max, useDecimal));
		}

		public static string ToString(IList<long> values, long min, long max, bool? useDecimal) {
			return ToString(values, v => ToString(v, min, max, useDecimal));
		}

		public static string ToString(IList<float> values) {
			return ToString(values, v => ToString(v));
		}

		public static string ToString(IList<double> values) {
			return ToString(values, v => ToString(v));
		}

		public static string ToString(IList<string> values, bool canHaveNull) {
			return ToString(values, v => ToString(v, canHaveNull));
		}

		static T[] ParseList<T>(string s, out string error, Func<string, Tuple<T, string>> parseValue) {
			var list = new List<T>();

			s = s.Trim();
			if (s == string.Empty) {
				error = null;
				return list.ToArray();
			}

			foreach (var elem in s.Split(',')) {
				var value = elem.Trim();
				if (value == string.Empty) {
					error = "Value in list can't be empty";
					return null;
				}
				var res = parseValue(value);
				if (res.Item2 != null) {
					error = res.Item2;
					return null;
				}
				list.Add(res.Item1);
			}

			error = null;
			return list.ToArray();
		}

		delegate T ParseListCallBack<T, U>(U data, string s, ref int index, out string error);

		static T[] ParseList<T, U>(string s, out string error, ParseListCallBack<T, U> parseValue, U data) {
			var list = new List<T>();

			if (s.Trim() == string.Empty) {
				error = null;
				return list.ToArray();
			}

			int index = 0;
			while (true) {
				int oldIndex = index;
				list.Add(parseValue(data, s, ref index, out error));
				if (error != null)
					return null;
				Debug.Assert(oldIndex < index);
				if (oldIndex >= index)
					throw new InvalidOperationException();
				SkipSpaces(s, ref index);
				if (index >= s.Length)
					break;
				if (s[index] != ',') {
					error = "List elements must be separated with commas";
					return null;
				}
				index++;
			}

			return list.ToArray();
		}

		public static bool[] ParseBooleanList(string s, out string error) {
			return ParseList(s, out error, v => { string err; var res = ParseBoolean(v, out err); return Tuple.Create(res, err); });
		}

		public static char[] ParseCharList(string s, out string error) {
			return ParseList(s, out error, ParseCharPart, 0);
		}

		static char ParseCharPart(int data, string s, ref int index, out string error) {
			return ParseChar(s, ref index, out error);
		}

		public static byte[] ParseByteList(string s, byte min, byte max, out string error) {
			return ParseList(s, out error, v => { string err; var res = ParseByte(v, min, max, out err); return Tuple.Create(res, err); });
		}

		public static ushort[] ParseUInt16List(string s, ushort min, ushort max, out string error) {
			return ParseList(s, out error, v => { string err; var res = ParseUInt16(v, min, max, out err); return Tuple.Create(res, err); });
		}

		public static uint[] ParseUInt32List(string s, uint min, uint max, out string error) {
			return ParseList(s, out error, v => { string err; var res = ParseUInt32(v, min, max, out err); return Tuple.Create(res, err); });
		}

		public static ulong[] ParseUInt64List(string s, ulong min, ulong max, out string error) {
			return ParseList(s, out error, v => { string err; var res = ParseUInt64(v, min, max, out err); return Tuple.Create(res, err); });
		}

		public static sbyte[] ParseSByteList(string s, sbyte min, sbyte max, out string error) {
			return ParseList(s, out error, v => { string err; var res = ParseSByte(v, min, max, out err); return Tuple.Create(res, err); });
		}

		public static short[] ParseInt16List(string s, short min, short max, out string error) {
			return ParseList(s, out error, v => { string err; var res = ParseInt16(v, min, max, out err); return Tuple.Create(res, err); });
		}

		public static int[] ParseInt32List(string s, int min, int max, out string error) {
			return ParseList(s, out error, v => { string err; var res = ParseInt32(v, min, max, out err); return Tuple.Create(res, err); });
		}

		public static long[] ParseInt64List(string s, long min, long max, out string error) {
			return ParseList(s, out error, v => { string err; var res = ParseInt64(v, min, max, out err); return Tuple.Create(res, err); });
		}

		public static float[] ParseSingleList(string s, out string error) {
			return ParseList(s, out error, v => { string err; var res = ParseSingle(v, out err); return Tuple.Create(res, err); });
		}

		public static double[] ParseDoubleList(string s, out string error) {
			return ParseList(s, out error, v => { string err; var res = ParseDouble(v, out err); return Tuple.Create(res, err); });
		}

		public static string[] ParseStringList(string s, bool canHaveNull, out string error) {
			return ParseList(s, out error, ParseStringPart, canHaveNull);
		}

		static string ParseStringPart(bool canHaveNull, string s, ref int index, out string error) {
			return ParseString(s, canHaveNull, ref index, out error);
		}
	}
}
