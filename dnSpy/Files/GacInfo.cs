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
using System.IO;
using ICSharpCode.ILSpy;

namespace dnSpy.Files {
	public static class GacInfo {
		public static readonly string[] GacPaths;
		public static readonly string[] OtherGacPaths;
		public static readonly string[] WinmdPaths;

		static GacInfo() {
			GacPaths = new string[] {
				Fusion.GetGacPath(false),
				Fusion.GetGacPath(true)
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
	}
}
