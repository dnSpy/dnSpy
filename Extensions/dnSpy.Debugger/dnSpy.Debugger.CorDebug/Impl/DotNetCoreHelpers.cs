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
using System.IO;

namespace dnSpy.Debugger.CorDebug.Impl {
	static class DotNetCoreHelpers {
		public const string DotNetExeName = "dotnet.exe";

		public static string GetPathToDotNetExeHost(int bitSize) {
			if (bitSize != 32 && bitSize != 64)
				throw new ArgumentOutOfRangeException(nameof(bitSize));
			var pathEnvVar = Environment.GetEnvironmentVariable("PATH");
			if (pathEnvVar == null)
				return null;
			foreach (var path in pathEnvVar.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)) {
				if (!Directory.Exists(path))
					continue;
				try {
					var file = Path.Combine(path, DotNetExeName);
					if (!File.Exists(file))
						continue;
					if (GetPeFileBitSize(file) == bitSize)
						return file;
				}
				catch {
				}
			}
			return null;
		}

		static int GetPeFileBitSize(string file) {
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

		public static string GetDebugShimFilename(int bitSize) {
			var basePath = Path.GetDirectoryName(typeof(DotNetCoreHelpers).Assembly.Location);
			basePath = Path.Combine(basePath, "debug");
			const string filename = "dbgshim.dll";
			switch (bitSize) {
			case 32:	return Path.Combine(basePath, "x86", filename);
			case 64:	return Path.Combine(basePath, "x64", filename);
			default:	throw new ArgumentOutOfRangeException(nameof(bitSize));
			}
		}
	}
}
