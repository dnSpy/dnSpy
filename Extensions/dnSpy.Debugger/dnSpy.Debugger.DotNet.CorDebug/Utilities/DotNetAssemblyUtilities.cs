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

namespace dnSpy.Debugger.DotNet.CorDebug.Utilities {
	static class DotNetAssemblyUtilities {
		public static int TryGetProgramBitness(string filename) {
			if (!File.Exists(filename))
				return 0;
			try {
				using (var peImage = new PEImage(filename)) {
					var dotNetDir = peImage.ImageNTHeaders.OptionalHeader.DataDirectories[14];
					if (dotNetDir.VirtualAddress == 0 || dotNetDir.Size < 0x48) {
						switch (peImage.ImageNTHeaders.FileHeader.Machine) {
						case Machine.I386:
						case Machine.ARMNT:
							return 32;
						case Machine.AMD64:
						case Machine.IA64:
						case Machine.ARM64:
							return 64;
						}
					}
					else {
						using (var module = ModuleDefMD.Load(peImage))
							return module.GetPointerSize(IntPtr.Size, 4) * 8;
					}
				}
			}
			catch {
			}
			return 0;
		}
	}
}
