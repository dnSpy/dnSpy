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

using System.Text;
using dnSpy.Contracts.Hex.Files;

namespace dnSpy.Hex.Files {
	struct NumberFormatter {
		public HexNumberOptions Options => options;
		readonly HexNumberOptions options;

		bool Decimal => (options & HexNumberOptions.NumberBaseMask) == HexNumberOptions.Decimal;
		bool MinimumDigits => (options & HexNumberOptions.MinimumDigits) != 0;

		public NumberFormatter(HexNumberOptions options) {
			this.options = options;
		}

		string AddHexIndicator(string text, bool isSigned) {
			switch (options & HexNumberOptions.NumberBaseMask) {
			case HexNumberOptions.HexCSharp:
				text = "0x" + text;
				break;
			case HexNumberOptions.HexVisualBasic:
				text = "&H" + text;
				break;
			case HexNumberOptions.HexAssembly:
				text = text + "h";
				break;
			}
			if (isSigned)
				return "-" + text;
			return text;
		}

		public string ToString(byte value) {
			if (Decimal || (MinimumDigits && value <= 9))
				return value.ToString();
			const string fullDigits = "2";
			string text;
			switch (options & (HexNumberOptions.LowerCaseHex | HexNumberOptions.MinimumDigits)) {
			default:
			case 0:
				text = value.ToString("X" + fullDigits);
				break;
			case HexNumberOptions.LowerCaseHex:
				text = value.ToString("x" + fullDigits);
				break;
			case HexNumberOptions.MinimumDigits:
				text = value.ToString("X");
				break;
			case HexNumberOptions.LowerCaseHex | HexNumberOptions.MinimumDigits:
				text = value.ToString("x");
				break;
			}
			return AddHexIndicator(text, isSigned: false);
		}

		public string ToString(ushort value) {
			if (Decimal || (MinimumDigits && value <= 9))
				return value.ToString();
			const string fullDigits = "4";
			string text;
			switch (options & (HexNumberOptions.LowerCaseHex | HexNumberOptions.MinimumDigits)) {
			default:
			case 0:
				text = value.ToString("X" + fullDigits);
				break;
			case HexNumberOptions.LowerCaseHex:
				text = value.ToString("x" + fullDigits);
				break;
			case HexNumberOptions.MinimumDigits:
				text = value.ToString("X");
				break;
			case HexNumberOptions.LowerCaseHex | HexNumberOptions.MinimumDigits:
				text = value.ToString("x");
				break;
			}
			return AddHexIndicator(text, isSigned: false);
		}

		public string ToString24(uint value) {
			if (Decimal || (MinimumDigits && value <= 9))
				return value.ToString();
			const string fullDigits = "6";
			string text;
			switch (options & (HexNumberOptions.LowerCaseHex | HexNumberOptions.MinimumDigits)) {
			default:
			case 0:
				text = value.ToString("X" + fullDigits);
				break;
			case HexNumberOptions.LowerCaseHex:
				text = value.ToString("x" + fullDigits);
				break;
			case HexNumberOptions.MinimumDigits:
				text = value.ToString("X");
				break;
			case HexNumberOptions.LowerCaseHex | HexNumberOptions.MinimumDigits:
				text = value.ToString("x");
				break;
			}
			return AddHexIndicator(text, isSigned: false);
		}

		public string ToString(uint value) {
			if (Decimal || (MinimumDigits && value <= 9))
				return value.ToString();
			const string fullDigits = "8";
			string text;
			switch (options & (HexNumberOptions.LowerCaseHex | HexNumberOptions.MinimumDigits)) {
			default:
			case 0:
				text = value.ToString("X" + fullDigits);
				break;
			case HexNumberOptions.LowerCaseHex:
				text = value.ToString("x" + fullDigits);
				break;
			case HexNumberOptions.MinimumDigits:
				text = value.ToString("X");
				break;
			case HexNumberOptions.LowerCaseHex | HexNumberOptions.MinimumDigits:
				text = value.ToString("x");
				break;
			}
			return AddHexIndicator(text, isSigned: false);
		}

