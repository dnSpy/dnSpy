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
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace dnSpy.Documents {
	sealed class DotNetPathProvider {
		readonly FrameworkPaths[] netPaths;

		public bool HasDotNet => netPaths.Length != 0;

		readonly struct DotNetPathInfo {
			public readonly string Directory;
			public readonly int Bitness;
			public DotNetPathInfo(string directory, int bitness) {
				Directory = directory ?? throw new ArgumentNullException(nameof(directory));
				Bitness = bitness;
			}
		}

		// It treats all previews/alphas as the same version. This is needed because .NET Core 3.0 preview's
		// shared frameworks don't have the same version numbers, eg.:
		//		shared\Microsoft.AspNetCore.App\3.0.0-preview-18579-0056
		//		shared\Microsoft.NETCore.App\3.0.0-preview-27216-02
		//		shared\Microsoft.WindowsDesktop.App\3.0.0-alpha-27214-12
		readonly struct FrameworkVersionIgnoreExtra : IEquatable<FrameworkVersionIgnoreExtra> {
			readonly FrameworkVersion version;

			public FrameworkVersionIgnoreExtra(FrameworkVersion version) => this.version = version;

			public bool Equals(FrameworkVersionIgnoreExtra other) =>
				version.Major == other.version.Major &&
				version.Minor == other.version.Minor &&
				version.Patch == other.version.Patch &&
				(version.Extra.Length == 0) == (other.version.Extra.Length == 0);

			public override bool Equals(object? obj) => obj is FrameworkVersionIgnoreExtra other && Equals(other);
			public override int GetHashCode() => version.Major ^ version.Minor ^ version.Patch ^ (version.Extra.Length == 0 ? 0 : -1);
		}

		public DotNetPathProvider() {
			var list = new List<FrameworkPath>();
			foreach (var info in GetDotNetBaseDirs())
				list.AddRange(GetDotNetPaths(info.Directory, info.Bitness));

			var paths = from p in list
						group p by new { Path = (Path.GetDirectoryName(Path.GetDirectoryName(p.Path)) ?? string.Empty).ToUpperInvariant(), p.Bitness, Version = new FrameworkVersionIgnoreExtra(p.Version) } into g
						where !string.IsNullOrEmpty(g.Key.Path)
						select new FrameworkPaths(g.ToArray());
			var array = paths.ToArray();
			Array.Sort(array);
			netPaths = array;
		}

		public string[]? TryGetDotNetPaths(Version version, int bitness) {
			Debug.Assert(bitness == 32 || bitness == 64);
			int bitness2 = bitness ^ 0x60;
			FrameworkPaths? info;

			info = TryGetDotNetPathsCore(version.Major, version.Minor, bitness) ??
				TryGetDotNetPathsCore(version.Major, version.Minor, bitness2);
			if (info is not null)
				return info.Paths;

			info = TryGetDotNetPathsCore(version.Major, bitness) ??
				TryGetDotNetPathsCore(version.Major, bitness2);
			if (info is not null)
				return info.Paths;

			info = TryGetDotNetPathsCore(bitness) ??
				TryGetDotNetPathsCore(bitness2);
			if (info is not null)
				return info.Paths;

			return null;
		}

		FrameworkPaths? TryGetDotNetPathsCore(int major, int minor, int bitness) {
			FrameworkPaths? fpMajor = null;
			FrameworkPaths? fpMajorMinor = null;
			for (int i = netPaths.Length - 1; i >= 0; i--) {
				var info = netPaths[i];
				if (info.Bitness == bitness && info.Version.Major == major) {
					if (fpMajor is null)
						fpMajor = info;
					else
						fpMajor = BestMinorVersion(minor, fpMajor, info);
					if (info.Version.Minor == minor) {
						if (info.HasDotNetAppPath)
							return info;
						if (fpMajorMinor is null)
							fpMajorMinor = info;
					}
				}
			}
			return fpMajorMinor ?? fpMajor;
		}

		static FrameworkPaths BestMinorVersion(int minor, FrameworkPaths a, FrameworkPaths b) {
			uint da = VerDist(minor, a.Version.Minor);
			uint db = VerDist(minor, b.Version.Minor);
			if (da < db)
				return a;
			if (db < da)
				return b;
			if (!string.IsNullOrEmpty(b.Version.Extra))
				return a;
			if (!string.IsNullOrEmpty(a.Version.Extra))
				return b;
			return a;
		}

		// Any ver < minVer is worse than any ver >= minVer
		static uint VerDist(int minVer, int ver) {
			if (ver >= minVer)
				return (uint)(ver - minVer);
			return 0x80000000 + (uint)minVer - (uint)ver - 1;
		}

		FrameworkPaths? TryGetDotNetPathsCore(int major, int bitness) {
			FrameworkPaths? fpMajor = null;
			for (int i = netPaths.Length - 1; i >= 0; i--) {
				var info = netPaths[i];
				if (info.Bitness == bitness && info.Version.Major == major) {
					if (info.HasDotNetAppPath)
						return info;
					if (fpMajor is null)
						fpMajor = info;
				}
			}
			return fpMajor;
		}

		FrameworkPaths? TryGetDotNetPathsCore(int bitness) {
			FrameworkPaths? best = null;
			for (int i = netPaths.Length - 1; i >= 0; i--) {
				var info = netPaths[i];
				if (info.Bitness == bitness) {
					if (info.HasDotNetAppPath)
						return info;
					if (best is null)
						best = info;
				}
			}
			return best;
		}

		const string DotNetExeName = "dotnet.exe";
		static IEnumerable<DotNetPathInfo> GetDotNetBaseDirs() {
			var hash = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var tmp in GetDotNetBaseDirCandidates()) {
				var path = tmp.Trim();
				if (!Directory.Exists(path))
					continue;
				try {
					path = Path.Combine(Path.GetDirectoryName(path)!, Path.GetFileName(path));
				}
				catch (ArgumentException) {
					continue;
				}
				catch (PathTooLongException) {
					continue;
				}
				if (!hash.Add(path))
					continue;
				string file;
				try {
					file = Path.Combine(path, DotNetExeName);
				}
				catch {
					continue;
				}
				if (!File.Exists(file))
					continue;
				int bitness;
				try {
					bitness = GetPeFileBitness(file);
				}
				catch {
					continue;
				}
				if (bitness == -1)
					continue;
				yield return new DotNetPathInfo(path, bitness);
			}
		}

		// NOTE: This same method exists in DotNetHelpers (CorDebug project). Update both methods if this one gets updated.
		static IEnumerable<string> GetDotNetBaseDirCandidates() {
			// Microsoft tools don't check the PATH env var, only the default locations (eg. ProgramFiles)
			var envVars = new string[] {
				"PATH",
				"DOTNET_ROOT(x86)",
				"DOTNET_ROOT",
			};
			foreach (var envVar in envVars) {
				var pathEnvVar = Environment.GetEnvironmentVariable(envVar) ?? string.Empty;
				foreach (var path in pathEnvVar.Split(new[] { Path.PathSeparator }, StringSplitOptions.RemoveEmptyEntries))
					yield return path;
			}

			var regPathFormat = IntPtr.Size == 4 ?
				@"SOFTWARE\dotnet\Setup\InstalledVersions\{0}" :
				@"SOFTWARE\WOW6432Node\dotnet\Setup\InstalledVersions\{0}";
			var archs = new[] { "x86", "x64" };
			foreach (var arch in archs) {
				var regPath = string.Format(regPathFormat, arch);
				if (TryGetInstallLocationFromRegistry(regPath, out var installLocation))
					yield return installLocation;
			}

			// Check default locations
			var progDirX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
			var progDir = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
			if (!string.IsNullOrEmpty(progDirX86) && StringComparer.OrdinalIgnoreCase.Equals(progDirX86, progDir) && Path.GetDirectoryName(progDir) is string baseDir)
				progDir = Path.Combine(baseDir, "Program Files");
			const string dotnetDirName = "dotnet";
			if (!string.IsNullOrEmpty(progDir))
				yield return Path.Combine(progDir, dotnetDirName);
			if (!string.IsNullOrEmpty(progDirX86))
				yield return Path.Combine(progDirX86, dotnetDirName);
		}

		static bool TryGetInstallLocationFromRegistry(string regPath, [NotNullWhen(true)] out string? installLocation) {
			using (var key = Registry.LocalMachine.OpenSubKey(regPath)) {
				installLocation = key?.GetValue("InstallLocation") as string;
				return installLocation is not null;
			}
		}

		static IEnumerable<FrameworkPath> GetDotNetPaths(string basePath, int bitness) {
			if (!Directory.Exists(basePath))
				yield break;
			var sharedDir = Path.Combine(basePath, "shared");
			if (!Directory.Exists(sharedDir))
				yield break;
			// Known dirs: Microsoft.NETCore.App, Microsoft.WindowsDesktop.App, Microsoft.AspNetCore.All, Microsoft.AspNetCore.App
			foreach (var versionsDir in GetDirectories(sharedDir)) {
				foreach (var dir in GetDirectories(versionsDir)) {
					var name = Path.GetFileName(dir);
					var m = Regex.Match(name, @"^(\d+)\.(\d+)\.(\d+)$");
					if (m.Groups.Count == 4) {
						var g = m.Groups;
						yield return new FrameworkPath(dir, bitness, ToFrameworkVersion(g[1].Value, g[2].Value, g[3].Value, string.Empty));
					}
					else {
						m = Regex.Match(name, @"^(\d+)\.(\d+)\.(\d+)-(.*)$");
						if (m.Groups.Count == 5) {
							var g = m.Groups;
							yield return new FrameworkPath(dir, bitness, ToFrameworkVersion(g[1].Value, g[2].Value, g[3].Value, g[4].Value));
						}
					}
				}
			}
		}

		static int ParseInt32(string s) => int.TryParse(s, out var res) ? res : 0;
		static FrameworkVersion ToFrameworkVersion(string a, string b, string c, string d) =>
			new FrameworkVersion(ParseInt32(a), ParseInt32(b), ParseInt32(c), d);

		static string[] GetDirectories(string dir) {
			try {
				return Directory.GetDirectories(dir);
			}
			catch {
			}
			return Array.Empty<string>();
		}

		static int GetPeFileBitness(string file) {
			using (var f = File.OpenRead(file)) {
				var r = new BinaryReader(f);
				if (r.ReadUInt16() != 0x5A4D)
					return -1;
				f.Position = 0x3C;
				f.Position = r.ReadUInt32();
				if (r.ReadUInt32() != 0x4550)
					return -1;
				f.Position += 0x14;
				ushort magic = r.ReadUInt16();
				if (magic == 0x10B)
					return 32;
				if (magic == 0x20B)
					return 64;
				return -1;
			}
		}

		public Version? TryGetDotNetVersion(string filename) {
			foreach (var info in netPaths) {
				foreach (var path in info.Paths) {
					if (FileUtils.IsFileInDir(path, filename))
						return info.SystemVersion;
				}
			}

			return null;
		}
	}
}
