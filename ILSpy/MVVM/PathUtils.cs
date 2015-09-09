/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using System.IO;

namespace dnSpy.MVVM {
	static class PathUtils {
		/// <summary>
		/// Validates a filename. Returns null if it's valid else a message that can be used in the
		/// UI. It won't find every problem though so exceptions could happen when trying to create
		/// the file.
		/// </summary>
		/// <param name="filename">Filename</param>
		/// <returns></returns>
		public static string ValidateFileName(this string filename) {
			if (string.IsNullOrEmpty(filename))
				return "Filename can't be empty.";
			if (string.IsNullOrWhiteSpace(filename))
				return "Filename can't contain only white space";

			if (filename.Length >= 260)
				return "Filename is too long.";

			if (filename.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
				return "Filename contains at least one invalid path character.";

			filename = filename.Replace('/', '\\');
			var dirs = filename.Split('\\');
			int start = Path.IsPathRooted(filename) ? 1 : 0;
			for (int i = start; i < dirs.Length; i++) {
				if (dirs[i].IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
					return "Filename contains at least one invalid path character.";
			}

			if (string.IsNullOrEmpty(Path.GetFileName(filename)))
				return "Filename can't be empty";
			var fnameNoExt = Path.GetFileNameWithoutExtension(filename);
			foreach (var invalidName in InvalidFileNamesNoExtension) {
				if (invalidName.Equals(fnameNoExt, StringComparison.OrdinalIgnoreCase))
					return "The filename is a reserved operating system filename and can't be used.";
			}

			// Don't check for dirs on the network. Could take a while and will block the UI
			if (!filename.StartsWith(@"\\")) {
				if (Directory.Exists(filename))
					return "A folder with the same name as the filename already exists.";
				var dir = Path.GetDirectoryName(filename);
				if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
					return string.Format("Folder '{0}' doesn't exist.", dir);
			}

			// We should've found most problems so return success
			return null;
		}

		static readonly string[] InvalidFileNamesNoExtension = new string[] {
			"CON", "PRN", "AUX", "NUL",
			"COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
			"LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
		};
	}
}
