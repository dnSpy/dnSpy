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
using System.Text;
using dnSpy.HexEditor;

namespace dnSpy.TreeNodes.Hex {
	sealed class ImageSectionHeaderVM : HexVM {
		public override string Name {
			get { return "IMAGE_SECTION_HEADER"; }
		}

		public StringHexField NameVM {
			get { return nameVM; }
		}
		readonly StringHexField nameVM;

		public UInt32HexField VirtualSizeVM {
			get { return virtualSizeVM; }
		}
		readonly UInt32HexField virtualSizeVM;

		public UInt32HexField VirtualAddressVM {
			get { return virtualAddressVM; }
		}
		readonly UInt32HexField virtualAddressVM;

		public UInt32HexField SizeOfRawDataVM {
			get { return sizeOfRawDataVM; }
		}
		readonly UInt32HexField sizeOfRawDataVM;

		public UInt32HexField PointerToRawDataVM {
			get { return pointerToRawDataVM; }
		}
		readonly UInt32HexField pointerToRawDataVM;

		public UInt32HexField PointerToRelocationsVM {
			get { return pointerToRelocationsVM; }
		}
		readonly UInt32HexField pointerToRelocationsVM;

		public UInt32HexField PointerToLinenumbersVM {
			get { return pointerToLinenumbersVM; }
		}
		readonly UInt32HexField pointerToLinenumbersVM;

		public UInt16HexField NumberOfRelocationsVM {
			get { return numberOfRelocationsVM; }
		}
		readonly UInt16HexField numberOfRelocationsVM;

		public UInt16HexField NumberOfLinenumbersVM {
			get { return numberOfLinenumbersVM; }
		}
		readonly UInt16HexField numberOfLinenumbersVM;

		public UInt32FlagsHexField CharacteristicsVM {
			get { return characteristicsVM; }
		}
		readonly UInt32FlagsHexField characteristicsVM;

		public override IEnumerable<HexField> HexFields {
			get { return hexFields; }
		}
		readonly HexField[] hexFields;

		static readonly IntegerHexBitFieldEnumInfo[] AlignInfos = new IntegerHexBitFieldEnumInfo[] {
			new IntegerHexBitFieldEnumInfo(0, "Default"),
			new IntegerHexBitFieldEnumInfo(1, "1 Byte"),
			new IntegerHexBitFieldEnumInfo(2, "2 Bytes"),
			new IntegerHexBitFieldEnumInfo(3, "4 Bytes"),
			new IntegerHexBitFieldEnumInfo(4, "8 Bytes"),
			new IntegerHexBitFieldEnumInfo(5, "16 Bytes"),
			new IntegerHexBitFieldEnumInfo(6, "32 Bytes"),
			new IntegerHexBitFieldEnumInfo(7, "64 Bytes"),
			new IntegerHexBitFieldEnumInfo(8, "128 Bytes"),
			new IntegerHexBitFieldEnumInfo(9, "256 Bytes"),
			new IntegerHexBitFieldEnumInfo(10, "512 Bytes"),
			new IntegerHexBitFieldEnumInfo(11, "1024 Bytes"),
			new IntegerHexBitFieldEnumInfo(12, "2048 Bytes"),
			new IntegerHexBitFieldEnumInfo(13, "4096 Bytes"),
			new IntegerHexBitFieldEnumInfo(14, "8192 Bytes"),
			new IntegerHexBitFieldEnumInfo(15, "Reserved"),
		};

