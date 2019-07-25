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
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using dnlib.DotNet;

namespace dnSpy.Contracts.Utilities {
	/// <summary>
	/// GAC file info
	/// </summary>
	public readonly struct GacFileInfo {
		/// <summary>
		/// Assembly
		/// </summary>
		public IAssembly Assembly { get; }

		/// <summary>
		/// Path to file
		/// </summary>
		public string Path { get; }

		internal GacFileInfo(IAssembly asm, string path) {
			Assembly = asm;
			Path = path;
		}
	}

	/// <summary>
	/// GAC version
	/// </summary>
	public enum GacVersion {
		/// <summary>
		/// .NET Framework 1.0-3.5
		/// </summary>
		V2,

		/// <summary>
		/// .NET Framework 4.0+
		/// </summary>
		V4,
	}

	/// <summary>
	/// GAC path info
	/// </summary>
	public readonly struct GacPathInfo {
		/// <summary>
		/// Path of dir containing assemblies
		/// </summary>
		public readonly string Path;

		/// <summary>
		/// GAC version
		/// </summary>
		public readonly GacVersion Version;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="path">Path</param>
		/// <param name="version">Version</param>
		public GacPathInfo(string path, GacVersion version) {
			Path = path ?? throw new ArgumentNullException(nameof(path));
			Version = version;
		}
	}

	/// <summary>
	/// GAC
	/// </summary>
	public static class GacInfo {
		/// <summary>
		/// All GAC paths
		/// </summary>
		public static GacPathInfo[] GacPaths { get; }

		/// <summary>
		/// Other GAC paths
		/// </summary>
		public static GacPathInfo[] OtherGacPaths { get; }

		/// <summary>
		/// WinMD paths
		/// </summary>
		public static string[] WinmdPaths { get; }

		/// <summary>
		/// Checks if .NET 2.0-3.5 GAC exists
		/// </summary>
		public static bool HasGAC2 { get; }

		sealed class GacDirInfo {
			public readonly int Version;
			public readonly string Path;
			public readonly string Prefix;
			public readonly string[] SubDirs;

			public GacDirInfo(int version, string prefix, string path, string[] subDirs) {
				Version = version;
				Prefix = prefix;
				Path = path;
				SubDirs = subDirs;
			}
		}
		static readonly GacDirInfo[] gacDirInfos;
		static readonly string[] monoVerDirs = new string[] {
			// The "-api" dirs are reference assembly dirs.
			"4.5", @"4.5\Facades", "4.5-api", @"4.5-api\Facades", "4.0", "4.0-api",
			"3.5", "3.5-api", "3.0", "3.0-api", "2.0", "2.0-api",
			"1.1", "1.0",
		};

		static GacInfo() {
			var gacDirInfosList = new List<GacDirInfo>();
			var newOtherGacPaths = new List<GacPathInfo>();
			var newWinmdPaths = new List<string>();

			bool hasGAC2;
			if (!(Type.GetType("Mono.Runtime") is null)) {
				hasGAC2 = false;
				var dirs = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
				var extraMonoPathsList = new List<GacPathInfo>();
				foreach (var prefix in FindMonoPrefixes()) {
					var dir = Path.Combine(Path.Combine(Path.Combine(prefix, "lib"), "mono"), "gac");
					if (dirs.ContainsKey(dir))
						continue;
					dirs[dir] = true;

					if (Directory.Exists(dir)) {
						gacDirInfosList.Add(new GacDirInfo(4, "", Path.GetDirectoryName(dir)!, new string[] {
							Path.GetFileName(dir)
						}));
					}

					dir = Path.GetDirectoryName(dir)!;
					foreach (var verDir in monoVerDirs) {
						var dir2 = dir;
						foreach (var d in verDir.Split(new char[] { '\\' }))
							dir2 = Path.Combine(dir2, d);
						if (Directory.Exists(dir2))
							extraMonoPathsList.Add(new GacPathInfo(dir2, GacVersion.V4));
					}
				}

				var paths = Environment.GetEnvironmentVariable("MONO_PATH");
				if (!(paths is null)) {
					foreach (var tmp in paths.Split(Path.PathSeparator)) {
						var path = tmp.Trim();
						if (path != string.Empty && Directory.Exists(path))
							extraMonoPathsList.Add(new GacPathInfo(path, GacVersion.V4));
					}
				}
				newOtherGacPaths.AddRange(extraMonoPathsList);
			}
			else {
				hasGAC2 = false;
				var windir = Environment.GetEnvironmentVariable("WINDIR");
				if (!string.IsNullOrEmpty(windir)) {
					string path;

					// .NET Framework 1.x and 2.x
					path = Path.Combine(windir, "assembly");
					if (Directory.Exists(path)) {
						hasGAC2 = File.Exists(Path.Combine(path, @"GAC_32\mscorlib\2.0.0.0__b77a5c561934e089\mscorlib.dll")) ||
							File.Exists(Path.Combine(path, @"GAC_64\mscorlib\2.0.0.0__b77a5c561934e089\mscorlib.dll"));
						if (hasGAC2) {
							gacDirInfosList.Add(new GacDirInfo(2, "", path, gacPaths4));
						}
					}

					// .NET Framework 4.x
					path = Path.Combine(Path.Combine(windir, "Microsoft.NET"), "assembly");
					if (Directory.Exists(path)) {
						gacDirInfosList.Add(new GacDirInfo(4, "v4.0_", path, gacPaths2));
					}
				}

				AddIfExists(newWinmdPaths, Environment.SystemDirectory, "WinMetadata");
			}

			OtherGacPaths = newOtherGacPaths.ToArray();
			WinmdPaths = newWinmdPaths.ToArray();

			gacDirInfos = gacDirInfosList.ToArray();
			GacPaths = gacDirInfos.Select(a => new GacPathInfo(a.Path, a.Version == 2 ? GacVersion.V2 : GacVersion.V4)).ToArray();
			HasGAC2 = hasGAC2;
		}
		// Prefer GAC_32 if this is a 32-bit process, and GAC_64 if this is a 64-bit process
		static readonly string[] gacPaths2 = IntPtr.Size == 4 ?
			new string[] { "GAC_32", "GAC_64", "GAC_MSIL" } :
			new string[] { "GAC_64", "GAC_32", "GAC_MSIL" };
		static readonly string[] gacPaths4 = IntPtr.Size == 4 ?
			new string[] { "GAC_32", "GAC_64", "GAC_MSIL", "GAC" } :
			new string[] { "GAC_64", "GAC_32", "GAC_MSIL", "GAC" };

