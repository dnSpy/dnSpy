/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
	sealed class ImageOptionalHeader32VM : ImageOptionalHeaderVM {
		public override string Name {
			get { return "IMAGE_OPTIONAL_HEADER32"; }
		}

		public override bool Is32Bit {
			get { return true; }
		}

		public UInt32HexField BaseOfDataVM {
			get { return baseOfDataVM; }
		}
		readonly UInt32HexField baseOfDataVM;

		public UInt32HexField ImageBaseVM {
			get { return imageBaseVM; }
		}
		readonly UInt32HexField imageBaseVM;

		public UInt32HexField SizeOfStackReserveVM {
			get { return sizeOfStackReserveVM; }
		}
		readonly UInt32HexField sizeOfStackReserveVM;

		public UInt32HexField SizeOfStackCommitVM {
			get { return sizeOfStackCommitVM; }
		}
		readonly UInt32HexField sizeOfStackCommitVM;

		public UInt32HexField SizeOfHeapReserveVM {
			get { return sizeOfHeapReserveVM; }
		}
		readonly UInt32HexField sizeOfHeapReserveVM;

		public UInt32HexField SizeOfHeapCommitVM {
			get { return sizeOfHeapCommitVM; }
		}
		readonly UInt32HexField sizeOfHeapCommitVM;

		public ImageOptionalHeader32VM(object owner, HexDocument doc, ulong startOffset, ulong endOffset)
			: base(owner, doc, startOffset, endOffset, 0x20, 0x58) {
			this.baseOfDataVM = new UInt32HexField(doc, Name, "BaseOfData", startOffset + 0x18);
			this.imageBaseVM = new UInt32HexField(doc, Name, "ImageBase", startOffset + 0x1C);

			this.sizeOfStackReserveVM = new UInt32HexField(doc, Name, "SizeOfStackReserve", startOffset + 0x48);
			this.sizeOfStackCommitVM = new UInt32HexField(doc, Name, "SizeOfStackCommit", startOffset + 0x4C);
			this.sizeOfHeapReserveVM = new UInt32HexField(doc, Name, "SizeOfHeapReserve", startOffset + 0x50);
			this.sizeOfHeapCommitVM = new UInt32HexField(doc, Name, "SizeOfHeapCommit", startOffset + 0x54);

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
