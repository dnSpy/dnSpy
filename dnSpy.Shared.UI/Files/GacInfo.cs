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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using dnlib.DotNet;

namespace dnSpy.Shared.UI.Files {
	public struct GacFileInfo {
		public readonly IAssembly Assembly;
		public readonly string Path;

		public GacFileInfo(IAssembly asm, string path) {
			this.Assembly = asm;
			this.Path = path;
		}
	}

	public static class GacInfo {
		public static string[] GacPaths { get; private set; }
		public static string[] OtherGacPaths { get; private set; }
		public static string[] WinmdPaths { get; private set; }

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

		[Flags]
		enum ASM_CACHE_FLAGS {
			ASM_CACHE_ZAP		= 0x01,
			ASM_CACHE_GAC		= 0x02,
			ASM_CACHE_DOWNLOAD	= 0x04,
			ASM_CACHE_ROOT		= 0x08,
			ASM_CACHE_ROOT_EX	= 0x80,
		}

		[DllImport("fusion"), PreserveSig]
		static extern int GetCachePath(ASM_CACHE_FLAGS dwCacheFlags, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwzCachePath, ref uint pcchPath);

		static string GetCachePath(ASM_CACHE_FLAGS flags) {
			var sb = new StringBuilder(0x400);
			uint len = (uint)sb.MaxCapacity;
			int hr = GetCachePath(flags, sb, ref len);
			Debug.Assert(hr == 0);
			if (hr != 0)
				return string.Empty;
			return sb.ToString();
		}

		static GacInfo() {
			GacPaths = new string[] {
				GetCachePath(ASM_CACHE_FLAGS.ASM_CACHE_ROOT),
				GetCachePath(ASM_CACHE_FLAGS.ASM_CACHE_ROOT_EX)
			};

			gacDirInfos = new GacDirInfo[] {
				new GacDirInfo(2, "", GacPaths[0], new string[] { "GAC_32", "GAC_64", "GAC_MSIL", "GAC" }),
				new GacDirInfo(4, "v4.0_", GacPaths[1], new string[] { "GAC_32", "GAC_64", "GAC_MSIL" }),
			};

			var newOtherGacPaths = new List<string>();
			var newWinmdPaths = new List<string>();

			var windir = Environment.GetEnvironmentVariable("WINDIR");
			if (!string.IsNullOrEmpty(windir)) {
				AddIfExists(newOtherGacPaths, windir, @"Microsoft.NET\Framework\v1.1.4322");
				AddIfExists(newOtherGacPaths, windir, @"Microsoft.NET\Framework\v1.0.3705");
			}

			var dirPF = Environment.GetEnvironmentVariable("ProgramFiles");
			AddWinMDPaths(newWinmdPaths, dirPF);
			var dirPFx86 = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
			if (!StringComparer.OrdinalIgnoreCase.Equals(dirPF, dirPFx86))
				AddWinMDPaths(newWinmdPaths, dirPFx86);
			AddIfExists(newWinmdPaths, Environment.SystemDirectory, "WinMetadata");

			OtherGacPaths = newOtherGacPaths.ToArray();
			WinmdPaths = newWinmdPaths.ToArray();
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

		public static bool IsGacPath(string filename) {
			if (!File.Exists(filename))
				return false;
			foreach (var p in GacInfo.GacPaths) {
				if (IsSubPath(p, filename))
					return true;
			}
			foreach (var p in GacInfo.OtherGacPaths) {
				if (IsSubPath(p, filename))
					return true;
			}
			return false;
		}

		static bool IsSubPath(string path, string filename) {
			filename = Path.GetFullPath(Path.GetDirectoryName(filename));
			var root = Path.GetPathRoot(filename);
			while (filename != root) {
				if (path == filename)
					return true;
				filename = Path.GetDirectoryName(filename);
			}
			return false;
		}

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
			var asmSimpleName = UTF8String.ToSystemStringOrEmpty(assembly.Name);
			foreach (var subDir in gacInfo.SubDirs) {
				var baseDir = Path.Combine(gacInfo.Path, subDir);
				baseDir = Path.Combine(baseDir, asmSimpleName);
				baseDir = Path.Combine(baseDir, string.Format("{0}{1}__{2}", gacInfo.Prefix, verString, pktString));
				var pathName = Path.Combine(baseDir, asmSimpleName + ".dll");
				if (File.Exists(pathName))
					yield return pathName;
			}
		}

		public static IEnumerable<GacFileInfo> GetAssemblies(int majorVersion) {
			if (majorVersion == 2)
				return GetAssemblies(gacDirInfos[0]);
			if (majorVersion == 4)
				return GetAssemblies(gacDirInfos[1]);
			Debug.Fail("Invalid version");
			return new GacFileInfo[0];
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
			return new string[0];
		}
	}
}
