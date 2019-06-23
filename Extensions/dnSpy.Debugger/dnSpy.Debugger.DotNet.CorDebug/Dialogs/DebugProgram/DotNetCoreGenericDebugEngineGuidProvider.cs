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
using dnSpy.Contracts.Debugger.StartDebugging;
using dnSpy.Debugger.DotNet.CorDebug.Utilities;

namespace dnSpy.Debugger.DotNet.CorDebug.Dialogs.DebugProgram {
	[ExportGenericDebugEngineGuidProvider(PredefinedGenericDebugEngineGuidProviderOrders.DotNetCore)]
	sealed class DotNetCoreGenericDebugEngineGuidProvider : GenericDebugEngineGuidProvider {
		public override Guid? GetEngineGuid(string filename) {
			if (!IsDotNetCoreAppHostFilename(filename))
				return null;
			return PredefinedGenericDebugEngineGuids.DotNetCore;
		}

		internal static bool IsDotNetCoreAppHostFilename(string filename) {
			if (!File.Exists(filename))
				return false;
			return IsDotNetCoreAppHost(filename) ||
				IsDotNetCoreBundle(filename);
		}

		static bool IsDotNetCoreAppHost(string filename) {
			// We detect the apphost.exe like so:
			//	- must have an exe extension
			//	- must be a PE file and an EXE (DLL bit cleared)
			//	- must not have .NET metadata
			//	- must have a file with the same name but a dll extension
			//	- this dll file must be a PE file and have .NET metadata

			if (!StringComparer.OrdinalIgnoreCase.Equals(Path.GetExtension(filename), ".exe"))
				return false;
			var dllFilename = Path.ChangeExtension(filename, "dll");
			if (!File.Exists(dllFilename))
				return false;
			if (!PortableExecutableFileHelpers.IsPE(filename, out bool isExe, out bool hasDotNetMetadata))
				return false;
			if (!isExe || hasDotNetMetadata)
				return false;
			if (!PortableExecutableFileHelpers.IsPE(dllFilename, out _, out hasDotNetMetadata))
				return false;
			if (!hasDotNetMetadata)
				return false;

			return true;
		}

		static bool IsDotNetCoreBundle(string filename) {
			if (!PortableExecutableFileHelpers.IsPE(filename, out bool isExe, out bool hasDotNetMetadata))
				return false;
			if (!isExe || hasDotNetMetadata)
				return false;
			try {
				using (var stream = File.OpenRead(filename)) {
					if (stream.Length < bundleSig.Length)
						return false;
					stream.Position = stream.Length - bundleSig.Length;
					var sig = new byte[bundleSig.Length];
					stream.Read(sig, 0, sig.Length);
					for (int i = 0; i < sig.Length; i++) {
						if (bundleSig[i] != sig[i])
							return false;
					}
					return true;
				}
			}
			catch {
			}
			return false;
		}
		// "\x0E.NetCoreBundle"
		static readonly byte[] bundleSig = new byte[] { 0x0E, 0x2E, 0x4E, 0x65, 0x74, 0x43, 0x6F, 0x72, 0x65, 0x42, 0x75, 0x6E, 0x64, 0x6C, 0x65 };
	}
}
