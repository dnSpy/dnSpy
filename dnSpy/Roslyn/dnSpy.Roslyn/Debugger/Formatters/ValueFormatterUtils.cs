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

namespace dnSpy.Roslyn.Debugger.Formatters {
	static class ValueFormatterUtils {
		public const int MaxStringLength = 256 * 1024;
		public const int DigitGroupSizeHex = 4;
		public const int DigitGroupSizeDecimal = 3;
		public const string DigitSeparator = "_";
		public const string NaN = "NaN";
		public const string NegativeInfinity = "-Infinity";
		public const string PositiveInfinity = "Infinity";

		public static string ToFormattedNumber(bool digitSeparators, string prefix, string number, int digitGroupSize) {
			if (digitSeparators)
				number = AddDigitSeparators(number, digitGroupSize, DigitSeparator);

			string res = number;
			if (prefix.Length != 0)
				res = prefix + res;
			return res;
		}

		static string AddDigitSeparators(string number, int digitGroupSize, string digitSeparator) {
			if (number.Length <= digitGroupSize)
				return number;

			var sb = ObjectCache.AllocStringBuilder();

			for (int i = 0; i < number.Length; i++) {
				int d = number.Length - i;
				if (i != 0 && (d % digitGroupSize) == 0 && number[i - 1] != '-')
					sb.Append(DigitSeparator);
				sb.Append(number[i]);
			}

			return ObjectCache.FreeAndToString(ref sb);
		}
	}
}
