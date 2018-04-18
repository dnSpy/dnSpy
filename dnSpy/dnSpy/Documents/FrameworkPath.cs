/*
    Copyright (C) 2014-2018 de4dot@gmail.com

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
using System.Linq;

namespace dnSpy.Documents {
	// It's a class since very few of these are created
	[DebuggerDisplay("{Bitness,d}-bit {Version,nq} {DebuggerPaths,nq}")]
	sealed class FrameworkPaths : IComparable<FrameworkPaths> {
		public readonly string[] Paths;
		public readonly int Bitness;
		public readonly FrameworkVersion Version;

		string DebuggerPaths => string.Join(Path.PathSeparator.ToString(), Paths);

		public FrameworkPaths(FrameworkPath[] paths) {
			var firstPath = paths[0];
#if DEBUG
			for (int i = 1; i < paths.Length; i++) {
				if (!StringComparer.OrdinalIgnoreCase.Equals(Path.GetDirectoryName(firstPath.Path), Path.GetDirectoryName(paths[i].Path)))
					throw new ArgumentException();
				if (firstPath.Bitness != paths[i].Bitness)
					throw new ArgumentException();
				if (!firstPath.Version.Equals(paths[i].Version))
					throw new ArgumentException();
			}
#endif
			Paths = paths.Select(a => a.Path).ToArray();
			Bitness = firstPath.Bitness;
			Version = firstPath.Version;
		}

		public int CompareTo(FrameworkPaths other) {
			int c = Version.CompareTo(other.Version);
			if (c != 0)
				return c;
			c = Bitness - other.Bitness;
			if (c != 0)
				return c;

			return StringComparer.OrdinalIgnoreCase.Compare(Path.GetDirectoryName(Paths[0]), Path.GetDirectoryName(other.Paths[0]));
		}

		internal bool HasDotNetCoreAppPath {
			get {
				foreach (var p in Paths) {
					if (StringComparer.OrdinalIgnoreCase.Equals(Path.GetFileName(Path.GetDirectoryName(p)), "Microsoft.NETCore.App"))
						return true;
				}
				return false;
			}
		}
	}

	// It's a class since very few of these are created
	[DebuggerDisplay("{Bitness,d}-bit {Version,nq} {Path,nq}")]
	sealed class FrameworkPath {
		public readonly string Path;
		public readonly int Bitness;
		public readonly FrameworkVersion Version;
		public FrameworkPath(string path, int bitness, FrameworkVersion version) {
			Path = path ?? throw new ArgumentNullException(nameof(path));
			Bitness = bitness;
			Version = version;
		}
	}

	readonly struct FrameworkVersion : IComparable<FrameworkVersion>, IEquatable<FrameworkVersion> {
		public readonly int Major;
		public readonly int Minor;
		public readonly int Patch;
		public readonly string Extra;
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

		public bool Equals(FrameworkVersion other) =>
			Major == other.Major &&
			Minor == other.Minor &&
			Patch == other.Patch &&
			StringComparer.Ordinal.Equals(Extra, other.Extra);

		public override bool Equals(object obj) => obj is FrameworkVersion other && Equals(other);
		public override int GetHashCode() => Major ^ Minor ^ Patch ^ StringComparer.Ordinal.GetHashCode(Extra ?? string.Empty);
	}
}
