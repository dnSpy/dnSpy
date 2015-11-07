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

using System;
using System.Collections.Generic;
using System.Globalization;
using dnSpy.Shared.UI.HexEditor;

namespace dnSpy.TreeNodes.Hex {
	sealed class ImageFileHeaderVM : HexVM {
		public override string Name {
			get { return "IMAGE_FILE_HEADER"; }
		}

		public UInt16FlagsHexField MachineVM {
			get { return machineVM; }
		}
		readonly UInt16FlagsHexField machineVM;

		public UInt16HexField NumberOfSectionsVM {
			get { return numberOfSectionsVM; }
		}
		readonly UInt16HexField numberOfSectionsVM;

		public UInt32HexField TimeDateStampVM {
			get { return timeDateStampVM; }
		}
		readonly UInt32HexField timeDateStampVM;

		public string TimeDateStampString {
			get {
				if (TimeDateStampVM.DataFieldVM.HasError)
					return string.Empty;

				var date = EpochToDate((uint)TimeDateStampVM.DataFieldVM.ObjectValue);
				return date.ToString(CultureInfo.CurrentUICulture.DateTimeFormat);
			}
		}

		DateTime EpochToDate(uint val) {
			return Epoch.AddSeconds(val);
		}
		static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

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

		public UInt16FlagsHexField CharacteristicsVM {
			get { return characteristicsVM; }
		}
		readonly UInt16FlagsHexField characteristicsVM;

		public override IEnumerable<HexField> HexFields {
			get { return hexFields; }
		}
		readonly HexField[] hexFields;

		static readonly IntegerHexBitFieldEnumInfo[] MachineInfos = new IntegerHexBitFieldEnumInfo[] {
			new IntegerHexBitFieldEnumInfo(0x014C, "I386"),
			new IntegerHexBitFieldEnumInfo(0x8664, "AMD64"),
			new IntegerHexBitFieldEnumInfo(0x0200, "IA64"),
			new IntegerHexBitFieldEnumInfo(0x01C4, "ARMNT"),
			new IntegerHexBitFieldEnumInfo(0xAA64, "ARM64"),
			new IntegerHexBitFieldEnumInfo(0, "Unknown"),
			new IntegerHexBitFieldEnumInfo(0x0162, "R3000"),
			new IntegerHexBitFieldEnumInfo(0x0166, "R4000"),
			new IntegerHexBitFieldEnumInfo(0x0168, "R10000"),
			new IntegerHexBitFieldEnumInfo(0x0169, "WCEMIPSV2"),
			new IntegerHexBitFieldEnumInfo(0x0184, "ALPHA"),
			new IntegerHexBitFieldEnumInfo(0x01A2, "SH3"),
			new IntegerHexBitFieldEnumInfo(0x01A3, "SH3DSP"),
			new IntegerHexBitFieldEnumInfo(0x01A4, "SH3E"),
			new IntegerHexBitFieldEnumInfo(0x01A6, "SH4"),
			new IntegerHexBitFieldEnumInfo(0x01A8, "SH5"),
			new IntegerHexBitFieldEnumInfo(0x01C0, "ARM"),
			new IntegerHexBitFieldEnumInfo(0x01C2, "THUMB"),
			new IntegerHexBitFieldEnumInfo(0x01D3, "AM33"),
			new IntegerHexBitFieldEnumInfo(0x01F0, "POWERPC"),
			new IntegerHexBitFieldEnumInfo(0x01F1, "POWERPCFP"),
			new IntegerHexBitFieldEnumInfo(0x0266, "MIPS16"),
			new IntegerHexBitFieldEnumInfo(0x0284, "ALPHA64"),
			new IntegerHexBitFieldEnumInfo(0x0366, "MIPSFPU"),
			new IntegerHexBitFieldEnumInfo(0x0466, "MIPSFPU16"),
			new IntegerHexBitFieldEnumInfo(0x0520, "TRICORE"),
			new IntegerHexBitFieldEnumInfo(0x0CEF, "CEF"),
			new IntegerHexBitFieldEnumInfo(0x0EBC, "EBC"),
			new IntegerHexBitFieldEnumInfo(0x9041, "M32R"),
			new IntegerHexBitFieldEnumInfo(0xC0EE, "CEE"),
		};

		public ImageFileHeaderVM(object owner, HexDocument doc, ulong startOffset)
			: base(owner) {
			this.machineVM = new UInt16FlagsHexField(doc, Name, "Machine", startOffset + 0);
			this.machineVM.Add(new IntegerHexBitField("Machine", 0, 16, MachineInfos));
			this.numberOfSectionsVM = new UInt16HexField(doc, Name, "NumberOfSections", startOffset + 2);
			this.timeDateStampVM = new UInt32HexField(doc, Name, "TimeDateStamp", startOffset + 4);
			this.timeDateStampVM.DataFieldVM.PropertyChanged += (s, e) => OnPropertyChanged("TimeDateStampString");
			this.pointerToSymbolTableVM = new UInt32HexField(doc, Name, "PointerToSymbolTable", startOffset + 8);
			this.numberOfSymbolsVM = new UInt32HexField(doc, Name, "NumberOfSymbols", startOffset + 0x0C);
			this.sizeOfOptionalHeaderVM = new UInt16HexField(doc, Name, "SizeOfOptionalHeader", startOffset + 0x10);
			this.characteristicsVM = new UInt16FlagsHexField(doc, Name, "Characteristics", startOffset + 0x12);
			this.characteristicsVM.Add(new BooleanHexBitField("Relocs Stripped", 0));
			this.characteristicsVM.Add(new BooleanHexBitField("Executable Image", 1));
			this.characteristicsVM.Add(new BooleanHexBitField("Line Nums Stripped", 2));
			this.characteristicsVM.Add(new BooleanHexBitField("Local Syms Stripped", 3));
			this.characteristicsVM.Add(new BooleanHexBitField("Aggressive WS Trim", 4));
			this.characteristicsVM.Add(new BooleanHexBitField("Large Address Aware", 5));
			this.characteristicsVM.Add(new BooleanHexBitField("Reserved 0040h", 6));
			this.characteristicsVM.Add(new BooleanHexBitField("Bytes Reversed Lo", 7));
			this.characteristicsVM.Add(new BooleanHexBitField("32-Bit Machine", 8));
			this.characteristicsVM.Add(new BooleanHexBitField("Debug Stripped", 9));
			this.characteristicsVM.Add(new BooleanHexBitField("Removable Run From Swap", 10));
			this.characteristicsVM.Add(new BooleanHexBitField("Net Run From Swap", 11));
			this.characteristicsVM.Add(new BooleanHexBitField("System", 12));
			this.characteristicsVM.Add(new BooleanHexBitField("Dll", 13));
			this.characteristicsVM.Add(new BooleanHexBitField("Up System Only", 14));
			this.characteristicsVM.Add(new BooleanHexBitField("Bytes Reversed Hi", 15));

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
