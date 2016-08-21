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
using dnSpy.Contracts.HexEditor;

namespace dnSpy.AsmEditor.Hex.Nodes {
	sealed class ImageOptionalHeader64VM : ImageOptionalHeaderVM {
		public override string Name => "IMAGE_OPTIONAL_HEADER64";
		public override bool Is32Bit => false;
		public UInt64HexField ImageBaseVM { get; }
		public UInt64HexField SizeOfStackReserveVM { get; }
		public UInt64HexField SizeOfStackCommitVM { get; }
		public UInt64HexField SizeOfHeapReserveVM { get; }
		public UInt64HexField SizeOfHeapCommitVM { get; }

		public ImageOptionalHeader64VM(object owner, HexDocument doc, ulong startOffset, ulong endOffset)
			: base(owner, doc, startOffset, endOffset, 0x20, 0x68) {
			this.ImageBaseVM = new UInt64HexField(doc, Name, "ImageBase", startOffset + 0x18);

			this.SizeOfStackReserveVM = new UInt64HexField(doc, Name, "SizeOfStackReserve", startOffset + 0x48);
			this.SizeOfStackCommitVM = new UInt64HexField(doc, Name, "SizeOfStackCommit", startOffset + 0x50);
			this.SizeOfHeapReserveVM = new UInt64HexField(doc, Name, "SizeOfHeapReserve", startOffset + 0x58);
			this.SizeOfHeapCommitVM = new UInt64HexField(doc, Name, "SizeOfHeapCommit", startOffset + 0x60);

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

			AddDataDirs(list, endOffset);
		}
	}
}
