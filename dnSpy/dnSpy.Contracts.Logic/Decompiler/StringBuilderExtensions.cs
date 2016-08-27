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

namespace dnSpy.Contracts.Decompiler {
	static class StringBuilderExtensions {
		/// <summary>
		/// Compares <paramref name="sb"/> with <paramref name="s"/>. No string is created.
		/// </summary>
		/// <param name="sb">This</param>
		/// <param name="s">String</param>
		/// <returns></returns>
		public static bool CheckEquals(this StringBuilder sb, string s) {
			if (s == null || sb.Length != s.Length)
				return false;
			for (int i = 0; i < s.Length; i++) {
				if (sb[i] != s[i])
					return false;
			}
			return true;
		}
	}
}
