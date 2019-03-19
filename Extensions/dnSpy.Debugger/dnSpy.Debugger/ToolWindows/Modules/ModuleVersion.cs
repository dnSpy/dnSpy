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

using System;

namespace dnSpy.Debugger.ToolWindows.Modules {
	readonly struct ModuleVersion {
		static readonly Version version_0_0_0_0 = new Version(0, 0, 0, 0);
		readonly Version version;
		readonly string remaining;

		public ModuleVersion(string versionString) {
			if (versionString == null)
				versionString = string.Empty;
			int index = GetEndIndex(versionString);
			var verSubStr = index == versionString.Length ? versionString : versionString.Substring(0, index);
			if (Version.TryParse(verSubStr, out version))
				remaining = index == versionString.Length ? string.Empty : versionString.Substring(index);
			else {
				remaining = versionString;
				version = version_0_0_0_0;
			}
		}

		static int GetEndIndex(string v) {
			for (int i = 0; i < v.Length; i++) {
				var c = v[i];
				if (!char.IsDigit(c) && c != '.')
					return i;
			}
			return v.Length;
		}

		public int CompareTo(ModuleVersion other) {
			int c = version.CompareTo(other.version);
			if (c != 0)
				return c;
			return StringComparer.OrdinalIgnoreCase.Compare(remaining, other.remaining);
		}
	}
}
