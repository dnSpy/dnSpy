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

		public UInt32HexField CharacteristicsVM {
			get { return characteristicsVM; }
		}
		readonly UInt32HexField characteristicsVM;

		readonly HexField[] hexFields;

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
			this.characteristicsVM = new UInt32HexField(doc, Name, "Characteristics", startOffset + 0x24);

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

		public override void OnDocumentModifiedOverride(ulong modifiedStart, ulong modifiedEnd) {
			foreach (var field in hexFields)
				field.OnDocumentModifiedOverride(modifiedStart, modifiedEnd);
		}
	}
}
