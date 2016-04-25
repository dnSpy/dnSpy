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
using System.Collections.Generic;
using System.Linq;

namespace dnSpy.Scripting.Roslyn.Common {
	static class RespFileUtils {
		public static IEnumerable<string> GetReferencePaths(string s) =>
			s.Split(new char[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries).Select(a => RemoveQuotes(a));

		public static IEnumerable<string> GetReferences(string s) {
			if (s.Length == 0)
				yield break;
			if (s[0] == '"') {
				yield return RemoveQuotes(s);
				yield break;
			}
			foreach (var x in s.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
				yield return x;
		}

		static string RemoveQuotes(string s) {
			if (s.Length == 0 || s[0] != '"')
				return s;
			s = s.Substring(1);
			if (s.Length > 0 && s[s.Length - 1] == '"')
				s = s.Substring(0, s.Length - 1);
			return s;
		}
	}
}
