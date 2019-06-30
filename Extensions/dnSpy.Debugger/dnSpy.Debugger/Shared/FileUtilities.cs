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
using System.Runtime.InteropServices;

namespace dnSpy.Debugger.Shared {
	static class FileUtilities {
		static readonly string DirectorySeparatorChar = Path.DirectorySeparatorChar.ToString();
		static readonly string AltDirectorySeparatorChar = Path.AltDirectorySeparatorChar.ToString();

		static bool CheckBaseName(string baseName) {
			if (baseName is null)
				return false;
			if (baseName.Contains(DirectorySeparatorChar) || baseName.Contains(AltDirectorySeparatorChar))
				return false;
			var extension = Path.GetExtension(baseName);
			if (StringComparer.OrdinalIgnoreCase.Equals(extension, ".exe") || StringComparer.OrdinalIgnoreCase.Equals(extension, ".dll"))
				return false;
			if (baseName.StartsWith("lib"))
				return false;

			return true;
		}

		/// <summary>
		/// Gets the name of an executable, eg. if it's Windows it returns <paramref name="baseName"/> + ".exe"
		/// </summary>
		/// <param name="baseName">Base name, eg. "dotnet" or "mono" without a ".exe" extension</param>
		/// <returns></returns>
		public static string GetNativeExeFilename(string baseName) {
			if (baseName is null)
				throw new ArgumentNullException(nameof(baseName));
			if (!CheckBaseName(baseName))
				throw new ArgumentException("Invalid base name");

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				return baseName + ".exe";
			return baseName;
		}

		/// <summary>
		/// Gets the name of a library, eg. if it's Windows it returns <paramref name="baseName"/> + ".dll"
		/// </summary>
		/// <param name="baseName">Base name, eg. "coreclr" or "dbgshim" without a ".dll" extension</param>
		/// <returns></returns>
		public static string GetNativeDllFilename(string baseName) {
			if (baseName is null)
				throw new ArgumentNullException(nameof(baseName));
			if (!CheckBaseName(baseName))
				throw new ArgumentException("Invalid base name");

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				return baseName + ".dll";
			if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
				return "lib" + baseName + ".dylib";
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
				return "lib" + baseName + ".so";
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("FREEBSD")))
				return "lib" + baseName + ".so";
			throw new InvalidOperationException("Unknown operating system");
		}

		/// <summary>
		/// Gets the file bitness (32 or 64) or -1 on failure
		/// </summary>
		/// <param name="file">Exe filename</param>
		/// <returns></returns>
		public static int GetNativeFileBitness(string file) {
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				return GetPeFileBitness(file);

			Debug.Fail("Unsupported OS");
			throw new InvalidOperationException("Unsupported OS");
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
	}
}
