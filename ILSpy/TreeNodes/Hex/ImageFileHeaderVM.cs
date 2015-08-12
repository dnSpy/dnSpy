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
using dnSpy.HexEditor;

namespace dnSpy.TreeNodes.Hex {
	sealed class ImageFileHeaderVM : HexVM {
		public override string Name {
			get { return "IMAGE_FILE_HEADER"; }
		}

		public UInt16HexField MachineVM {
			get { return machineVM; }
		}
		readonly UInt16HexField machineVM;

		public UInt16HexField NumberOfSectionsVM {
			get { return numberOfSectionsVM; }
		}
		readonly UInt16HexField numberOfSectionsVM;

		public UInt32HexField TimeDateStampVM {
			get { return timeDateStampVM; }
		}
		readonly UInt32HexField timeDateStampVM;

		public UInt32HexField PointerToSymbolTableVM {
			get { return pointerToSymbolTableVM; }
		}
		readonly UInt32HexField pointerToSymbolTableVM;

		public UInt32HexField NumberOfSymbolsVM {
			get { return numberOfSymbolsVM; }
		}
		readonly UInt32HexField numberOfSymbolsVM;

		public UInt16HexField SizeOfOptionalHeaderVM {
			get { return sizeOfOptionalHeaderVM; }
		}
		readonly UInt16HexField sizeOfOptionalHeaderVM;

		public UInt16HexField CharacteristicsVM {
			get { return characteristicsVM; }
		}
		readonly UInt16HexField characteristicsVM;

		public override IEnumerable<HexField> HexFields {
			get { return hexFields; }
		}
		readonly HexField[] hexFields;

		public ImageFileHeaderVM(HexDocument doc, ulong startOffset) {
			this.machineVM = new UInt16HexField(doc, Name, "Machine", startOffset + 0);
			this.numberOfSectionsVM = new UInt16HexField(doc, Name, "NumberOfSections", startOffset + 2);
			this.timeDateStampVM = new UInt32HexField(doc, Name, "TimeDateStamp", startOffset + 4);
			this.pointerToSymbolTableVM = new UInt32HexField(doc, Name, "PointerToSymbolTable", startOffset + 8);
			this.numberOfSymbolsVM = new UInt32HexField(doc, Name, "NumberOfSymbols", startOffset + 0x0C);
			this.sizeOfOptionalHeaderVM = new UInt16HexField(doc, Name, "SizeOfOptionalHeader", startOffset + 0x10);
			this.characteristicsVM = new UInt16HexField(doc, Name, "Characteristics", startOffset + 0x12);

			this.hexFields = new HexField[] {
				machineVM,
				numberOfSectionsVM,
				timeDateStampVM,
				pointerToSymbolTableVM,
				numberOfSymbolsVM,
				sizeOfOptionalHeaderVM,
				characteristicsVM,
			};
		}
	}
}
