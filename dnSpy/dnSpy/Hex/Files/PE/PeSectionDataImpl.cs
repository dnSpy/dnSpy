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
using System.Text;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Files;
using dnSpy.Contracts.Hex.Files.PE;

namespace dnSpy.Hex.Files.PE {
	sealed class PeSectionDataImpl : PeSectionData {
		public override StructField<StringData> SectionName { get; }
		public override StructField<UInt32Data> VirtualSize { get; }
		public override StructField<RvaData> VirtualAddress { get; }
		public override StructField<UInt32Data> SizeOfRawData { get; }
		public override StructField<FileOffsetData> PointerToRawData { get; }
		public override StructField<FileOffsetData> PointerToRelocations { get; }
		public override StructField<FileOffsetData> PointerToLinenumbers { get; }
		public override StructField<UInt16Data> NumberOfRelocations { get; }
		public override StructField<UInt16Data> NumberOfLinenumbers { get; }
		public override StructField<UInt32FlagsData> Characteristics { get; }

		protected override BufferField[] Fields { get; }

		static readonly ReadOnlyCollection<FlagInfo> characteristicsFlagInfos = new ReadOnlyCollection<FlagInfo>(new FlagInfo[] {
			new FlagInfo(0x00000008, "TYPE_NO_PAD"),
			new FlagInfo(0x00000020, "CNT_CODE"),
			new FlagInfo(0x00000040, "CNT_INITIALIZED_DATA"),
			new FlagInfo(0x00000080, "CNT_UNINITIALIZED_DATA"),
			new FlagInfo(0x00000100, "LNK_OTHER"),
			new FlagInfo(0x00000200, "LNK_INFO"),
			new FlagInfo(0x00000800, "LNK_REMOVE"),
			new FlagInfo(0x00001000, "LNK_COMDAT"),
			new FlagInfo(0x00004000, "NO_DEFER_SPEC_EXC"),
			new FlagInfo(0x00008000, "GPREL"),
			new FlagInfo(0x00020000, "MEM_PURGEABLE"),
			new FlagInfo(0x00040000, "MEM_LOCKED"),
			new FlagInfo(0x00080000, "MEM_PRELOAD"),
			FlagInfo.CreateEnumName(0x00F00000, "ALIGNMENT"),
			new FlagInfo(0x00F00000, 0x00100000, "ALIGN_1BYTES"),
			new FlagInfo(0x00F00000, 0x00200000, "ALIGN_2BYTES"),
			new FlagInfo(0x00F00000, 0x00300000, "ALIGN_4BYTES"),
			new FlagInfo(0x00F00000, 0x00400000, "ALIGN_8BYTES"),
			new FlagInfo(0x00F00000, 0x00500000, "ALIGN_16BYTES"),
			new FlagInfo(0x00F00000, 0x00600000, "ALIGN_32BYTES"),
			new FlagInfo(0x00F00000, 0x00700000, "ALIGN_64BYTES"),
			new FlagInfo(0x00F00000, 0x00800000, "ALIGN_128BYTES"),
			new FlagInfo(0x00F00000, 0x00900000, "ALIGN_256BYTES"),
			new FlagInfo(0x00F00000, 0x00A00000, "ALIGN_512BYTES"),
			new FlagInfo(0x00F00000, 0x00B00000, "ALIGN_1024BYTES"),
			new FlagInfo(0x00F00000, 0x00C00000, "ALIGN_2048BYTES"),
			new FlagInfo(0x00F00000, 0x00D00000, "ALIGN_4096BYTES"),
			new FlagInfo(0x00F00000, 0x00E00000, "ALIGN_8192BYTES"),
			new FlagInfo(0x01000000, "LNK_NRELOC_OVFL"),
			new FlagInfo(0x02000000, "MEM_DISCARDABLE"),
			new FlagInfo(0x04000000, "MEM_NOT_CACHED"),
			new FlagInfo(0x08000000, "MEM_NOT_PAGED"),
			new FlagInfo(0x10000000, "MEM_SHARED"),
			new FlagInfo(0x20000000, "MEM_EXECUTE"),
			new FlagInfo(0x40000000, "MEM_READ"),
			new FlagInfo(0x80000000, "MEM_WRITE"),
		});

		PeSectionDataImpl(HexBufferSpan span)
			: base(span) {
			var buffer = span.Buffer;
			var pos = span.Start.Position;
			SectionName = new StructField<StringData>("Name", new StringData(buffer, pos, 8, Encoding.ASCII));
			VirtualSize = new StructField<UInt32Data>("VirtualSize", new UInt32Data(buffer, pos + 8));
			VirtualAddress = new StructField<RvaData>("VirtualAddress", new RvaData(buffer, pos + 0x0C));
			SizeOfRawData = new StructField<UInt32Data>("SizeOfRawData", new UInt32Data(buffer, pos + 0x10));
			PointerToRawData = new StructField<FileOffsetData>("PointerToRawData", new FileOffsetData(buffer, pos + 0x14));
			PointerToRelocations = new StructField<FileOffsetData>("PointerToRelocations", new FileOffsetData(buffer, pos + 0x18));
			PointerToLinenumbers = new StructField<FileOffsetData>("PointerToLinenumbers", new FileOffsetData(buffer, pos + 0x1C));
			NumberOfRelocations = new StructField<UInt16Data>("NumberOfRelocations", new UInt16Data(buffer, pos + 0x20));
			NumberOfLinenumbers = new StructField<UInt16Data>("NumberOfLinenumbers", new UInt16Data(buffer, pos + 0x22));
			Characteristics = new StructField<UInt32FlagsData>("Characteristics", new UInt32FlagsData(buffer, pos + 0x24, characteristicsFlagInfos));
			Fields = new BufferField[] {
				SectionName,
				VirtualSize,
				VirtualAddress,
				SizeOfRawData,
				PointerToRawData,
				PointerToRelocations,
				PointerToLinenumbers,
				NumberOfRelocations,
				NumberOfLinenumbers,
				Characteristics,
			};
		}

		public static PeSectionData TryCreate(HexBufferFile file, HexPosition position) {
			if (file == null)
				throw new ArgumentNullException(nameof(file));
			if (!file.Span.Contains(position) || !file.Span.Contains(position + 0x28 - 1))
				return null;
			return new PeSectionDataImpl(new HexBufferSpan(file.Buffer, new HexSpan(position, 0x28)));
		}
	}
}
