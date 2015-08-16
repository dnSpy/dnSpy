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
	abstract class ImageOptionalHeaderVM : HexVM {
		public abstract bool Is32Bit { get; }

		public UInt16HexField MagicVM {
			get { return magicVM; }
		}
		readonly UInt16HexField magicVM;

		public ByteHexField MajorLinkerVersionVM {
			get { return majorLinkerVersionVM; }
		}
		readonly ByteHexField majorLinkerVersionVM;

		public ByteHexField MinorLinkerVersionVM {
			get { return minorLinkerVersionVM; }
		}
		readonly ByteHexField minorLinkerVersionVM;

		public UInt32HexField SizeOfCodeVM {
			get { return sizeOfCodeVM; }
		}
		readonly UInt32HexField sizeOfCodeVM;

		public UInt32HexField SizeOfInitializedDataVM {
			get { return sizeOfInitializedDataVM; }
		}
		readonly UInt32HexField sizeOfInitializedDataVM;

		public UInt32HexField SizeOfUninitializedDataVM {
			get { return sizeOfUninitializedDataVM; }
		}
		readonly UInt32HexField sizeOfUninitializedDataVM;

		public UInt32HexField AddressOfEntryPointVM {
			get { return addressOfEntryPointVM; }
		}
		readonly UInt32HexField addressOfEntryPointVM;

		public UInt32HexField BaseOfCodeVM {
			get { return baseOfCodeVM; }
		}
		readonly UInt32HexField baseOfCodeVM;

		public UInt32HexField SectionAlignmentVM {
			get { return sectionAlignmentVM; }
		}
		readonly UInt32HexField sectionAlignmentVM;

		public UInt32HexField FileAlignmentVM {
			get { return fileAlignmentVM; }
		}
		readonly UInt32HexField fileAlignmentVM;

		public UInt16HexField MajorOperatingSystemVersionVM {
			get { return majorOperatingSystemVersionVM; }
		}
		readonly UInt16HexField majorOperatingSystemVersionVM;

		public UInt16HexField MinorOperatingSystemVersionVM {
			get { return minorOperatingSystemVersionVM; }
		}
		readonly UInt16HexField minorOperatingSystemVersionVM;

		public UInt16HexField MajorImageVersionVM {
			get { return majorImageVersionVM; }
		}
		readonly UInt16HexField majorImageVersionVM;

		public UInt16HexField MinorImageVersionVM {
			get { return minorImageVersionVM; }
		}
		readonly UInt16HexField minorImageVersionVM;

		public UInt16HexField MajorSubsystemVersionVM {
			get { return majorSubsystemVersionVM; }
		}
		readonly UInt16HexField majorSubsystemVersionVM;

		public UInt16HexField MinorSubsystemVersionVM {
			get { return minorSubsystemVersionVM; }
		}
		readonly UInt16HexField minorSubsystemVersionVM;

		public UInt32HexField Win32VersionValueVM {
			get { return win32VersionValueVM; }
		}
		readonly UInt32HexField win32VersionValueVM;

		public UInt32HexField SizeOfImageVM {
			get { return sizeOfImageVM; }
		}
		readonly UInt32HexField sizeOfImageVM;

		public UInt32HexField SizeOfHeadersVM {
			get { return sizeOfHeadersVM; }
		}
		readonly UInt32HexField sizeOfHeadersVM;

		public UInt32HexField CheckSumVM {
			get { return checkSumVM; }
		}
		readonly UInt32HexField checkSumVM;

		public UInt16FlagsHexField SubsystemVM {
			get { return subsystemVM; }
		}
		readonly UInt16FlagsHexField subsystemVM;

		public UInt16FlagsHexField DllCharacteristicsVM {
			get { return dllCharacteristicsVM; }
		}
		readonly UInt16FlagsHexField dllCharacteristicsVM;

		public UInt32HexField LoaderFlagsVM {
			get { return loaderFlagsVM; }
		}
		readonly UInt32HexField loaderFlagsVM;

		public UInt32HexField NumberOfRvaAndSizesVM {
			get { return numberOfRvaAndSizesVM; }
		}
		readonly UInt32HexField numberOfRvaAndSizesVM;

		public DataDirVM DataDir0VM {
			get { return dataDir0VM; }
		}
		readonly DataDirVM dataDir0VM;

		public DataDirVM DataDir1VM {
			get { return dataDir1VM; }
		}
		readonly DataDirVM dataDir1VM;

		public DataDirVM DataDir2VM {
			get { return dataDir2VM; }
		}
		readonly DataDirVM dataDir2VM;

		public DataDirVM DataDir3VM {
			get { return dataDir3VM; }
		}
		readonly DataDirVM dataDir3VM;

		public DataDirVM DataDir4VM {
			get { return dataDir4VM; }
		}
		readonly DataDirVM dataDir4VM;

		public DataDirVM DataDir5VM {
			get { return dataDir5VM; }
		}
		readonly DataDirVM dataDir5VM;

		public DataDirVM DataDir6VM {
			get { return dataDir6VM; }
		}
		readonly DataDirVM dataDir6VM;

		public DataDirVM DataDir7VM {
			get { return dataDir7VM; }
		}
		readonly DataDirVM dataDir7VM;

		public DataDirVM DataDir8VM {
			get { return dataDir8VM; }
		}
		readonly DataDirVM dataDir8VM;

		public DataDirVM DataDir9VM {
			get { return dataDir9VM; }
		}
		readonly DataDirVM dataDir9VM;

		public DataDirVM DataDir10VM {
			get { return dataDir10VM; }
		}
		readonly DataDirVM dataDir10VM;

		public DataDirVM DataDir11VM {
			get { return dataDir11VM; }
		}
		readonly DataDirVM dataDir11VM;

		public DataDirVM DataDir12VM {
			get { return dataDir12VM; }
		}
		readonly DataDirVM dataDir12VM;

		public DataDirVM DataDir13VM {
			get { return dataDir13VM; }
		}
		readonly DataDirVM dataDir13VM;

		public DataDirVM DataDir14VM {
			get { return dataDir14VM; }
		}
		readonly DataDirVM dataDir14VM;

		public DataDirVM DataDir15VM {
			get { return dataDir15VM; }
		}
		readonly DataDirVM dataDir15VM;

		public override IEnumerable<HexField> HexFields {
			get { return hexFields; }
		}
		HexField[] hexFields;

		static readonly IntegerHexBitFieldEnumInfo[] SubsystemInfos = new IntegerHexBitFieldEnumInfo[] {
			new IntegerHexBitFieldEnumInfo(0, "Unknown"),
			new IntegerHexBitFieldEnumInfo(1, "Native"),
			new IntegerHexBitFieldEnumInfo(2, "WindowsGui"),
			new IntegerHexBitFieldEnumInfo(3, "WindowsCui"),
			new IntegerHexBitFieldEnumInfo(5, "Os2Cui"),
			new IntegerHexBitFieldEnumInfo(7, "PosixCui"),
			new IntegerHexBitFieldEnumInfo(8, "NativeWindows"),
			new IntegerHexBitFieldEnumInfo(9, "WindowsCeGui"),
			new IntegerHexBitFieldEnumInfo(10, "EfiApplication"),
			new IntegerHexBitFieldEnumInfo(11, "EfiBootServiceDriver"),
			new IntegerHexBitFieldEnumInfo(12, "EfiRuntimeDriver"),
			new IntegerHexBitFieldEnumInfo(13, "EfiRom"),
			new IntegerHexBitFieldEnumInfo(14, "Xbox"),
			new IntegerHexBitFieldEnumInfo(16, "WindowsBootApplication"),
		};

		protected ImageOptionalHeaderVM(HexDocument doc, ulong startOffset, ulong endOffset, ulong offs1, ulong offs2) {
			this.magicVM = new UInt16HexField(doc, Name, "Magic", startOffset + 0);
			this.majorLinkerVersionVM = new ByteHexField(doc, Name, "MajorLinkerVersion", startOffset + 2);
			this.minorLinkerVersionVM = new ByteHexField(doc, Name, "MinorLinkerVersion", startOffset + 3);
			this.sizeOfCodeVM = new UInt32HexField(doc, Name, "SizeOfCode", startOffset + 4);
			this.sizeOfInitializedDataVM = new UInt32HexField(doc, Name, "SizeOfInitializedData", startOffset + 8);
			this.sizeOfUninitializedDataVM = new UInt32HexField(doc, Name, "SizeOfUninitializedData", startOffset + 0x0C);
			this.addressOfEntryPointVM = new UInt32HexField(doc, Name, "AddressOfEntryPoint", startOffset + 0x10);
			this.baseOfCodeVM = new UInt32HexField(doc, Name, "BaseOfCode", startOffset + 0x14);

			this.sectionAlignmentVM = new UInt32HexField(doc, Name, "SectionAlignment", startOffset + offs1 + 0);
			this.fileAlignmentVM = new UInt32HexField(doc, Name, "FileAlignment", startOffset + offs1 + 4);
			this.majorOperatingSystemVersionVM = new UInt16HexField(doc, Name, "MajorOperatingSystemVersion", startOffset + offs1 + 8);
			this.minorOperatingSystemVersionVM = new UInt16HexField(doc, Name, "MinorOperatingSystemVersion", startOffset + offs1 + 0x0A);
			this.majorImageVersionVM = new UInt16HexField(doc, Name, "MajorImageVersion", startOffset + offs1 + 0x0C);
			this.minorImageVersionVM = new UInt16HexField(doc, Name, "MinorImageVersion", startOffset + offs1 + 0x0E);
			this.majorSubsystemVersionVM = new UInt16HexField(doc, Name, "MajorSubsystemVersion", startOffset + offs1 + 0x10);
			this.minorSubsystemVersionVM = new UInt16HexField(doc, Name, "MinorSubsystemVersion", startOffset + offs1 + 0x12);
			this.win32VersionValueVM = new UInt32HexField(doc, Name, "Win32VersionValue", startOffset + offs1 + 0x14);
			this.sizeOfImageVM = new UInt32HexField(doc, Name, "SizeOfImage", startOffset + offs1 + 0x18);
			this.sizeOfHeadersVM = new UInt32HexField(doc, Name, "SizeOfHeaders", startOffset + offs1 + 0x1C);
			this.checkSumVM = new UInt32HexField(doc, Name, "CheckSum", startOffset + offs1 + 0x20);
			this.subsystemVM = new UInt16FlagsHexField(doc, Name, "Subsystem", startOffset + offs1 + 0x24);
			this.subsystemVM.Add(new IntegerHexBitField("Subsystem", 0, 16, SubsystemInfos));
			this.dllCharacteristicsVM = new UInt16FlagsHexField(doc, Name, "DllCharacteristics", startOffset + offs1 + 0x26);
			this.dllCharacteristicsVM.Add(new BooleanHexBitField("Reserved1", 0));
			this.dllCharacteristicsVM.Add(new BooleanHexBitField("Reserved2", 1));
			this.dllCharacteristicsVM.Add(new BooleanHexBitField("Reserved3", 2));
			this.dllCharacteristicsVM.Add(new BooleanHexBitField("Reserved4", 3));
			this.dllCharacteristicsVM.Add(new BooleanHexBitField("Reserved5", 4));
			this.dllCharacteristicsVM.Add(new BooleanHexBitField("High Entropy VA", 5));
			this.dllCharacteristicsVM.Add(new BooleanHexBitField("Dynamic Base", 6));
			this.dllCharacteristicsVM.Add(new BooleanHexBitField("Force Integrity", 7));
			this.dllCharacteristicsVM.Add(new BooleanHexBitField("NX Compat", 8));
			this.dllCharacteristicsVM.Add(new BooleanHexBitField("No Isolation", 9));
			this.dllCharacteristicsVM.Add(new BooleanHexBitField("No SEH", 10));
			this.dllCharacteristicsVM.Add(new BooleanHexBitField("No Bind", 11));
			this.dllCharacteristicsVM.Add(new BooleanHexBitField("AppContainer", 12));
			this.dllCharacteristicsVM.Add(new BooleanHexBitField("WDM Driver", 13));
			this.dllCharacteristicsVM.Add(new BooleanHexBitField("Guard CF", 14));
			this.dllCharacteristicsVM.Add(new BooleanHexBitField("Terminal Server Aware", 15));
			this.loaderFlagsVM = new UInt32HexField(doc, Name, "LoaderFlags", startOffset + offs2 + 0);
			this.numberOfRvaAndSizesVM = new UInt32HexField(doc, Name, "NumberOfRvaAndSizes", startOffset + offs2 + 4);

			ulong doffs = offs2 + 8;
			this.dataDir0VM = new DataDirVM(doc, Name, "Export", startOffset + doffs + 0);
			this.dataDir1VM = new DataDirVM(doc, Name, "Import", startOffset + doffs + 8);
			this.dataDir2VM = new DataDirVM(doc, Name, "Resource", startOffset + doffs + 0x10);
			this.dataDir3VM = new DataDirVM(doc, Name, "Exception", startOffset + doffs + 0x18);
			this.dataDir4VM = new DataDirVM(doc, Name, "Security", startOffset + doffs + 0x20);
			this.dataDir5VM = new DataDirVM(doc, Name, "Base Reloc", startOffset + doffs + 0x28);
			this.dataDir6VM = new DataDirVM(doc, Name, "Debug", startOffset + doffs + 0x30);
			this.dataDir7VM = new DataDirVM(doc, Name, "Architecture", startOffset + doffs + 0x38);
			this.dataDir8VM = new DataDirVM(doc, Name, "Global Ptr", startOffset + doffs + 0x40);
			this.dataDir9VM = new DataDirVM(doc, Name, "TLS", startOffset + doffs + 0x48);
			this.dataDir10VM = new DataDirVM(doc, Name, "Load Config", startOffset + doffs + 0x50);
			this.dataDir11VM = new DataDirVM(doc, Name, "Bound Import", startOffset + doffs + 0x58);
			this.dataDir12VM = new DataDirVM(doc, Name, "IAT", startOffset + doffs + 0x60);
			this.dataDir13VM = new DataDirVM(doc, Name, "Delay Import", startOffset + doffs + 0x68);
			this.dataDir14VM = new DataDirVM(doc, Name, ".NET", startOffset + doffs + 0x70);
			this.dataDir15VM = new DataDirVM(doc, Name, "Reserved15", startOffset + doffs + 0x78);
		}

		protected void AddDataDirs(List<HexField> fields, ulong endOffset) {
			var dataDirs = new DataDirVM[16] {
				this.dataDir0VM,
				this.dataDir1VM,
				this.dataDir2VM,
				this.dataDir3VM,
				this.dataDir4VM,
				this.dataDir5VM,
				this.dataDir6VM,
				this.dataDir7VM,
				this.dataDir8VM,
				this.dataDir9VM,
				this.dataDir10VM,
				this.dataDir11VM,
				this.dataDir12VM,
				this.dataDir13VM,
				this.dataDir14VM,
				this.dataDir15VM,
			};

			ulong offs = dataDirs[0].RVAVM.StartOffset;
			for (int i = 0; i < dataDirs.Length; i++, offs += 8) {
				if (offs + 7 <= endOffset) {
					fields.Add(dataDirs[i].RVAVM);
					fields.Add(dataDirs[i].SizeVM);
				}
				else
					dataDirs[i].IsVisible = false;
			}

			this.hexFields = fields.ToArray();
		}
	}
}
