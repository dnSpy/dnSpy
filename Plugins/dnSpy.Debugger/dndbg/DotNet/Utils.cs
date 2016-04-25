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

using dnlib.DotNet;

namespace dndbg.DotNet {
	static class Utils {
		public static void SplitNameAndNamespace(UTF8String utf8Name, string fullName, out UTF8String ns, out UTF8String name) {
			if (fullName == null)
				fullName = string.Empty;

			if (!UTF8String.IsNull(utf8Name)) {
				if (fullName == utf8Name.String) {
					ns = UTF8String.Empty;
					name = utf8Name;
					return;
				}

				if (fullName.EndsWith("." + utf8Name.String)) {
					ns = fullName.Substring(0, fullName.Length - utf8Name.String.Length - 1);
					name = utf8Name;
					return;
				}
			}

			int i = fullName.LastIndexOf('.');
			if (i < 0) {
				ns = UTF8String.Empty;
				name = fullName;
			}
			else {
				ns = fullName.Substring(0, i);
				name = fullName.Substring(i + 1);
			}
		}

		public static UTF8String GetUTF8String(UTF8String utf8String, string s) {
			if (!UTF8String.IsNull(utf8String))
				return utf8String;
			return s;
		}
	}
}
