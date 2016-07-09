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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using dnlib.DotNet;
using Microsoft.Win32;

namespace dnSpy.Contracts.Utilities {
	/// <summary>
	/// GAC file info
	/// </summary>
	public struct GacFileInfo {
		/// <summary>
		/// Assembly
		/// </summary>
		public IAssembly Assembly { get; }

		/// <summary>
		/// Path to file
		/// </summary>
		public string Path { get; }

		internal GacFileInfo(IAssembly asm, string path) {
			this.Assembly = asm;
			this.Path = path;
		}
	}

	/// <summary>
	/// GAC
	/// </summary>
	public static class GacInfo {
		/// <summary>
		/// All GAC paths
		/// </summary>
		public static string[] GacPaths { get; }

		/// <summary>
		/// Other GAC paths
		/// </summary>
		public static string[] OtherGacPaths { get; }

		/// <summary>
		/// WinMD paths
		/// </summary>
		public static string[] WinmdPaths { get; }

		sealed class GacDirInfo {
			public readonly int Version;
			public readonly string Path;
			public readonly string Prefix;
			public readonly IList<string> SubDirs;

			public GacDirInfo(int version, string prefix, string path, IList<string> subDirs) {
				this.Version = version;
				this.Prefix = prefix;
				this.Path = path;
				this.SubDirs = subDirs;
			}
		}
		static readonly GacDirInfo[] gacDirInfos;
		static readonly string[] extraMonoPaths;
		static readonly string[] monoVerDirs = new string[] {
			// The "-api" dirs are reference assembly dirs.
			"4.5", @"4.5\Facades", "4.5-api", @"4.5-api\Facades", "4.0", "4.0-api",
			"3.5", "3.5-api", "3.0", "3.0-api", "2.0", "2.0-api",
			"1.1", "1.0",
		};

		static GacInfo() {
			var gacDirInfosList = new List<GacDirInfo>();
			var newOtherGacPaths = new List<string>();
			var newWinmdPaths = new List<string>();

			if (Type.GetType("Mono.Runtime") != null) {
				var dirs = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
				var extraMonoPathsList = new List<string>();
				foreach (var prefix in FindMonoPrefixes()) {
					var dir = Path.Combine(Path.Combine(Path.Combine(prefix, "lib"), "mono"), "gac");
					if (dirs.ContainsKey(dir))
						continue;
					dirs[dir] = true;

					if (Directory.Exists(dir)) {
						gacDirInfosList.Add(new GacDirInfo(4, "", Path.GetDirectoryName(dir), new string[] {
							Path.GetFileName(dir)
						}));
					}

					dir = Path.GetDirectoryName(dir);
					foreach (var verDir in monoVerDirs) {
						var dir2 = dir;
						foreach (var d in verDir.Split(new char[] { '\\' }))
							dir2 = Path.Combine(dir2, d);
						if (Directory.Exists(dir2))
							extraMonoPathsList.Add(dir2);
					}
				}

				var paths = Environment.GetEnvironmentVariable("MONO_PATH");
				if (paths != null) {
					foreach (var path in paths.Split(Path.PathSeparator)) {
						if (path != string.Empty && Directory.Exists(path))
							extraMonoPathsList.Add(path);
					}
				}
				extraMonoPaths = extraMonoPathsList.ToArray();
				newOtherGacPaths.AddRange(extraMonoPaths);
			}
			else {
				var windir = Environment.GetEnvironmentVariable("WINDIR");
				if (!string.IsNullOrEmpty(windir)) {
					string path;

					// .NET 1.x and 2.x
					path = Path.Combine(windir, "assembly");
					if (Directory.Exists(path)) {
						gacDirInfosList.Add(new GacDirInfo(2, "", path, new string[] {
							"GAC_32", "GAC_64", "GAC_MSIL", "GAC"
						}));
					}

					// .NET 4.x
					path = Path.Combine(Path.Combine(windir, "Microsoft.NET"), "assembly");
					if (Directory.Exists(path)) {
						gacDirInfosList.Add(new GacDirInfo(4, "v4.0_", path, new string[] {
							"GAC_32", "GAC_64", "GAC_MSIL"
						}));
					}

					AddIfExists(newOtherGacPaths, windir, @"Microsoft.NET\Framework\v1.1.4322");
					AddIfExists(newOtherGacPaths, windir, @"Microsoft.NET\Framework\v1.0.3705");
				}

				foreach (var path in GetDotNetInstallDirectories())
					AddIfExists(newOtherGacPaths, path, string.Empty);

				var dirPF = Environment.GetEnvironmentVariable("ProgramFiles");
				AddWinMDPaths(newWinmdPaths, dirPF);
				var dirPFx86 = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
				if (!StringComparer.OrdinalIgnoreCase.Equals(dirPF, dirPFx86))
					AddWinMDPaths(newWinmdPaths, dirPFx86);
				AddIfExists(newWinmdPaths, Environment.SystemDirectory, "WinMetadata");
			}

			OtherGacPaths = newOtherGacPaths.ToArray();
			WinmdPaths = newWinmdPaths.ToArray();

			gacDirInfos = gacDirInfosList.ToArray();
			GacPaths = gacDirInfos.Select(a => a.Path).ToArray();
		}

