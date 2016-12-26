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

using System.Collections.Generic;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Files.PE;

namespace dnSpy.AsmEditor.Hex.PE {
	sealed class ImageOptionalHeader64VM : ImageOptionalHeaderVM {
		public override string Name { get; }
		public override bool Is32Bit => false;
		public UInt64HexField ImageBaseVM { get; }
		public UInt64HexField SizeOfStackReserveVM { get; }
		public UInt64HexField SizeOfStackCommitVM { get; }
		public UInt64HexField SizeOfHeapReserveVM { get; }
		public UInt64HexField SizeOfHeapCommitVM { get; }

		public ImageOptionalHeader64VM(HexBuffer buffer, PeOptionalHeader64Data optionalHeader)
			: base(buffer, optionalHeader) {
			Name = optionalHeader.Name;
			ImageBaseVM = new UInt64HexField(optionalHeader.ImageBase);

			SizeOfStackReserveVM = new UInt64HexField(optionalHeader.SizeOfStackReserve);
			SizeOfStackCommitVM = new UInt64HexField(optionalHeader.SizeOfStackCommit);
			SizeOfHeapReserveVM = new UInt64HexField(optionalHeader.SizeOfHeapReserve);
			SizeOfHeapCommitVM = new UInt64HexField(optionalHeader.SizeOfHeapCommit);

			var list = new List<HexField> {
				MagicVM,
				MajorLinkerVersionVM,
				MinorLinkerVersionVM,
				SizeOfCodeVM,
				SizeOfInitializedDataVM,
				SizeOfUninitializedDataVM,
				AddressOfEntryPointVM,
				BaseOfCodeVM,
				ImageBaseVM,
				SectionAlignmentVM,
				FileAlignmentVM,
				MajorOperatingSystemVersionVM,
				MinorOperatingSystemVersionVM,
				MajorImageVersionVM,
				MinorImageVersionVM,
				MajorSubsystemVersionVM,
				MinorSubsystemVersionVM,
				Win32VersionValueVM,
				SizeOfImageVM,
				SizeOfHeadersVM,
				CheckSumVM,
				SubsystemVM,
				DllCharacteristicsVM,
				SizeOfStackReserveVM,
				SizeOfStackCommitVM,
				SizeOfHeapReserveVM,
				SizeOfHeapCommitVM,
				LoaderFlagsVM,
				NumberOfRvaAndSizesVM,
			};

			AddDataDirs(list, optionalHeader);
		}
	}
}