		public ImageSectionHeaderVM(HexDocument doc, ulong startOffset) {
			this.nameVM = new StringHexField(doc, Name, "Name", startOffset + 0, Encoding.UTF8, 8);
			this.virtualSizeVM = new UInt32HexField(doc, Name, "VirtualSize", startOffset + 8);
			this.virtualAddressVM = new UInt32HexField(doc, Name, "VirtualAddress", startOffset + 0x0C);
			this.sizeOfRawDataVM = new UInt32HexField(doc, Name, "SizeOfRawData", startOffset + 0x10);
			this.pointerToRawDataVM = new UInt32HexField(doc, Name, "PointerToRawData", startOffset + 0x14);
			this.pointerToRelocationsVM = new UInt32HexField(doc, Name, "PointerToRelocations", startOffset + 0x18);
			this.pointerToLinenumbersVM = new UInt32HexField(doc, Name, "PointerToLinenumbers", startOffset + 0x1C);
			this.numberOfRelocationsVM = new UInt16HexField(doc, Name, "NumberOfRelocations", startOffset + 0x20);
			this.numberOfLinenumbersVM = new UInt16HexField(doc, Name, "NumberOfLinenumbers", startOffset + 0x22);
			this.characteristicsVM = new UInt32FlagsHexField(doc, Name, "Characteristics", startOffset + 0x24);
			this.characteristicsVM.Add(new BooleanHexBitField("TYPE_DSECT", 0));
			this.characteristicsVM.Add(new BooleanHexBitField("TYPE_NOLOAD", 1));
			this.characteristicsVM.Add(new BooleanHexBitField("TYPE_GROUP", 2));
			this.characteristicsVM.Add(new BooleanHexBitField("TYPE_NO_PAD", 3));
			this.characteristicsVM.Add(new BooleanHexBitField("TYPE_COPY", 4));
			this.characteristicsVM.Add(new BooleanHexBitField("CNT_CODE", 5));
			this.characteristicsVM.Add(new BooleanHexBitField("CNT_INITIALIZED_DATA", 6));
			this.characteristicsVM.Add(new BooleanHexBitField("CNT_UNINITIALIZED_DATA", 7));
			this.characteristicsVM.Add(new BooleanHexBitField("LNK_OTHER", 8));
			this.characteristicsVM.Add(new BooleanHexBitField("LNK_INFO", 9));
			this.characteristicsVM.Add(new BooleanHexBitField("TYPE_OVER", 10));
			this.characteristicsVM.Add(new BooleanHexBitField("LNK_REMOVE", 11));
			this.characteristicsVM.Add(new BooleanHexBitField("LNK_COMDAT", 12));
			this.characteristicsVM.Add(new BooleanHexBitField("RESERVED", 13));
			this.characteristicsVM.Add(new BooleanHexBitField("NO_DEFER_SPEC_EXC", 14));
			this.characteristicsVM.Add(new BooleanHexBitField("GPREL", 15));
			this.characteristicsVM.Add(new BooleanHexBitField("MEM_SYSHEAP", 16));
			this.characteristicsVM.Add(new BooleanHexBitField("MEM_PURGEABLE", 17));
			this.characteristicsVM.Add(new BooleanHexBitField("MEM_LOCKED", 18));
			this.characteristicsVM.Add(new BooleanHexBitField("MEM_PRELOAD", 19));
			this.characteristicsVM.Add(new IntegerHexBitField("Alignment", 20, 4, AlignInfos));
			this.characteristicsVM.Add(new BooleanHexBitField("LNK_NRELOC_OVFL", 24));
			this.characteristicsVM.Add(new BooleanHexBitField("MEM_DISCARDABLE", 25));
			this.characteristicsVM.Add(new BooleanHexBitField("MEM_NOT_CACHED", 26));
			this.characteristicsVM.Add(new BooleanHexBitField("MEM_NOT_PAGED", 27));
			this.characteristicsVM.Add(new BooleanHexBitField("MEM_SHARED", 28));
			this.characteristicsVM.Add(new BooleanHexBitField("MEM_EXECUTE", 29));
			this.characteristicsVM.Add(new BooleanHexBitField("MEM_READ", 30));
			this.characteristicsVM.Add(new BooleanHexBitField("MEM_WRITE", 31));

			this.hexFields = new HexField[] {
				this.nameVM,
				this.virtualSizeVM,
				this.virtualAddressVM,
				this.sizeOfRawDataVM,
				this.pointerToRawDataVM,
				this.pointerToRelocationsVM,
				this.pointerToLinenumbersVM,
				this.numberOfRelocationsVM,
				this.numberOfLinenumbersVM,
				this.characteristicsVM,
			};
		}
	}
}
