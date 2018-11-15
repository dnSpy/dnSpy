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

using System.Runtime.CompilerServices;

namespace dnSpy.Contracts.DnSpy.Text.WPF {
	/// <summary>
	/// Workaround for a WPF bug that terminates the process if any WPF control tries to format
	/// a string with too many combining marks.
	/// Test string: new string('\u0300', 512)
	/// </summary>
	static class WpfUnicodeUtils {
		// The real limit seems to be 512
		public const int MAX_BAD_CHARS = 500;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsBadWpfCombiningMark(char c) =>
			(c >= 0x0300 && c <= 0x036F) ||// Combining Diacritical Marks
			(c >= 0x1AB0 && c <= 0x1AFF) ||// Combining Diacritical Marks Extended
			(c >= 0x1DC0 && c <= 0x1DFF) ||// Combining Diacritical Marks Supplement
			(c >= 0x20D0 && c <= 0x20FF) ||// Combining Diacritical Marks for Symbols
			(c >= 0xFE20 && c <= 0xFE2F);  // Combining Half Marks

		public static string ReplaceBadChars(string s) {
			bool hasBadChar = false;
			foreach (var c in s) {
				if (IsBadWpfCombiningMark(c)) {
					hasBadChar = true;
					break;
				}
			}
			if (!hasBadChar)
				return s;

			var chars = new char[s.Length];
			int badChars = 0;
			for (int i = 0; i < s.Length; i++) {
				var c = s[i];
				if (IsBadWpfCombiningMark(c)) {
					badChars++;
					if (badChars == MAX_BAD_CHARS) {
						c = '?';
						badChars = 0;
					}
				}
				chars[i] = c;
			}
			return new string(chars);
		}
	}
}
