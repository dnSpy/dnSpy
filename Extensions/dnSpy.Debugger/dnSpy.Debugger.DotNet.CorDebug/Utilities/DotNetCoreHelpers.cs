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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using dnlib.DotNet;
using dnlib.PE;
using dnSpy.Debugger.Shared;
using Microsoft.Win32;

namespace dnSpy.Debugger.DotNet.CorDebug.Utilities {
	static class DotNetCoreHelpers {
		public static readonly string DotNetExeName = FileUtilities.GetNativeExeFilename("dotnet");

		public static string? GetPathToDotNetExeHost(int bitness) {
			if (bitness != 32 && bitness != 64)
				throw new ArgumentOutOfRangeException(nameof(bitness));
			var pathEnvVar = Environment.GetEnvironmentVariable("PATH");
			if (pathEnvVar is null)
				return null;
			foreach (var tmp in GetDotNetCoreBaseDirCandidates()) {
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
				try {
					var file = Path.Combine(path, DotNetExeName);
					if (!File.Exists(file))
						continue;
					if (FileUtilities.GetNativeFileBitness(file) == bitness)
						return file;
				}
				catch {
				}
			}
			return null;
		}

		// NOTE: This same method exists in DotNetCorePathProvider (dnSpy project). Update both methods if this one gets updated.
		static IEnumerable<string> GetDotNetCoreBaseDirCandidates() {
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
				return !(installLocation is null);
			}
		}

		public static string GetDebugShimFilename(int bitness) {
#if NETFRAMEWORK
			var basePath = Contracts.App.AppDirectories.BinDirectory;
			basePath = Path.Combine(basePath, "debug", "core");
			var filename = FileUtilities.GetNativeDllFilename("dbgshim");
			switch (bitness) {
			case 32:	return Path.Combine(basePath, "x86", filename);
			case 64:	return Path.Combine(basePath, "x64", filename);
			default:	throw new ArgumentOutOfRangeException(nameof(bitness));
			}
#elif NETCOREAPP
			var filename = FileUtilities.GetNativeDllFilename("dbgshim");
			return Path.Combine(Path.GetDirectoryName(typeof(void).Assembly.Location)!, filename);
#else
#error Unknown target framework
#endif
		}

		public static bool IsDotNetCoreExecutable(string filename) {
			if (!File.Exists(filename))
				return false;
			if (!PortableExecutableFileHelpers.IsExecutable(filename))
				return false;
			try {
				using (var peImage = new PEImage(filename)) {
					if ((peImage.ImageNTHeaders.FileHeader.Characteristics & Characteristics.Dll) != 0)
						return false;
					var dd = peImage.ImageNTHeaders.OptionalHeader.DataDirectories[14];
					if (dd.VirtualAddress == 0 || dd.Size < 0x48)
						return false;

					using (var mod = ModuleDefMD.Load(peImage, new ModuleCreationOptions())) {
						var asm = mod.Assembly;
						if (asm is null)
							return false;

						var ca = asm.CustomAttributes.Find("System.Runtime.Versioning.TargetFrameworkAttribute");
						if (ca is null)
							return false;
						if (ca.ConstructorArguments.Count != 1)
							return false;
						string s = ca.ConstructorArguments[0].Value as UTF8String;
						if (s is null)
							return false;

						// See corclr/src/mscorlib/src/System/Runtime/Versioning/BinaryCompatibility.cs
						var values = s.Split(new char[] { ',' });
						if (values.Length >= 2 && values.Length <= 3) {
							var framework = values[0].Trim();
							if (framework == ".NETCoreApp")
								return true;
						}

						return false;
					}
				}
			}
			catch {
			}
			return false;
		}
	}
}