		static string GetCurrentMonoPrefix() {
			var path = typeof(object).Module.FullyQualifiedName;
			for (int i = 0; i < 4; i++)
				path = Path.GetDirectoryName(path);
			return path;
		}

		static IEnumerable<string> FindMonoPrefixes() {
			yield return GetCurrentMonoPrefix();

			var prefixes = Environment.GetEnvironmentVariable("MONO_GAC_PREFIX");
			if (!string.IsNullOrEmpty(prefixes)) {
				foreach (var prefix in prefixes.Split(Path.PathSeparator)) {
					if (prefix != string.Empty)
						yield return prefix;
				}
			}
		}

		static IEnumerable<string> GetDotNetInstallDirectories() {
			var hash = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			try {
				using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\.NETFramework")) {
					var path = key == null ? null : key.GetValue("InstallRoot") as string;
					if (Directory.Exists(path))
						hash.Add(path);
				}
			}
			catch {
			}
			try {
				using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\.NETFramework")) {
					var path = key == null ? null : key.GetValue("InstallRoot") as string;
					if (Directory.Exists(path))
						hash.Add(path);
				}
			}
			catch {
			}
			var dirs = hash.ToArray();
			hash.Clear();
			hash.Add(Path.GetDirectoryName(typeof(int).Assembly.Location));
			foreach (var tmp in dirs) {
				// Remove last backslash
				var dir = Path.Combine(Path.GetDirectoryName(tmp), Path.GetFileName(tmp));
				hash.Add(dir);
				var name = Path.GetFileName(dir);
				if (name.Equals("Framework", StringComparison.OrdinalIgnoreCase) || name.Equals("Framework64", StringComparison.OrdinalIgnoreCase)) {
					var d = Path.GetDirectoryName(dir);
					hash.Add(Path.Combine(d, "Framework"));
					hash.Add(Path.Combine(d, "Framework64"));
				}
			}
			return hash;
		}

		static void AddWinMDPaths(IList<string> paths, string path) {
			if (string.IsNullOrEmpty(path))
				return;

			// Add latest versions first since all the Windows.winmd files have the same assembly name
			AddIfExists(paths, path, @"Windows Kits\10\UnionMetadata");
			AddIfExists(paths, path, @"Windows Kits\8.1\References\CommonConfiguration\Neutral");
			AddIfExists(paths, path, @"Windows Kits\8.0\References\CommonConfiguration\Neutral");
		}

		static void AddIfExists(IList<string> paths, string basePath, string extraPath) {
			var path = Path.Combine(basePath, extraPath);
			if (Directory.Exists(path))
				paths.Add(path);
		}

		/// <summary>
		/// Checks whether <paramref name="filename"/> is in the GAC
		/// </summary>
		/// <param name="filename">Filename</param>
		/// <returns></returns>
		public static bool IsGacPath(string filename) {
			if (!File.Exists(filename))
				return false;
			foreach (var p in GacPaths) {
				if (IsSubPath(p, filename))
					return true;
			}
			foreach (var p in OtherGacPaths) {
				if (IsSubPath(p, filename))
					return true;
			}
			return false;
		}

		static bool IsSubPath(string path, string filename) {
			filename = Path.GetFullPath(Path.GetDirectoryName(filename));
			var root = Path.GetPathRoot(filename);
			while (!StringComparer.OrdinalIgnoreCase.Equals(filename, root)) {
				if (StringComparer.OrdinalIgnoreCase.Equals(path, filename))
					return true;
				filename = Path.GetDirectoryName(filename);
			}
			return false;
		}

		/// <summary>
		/// Finds an assembly in the GAC
		/// </summary>
		/// <param name="asm">Assembly</param>
		/// <returns></returns>
		public static string FindInGac(IAssembly asm) {
			if (asm == null)
				return null;
			var pkt = PublicKeyBase.ToPublicKeyToken(asm.PublicKeyOrToken);
			if (PublicKeyBase.IsNullOrEmpty2(pkt))
				return null;

			foreach (var info in gacDirInfos) {
				foreach (var name in GetAssemblies(info, pkt, asm))
					return name;
			}

			return null;
		}

		static IEnumerable<string> GetAssemblies(GacDirInfo gacInfo, PublicKeyToken pkt, IAssembly assembly) {
			string pktString = pkt.ToString();
			string verString = assembly.Version.ToString();
			var cultureString = UTF8String.ToSystemStringOrEmpty(assembly.Culture);
			if (cultureString.Equals("neutral", StringComparison.OrdinalIgnoreCase))
				cultureString = string.Empty;
			var asmSimpleName = UTF8String.ToSystemStringOrEmpty(assembly.Name);
			foreach (var subDir in gacInfo.SubDirs) {
				var baseDir = Path.Combine(gacInfo.Path, subDir);
				baseDir = Path.Combine(baseDir, asmSimpleName);
				baseDir = Path.Combine(baseDir, $"{gacInfo.Prefix}{verString}_{cultureString}_{pktString}");
				var pathName = Path.Combine(baseDir, asmSimpleName + ".dll");
				if (File.Exists(pathName))
					yield return pathName;
			}
		}

		/// <summary>
		/// Gets all assemblies in the GAC
		/// </summary>
		/// <param name="majorVersion">CLR major version, eg. 2 or 4</param>
		/// <returns></returns>
		public static IEnumerable<GacFileInfo> GetAssemblies(int majorVersion) {
			foreach (var info in gacDirInfos) {
				if (info.Version == majorVersion)
					return GetAssemblies(info);
			}
			Debug.Fail("Invalid version");
			return Array.Empty<GacFileInfo>();
		}

		static IEnumerable<GacFileInfo> GetAssemblies(GacDirInfo gacInfo) {
			foreach (var subDir in gacInfo.SubDirs) {
				var baseDir = Path.Combine(gacInfo.Path, subDir);
				foreach (var dir in GetDirectories(baseDir)) {
					foreach (var dir2 in GetDirectories(dir)) {
						Version version;
						string culture;
						PublicKeyToken pkt;
						if (gacInfo.Version == 2) {
							var m = gac2Regex.Match(Path.GetFileName(dir2));
							if (!m.Success || m.Groups.Count != 4)
								continue;
							if (!Version.TryParse(m.Groups[1].Value, out version))
								continue;
							culture = m.Groups[2].Value;
							pkt = new PublicKeyToken(m.Groups[3].Value);
							if (PublicKeyBase.IsNullOrEmpty2(pkt))
								continue;
						}
						else if (gacInfo.Version == 4) {
							var m = gac4Regex.Match(Path.GetFileName(dir2));
							if (!m.Success || m.Groups.Count != 4)
								continue;
							if (!Version.TryParse(m.Groups[1].Value, out version))
								continue;
							culture = m.Groups[2].Value;
							pkt = new PublicKeyToken(m.Groups[3].Value);
							if (PublicKeyBase.IsNullOrEmpty2(pkt))
								continue;
						}
						else
							throw new InvalidOperationException();
						var asmName = Path.GetFileName(dir);
						var file = Path.Combine(dir2, asmName) + ".dll";
						if (!File.Exists(file)) {
							file = Path.Combine(dir2, asmName) + ".exe";
							if (!File.Exists(file))
								continue;
						}
						var asmInfo = new AssemblyNameInfo {
							Name = asmName,
							Version = version,
							Culture = culture,
							PublicKeyOrToken = pkt,
						};
						yield return new GacFileInfo(asmInfo, file);
					}
				}
			}
		}
		static readonly Regex gac2Regex = new Regex("^([^_]+)_([^_]*)_([a-fA-F0-9]{16})$", RegexOptions.Compiled);
		static readonly Regex gac4Regex = new Regex("^v[^_]+_([^_]+)_([^_]*)_([a-fA-F0-9]{16})$", RegexOptions.Compiled);

		static string[] GetDirectories(string dir) {
			try {
				return Directory.GetDirectories(dir);
			}
			catch {
			}
			return Array.Empty<string>();
		}
	}
}
