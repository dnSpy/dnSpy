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

using System;
using System.Collections.ObjectModel;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Files;
using dnSpy.Contracts.Hex.Files.PE;

namespace dnSpy.Hex.Files.PE {
	sealed class PeFileHeaderDataImpl : PeFileHeaderData {
		public override StructField<UInt16EnumData> Machine { get; }
		public override StructField<UInt16Data> NumberOfSections { get; }
		public override StructField<UnixTime32Data> TimeDateStamp { get; }
		public override StructField<FileOffsetData> PointerToSymbolTable { get; }
		public override StructField<UInt32Data> NumberOfSymbols { get; }
		public override StructField<UInt16Data> SizeOfOptionalHeader { get; }
		public override StructField<UInt16FlagsData> Characteristics { get; }

		protected override BufferField[] Fields { get; }

		static readonly ReadOnlyCollection<EnumFieldInfo> machineEnumFieldInfos = new ReadOnlyCollection<EnumFieldInfo>(new EnumFieldInfo[] {
			new EnumFieldInfo(0x014C, "I386"),
			new EnumFieldInfo(0x8664, "AMD64"),
			new EnumFieldInfo(0x0200, "IA64"),
			new EnumFieldInfo(0x01C4, "ARMNT"),
			new EnumFieldInfo(0xAA64, "ARM64"),
			new EnumFieldInfo(0x0000, "UNKNOWN"),
			new EnumFieldInfo(0x0162, "R3000"),
			new EnumFieldInfo(0x0166, "R4000"),
			new EnumFieldInfo(0x0168, "R10000"),
			new EnumFieldInfo(0x0169, "WCEMIPSV2"),
			new EnumFieldInfo(0x0184, "ALPHA"),
			new EnumFieldInfo(0x01A2, "SH3"),
			new EnumFieldInfo(0x01A3, "SH3DSP"),
			new EnumFieldInfo(0x01A4, "SH3E"),
			new EnumFieldInfo(0x01A6, "SH4"),
			new EnumFieldInfo(0x01A8, "SH5"),
			new EnumFieldInfo(0x01C0, "ARM"),
			new EnumFieldInfo(0x01C2, "THUMB"),
			new EnumFieldInfo(0x01D3, "AM33"),
			new EnumFieldInfo(0x01F0, "POWERPC"),
			new EnumFieldInfo(0x01F1, "POWERPCFP"),
			new EnumFieldInfo(0x0266, "MIPS16"),
			new EnumFieldInfo(0x0284, "ALPHA64"),
			new EnumFieldInfo(0x0366, "MIPSFPU"),
			new EnumFieldInfo(0x0466, "MIPSFPU16"),
			new EnumFieldInfo(0x0520, "TRICORE"),
			new EnumFieldInfo(0x0CEF, "CEF"),
			new EnumFieldInfo(0x0EBC, "EBC"),
			new EnumFieldInfo(0x9041, "M32R"),
			new EnumFieldInfo(0xC0EE, "CEE"),
		});

		static readonly ReadOnlyCollection<FlagInfo> characteristicsFlagInfos = new ReadOnlyCollection<FlagInfo>(new FlagInfo[] {
			new FlagInfo(0x0001, "RELOCS_STRIPPED"),
			new FlagInfo(0x0002, "EXECUTABLE_IMAGE"),
			new FlagInfo(0x0004, "LINE_NUMS_STRIPPED"),
			new FlagInfo(0x0008, "LOCAL_SYMS_STRIPPED"),
			new FlagInfo(0x0010, "AGGRESIVE_WS_TRIM"),
			new FlagInfo(0x0020, "LARGE_ADDRESS_AWARE"),
			new FlagInfo(0x0040, "RESERVED"),
			new FlagInfo(0x0080, "BYTES_REVERSED_LO"),
			new FlagInfo(0x0100, "32BIT_MACHINE"),
			new FlagInfo(0x0200, "DEBUG_STRIPPED"),
			new FlagInfo(0x0400, "REMOVABLE_RUN_FROM_SWAP"),
			new FlagInfo(0x0800, "NET_RUN_FROM_SWAP"),
			new FlagInfo(0x1000, "SYSTEM"),
			new FlagInfo(0x2000, "DLL"),
			new FlagInfo(0x4000, "UP_SYSTEM_ONLY"),
			new FlagInfo(0x8000, "BYTES_REVERSED_HI"),
		});

		PeFileHeaderDataImpl(HexBufferSpan span)
			: base(span) {
			var buffer = span.Buffer;
			var pos = span.Start.Position;
			Machine = new StructField<UInt16EnumData>("Machine", new UInt16EnumData(buffer, pos, machineEnumFieldInfos));
			NumberOfSections = new StructField<UInt16Data>("NumberOfSections", new UInt16Data(buffer, pos + 2));
			TimeDateStamp = new StructField<UnixTime32Data>("TimeDateStamp", new UnixTime32Data(buffer, pos + 4));
			PointerToSymbolTable = new StructField<FileOffsetData>("PointerToSymbolTable", new FileOffsetData(buffer, pos + 8));
			NumberOfSymbols = new StructField<UInt32Data>("NumberOfSymbols", new UInt32Data(buffer, pos + 0x0C));
			SizeOfOptionalHeader = new StructField<UInt16Data>("SizeOfOptionalHeader", new UInt16Data(buffer, pos + 0x10));
			Characteristics = new StructField<UInt16FlagsData>("Characteristics", new UInt16FlagsData(buffer, pos + 0x12, characteristicsFlagInfos));
			Fields = new StructField[] {
				Machine,
				NumberOfSections,
				TimeDateStamp,
				PointerToSymbolTable,
				NumberOfSymbols,
				SizeOfOptionalHeader,
				Characteristics,
			};
		}

		public static PeFileHeaderData TryCreate(HexBufferFile file, HexPosition position) {
			if (file == null)
				throw new ArgumentNullException(nameof(file));
			if (!file.Span.Contains(position) || !file.Span.Contains(position + 0x14 - 1))
				return null;
			return new PeFileHeaderDataImpl(new HexBufferSpan(file.Buffer, new HexSpan(position, 0x14)));
		}
	}
}
