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

namespace dnSpy.Hex.Files.DotNet {
	static class NameUtils {
		const int MAX_NAME_LENGTH = 512;

		public static string FilterName(string name) => FilterName(name, MAX_NAME_LENGTH);

		static string FilterName(string name, int maxLength) {
			int length = Math.Min(maxLength, name.Length);
			int i;
			for (i = 0; i < length; i++) {
				if (!IsValidChar(name[i]))
					break;
			}
			if (i == length) {
				if (name.Length == length)
					return name;
				return name.Substring(0, length);
			}
			var chars = new char[length];
			for (i = 0; i < chars.Length; i++) {
				var c = name[i];
				if (IsValidChar(c))
					chars[i] = c;
				else
					chars[i] = '?';
			}
			return new string(chars);
		}

		static bool IsValidChar(char c) => c >= 0x20;
	}
}