		public string ToString(ulong value) {
			if (Decimal || (MinimumDigits && value <= 9))
				return value.ToString();
			const string fullDigits = "16";
			string text;
			switch (options & (HexNumberOptions.LowerCaseHex | HexNumberOptions.MinimumDigits)) {
			default:
			case 0:
				text = value.ToString("X" + fullDigits);
				break;
			case HexNumberOptions.LowerCaseHex:
				text = value.ToString("x" + fullDigits);
				break;
			case HexNumberOptions.MinimumDigits:
				text = value.ToString("X");
				break;
			case HexNumberOptions.LowerCaseHex | HexNumberOptions.MinimumDigits:
				text = value.ToString("x");
				break;
			}
			return AddHexIndicator(text, isSigned: false);
		}

		public string ToString(sbyte value) {
			if (Decimal || (MinimumDigits && -9 <= value && value <= 9))
				return value.ToString();
			bool isSigned = value < 0;
			if (isSigned)
				value = (sbyte)-value;
			const string fullDigits = "2";
			string text;
			switch (options & (HexNumberOptions.LowerCaseHex | HexNumberOptions.MinimumDigits)) {
			default:
			case 0:
				text = value.ToString("X" + fullDigits);
				break;
			case HexNumberOptions.LowerCaseHex:
				text = value.ToString("x" + fullDigits);
				break;
			case HexNumberOptions.MinimumDigits:
				text = value.ToString("X");
				break;
			case HexNumberOptions.LowerCaseHex | HexNumberOptions.MinimumDigits:
				text = value.ToString("x");
				break;
			}
			return AddHexIndicator(text, isSigned);
		}

		public string ToString(short value) {
			if (Decimal || (MinimumDigits && -9 <= value && value <= 9))
				return value.ToString();
			bool isSigned = value < 0;
			if (isSigned)
				value = (short)-value;
			const string fullDigits = "4";
			string text;
			switch (options & (HexNumberOptions.LowerCaseHex | HexNumberOptions.MinimumDigits)) {
			default:
			case 0:
				text = value.ToString("X" + fullDigits);
				break;
			case HexNumberOptions.LowerCaseHex:
				text = value.ToString("x" + fullDigits);
				break;
			case HexNumberOptions.MinimumDigits:
				text = value.ToString("X");
				break;
			case HexNumberOptions.LowerCaseHex | HexNumberOptions.MinimumDigits:
				text = value.ToString("x");
				break;
			}
			return AddHexIndicator(text, isSigned);
		}

		public string ToString(int value) {
			if (Decimal || (MinimumDigits && -9 <= value && value <= 9))
				return value.ToString();
			bool isSigned = value < 0;
			if (isSigned)
				value = -value;
			const string fullDigits = "8";
			string text;
			switch (options & (HexNumberOptions.LowerCaseHex | HexNumberOptions.MinimumDigits)) {
			default:
			case 0:
				text = value.ToString("X" + fullDigits);
				break;
			case HexNumberOptions.LowerCaseHex:
				text = value.ToString("x" + fullDigits);
				break;
			case HexNumberOptions.MinimumDigits:
				text = value.ToString("X");
				break;
			case HexNumberOptions.LowerCaseHex | HexNumberOptions.MinimumDigits:
				text = value.ToString("x");
				break;
			}
			return AddHexIndicator(text, isSigned);
		}

		public string ToString(long value) {
			if (Decimal || (MinimumDigits && -9 <= value && value <= 9))
				return value.ToString();
			bool isSigned = value < 0;
			if (isSigned)
				value = -value;
			const string fullDigits = "16";
			string text;
			switch (options & (HexNumberOptions.LowerCaseHex | HexNumberOptions.MinimumDigits)) {
			default:
			case 0:
				text = value.ToString("X" + fullDigits);
				break;
			case HexNumberOptions.LowerCaseHex:
				text = value.ToString("x" + fullDigits);
				break;
			case HexNumberOptions.MinimumDigits:
				text = value.ToString("X");
				break;
			case HexNumberOptions.LowerCaseHex | HexNumberOptions.MinimumDigits:
				text = value.ToString("x");
				break;
			}
			return AddHexIndicator(text, isSigned);
		}

		public string ToString(float value) => value.ToString();
		public string ToString(double value) => value.ToString();
		public string ToString(decimal value) => value.ToString();

		public string ToString(string value) {
			var sb = new StringBuilder(value.Length + 10);
			sb.Append('"');
			foreach (var c in value) {
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

		public string ToString(char value) {
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
	}
}
