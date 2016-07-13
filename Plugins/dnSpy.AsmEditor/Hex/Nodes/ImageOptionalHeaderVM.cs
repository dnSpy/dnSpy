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
using dnSpy.Contracts.HexEditor;

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

		protected ImageOptionalHeaderVM(object owner, HexDocument doc, ulong startOffset, ulong endOffset, ulong offs1, ulong offs2)
			: base(owner) {
			this.MagicVM = new UInt16HexField(doc, Name, "Magic", startOffset + 0);
			this.MajorLinkerVersionVM = new ByteHexField(doc, Name, "MajorLinkerVersion", startOffset + 2, true);
			this.MinorLinkerVersionVM = new ByteHexField(doc, Name, "MinorLinkerVersion", startOffset + 3, true);
			this.SizeOfCodeVM = new UInt32HexField(doc, Name, "SizeOfCode", startOffset + 4);
			this.SizeOfInitializedDataVM = new UInt32HexField(doc, Name, "SizeOfInitializedData", startOffset + 8);
			this.SizeOfUninitializedDataVM = new UInt32HexField(doc, Name, "SizeOfUninitializedData", startOffset + 0x0C);
			this.AddressOfEntryPointVM = new UInt32HexField(doc, Name, "AddressOfEntryPoint", startOffset + 0x10);
			this.BaseOfCodeVM = new UInt32HexField(doc, Name, "BaseOfCode", startOffset + 0x14);

			this.SectionAlignmentVM = new UInt32HexField(doc, Name, "SectionAlignment", startOffset + offs1 + 0);
			this.FileAlignmentVM = new UInt32HexField(doc, Name, "FileAlignment", startOffset + offs1 + 4);
			this.MajorOperatingSystemVersionVM = new UInt16HexField(doc, Name, "MajorOperatingSystemVersion", startOffset + offs1 + 8, true);
			this.MinorOperatingSystemVersionVM = new UInt16HexField(doc, Name, "MinorOperatingSystemVersion", startOffset + offs1 + 0x0A, true);
			this.MajorImageVersionVM = new UInt16HexField(doc, Name, "MajorImageVersion", startOffset + offs1 + 0x0C, true);
			this.MinorImageVersionVM = new UInt16HexField(doc, Name, "MinorImageVersion", startOffset + offs1 + 0x0E, true);
			this.MajorSubsystemVersionVM = new UInt16HexField(doc, Name, "MajorSubsystemVersion", startOffset + offs1 + 0x10, true);
			this.MinorSubsystemVersionVM = new UInt16HexField(doc, Name, "MinorSubsystemVersion", startOffset + offs1 + 0x12, true);
			this.Win32VersionValueVM = new UInt32HexField(doc, Name, "Win32VersionValue", startOffset + offs1 + 0x14, true);
			this.SizeOfImageVM = new UInt32HexField(doc, Name, "SizeOfImage", startOffset + offs1 + 0x18);
			this.SizeOfHeadersVM = new UInt32HexField(doc, Name, "SizeOfHeaders", startOffset + offs1 + 0x1C);
			this.CheckSumVM = new UInt32HexField(doc, Name, "CheckSum", startOffset + offs1 + 0x20);
			this.SubsystemVM = new UInt16FlagsHexField(doc, Name, "Subsystem", startOffset + offs1 + 0x24);
			this.SubsystemVM.Add(new IntegerHexBitField("Subsystem", 0, 16, SubsystemInfos));
			this.DllCharacteristicsVM = new UInt16FlagsHexField(doc, Name, "DllCharacteristics", startOffset + offs1 + 0x26);
			this.DllCharacteristicsVM.Add(new BooleanHexBitField("Reserved1", 0));
			this.DllCharacteristicsVM.Add(new BooleanHexBitField("Reserved2", 1));
			this.DllCharacteristicsVM.Add(new BooleanHexBitField("Reserved3", 2));
			this.DllCharacteristicsVM.Add(new BooleanHexBitField("Reserved4", 3));
			this.DllCharacteristicsVM.Add(new BooleanHexBitField("Reserved5", 4));
			this.DllCharacteristicsVM.Add(new BooleanHexBitField("High Entropy VA", 5));
			this.DllCharacteristicsVM.Add(new BooleanHexBitField("Dynamic Base", 6));
			this.DllCharacteristicsVM.Add(new BooleanHexBitField("Force Integrity", 7));
			this.DllCharacteristicsVM.Add(new BooleanHexBitField("NX Compat", 8));
			this.DllCharacteristicsVM.Add(new BooleanHexBitField("No Isolation", 9));
			this.DllCharacteristicsVM.Add(new BooleanHexBitField("No SEH", 10));
			this.DllCharacteristicsVM.Add(new BooleanHexBitField("No Bind", 11));
			this.DllCharacteristicsVM.Add(new BooleanHexBitField("AppContainer", 12));
			this.DllCharacteristicsVM.Add(new BooleanHexBitField("WDM Driver", 13));
			this.DllCharacteristicsVM.Add(new BooleanHexBitField("Guard CF", 14));
			this.DllCharacteristicsVM.Add(new BooleanHexBitField("Terminal Server Aware", 15));
			this.LoaderFlagsVM = new UInt32HexField(doc, Name, "LoaderFlags", startOffset + offs2 + 0);
			this.NumberOfRvaAndSizesVM = new UInt32HexField(doc, Name, "NumberOfRvaAndSizes", startOffset + offs2 + 4);

			ulong doffs = offs2 + 8;
			this.DataDir0VM = new DataDirVM(doc, Name, "Export", startOffset + doffs + 0);
			this.DataDir1VM = new DataDirVM(doc, Name, "Import", startOffset + doffs + 8);
			this.DataDir2VM = new DataDirVM(doc, Name, "Resource", startOffset + doffs + 0x10);
			this.DataDir3VM = new DataDirVM(doc, Name, "Exception", startOffset + doffs + 0x18);
			this.DataDir4VM = new DataDirVM(doc, Name, "Security", startOffset + doffs + 0x20);
			this.DataDir5VM = new DataDirVM(doc, Name, "Base Reloc", startOffset + doffs + 0x28);
			this.DataDir6VM = new DataDirVM(doc, Name, "Debug", startOffset + doffs + 0x30);
			this.DataDir7VM = new DataDirVM(doc, Name, "Architecture", startOffset + doffs + 0x38);
			this.DataDir8VM = new DataDirVM(doc, Name, "Global Ptr", startOffset + doffs + 0x40);
			this.DataDir9VM = new DataDirVM(doc, Name, "TLS", startOffset + doffs + 0x48);
			this.DataDir10VM = new DataDirVM(doc, Name, "Load Config", startOffset + doffs + 0x50);
			this.DataDir11VM = new DataDirVM(doc, Name, "Bound Import", startOffset + doffs + 0x58);
			this.DataDir12VM = new DataDirVM(doc, Name, "IAT", startOffset + doffs + 0x60);
			this.DataDir13VM = new DataDirVM(doc, Name, "Delay Import", startOffset + doffs + 0x68);
			this.DataDir14VM = new DataDirVM(doc, Name, ".NET", startOffset + doffs + 0x70);
			this.DataDir15VM = new DataDirVM(doc, Name, "Reserved15", startOffset + doffs + 0x78);
		}

		protected void AddDataDirs(List<HexField> fields, ulong endOffset) {
			var dataDirs = new DataDirVM[16] {
				this.DataDir0VM,
				this.DataDir1VM,
				this.DataDir2VM,
				this.DataDir3VM,
				this.DataDir4VM,
				this.DataDir5VM,
				this.DataDir6VM,
				this.DataDir7VM,
				this.DataDir8VM,
				this.DataDir9VM,
				this.DataDir10VM,
				this.DataDir11VM,
				this.DataDir12VM,
				this.DataDir13VM,
				this.DataDir14VM,
				this.DataDir15VM,
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
