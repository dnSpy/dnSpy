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
using dnSpy.Shared.UI.HexEditor;

namespace dnSpy.AsmEditor.Hex.Nodes {
	sealed class ImageOptionalHeader64VM : ImageOptionalHeaderVM {
		public override string Name {
			get { return "IMAGE_OPTIONAL_HEADER64"; }
		}

		public override bool Is32Bit {
			get { return false; }
		}

		public UInt64HexField ImageBaseVM {
			get { return imageBaseVM; }
		}
		readonly UInt64HexField imageBaseVM;

		public UInt64HexField SizeOfStackReserveVM {
			get { return sizeOfStackReserveVM; }
		}
		readonly UInt64HexField sizeOfStackReserveVM;

		public UInt64HexField SizeOfStackCommitVM {
			get { return sizeOfStackCommitVM; }
		}
		readonly UInt64HexField sizeOfStackCommitVM;

		public UInt64HexField SizeOfHeapReserveVM {
			get { return sizeOfHeapReserveVM; }
		}
		readonly UInt64HexField sizeOfHeapReserveVM;

		public UInt64HexField SizeOfHeapCommitVM {
			get { return sizeOfHeapCommitVM; }
		}
		readonly UInt64HexField sizeOfHeapCommitVM;

		public ImageOptionalHeader64VM(object owner, HexDocument doc, ulong startOffset, ulong endOffset)
			: base(owner, doc, startOffset, endOffset, 0x20, 0x68) {
			this.imageBaseVM = new UInt64HexField(doc, Name, "ImageBase", startOffset + 0x18);

			this.sizeOfStackReserveVM = new UInt64HexField(doc, Name, "SizeOfStackReserve", startOffset + 0x48);
			this.sizeOfStackCommitVM = new UInt64HexField(doc, Name, "SizeOfStackCommit", startOffset + 0x50);
			this.sizeOfHeapReserveVM = new UInt64HexField(doc, Name, "SizeOfHeapReserve", startOffset + 0x58);
			this.sizeOfHeapCommitVM = new UInt64HexField(doc, Name, "SizeOfHeapCommit", startOffset + 0x60);

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
