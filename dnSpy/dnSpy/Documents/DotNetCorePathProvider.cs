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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using dnlib.DotNet;

namespace dnSpy.Documents {
	sealed class DotNetCorePathProvider {
		readonly FrameworkPaths[] netcorePaths;

		public DotNetCorePathProvider() {
			var list = new List<FrameworkPath>();
			foreach (var info in GetDotNetCoreBaseDirs())
				list.AddRange(GetDotNetCorePaths(info.path, info.bitness));

			var paths = from p in list
						group p by new { Path = Path.GetDirectoryName(p.Path).ToUpperInvariant(), p.Bitness, p.Version } into g
						select new FrameworkPaths(g.ToArray());
			var array = paths.ToArray();
			Array.Sort(array);
			netcorePaths = array;
		}

		public string[] TryGetDotNetCorePaths(Version version, int bitness) {
			Debug.Assert(bitness == 32 || bitness == 64);
			int bitness2 = bitness ^ 0x60;
			FrameworkPaths info;

			info = TryGetDotNetCorePathsCore(version.Major, version.Minor, bitness) ??
				TryGetDotNetCorePathsCore(version.Major, version.Minor, bitness2);
			if (info != null)
				return info.Paths;

			info = TryGetDotNetCorePathsCore(version.Major, bitness) ??
				TryGetDotNetCorePathsCore(version.Major, bitness2);
			if (info != null)
				return info.Paths;

			info = TryGetDotNetCorePathsCore(bitness) ??
				TryGetDotNetCorePathsCore(bitness2);
			if (info != null)
				return info.Paths;

			return null;
		}

		FrameworkPaths TryGetDotNetCorePathsCore(int major, int minor, int bitness) {
			FrameworkPaths fpMajor = null;
			FrameworkPaths fpMajorMinor = null;
			for (int i = netcorePaths.Length - 1; i >= 0; i--) {
				var info = netcorePaths[i];
				if (info.Bitness == bitness && info.Version.Major == major) {
					if (fpMajor == null)
						fpMajor = info;
					if (info.Version.Minor == minor) {
						if (info.HasDotNetCoreAppPath)
							return info;
						if (fpMajorMinor == null)
							fpMajorMinor = info;
					}
				}
			}
			return fpMajorMinor ?? fpMajor;
		}

		FrameworkPaths TryGetDotNetCorePathsCore(int major, int bitness) {
			FrameworkPaths fpMajor = null;
			for (int i = netcorePaths.Length - 1; i >= 0; i--) {
				var info = netcorePaths[i];
				if (info.Bitness == bitness && info.Version.Major == major) {
					if (info.HasDotNetCoreAppPath)
						return info;
					if (fpMajor == null)
						fpMajor = info;
				}
			}
			return fpMajor;
		}

		FrameworkPaths TryGetDotNetCorePathsCore(int bitness) {
			FrameworkPaths best = null;
			for (int i = netcorePaths.Length - 1; i >= 0; i--) {
				var info = netcorePaths[i];
				if (info.Bitness == bitness) {
					if (info.HasDotNetCoreAppPath)
						return info;
					if (best == null)
						best = info;
				}
			}
			return best;
		}

		const string DotNetExeName = "dotnet.exe";
		static IEnumerable<(string path, int bitness)> GetDotNetCoreBaseDirs() {
			var pathEnvVar = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
			foreach (var tmp in pathEnvVar.Split(new[] { Path.PathSeparator }, StringSplitOptions.RemoveEmptyEntries)) {
				var path = tmp.Trim();
				if (!Directory.Exists(path))
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
				yield return (path, bitness);
			}
		}

		static IEnumerable<FrameworkPath> GetDotNetCorePaths(string basePath, int bitness) {
			if (!Directory.Exists(basePath))
				yield break;
			var sharedDir = Path.Combine(basePath, "shared");
			if (!Directory.Exists(sharedDir))
				yield break;
			// Known dirs: Microsoft.NETCore.App, Microsoft.AspNetCore.All, Microsoft.AspNetCore.App
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

		public Version TryGetDotNetCoreVersion(ModuleDef module) {
			if (TargetFrameworkAttributeInfo.TryCreateTargetFrameworkInfo(module, out var info) && info.IsDotNetCore)
				return info.Version;
			return null;
		}
	}
}
