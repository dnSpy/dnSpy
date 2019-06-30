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
using dnlib.DotNet;
using dnlib.PE;
using dnSpy.Contracts.Debugger.StartDebugging;

namespace dnSpy.Debugger.DotNet.Dialogs.DebugProgram {
	[ExportGenericDebugEngineGuidProvider(PredefinedGenericDebugEngineGuidProviderOrders.DotNet)]
	sealed class DotNetGenericDebugEngineGuidProvider : GenericDebugEngineGuidProvider {
		public override Guid? GetEngineGuid(string filename) {
			if (!File.Exists(filename))
				return null;
			if (!PortableExecutableFileHelpers.IsExecutable(filename))
				return null;
			try {
				using (var peImage = new PEImage(filename)) {
					if ((peImage.ImageNTHeaders.FileHeader.Characteristics & Characteristics.Dll) != 0)
						return null;
					var dd = peImage.ImageNTHeaders.OptionalHeader.DataDirectories[14];
					if (dd.VirtualAddress == 0 || dd.Size < 0x48)
						return null;

					using (var mod = ModuleDefMD.Load(peImage, new ModuleCreationOptions())) {
						var asm = mod.Assembly;
						if (asm is null)
							return null;

						var defaultGuid = PredefinedGenericDebugEngineGuids.DotNetFramework;
						var ca = asm.CustomAttributes.Find("System.Runtime.Versioning.TargetFrameworkAttribute");
						if (ca is null)
							return defaultGuid;
						if (ca.ConstructorArguments.Count != 1)
							return defaultGuid;
						string s = ca.ConstructorArguments[0].Value as UTF8String;
						if (s is null)
							return defaultGuid;

						// See corclr/src/mscorlib/src/System/Runtime/Versioning/BinaryCompatibility.cs
						var values = s.Split(new char[] { ',' });
						if (values.Length >= 2 && values.Length <= 3) {
							var framework = values[0].Trim();
							if (framework == ".NETCoreApp")
								return PredefinedGenericDebugEngineGuids.DotNetCore;
						}

						return defaultGuid;
					}
				}
			}
			catch {
			}
			return null;
		}
	}
}