		static string? GetCurrentMonoPrefix() {
			string? path = typeof(object).Module.FullyQualifiedName;
			for (int i = 0; i < 4; i++)
				path = Path.GetDirectoryName(path);
			return path;
		}

		static IEnumerable<string> FindMonoPrefixes() {
			if (GetCurrentMonoPrefix() is string monoPrefix)
				yield return monoPrefix;

			var prefixes = Environment.GetEnvironmentVariable("MONO_GAC_PREFIX");
			if (!string.IsNullOrEmpty(prefixes)) {
				foreach (var tmp in prefixes.Split(Path.PathSeparator)) {
					var prefix = tmp.Trim();
					if (prefix != string.Empty)
						yield return prefix;
				}
			}
		}

		static void AddIfExists(List<string> paths, string basePath, string extraPath) {
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
			foreach (var info in GacPaths) {
				if (IsSubPath(info.Path, filename))
					return true;
			}
			foreach (var info in OtherGacPaths) {
				if (IsSubPath(info.Path, filename))
					return true;
			}
			return false;
		}

		static bool IsSubPath(string path, string filename) {
			filename = Path.GetFullPath(Path.GetDirectoryName(filename)!);
			var root = Path.GetPathRoot(filename);
			while (!StringComparer.OrdinalIgnoreCase.Equals(filename, root)) {
				if (StringComparer.OrdinalIgnoreCase.Equals(path, filename))
					return true;
				filename = Path.GetDirectoryName(filename)!;
			}
			return false;
		}

		/// <summary>
		/// Finds an assembly in the GAC
		/// </summary>
		/// <param name="asm">Assembly</param>
		/// <returns></returns>
		public static string? FindInGac(IAssembly asm) => FindInGac(asm, -1);

		/// <summary>
		/// Finds an assembly in the GAC
		/// </summary>
		/// <param name="asm">Assembly</param>
		/// <param name="version">2, 4, or -1</param>
		/// <returns></returns>
		public static string? FindInGac(IAssembly? asm, int version) {
			if (asm is null)
				return null;
			var pkt = PublicKeyBase.ToPublicKeyToken(asm.PublicKeyOrToken);
			if (PublicKeyBase.IsNullOrEmpty2(pkt))
				return null;

			foreach (var info in gacDirInfos) {
				if (version != -1 && version != info.Version)
					continue;
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
				string pathName;
				try {
					baseDir = Path.Combine(baseDir, asmSimpleName);
					baseDir = Path.Combine(baseDir, $"{gacInfo.Prefix}{verString}_{cultureString}_{pktString}");
					pathName = Path.Combine(baseDir, asmSimpleName + ".dll");
				}
				catch (ArgumentException) {
					// Invalid char(s) in asmSimpleName, cultureString
					yield break;
				}
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
						Version? version;
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
