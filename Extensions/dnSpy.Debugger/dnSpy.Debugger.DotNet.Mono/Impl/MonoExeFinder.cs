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
using System.IO;
using System.Runtime.InteropServices;
using dnSpy.Contracts.Debugger.DotNet.Mono;
using dnSpy.Debugger.Shared;

namespace dnSpy.Debugger.DotNet.Mono.Impl {
	static class MonoExeFinder {
		public static readonly string MONO_EXE = FileUtilities.GetNativeExeFilename("mono");
		const string MONO_PROGRAM_FILES_DIR = "Mono";
		const string MONO_PROGRAM_FILES_DIR_BIN = "bin";

		public static string? Find(MonoExeOptions options) {
			if ((options & (MonoExeOptions.Prefer32 | MonoExeOptions.Prefer64)) == 0) {
				if (IntPtr.Size == 4)
					options |= MonoExeOptions.Prefer32;
				else
					options |= MonoExeOptions.Prefer64;
			}

			Find(options, out var mono32, out var mono64);

			if ((options & MonoExeOptions.Prefer32) != 0 && (options & MonoExeOptions.Debug32) != 0 && mono32 is not null)
				return mono32;
			if ((options & MonoExeOptions.Prefer64) != 0 && (options & MonoExeOptions.Debug64) != 0 && mono64 is not null)
				return mono64;

			if ((options & MonoExeOptions.Debug32) != 0 && mono32 is not null)
				return mono32;
			if ((options & MonoExeOptions.Debug64) != 0 && mono64 is not null)
				return mono64;

			return null;
		}

		static void Find(MonoExeOptions options, out string? mono32, out string? mono64) {
			mono32 = null;
			mono64 = null;
			foreach (var dir in GetDirectories()) {
				bool has32 = mono32 is not null || (options & MonoExeOptions.Debug32) == 0;
				bool has64 = mono64 is not null || (options & MonoExeOptions.Debug64) == 0;
				if (has32 && has64)
					break;
				if (!Directory.Exists(dir))
					continue;
				try {
					var file = Path.Combine(dir, MONO_EXE);
					if (!File.Exists(file))
						continue;
					switch (FileUtilities.GetNativeFileBitness(file)) {
					case 32:
						if (mono32 is null)
							mono32 = file;
						break;

					case 64:
						if (mono64 is null)
							mono64 = file;
						break;
					}
				}
				catch {
				}
			}
		}

		static IEnumerable<string> GetDirectories() {
			var pathEnvVar = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
			foreach (var tmp in pathEnvVar.Split(new[] { Path.PathSeparator }, StringSplitOptions.RemoveEmptyEntries)) {
				var path = tmp.Trim();
				if (!Directory.Exists(path))
					continue;
				yield return path;
			}
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
				var progFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
				if (Directory.Exists(progFiles))
					yield return Path.Combine(progFiles, MONO_PROGRAM_FILES_DIR, MONO_PROGRAM_FILES_DIR_BIN);
				var progFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
				if (!StringComparer.OrdinalIgnoreCase.Equals(progFilesX86, progFiles) && Directory.Exists(progFilesX86))
					yield return Path.Combine(progFilesX86, MONO_PROGRAM_FILES_DIR, MONO_PROGRAM_FILES_DIR_BIN);
			}
		}
	}
}
