/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
	sealed class ImageSectionHeaderVM : HexVM {
		public override string Name { get; }
		public StringHexField NameVM { get; }
		public UInt32HexField VirtualSizeVM { get; }
		public UInt32HexField VirtualAddressVM { get; }
		public UInt32HexField SizeOfRawDataVM { get; }
		public UInt32HexField PointerToRawDataVM { get; }
		public UInt32HexField PointerToRelocationsVM { get; }
		public UInt32HexField PointerToLinenumbersVM { get; }
		public UInt16HexField NumberOfRelocationsVM { get; }
		public UInt16HexField NumberOfLinenumbersVM { get; }
		public UInt32FlagsHexField CharacteristicsVM { get; }

		public override IEnumerable<HexField> HexFields => hexFields;
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

		public ImageSectionHeaderVM(HexBuffer buffer, PeSectionData section)
			: base(section.Span) {
			Name = section.Name;
			NameVM = new StringHexField(section.SectionName);
			VirtualSizeVM = new UInt32HexField(section.VirtualSize);
			VirtualAddressVM = new UInt32HexField(section.VirtualAddress);
			SizeOfRawDataVM = new UInt32HexField(section.SizeOfRawData);
			PointerToRawDataVM = new UInt32HexField(section.PointerToRawData);
			PointerToRelocationsVM = new UInt32HexField(section.PointerToRelocations);
			PointerToLinenumbersVM = new UInt32HexField(section.PointerToLinenumbers);
			NumberOfRelocationsVM = new UInt16HexField(section.NumberOfRelocations);
			NumberOfLinenumbersVM = new UInt16HexField(section.NumberOfLinenumbers);
			CharacteristicsVM = new UInt32FlagsHexField(section.Characteristics);
			CharacteristicsVM.Add(new BooleanHexBitField("TYPE_DSECT", 0));
			CharacteristicsVM.Add(new BooleanHexBitField("TYPE_NOLOAD", 1));
			CharacteristicsVM.Add(new BooleanHexBitField("TYPE_GROUP", 2));
			CharacteristicsVM.Add(new BooleanHexBitField("TYPE_NO_PAD", 3));
			CharacteristicsVM.Add(new BooleanHexBitField("TYPE_COPY", 4));
			CharacteristicsVM.Add(new BooleanHexBitField("CNT_CODE", 5));
			CharacteristicsVM.Add(new BooleanHexBitField("CNT_INITIALIZED_DATA", 6));
			CharacteristicsVM.Add(new BooleanHexBitField("CNT_UNINITIALIZED_DATA", 7));
			CharacteristicsVM.Add(new BooleanHexBitField("LNK_OTHER", 8));
			CharacteristicsVM.Add(new BooleanHexBitField("LNK_INFO", 9));
			CharacteristicsVM.Add(new BooleanHexBitField("TYPE_OVER", 10));
			CharacteristicsVM.Add(new BooleanHexBitField("LNK_REMOVE", 11));
			CharacteristicsVM.Add(new BooleanHexBitField("LNK_COMDAT", 12));
			CharacteristicsVM.Add(new BooleanHexBitField("RESERVED", 13));
			CharacteristicsVM.Add(new BooleanHexBitField("NO_DEFER_SPEC_EXC", 14));
			CharacteristicsVM.Add(new BooleanHexBitField("GPREL", 15));
			CharacteristicsVM.Add(new BooleanHexBitField("MEM_SYSHEAP", 16));
			CharacteristicsVM.Add(new BooleanHexBitField("MEM_PURGEABLE", 17));
			CharacteristicsVM.Add(new BooleanHexBitField("MEM_LOCKED", 18));
			CharacteristicsVM.Add(new BooleanHexBitField("MEM_PRELOAD", 19));
			CharacteristicsVM.Add(new IntegerHexBitField("Alignment", 20, 4, AlignInfos));
			CharacteristicsVM.Add(new BooleanHexBitField("LNK_NRELOC_OVFL", 24));
			CharacteristicsVM.Add(new BooleanHexBitField("MEM_DISCARDABLE", 25));
			CharacteristicsVM.Add(new BooleanHexBitField("MEM_NOT_CACHED", 26));
			CharacteristicsVM.Add(new BooleanHexBitField("MEM_NOT_PAGED", 27));
			CharacteristicsVM.Add(new BooleanHexBitField("MEM_SHARED", 28));
			CharacteristicsVM.Add(new BooleanHexBitField("MEM_EXECUTE", 29));
			CharacteristicsVM.Add(new BooleanHexBitField("MEM_READ", 30));
			CharacteristicsVM.Add(new BooleanHexBitField("MEM_WRITE", 31));

			hexFields = new HexField[] {
				NameVM,
				VirtualSizeVM,
				VirtualAddressVM,
				SizeOfRawDataVM,
				PointerToRawDataVM,
				PointerToRelocationsVM,
				PointerToLinenumbersVM,
				NumberOfRelocationsVM,
				NumberOfLinenumbersVM,
				CharacteristicsVM,
			};
		}
	}
}
