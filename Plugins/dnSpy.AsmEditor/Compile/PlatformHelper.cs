/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
using dnlib.DotNet;
using dnlib.PE;
using dnSpy.Contracts.AsmEditor.Compile;

namespace dnSpy.AsmEditor.Compile {
	static class PlatformHelper {
		public static CompilePlatform GetPlatform(ModuleDef module) {
			if (module == null)
				throw new ArgumentNullException(nameof(module));

			switch (module.Machine) {
			case Machine.I386:
				// See https://github.com/dotnet/coreclr/blob/master/src/inc/corhdr.h
				int c = (module.Is32BitRequired ? 2 : 0) + (module.Is32BitPreferred ? 1 : 0);
				switch (c) {
				case 0: // no special meaning, MachineType and ILONLY flag determine image requirements
					if (!module.IsILOnly)
						return CompilePlatform.X86;
					return CompilePlatform.AnyCpu;
				case 1: // illegal, reserved for future use
					return CompilePlatform.AnyCpu;
				default:
				case 2: // image is x86-specific
					return CompilePlatform.X86;
				case 3: // image is platform neutral and prefers to be loaded 32-bit when possible
					return CompilePlatform.AnyCpu32BitPreferred;
				}

			case Machine.AMD64:
				return CompilePlatform.X64;

			case Machine.IA64:
				return CompilePlatform.Itanium;

			case Machine.ARMNT:
				return CompilePlatform.Arm;

			case Machine.ARM64:
				return CompilePlatform.Arm;

			default:
				return CompilePlatform.AnyCpu;
			}
		}
	}
}
