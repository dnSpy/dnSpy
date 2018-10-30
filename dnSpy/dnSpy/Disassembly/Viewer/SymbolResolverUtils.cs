/*
    Copyright (C) 2014-2018 de4dot@gmail.com

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

namespace dnSpy.Disassembly.Viewer {
	static class SymbolResolverUtils {
		const int MAX_SYM_NAME_LEN = 1024;

		public static string FixSymbol(string symbol) {
			if (string.IsNullOrEmpty(symbol))
				return string.Empty;

			int i = 0;
			if (symbol.Length <= MAX_SYM_NAME_LEN) {
				for (; ; i++) {
					if (i >= symbol.Length)
						return symbol;
					if (!IsValidSymbolChar(symbol[i]))
						break;
				}
			}

			var sb = new StringBuilder(symbol.Length + 10);
			sb.Clear();
			if (i != 0)
				sb.Append(symbol, 0, i);

			for (; i < symbol.Length; i++) {
				char c = symbol[i];
				if (!IsValidSymbolChar(c)) {
					sb.Append(@"\u");
					sb.Append(((ushort)c).ToString("X4"));
				}
				else
					sb.Append(c);
				if (sb.Length >= MAX_SYM_NAME_LEN)
					break;
			}

			if (sb.Length > MAX_SYM_NAME_LEN) {
				sb.Length = MAX_SYM_NAME_LEN;
				sb.Append("...");
			}

			return sb.ToString();
		}

		static bool IsValidSymbolChar(char c) {
			switch (char.GetUnicodeCategory(c)) {
			case UnicodeCategory.UppercaseLetter:
			case UnicodeCategory.LowercaseLetter:
			case UnicodeCategory.TitlecaseLetter:
			case UnicodeCategory.ModifierLetter:
			case UnicodeCategory.OtherLetter:
			case UnicodeCategory.NonSpacingMark:
			case UnicodeCategory.SpacingCombiningMark:
			case UnicodeCategory.EnclosingMark:
			case UnicodeCategory.DecimalDigitNumber:
			case UnicodeCategory.LetterNumber:
			case UnicodeCategory.OtherNumber:
			case UnicodeCategory.SpaceSeparator:
			case UnicodeCategory.Surrogate:
			case UnicodeCategory.ConnectorPunctuation:
			case UnicodeCategory.DashPunctuation:
			case UnicodeCategory.OpenPunctuation:
			case UnicodeCategory.ClosePunctuation:
			case UnicodeCategory.InitialQuotePunctuation:
			case UnicodeCategory.FinalQuotePunctuation:
			case UnicodeCategory.OtherPunctuation:
			case UnicodeCategory.MathSymbol:
			case UnicodeCategory.CurrencySymbol:
			case UnicodeCategory.ModifierSymbol:
			case UnicodeCategory.OtherSymbol:
			case UnicodeCategory.OtherNotAssigned:
				return true;

			case UnicodeCategory.LineSeparator:
			case UnicodeCategory.ParagraphSeparator:
			case UnicodeCategory.Control:
			case UnicodeCategory.Format:
			case UnicodeCategory.PrivateUse:
				return false;

			default:
				Debug.Fail($"Unknown unicode category: {char.GetUnicodeCategory(c)}");
				return false;
			}
		}
	}
}
