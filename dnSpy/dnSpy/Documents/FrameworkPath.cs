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
using System.Linq;

namespace dnSpy.Documents {
	// It's a class since very few of these are created
	[DebuggerDisplay("{Bitness,d}-bit {Version,nq} {DebuggerPaths,nq}")]
	sealed class FrameworkPaths : IComparable<FrameworkPaths> {
		public readonly string[] Paths;
		public readonly int Bitness;
		public readonly FrameworkVersion Version;
		public readonly Version SystemVersion;

		string DebuggerPaths => string.Join(Path.PathSeparator.ToString(), Paths);

		public FrameworkPaths(FrameworkPath[] paths) {
			var firstPath = paths[0];
#if DEBUG
			for (int i = 1; i < paths.Length; i++) {
				if (firstPath.Bitness != paths[i].Bitness)
					throw new ArgumentException();
				// Ignore Extra since it can be different if it's a preview
				if (firstPath.Version.Major != paths[i].Version.Major ||
					firstPath.Version.Minor != paths[i].Version.Minor ||
					firstPath.Version.Patch != paths[i].Version.Patch)
					throw new ArgumentException();
			}
#endif
			var allPaths = paths.Select(a => a.Path).ToArray();
			Array.Sort(allPaths, SortPaths);
			Paths = allPaths;
			Bitness = firstPath.Bitness;
			Version = firstPath.Version;
			SystemVersion = new Version(firstPath.Version.Major, firstPath.Version.Minor, firstPath.Version.Patch, 0);

			foreach (var p in Paths) {
				if (StringComparer.OrdinalIgnoreCase.Equals(Path.GetFileName(Path.GetDirectoryName(p)), DotNetCoreAppDir)) {
					HasDotNetCoreAppPath = true;
					break;
				}
			}
		}

		// Sort the .NET Core dir last since it also contains some assemblies that exist in some other
		// dirs, eg. WindowsBase.dll is in both Microsoft.NETCore.App and Microsoft.WindowsDesktop.App
		// and the one in Microsoft.NETCore.App isn't the same one WPF apps expect (it has no types).
		// There are other dupe assemblies, eg. Microsoft.Win32.Registry.dll exists both in
		// Microsoft.NETCore.App and Microsoft.WindowsDesktop.App.
		const string DotNetCoreAppDir = "Microsoft.NETCore.App";
		static int SortPaths(string x, string y) {
			int c = GetPathGroupOrder(x) - GetPathGroupOrder(y);
			if (c != 0)
				return c;
			return StringComparer.OrdinalIgnoreCase.Compare(x, y);
		}

		static int GetPathGroupOrder(string path) {
			if (StringComparer.OrdinalIgnoreCase.Equals(Path.GetFileName(Path.GetDirectoryName(path)), DotNetCoreAppDir))
				return int.MaxValue;
			return 0;
		}

		public int CompareTo(FrameworkPaths other) {
			int c = Version.CompareTo(other.Version);
			if (c != 0)
				return c;
			c = Bitness - other.Bitness;
			if (c != 0)
				return c;

			return CompareTo(Paths, other.Paths);
		}

		static int CompareTo(string[] a, string[] b) {
			if (a.Length != b.Length)
				return a.Length - b.Length;
			for (int i = 0; i < a.Length; i++) {
				int c = StringComparer.OrdinalIgnoreCase.Compare(Path.GetDirectoryName(a[i]), Path.GetDirectoryName(b[i]));
				if (c != 0)
					return c;
			}
			return 0;
		}

		internal bool HasDotNetCoreAppPath { get; }
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
