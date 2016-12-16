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

namespace dnSpy.AsmEditor.Hex.PE {
	sealed class ImageOptionalHeader32VM : ImageOptionalHeaderVM {
		public override string Name => "IMAGE_OPTIONAL_HEADER32";
		public override bool Is32Bit => true;
		public UInt32HexField BaseOfDataVM { get; }
		public UInt32HexField ImageBaseVM { get; }
		public UInt32HexField SizeOfStackReserveVM { get; }
		public UInt32HexField SizeOfStackCommitVM { get; }
		public UInt32HexField SizeOfHeapReserveVM { get; }
		public UInt32HexField SizeOfHeapCommitVM { get; }

		public ImageOptionalHeader32VM(HexBuffer buffer, HexPosition startOffset, HexPosition endOffset)
			: base(buffer, startOffset, endOffset, 0x20, 0x58) {
			BaseOfDataVM = new UInt32HexField(buffer, Name, "BaseOfData", startOffset + 0x18);
			ImageBaseVM = new UInt32HexField(buffer, Name, "ImageBase", startOffset + 0x1C);

			SizeOfStackReserveVM = new UInt32HexField(buffer, Name, "SizeOfStackReserve", startOffset + 0x48);
			SizeOfStackCommitVM = new UInt32HexField(buffer, Name, "SizeOfStackCommit", startOffset + 0x4C);
			SizeOfHeapReserveVM = new UInt32HexField(buffer, Name, "SizeOfHeapReserve", startOffset + 0x50);
			SizeOfHeapCommitVM = new UInt32HexField(buffer, Name, "SizeOfHeapCommit", startOffset + 0x54);

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

			AddDataDirs(list, endOffset);
		}
	}
}
