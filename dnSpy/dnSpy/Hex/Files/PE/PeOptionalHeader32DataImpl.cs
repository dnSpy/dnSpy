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
	sealed class PeOptionalHeader32DataImpl : PeOptionalHeader32Data {
		public override StructField<UInt16Data> Magic { get; }
		public override StructField<ByteData> MajorLinkerVersion { get; }
		public override StructField<ByteData> MinorLinkerVersion { get; }
		public override StructField<UInt32Data> SizeOfCode { get; }
		public override StructField<UInt32Data> SizeOfInitializedData { get; }
		public override StructField<UInt32Data> SizeOfUninitializedData { get; }
		public override StructField<RvaData> AddressOfEntryPoint { get; }
		public override StructField<RvaData> BaseOfCode { get; }
		public override StructField<RvaData> BaseOfData { get; }
		public override StructField<UInt32Data> ImageBase { get; }
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
		public override StructField<UInt32Data> SizeOfStackReserve { get; }
		public override StructField<UInt32Data> SizeOfStackCommit { get; }
		public override StructField<UInt32Data> SizeOfHeapReserve { get; }
		public override StructField<UInt32Data> SizeOfHeapCommit { get; }
		public override StructField<UInt32Data> LoaderFlags { get; }
		public override StructField<UInt32Data> NumberOfRvaAndSizes { get; }
		public override StructField<ArrayData<DataDirectoryData>> DataDirectory { get; }

		protected override BufferField[] Fields { get; }

		internal static readonly ReadOnlyCollection<EnumFieldInfo> subsystemEnumFieldInfos = new ReadOnlyCollection<EnumFieldInfo>(new EnumFieldInfo[] {
			new EnumFieldInfo(0, "UNKNOWN"),
			new EnumFieldInfo(1, "NATIVE"),
			new EnumFieldInfo(2, "WINDOWS_GUI"),
			new EnumFieldInfo(3, "WINDOWS_CUI"),
			new EnumFieldInfo(5, "OS2_CUI"),
			new EnumFieldInfo(7, "POSIX_CUI"),
			new EnumFieldInfo(8, "NATIVE_WINDOWS"),
			new EnumFieldInfo(9, "WINDOWS_CE_GUI"),
			new EnumFieldInfo(10, "EFI_APPLICATION"),
			new EnumFieldInfo(11, "EFI_BOOT_SERVICE_DRIVER"),
			new EnumFieldInfo(12, "EFI_RUNTIME_DRIVER"),
			new EnumFieldInfo(13, "EFI_ROM"),
			new EnumFieldInfo(14, "XBOX"),
			new EnumFieldInfo(16, "WINDOWS_BOOT_APPLICATION"),
			new EnumFieldInfo(17, "XBOX_CODE_CATALOG"),
		});

		internal static readonly ReadOnlyCollection<FlagInfo> dllCharacteristicsFlagInfos = new ReadOnlyCollection<FlagInfo>(new FlagInfo[] {
			new FlagInfo(0x0001, "RESERVED0"),
			new FlagInfo(0x0002, "RESERVED1"),
			new FlagInfo(0x0004, "RESERVED2"),
			new FlagInfo(0x0008, "RESERVED3"),
			new FlagInfo(0x0010, "RESERVED4"),
			new FlagInfo(0x0020, "HIGH_ENTROPY_VA"),
			new FlagInfo(0x0040, "DYNAMIC_BASE"),
			new FlagInfo(0x0080, "FORCE_INTEGRITY"),
			new FlagInfo(0x0100, "NX_COMPAT"),
			new FlagInfo(0x0200, "NO_ISOLATION"),
			new FlagInfo(0x0400, "NO_SEH"),
			new FlagInfo(0x0800, "NO_BIND"),
			new FlagInfo(0x1000, "APPCONTAINER"),
			new FlagInfo(0x2000, "WDM_DRIVER"),
			new FlagInfo(0x4000, "GUARD_CF"),
			new FlagInfo(0x8000, "TERMINAL_SERVER_AWARE"),
		});

		PeOptionalHeader32DataImpl(HexBufferSpan span)
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
			BaseOfData = new StructField<RvaData>("BaseOfData", new RvaData(buffer, pos + 0x18));
			ImageBase = new StructField<UInt32Data>("ImageBase", new UInt32Data(buffer, pos + 0x1C));
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
			Subsystem = new StructField<UInt16EnumData>("Subsystem", new UInt16EnumData(buffer, pos + 0x44, subsystemEnumFieldInfos));
			DllCharacteristics = new StructField<UInt16FlagsData>("DllCharacteristics", new UInt16FlagsData(buffer, pos + 0x46, dllCharacteristicsFlagInfos));
			SizeOfStackReserve = new StructField<UInt32Data>("SizeOfStackReserve", new UInt32Data(buffer, pos + 0x48));
			SizeOfStackCommit = new StructField<UInt32Data>("SizeOfStackCommit", new UInt32Data(buffer, pos + 0x4C));
			SizeOfHeapReserve = new StructField<UInt32Data>("SizeOfHeapReserve", new UInt32Data(buffer, pos + 0x50));
			SizeOfHeapCommit = new StructField<UInt32Data>("SizeOfHeapCommit", new UInt32Data(buffer, pos + 0x54));
			LoaderFlags = new StructField<UInt32Data>("LoaderFlags", new UInt32Data(buffer, pos + 0x58));
			NumberOfRvaAndSizes = new StructField<UInt32Data>("NumberOfRvaAndSizes", new UInt32Data(buffer, pos + 0x5C));
			DataDirectory = new StructField<ArrayData<DataDirectoryData>>("DataDirectory", CreateDataDirectoryArray(buffer, pos + 0x60, span.End));
			Fields = new StructField[] {
				Magic,
				MajorLinkerVersion,
				MinorLinkerVersion,
				SizeOfCode,
				SizeOfInitializedData,
				SizeOfUninitializedData,
				AddressOfEntryPoint,
				BaseOfCode,
				BaseOfData,
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

		public static PeOptionalHeader32Data TryCreate(HexBufferFile file, HexPosition position, uint size) {
			if (file == null)
				throw new ArgumentNullException(nameof(file));
			if (size < 0x60)
				return null;
			size = Math.Min(0xE0, size & ~7U);
			if (!file.Span.Contains(position) || !file.Span.Contains(position + size - 1))
				return null;
			return new PeOptionalHeader32DataImpl(new HexBufferSpan(file.Buffer, new HexSpan(position, size)));
		}

		internal static ArrayData<DataDirectoryData> CreateDataDirectoryArray(HexBuffer buffer, HexPosition position, HexPosition end) {
			int count = (int)Math.Min((end - position).ToUInt64() / 8, 16);
			var fields = new ArrayField<DataDirectoryData>[count];
			var currPos = position;
			for (int i = 0; i < fields.Length; i++) {
				var field = new ArrayField<DataDirectoryData>(new DataDirectoryData(new HexBufferSpan(buffer, new HexSpan(currPos, 8))), (uint)i);
				fields[i] = field;
				currPos = field.Data.Span.End;
			}
			return new ArrayData<DataDirectoryData>(string.Empty, new HexBufferSpan(buffer, HexSpan.FromBounds(position, currPos)), fields);
		}
	}
}
