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

using System.IO;

namespace dnSpy.Decompiler.MSBuild {
	// Doesn't rely on Path methods since those can throw if input contains invalid chars
	static class FileUtils {
		static readonly char[] dirSepChars = new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

		public static string GetFilename(string name) {
			int i = name.LastIndexOfAny(dirSepChars);
			if (i >= 0)
				name = name.Substring(i + 1);
			return name;
		}

		public static string GetExtension(string name) {
			int i = name.LastIndexOf('.');
			if (i < 0)
				return string.Empty;
			return name.Substring(i);
		}

		public static string GetFileNameWithoutExtension(string name) {
			int i = name.LastIndexOfAny(new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar });
			if (i >= 0)
				name = name.Substring(i);
			i = name.LastIndexOf('.');
			if (i < 0)
				return name;
			return name.Substring(0, i);
		}

		public static string RemoveExtension(string name) {
			int i = name.LastIndexOf('.');
			if (i < 0)
				return name;
			return name.Substring(0, i);
		}
	}
}
