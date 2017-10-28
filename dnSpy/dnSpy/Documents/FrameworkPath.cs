/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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

namespace dnSpy.Documents {
	// It's a class since very few of these are created
	[DebuggerDisplay("{Bitness,d}-bit {Version,nq} {Path,nq}")]
	sealed class FrameworkPath : IComparable<FrameworkPath> {
		public readonly string Path;
		public readonly int Bitness;
		public /*readonly*/ FrameworkVersion Version;
		public FrameworkPath(string path, int bitness, FrameworkVersion version) {
			Path = path ?? throw new ArgumentNullException(nameof(path));
			Bitness = bitness;
			Version = version;
		}

		public int CompareTo(FrameworkPath other) {
			int c = Version.CompareTo(other.Version);
			if (c != 0)
				return c;
			c = Bitness - other.Bitness;
			if (c != 0)
				return c;
			return StringComparer.OrdinalIgnoreCase.Compare(Path, other.Path);
		}
	}

	struct FrameworkVersion : IComparable<FrameworkVersion> {
		public int Major;
		public int Minor;
		public int Patch;
		public string Extra;
		public FrameworkVersion(int major, int minor, int patch, string extra) {
			Major = major;
			Minor = minor;
			Patch = patch;
			Extra = extra;
		}
		public override string ToString() {
			if (Extra.Length == 0)
				return $"{Major}.{Minor}.{Patch}";
			return $"{Major}.{Minor}.{Patch}-{Extra}";
		}

		public int CompareTo(FrameworkVersion other) {
			int c = Major.CompareTo(other.Major);
			if (c != 0)
				return c;
			c = Minor.CompareTo(other.Minor);
			if (c != 0)
				return c;
			c = Patch.CompareTo(other.Patch);
			if (c != 0)
				return c;
			return CompareExtra(Extra, other.Extra);
		}

		static int CompareExtra(string a, string b) {
			if (a.Length == 0 && b.Length == 0)
				return 0;
			if (a.Length == 0)
				return 1;
			if (b.Length == 0)
				return -1;
			return StringComparer.Ordinal.Compare(a, b);
		}
	}
}
