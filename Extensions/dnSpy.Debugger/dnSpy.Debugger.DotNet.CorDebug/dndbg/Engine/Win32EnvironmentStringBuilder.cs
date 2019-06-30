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

using System.Collections.Generic;
using System.Text;

namespace dndbg.Engine {
	static class Win32EnvironmentStringBuilder {
		public static string CreateEnvironmentUnicodeString(IEnumerable<KeyValuePair<string, string>> environment) {
			var sb = new StringBuilder();

			foreach (var kv in environment) {
				sb.Append(kv.Key);
				sb.Append('=');
				sb.Append(kv.Value);
				sb.Append('\0');
			}

			sb.Append('\0');
			return sb.ToString();
		}
	}
}
