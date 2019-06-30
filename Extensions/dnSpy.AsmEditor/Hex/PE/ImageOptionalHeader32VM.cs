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

using System.Collections.Generic;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Files.PE;

namespace dnSpy.AsmEditor.Hex.PE {
	sealed class ImageOptionalHeader32VM : ImageOptionalHeaderVM {
		public override string Name { get; }
		public override bool Is32Bit => true;
		public UInt32HexField BaseOfDataVM { get; }
		public UInt32HexField ImageBaseVM { get; }
		public UInt32HexField SizeOfStackReserveVM { get; }
		public UInt32HexField SizeOfStackCommitVM { get; }
		public UInt32HexField SizeOfHeapReserveVM { get; }
		public UInt32HexField SizeOfHeapCommitVM { get; }

		public ImageOptionalHeader32VM(HexBuffer buffer, PeOptionalHeader32Data optionalHeader)
			: base(buffer, optionalHeader) {
			Name = optionalHeader.Name;
			BaseOfDataVM = new UInt32HexField(optionalHeader.BaseOfData);
			ImageBaseVM = new UInt32HexField(optionalHeader.ImageBase);

			SizeOfStackReserveVM = new UInt32HexField(optionalHeader.SizeOfStackReserve);
			SizeOfStackCommitVM = new UInt32HexField(optionalHeader.SizeOfStackCommit);
			SizeOfHeapReserveVM = new UInt32HexField(optionalHeader.SizeOfHeapReserve);
			SizeOfHeapCommitVM = new UInt32HexField(optionalHeader.SizeOfHeapCommit);

			var list = new List<HexField> {
				MagicVM,
				MajorLinkerVersionVM,
				MinorLinkerVersionVM,
				SizeOfCodeVM,
				SizeOfInitializedDataVM,
				SizeOfUninitializedDataVM,
				AddressOfEntryPointVM,
				BaseOfCodeVM,
				BaseOfDataVM,
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
