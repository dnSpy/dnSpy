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
using dnSpy.AsmEditor.Properties;
using dnSpy.Contracts.Hex;

namespace dnSpy.AsmEditor.Hex.Nodes {
	abstract class ImageOptionalHeaderVM : HexVM {
		public abstract bool Is32Bit { get; }
		public UInt16HexField MagicVM { get; }
		public ByteHexField MajorLinkerVersionVM { get; }
		public ByteHexField MinorLinkerVersionVM { get; }
		public UInt32HexField SizeOfCodeVM { get; }
		public UInt32HexField SizeOfInitializedDataVM { get; }
		public UInt32HexField SizeOfUninitializedDataVM { get; }
		public UInt32HexField AddressOfEntryPointVM { get; }
		public UInt32HexField BaseOfCodeVM { get; }
		public UInt32HexField SectionAlignmentVM { get; }
		public UInt32HexField FileAlignmentVM { get; }
		public UInt16HexField MajorOperatingSystemVersionVM { get; }
		public UInt16HexField MinorOperatingSystemVersionVM { get; }
		public UInt16HexField MajorImageVersionVM { get; }
		public UInt16HexField MinorImageVersionVM { get; }
		public UInt16HexField MajorSubsystemVersionVM { get; }
		public UInt16HexField MinorSubsystemVersionVM { get; }
		public UInt32HexField Win32VersionValueVM { get; }
		public UInt32HexField SizeOfImageVM { get; }
		public UInt32HexField SizeOfHeadersVM { get; }
		public UInt32HexField CheckSumVM { get; }
		public UInt16FlagsHexField SubsystemVM { get; }
		public UInt16FlagsHexField DllCharacteristicsVM { get; }
		public UInt32HexField LoaderFlagsVM { get; }
		public UInt32HexField NumberOfRvaAndSizesVM { get; }
		public DataDirVM DataDir0VM { get; }
		public DataDirVM DataDir1VM { get; }
		public DataDirVM DataDir2VM { get; }
		public DataDirVM DataDir3VM { get; }
		public DataDirVM DataDir4VM { get; }
		public DataDirVM DataDir5VM { get; }
		public DataDirVM DataDir6VM { get; }
		public DataDirVM DataDir7VM { get; }
		public DataDirVM DataDir8VM { get; }
		public DataDirVM DataDir9VM { get; }
		public DataDirVM DataDir10VM { get; }
		public DataDirVM DataDir11VM { get; }
		public DataDirVM DataDir12VM { get; }
		public DataDirVM DataDir13VM { get; }
		public DataDirVM DataDir14VM { get; }
		public DataDirVM DataDir15VM { get; }

		public override IEnumerable<HexField> HexFields => hexFields;
		HexField[] hexFields;

