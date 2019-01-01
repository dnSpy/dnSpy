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
using System.Diagnostics;
using System.IO;

namespace dnSpy.Documents {
	static class FileUtils {
		public static bool IsFileInDir(string dir, string file) {
			Debug.Assert(dir.Length != 0);
			if (dir.Length >= file.Length)
				return false;
			var c = file[dir.Length];
			if (c != Path.DirectorySeparatorChar && c != Path.AltDirectorySeparatorChar)
				return false;
			if (!file.StartsWith(dir, StringComparison.OrdinalIgnoreCase))
				return false;
			return file.IndexOfAny(dirSeps, dir.Length + 1) < 0;
		}
		static readonly char[] dirSeps = Path.DirectorySeparatorChar == Path.AltDirectorySeparatorChar ?
			new[] { Path.DirectorySeparatorChar } :
			new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
	}
}
