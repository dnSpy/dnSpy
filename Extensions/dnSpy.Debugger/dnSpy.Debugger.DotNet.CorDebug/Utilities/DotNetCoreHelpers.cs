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
using System.IO;
using System.Reflection;
using dnlib.DotNet;
using dnlib.PE;
using dnSpy.Debugger.Shared;

namespace dnSpy.Debugger.DotNet.CorDebug.Utilities {
	static class DotNetCoreHelpers {
		public static readonly string DotNetExeName = FileUtilities.GetNativeExeFilename("dotnet");

		public static string GetPathToDotNetExeHost(int bitness) {
			if (bitness != 32 && bitness != 64)
				throw new ArgumentOutOfRangeException(nameof(bitness));
			var pathEnvVar = Environment.GetEnvironmentVariable("PATH");
			if (pathEnvVar == null)
				return null;
			foreach (var tmp in pathEnvVar.Split(new[] { Path.PathSeparator }, StringSplitOptions.RemoveEmptyEntries)) {
				var path = tmp.Trim();
				if (!Directory.Exists(path))
					continue;
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

		public static string GetDebugShimFilename(int bitness) {
			var basePath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
			basePath = Path.Combine(basePath, "debug", "core");
			var filename = FileUtilities.GetNativeDllFilename("dbgshim");
			switch (bitness) {
			case 32:	return Path.Combine(basePath, "x86", filename);
			case 64:	return Path.Combine(basePath, "x64", filename);
			default:	throw new ArgumentOutOfRangeException(nameof(bitness));
			}
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
						if (asm == null)
							return false;

						var ca = asm.CustomAttributes.Find("System.Runtime.Versioning.TargetFrameworkAttribute");
						if (ca == null)
							return false;
						if (ca.ConstructorArguments.Count != 1)
							return false;
						string s = ca.ConstructorArguments[0].Value as UTF8String;
						if (s == null)
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
