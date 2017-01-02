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
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Files;
using dnSpy.Contracts.Hex.Files.PE;

namespace dnSpy.Hex.Files.PE {
	sealed class PeOptionalHeader64DataImpl : PeOptionalHeader64Data {
		public override StructField<UInt16Data> Magic { get; }
		public override StructField<ByteData> MajorLinkerVersion { get; }
		public override StructField<ByteData> MinorLinkerVersion { get; }
		public override StructField<UInt32Data> SizeOfCode { get; }
		public override StructField<UInt32Data> SizeOfInitializedData { get; }
		public override StructField<UInt32Data> SizeOfUninitializedData { get; }
		public override StructField<RvaData> AddressOfEntryPoint { get; }
		public override StructField<RvaData> BaseOfCode { get; }
		public override StructField<UInt64Data> ImageBase { get; }
		public override StructField<UInt32Data> SectionAlignment { get; }
		public override StructField<UInt32Data> FileAlignment { get; }
		public override StructField<UInt16Data> MajorOperatingSystemVersion { get; }
		public override StructField<UInt16Data> MinorOperatingSystemVersion { get; }
		public override StructField<UInt16Data> MajorImageVersion { get; }
		public override StructField<UInt16Data> MinorImageVersion { get; }
		public override StructField<UInt16Data> MajorSubsystemVersion { get; }
		public override StructField<UInt16Data> MinorSubsystemVersion { get; }
		public override StructField<UInt32Data> Win32VersionValue { get; }
		public override StructField<UInt32Data> SizeOfImage { get; }
		public override StructField<UInt32Data> SizeOfHeaders { get; }
		public override StructField<UInt32Data> CheckSum { get; }
		public override StructField<UInt16EnumData> Subsystem { get; }
		public override StructField<UInt16FlagsData> DllCharacteristics { get; }
		public override StructField<UInt64Data> SizeOfStackReserve { get; }
		public override StructField<UInt64Data> SizeOfStackCommit { get; }
		public override StructField<UInt64Data> SizeOfHeapReserve { get; }
		public override StructField<UInt64Data> SizeOfHeapCommit { get; }
		public override StructField<UInt32Data> LoaderFlags { get; }
		public override StructField<UInt32Data> NumberOfRvaAndSizes { get; }
		public override StructField<ArrayData<DataDirectoryData>> DataDirectory { get; }

		protected override BufferField[] Fields { get; }

