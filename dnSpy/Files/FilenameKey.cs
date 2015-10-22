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

namespace dnSpy.Files {
	sealed class FilenameKey : IDnSpyFilenameKey, IEquatable<FilenameKey> {
		readonly string filename;

		public FilenameKey(string filename) {
			this.filename = GetFullPath(filename);
		}

		static string GetFullPath(string filename) {
			try {
				// Prevent slow exceptions
				if (string.IsNullOrEmpty(filename))
					return filename;
				return Path.GetFullPath(filename);
			}
			catch {
			}
			return filename;
		}

		public bool Equals(FilenameKey other) {
			return other != null &&
					StringComparer.OrdinalIgnoreCase.Equals(filename, other.filename);
		}

		public override bool Equals(object obj) {
			return Equals(obj as FilenameKey);
		}

		public override int GetHashCode() {
			return StringComparer.OrdinalIgnoreCase.GetHashCode(filename);
		}

		public override string ToString() {
			return filename;
		}
	}
}
