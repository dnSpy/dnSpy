/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using dnSpy.Contracts.Hex.Files.PE;

namespace dnSpy.AsmEditor.Hex.PE {
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
		public DataDirectoryVM DataDir0VM { get; }
		public DataDirectoryVM DataDir1VM { get; }
		public DataDirectoryVM DataDir2VM { get; }
		public DataDirectoryVM DataDir3VM { get; }
		public DataDirectoryVM DataDir4VM { get; }
		public DataDirectoryVM DataDir5VM { get; }
		public DataDirectoryVM DataDir6VM { get; }
		public DataDirectoryVM DataDir7VM { get; }
		public DataDirectoryVM DataDir8VM { get; }
		public DataDirectoryVM DataDir9VM { get; }
		public DataDirectoryVM DataDir10VM { get; }
		public DataDirectoryVM DataDir11VM { get; }
		public DataDirectoryVM DataDir12VM { get; }
		public DataDirectoryVM DataDir13VM { get; }
		public DataDirectoryVM DataDir14VM { get; }
		public DataDirectoryVM DataDir15VM { get; }

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
			new IntegerHexBitFieldEnumInfo(17, "XboxCodeCatalog"),
		};

		protected ImageOptionalHeaderVM(HexBuffer buffer, PeOptionalHeaderData optionalHeader)
			: base(optionalHeader.Span) {
			hexFields = null!;
			MagicVM = new UInt16HexField(optionalHeader.Magic);
			MajorLinkerVersionVM = new ByteHexField(optionalHeader.MajorLinkerVersion, true);
			MinorLinkerVersionVM = new ByteHexField(optionalHeader.MinorLinkerVersion, true);
			SizeOfCodeVM = new UInt32HexField(optionalHeader.SizeOfCode);
			SizeOfInitializedDataVM = new UInt32HexField(optionalHeader.SizeOfInitializedData);
			SizeOfUninitializedDataVM = new UInt32HexField(optionalHeader.SizeOfUninitializedData);
			AddressOfEntryPointVM = new UInt32HexField(optionalHeader.AddressOfEntryPoint);
			BaseOfCodeVM = new UInt32HexField(optionalHeader.BaseOfCode);

			SectionAlignmentVM = new UInt32HexField(optionalHeader.SectionAlignment);
			FileAlignmentVM = new UInt32HexField(optionalHeader.FileAlignment);
			MajorOperatingSystemVersionVM = new UInt16HexField(optionalHeader.MajorOperatingSystemVersion, true);
			MinorOperatingSystemVersionVM = new UInt16HexField(optionalHeader.MinorOperatingSystemVersion, true);
			MajorImageVersionVM = new UInt16HexField(optionalHeader.MajorImageVersion, true);
			MinorImageVersionVM = new UInt16HexField(optionalHeader.MinorImageVersion, true);
			MajorSubsystemVersionVM = new UInt16HexField(optionalHeader.MajorSubsystemVersion, true);
			MinorSubsystemVersionVM = new UInt16HexField(optionalHeader.MinorSubsystemVersion, true);
			Win32VersionValueVM = new UInt32HexField(optionalHeader.Win32VersionValue, true);
			SizeOfImageVM = new UInt32HexField(optionalHeader.SizeOfImage);
			SizeOfHeadersVM = new UInt32HexField(optionalHeader.SizeOfHeaders);
			CheckSumVM = new UInt32HexField(optionalHeader.CheckSum);
			SubsystemVM = new UInt16FlagsHexField(optionalHeader.Subsystem);
			SubsystemVM.Add(new IntegerHexBitField("Subsystem", 0, 16, SubsystemInfos));
			DllCharacteristicsVM = new UInt16FlagsHexField(optionalHeader.DllCharacteristics);
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
			LoaderFlagsVM = new UInt32HexField(optionalHeader.LoaderFlags);
			NumberOfRvaAndSizesVM = new UInt32HexField(optionalHeader.NumberOfRvaAndSizes);

			DataDir0VM = Create(optionalHeader, 0, "Export");
			DataDir1VM = Create(optionalHeader, 1, "Import");
			DataDir2VM = Create(optionalHeader, 2, "Resource");
			DataDir3VM = Create(optionalHeader, 3, "Exception");
			DataDir4VM = Create(optionalHeader, 4, "Security");
			DataDir5VM = Create(optionalHeader, 5, "Base Reloc");
			DataDir6VM = Create(optionalHeader, 6, "Debug");
			DataDir7VM = Create(optionalHeader, 7, "Architecture");
			DataDir8VM = Create(optionalHeader, 8, "Global Ptr");
			DataDir9VM = Create(optionalHeader, 9, "TLS");
			DataDir10VM = Create(optionalHeader, 10, "Load Config");
			DataDir11VM = Create(optionalHeader, 11, "Bound Import");
			DataDir12VM = Create(optionalHeader, 12, "IAT");
			DataDir13VM = Create(optionalHeader, 13, "Delay Import");
			DataDir14VM = Create(optionalHeader, 14, ".NET");
			DataDir15VM = Create(optionalHeader, 15, "Reserved15");
		}

		static DataDirectoryVM Create(PeOptionalHeaderData optionalHeader, int index, string name) {
			if (index < optionalHeader.DataDirectory.Data.FieldCount)
				return new DataDirectoryVM(optionalHeader.DataDirectory.Data[index].Data, name);
			return DataDirectoryVM.CreateEmpty();
		}

		protected void AddDataDirs(List<HexField> fields, PeOptionalHeaderData optionalHeader) {
			var dataDirs = new DataDirectoryVM[16] {
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

			for (int i = 0; i < dataDirs.Length; i++) {
				if (i < optionalHeader.DataDirectory.Data.FieldCount) {
					fields.Add(dataDirs[i].RVAVM);
					fields.Add(dataDirs[i].SizeVM);
				}
				else
					dataDirs[i].IsVisible = false;
			}

			hexFields = fields.ToArray();
		}
	}
}