		PeOptionalHeader64DataImpl(HexBufferSpan span)
			: base(span) {
			var buffer = span.Buffer;
			var pos = span.Start.Position;
			Magic = new StructField<UInt16Data>("Magic", new UInt16Data(buffer, pos));
			MajorLinkerVersion = new StructField<ByteData>("MajorLinkerVersion", new ByteData(buffer, pos + 2));
			MinorLinkerVersion = new StructField<ByteData>("MinorLinkerVersion", new ByteData(buffer, pos + 3));
			SizeOfCode = new StructField<UInt32Data>("SizeOfCode", new UInt32Data(buffer, pos + 4));
			SizeOfInitializedData = new StructField<UInt32Data>("SizeOfInitializedData", new UInt32Data(buffer, pos + 8));
			SizeOfUninitializedData = new StructField<UInt32Data>("SizeOfUninitializedData", new UInt32Data(buffer, pos + 0x0C));
			AddressOfEntryPoint = new StructField<RvaData>("AddressOfEntryPoint", new RvaData(buffer, pos + 0x10));
			BaseOfCode = new StructField<RvaData>("BaseOfCode", new RvaData(buffer, pos + 0x14));
			ImageBase = new StructField<UInt64Data>("ImageBase", new UInt64Data(buffer, pos + 0x18));
			SectionAlignment = new StructField<UInt32Data>("SectionAlignment", new UInt32Data(buffer, pos + 0x20));
			FileAlignment = new StructField<UInt32Data>("FileAlignment", new UInt32Data(buffer, pos + 0x24));
			MajorOperatingSystemVersion = new StructField<UInt16Data>("MajorOperatingSystemVersion", new UInt16Data(buffer, pos + 0x28));
			MinorOperatingSystemVersion = new StructField<UInt16Data>("MinorOperatingSystemVersion", new UInt16Data(buffer, pos + 0x2A));
			MajorImageVersion = new StructField<UInt16Data>("MajorImageVersion", new UInt16Data(buffer, pos + 0x2C));
			MinorImageVersion = new StructField<UInt16Data>("MinorImageVersion", new UInt16Data(buffer, pos + 0x2E));
			MajorSubsystemVersion = new StructField<UInt16Data>("MajorSubsystemVersion", new UInt16Data(buffer, pos + 0x30));
			MinorSubsystemVersion = new StructField<UInt16Data>("MinorSubsystemVersion", new UInt16Data(buffer, pos + 0x32));
			Win32VersionValue = new StructField<UInt32Data>("Win32VersionValue", new UInt32Data(buffer, pos + 0x34));
			SizeOfImage = new StructField<UInt32Data>("SizeOfImage", new UInt32Data(buffer, pos + 0x38));
			SizeOfHeaders = new StructField<UInt32Data>("SizeOfHeaders", new UInt32Data(buffer, pos + 0x3C));
			CheckSum = new StructField<UInt32Data>("CheckSum", new UInt32Data(buffer, pos + 0x40));
			Subsystem = new StructField<UInt16EnumData>("Subsystem", new UInt16EnumData(buffer, pos + 0x44, PeOptionalHeader32DataImpl.subsystemEnumFieldInfos));
			DllCharacteristics = new StructField<UInt16FlagsData>("DllCharacteristics", new UInt16FlagsData(buffer, pos + 0x46, PeOptionalHeader32DataImpl.dllCharacteristicsFlagInfos));
			SizeOfStackReserve = new StructField<UInt64Data>("SizeOfStackReserve", new UInt64Data(buffer, pos + 0x48));
			SizeOfStackCommit = new StructField<UInt64Data>("SizeOfStackCommit", new UInt64Data(buffer, pos + 0x50));
			SizeOfHeapReserve = new StructField<UInt64Data>("SizeOfHeapReserve", new UInt64Data(buffer, pos + 0x58));
			SizeOfHeapCommit = new StructField<UInt64Data>("SizeOfHeapCommit", new UInt64Data(buffer, pos + 0x60));
			LoaderFlags = new StructField<UInt32Data>("LoaderFlags", new UInt32Data(buffer, pos + 0x68));
			NumberOfRvaAndSizes = new StructField<UInt32Data>("NumberOfRvaAndSizes", new UInt32Data(buffer, pos + 0x6C));
			DataDirectory = new StructField<ArrayData<DataDirectoryData>>("DataDirectory", PeOptionalHeader32DataImpl.CreateDataDirectoryArray(buffer, pos + 0x70, span.End));
			Fields = new StructField[] {
				Magic,
				MajorLinkerVersion,
				MinorLinkerVersion,
				SizeOfCode,
				SizeOfInitializedData,
				SizeOfUninitializedData,
				AddressOfEntryPoint,
				BaseOfCode,
				ImageBase,
				SectionAlignment,
				FileAlignment,
				MajorOperatingSystemVersion,
				MinorOperatingSystemVersion,
				MajorImageVersion,
				MinorImageVersion,
				MajorSubsystemVersion,
				MinorSubsystemVersion,
				Win32VersionValue,
				SizeOfImage,
				SizeOfHeaders,
				CheckSum,
				Subsystem,
				DllCharacteristics,
				SizeOfStackReserve,
				SizeOfStackCommit,
				SizeOfHeapReserve,
				SizeOfHeapCommit,
				LoaderFlags,
				NumberOfRvaAndSizes,
				DataDirectory,
			};
		}

		public static PeOptionalHeader64Data TryCreate(HexBufferFile file, HexPosition position, uint size) {
			if (file == null)
				throw new ArgumentNullException(nameof(file));
			if (size < 0x70)
				return null;
			size = Math.Min(0xF0, size & ~7U);
			if (!file.Span.Contains(position) || !file.Span.Contains(position + size - 1))
				return null;
			return new PeOptionalHeader64DataImpl(new HexBufferSpan(file.Buffer, new HexSpan(position, size)));
		}
	}
}
