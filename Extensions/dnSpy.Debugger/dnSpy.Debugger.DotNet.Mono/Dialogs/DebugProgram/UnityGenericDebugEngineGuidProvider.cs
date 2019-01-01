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
using dnlib.PE;
using dnSpy.Contracts.Debugger.StartDebugging;

namespace dnSpy.Debugger.DotNet.Mono.Dialogs.DebugProgram {
	[ExportGenericDebugEngineGuidProvider(PredefinedGenericDebugEngineGuidProviderOrders.DotNetUnity)]
	sealed class UnityGenericDebugEngineGuidProvider : GenericDebugEngineGuidProvider {
		public override Guid? GetEngineGuid(string filename) {
			if (!File.Exists(filename))
				return null;
			try {
				using (var peImage = new PEImage(filename)) {
					if ((peImage.ImageNTHeaders.FileHeader.Characteristics & Characteristics.Dll) != 0)
						return null;
					var dd = peImage.ImageNTHeaders.OptionalHeader.DataDirectories[14];
					if (dd.VirtualAddress != 0 && dd.Size >= 0x48)
						return null;

					var dirName = Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename) + "_Data");
					if (!Directory.Exists(dirName))
						return null;

					// Probably a Unity game
					return PredefinedGenericDebugEngineGuids.DotNetUnity;
				}
			}
			catch {
			}
			return null;
		}
	}
}