		static readonly IntegerHexBitFieldEnumInfo[] SubsystemInfos = new IntegerHexBitFieldEnumInfo[] {
			new IntegerHexBitFieldEnumInfo(0, dnSpy_AsmEditor_Resources.Unknown),
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

		protected ImageOptionalHeaderVM(object owner, HexBuffer buffer, HexPosition startOffset, HexPosition endOffset, ulong offs1, ulong offs2)
			: base(owner) {
			MagicVM = new UInt16HexField(buffer, Name, "Magic", startOffset + 0);
			MajorLinkerVersionVM = new ByteHexField(buffer, Name, "MajorLinkerVersion", startOffset + 2, true);
			MinorLinkerVersionVM = new ByteHexField(buffer, Name, "MinorLinkerVersion", startOffset + 3, true);
			SizeOfCodeVM = new UInt32HexField(buffer, Name, "SizeOfCode", startOffset + 4);
			SizeOfInitializedDataVM = new UInt32HexField(buffer, Name, "SizeOfInitializedData", startOffset + 8);
			SizeOfUninitializedDataVM = new UInt32HexField(buffer, Name, "SizeOfUninitializedData", startOffset + 0x0C);
			AddressOfEntryPointVM = new UInt32HexField(buffer, Name, "AddressOfEntryPoint", startOffset + 0x10);
			BaseOfCodeVM = new UInt32HexField(buffer, Name, "BaseOfCode", startOffset + 0x14);

			SectionAlignmentVM = new UInt32HexField(buffer, Name, "SectionAlignment", startOffset + offs1 + 0);
			FileAlignmentVM = new UInt32HexField(buffer, Name, "FileAlignment", startOffset + offs1 + 4);
			MajorOperatingSystemVersionVM = new UInt16HexField(buffer, Name, "MajorOperatingSystemVersion", startOffset + offs1 + 8, true);
			MinorOperatingSystemVersionVM = new UInt16HexField(buffer, Name, "MinorOperatingSystemVersion", startOffset + offs1 + 0x0A, true);
			MajorImageVersionVM = new UInt16HexField(buffer, Name, "MajorImageVersion", startOffset + offs1 + 0x0C, true);
			MinorImageVersionVM = new UInt16HexField(buffer, Name, "MinorImageVersion", startOffset + offs1 + 0x0E, true);
			MajorSubsystemVersionVM = new UInt16HexField(buffer, Name, "MajorSubsystemVersion", startOffset + offs1 + 0x10, true);
			MinorSubsystemVersionVM = new UInt16HexField(buffer, Name, "MinorSubsystemVersion", startOffset + offs1 + 0x12, true);
			Win32VersionValueVM = new UInt32HexField(buffer, Name, "Win32VersionValue", startOffset + offs1 + 0x14, true);
			SizeOfImageVM = new UInt32HexField(buffer, Name, "SizeOfImage", startOffset + offs1 + 0x18);
			SizeOfHeadersVM = new UInt32HexField(buffer, Name, "SizeOfHeaders", startOffset + offs1 + 0x1C);
			CheckSumVM = new UInt32HexField(buffer, Name, "CheckSum", startOffset + offs1 + 0x20);
			SubsystemVM = new UInt16FlagsHexField(buffer, Name, "Subsystem", startOffset + offs1 + 0x24);
			SubsystemVM.Add(new IntegerHexBitField("Subsystem", 0, 16, SubsystemInfos));
			DllCharacteristicsVM = new UInt16FlagsHexField(buffer, Name, "DllCharacteristics", startOffset + offs1 + 0x26);
			DllCharacteristicsVM.Add(new BooleanHexBitField("Reserved1", 0));
			DllCharacteristicsVM.Add(new BooleanHexBitField("Reserved2", 1));
			DllCharacteristicsVM.Add(new BooleanHexBitField("Reserved3", 2));
			DllCharacteristicsVM.Add(new BooleanHexBitField("Reserved4", 3));
			DllCharacteristicsVM.Add(new BooleanHexBitField("Reserved5", 4));
			DllCharacteristicsVM.Add(new BooleanHexBitField("High Entropy VA", 5));
			DllCharacteristicsVM.Add(new BooleanHexBitField("Dynamic Base", 6));
			DllCharacteristicsVM.Add(new BooleanHexBitField("Force Integrity", 7));
			DllCharacteristicsVM.Add(new BooleanHexBitField("NX Compat", 8));
			DllCharacteristicsVM.Add(new BooleanHexBitField("No Isolation", 9));
			DllCharacteristicsVM.Add(new BooleanHexBitField("No SEH", 10));
			DllCharacteristicsVM.Add(new BooleanHexBitField("No Bind", 11));
			DllCharacteristicsVM.Add(new BooleanHexBitField("AppContainer", 12));
			DllCharacteristicsVM.Add(new BooleanHexBitField("WDM Driver", 13));
			DllCharacteristicsVM.Add(new BooleanHexBitField("Guard CF", 14));
			DllCharacteristicsVM.Add(new BooleanHexBitField("Terminal Server Aware", 15));
			LoaderFlagsVM = new UInt32HexField(buffer, Name, "LoaderFlags", startOffset + offs2 + 0);
			NumberOfRvaAndSizesVM = new UInt32HexField(buffer, Name, "NumberOfRvaAndSizes", startOffset + offs2 + 4);

			ulong doffs = offs2 + 8;
			DataDir0VM = new DataDirVM(buffer, Name, "Export", startOffset + doffs + 0);
			DataDir1VM = new DataDirVM(buffer, Name, "Import", startOffset + doffs + 8);
			DataDir2VM = new DataDirVM(buffer, Name, "Resource", startOffset + doffs + 0x10);
			DataDir3VM = new DataDirVM(buffer, Name, "Exception", startOffset + doffs + 0x18);
			DataDir4VM = new DataDirVM(buffer, Name, "Security", startOffset + doffs + 0x20);
			DataDir5VM = new DataDirVM(buffer, Name, "Base Reloc", startOffset + doffs + 0x28);
			DataDir6VM = new DataDirVM(buffer, Name, "Debug", startOffset + doffs + 0x30);
			DataDir7VM = new DataDirVM(buffer, Name, "Architecture", startOffset + doffs + 0x38);
			DataDir8VM = new DataDirVM(buffer, Name, "Global Ptr", startOffset + doffs + 0x40);
			DataDir9VM = new DataDirVM(buffer, Name, "TLS", startOffset + doffs + 0x48);
			DataDir10VM = new DataDirVM(buffer, Name, "Load Config", startOffset + doffs + 0x50);
			DataDir11VM = new DataDirVM(buffer, Name, "Bound Import", startOffset + doffs + 0x58);
			DataDir12VM = new DataDirVM(buffer, Name, "IAT", startOffset + doffs + 0x60);
			DataDir13VM = new DataDirVM(buffer, Name, "Delay Import", startOffset + doffs + 0x68);
			DataDir14VM = new DataDirVM(buffer, Name, ".NET", startOffset + doffs + 0x70);
			DataDir15VM = new DataDirVM(buffer, Name, "Reserved15", startOffset + doffs + 0x78);
		}

		protected void AddDataDirs(List<HexField> fields, HexPosition end) {
			var dataDirs = new DataDirVM[16] {
				DataDir0VM,
				DataDir1VM,
				DataDir2VM,
				DataDir3VM,
				DataDir4VM,
				DataDir5VM,
				DataDir6VM,
				DataDir7VM,
				DataDir8VM,
				DataDir9VM,
				DataDir10VM,
				DataDir11VM,
				DataDir12VM,
				DataDir13VM,
				DataDir14VM,
				DataDir15VM,
			};

			var position = dataDirs[0].RVAVM.Span.Start;
			for (int i = 0; i < dataDirs.Length; i++) {
				var nextPosition = position + 8;
				if (nextPosition <= end) {
					fields.Add(dataDirs[i].RVAVM);
					fields.Add(dataDirs[i].SizeVM);
				}
				else
					dataDirs[i].IsVisible = false;
				position = nextPosition;
			}

			hexFields = fields.ToArray();
		}
	}
}
