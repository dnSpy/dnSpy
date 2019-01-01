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

using dnlib.DotNet;
using dnlib.PE;

namespace dnSpy.AsmEditor.SaveModule {
	static class CharacteristicsHelper {
		public static Characteristics GetCharacteristics(Characteristics characteristics, ModuleKind moduleKind) {
			if (moduleKind == ModuleKind.Dll || moduleKind == ModuleKind.NetModule)
				characteristics |= Characteristics.Dll;
			else
				characteristics &= ~Characteristics.Dll;
			return characteristics;
		}

		public static Characteristics GetCharacteristics(Characteristics characteristics, Machine machine) {
			if (machine.Is64Bit()) {
				characteristics &= ~Characteristics.Bit32Machine;
				characteristics |= Characteristics.LargeAddressAware;
			}
			else
				characteristics |= Characteristics.Bit32Machine;
			return characteristics;
		}
	}
}
