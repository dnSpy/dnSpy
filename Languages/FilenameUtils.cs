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
using System.IO;

namespace dnSpy.Languages {
	public static class FilenameUtils {
		static readonly HashSet<string> ReservedFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
			"CON", "PRN", "AUX", "NUL",
			"COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
			"LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
		};

		static readonly HashSet<char> invalidFileNameChars = new HashSet<char>(Path.GetInvalidFileNameChars());

		public static string CleanName(string text) {
			int pos = text.IndexOf(':');
			if (pos > 0)
				text = text.Substring(0, pos);
			pos = text.IndexOf('`');
			if (pos > 0)
				text = text.Substring(0, pos);
			text = text.Trim();

			char[] textChars = null;
			for (int i = 0; i < text.Length; i++) {
				if (invalidFileNameChars.Contains(text[i])) {
					if (textChars == null)
						textChars = text.ToCharArray();
					textChars[i] = '-';
				}
			}
			if (textChars != null)
				text = new string(textChars);

			if (ReservedFileNames.Contains(text))
				text = text + "_";
			return text;
		}

		// ("C:\dir1\dir2\dir3", "d:\Dir1\Dir2\Dir3\file.dll") = "d:\Dir1\Dir2\Dir3\file.dll"
		// ("C:\dir1\dir2\dir3", "c:\Dir1\dirA\dirB\file.dll") = "..\..\dirA\dirB\file.dll"
		// ("C:\dir1\dir2\dir3", "c:\Dir1\Dir2\Dir3\Dir4\Dir5\file.dll") = "Dir4\Dir5\file.dll"
		internal static string GetRelativePath(string sourceDir, string destFile) {
			sourceDir = Path.GetFullPath(sourceDir);
			destFile = Path.GetFullPath(destFile);
			if (!Path.GetPathRoot(sourceDir).Equals(Path.GetPathRoot(destFile), StringComparison.OrdinalIgnoreCase))
				return destFile;
			var sourceDirs = GetPathNames(sourceDir);
			var destDirs = GetPathNames(Path.GetDirectoryName(destFile));

			var hintPath = string.Empty;
			int i;
			for (i = 0; i < sourceDirs.Count && i < destDirs.Count; i++) {
				if (!sourceDirs[i].Equals(destDirs[i], StringComparison.OrdinalIgnoreCase))
					break;
			}
			for (int j = i; j < sourceDirs.Count; j++)
				hintPath = Path.Combine(hintPath, "..");
			for (; i < destDirs.Count; i++)
				hintPath = Path.Combine(hintPath, destDirs[i]);
			hintPath = Path.Combine(hintPath, Path.GetFileName(destFile));

			return hintPath;
		}

		static List<string> GetPathNames(string path) {
			var list = new List<string>();
			var root = Path.GetPathRoot(path);
			while (path != root) {
				list.Add(Path.GetFileName(path));
				path = Path.GetDirectoryName(path);
			}
			list.Add(root);
			list.Reverse();
			return list;
		}
	}
}
