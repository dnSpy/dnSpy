/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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

namespace dnSpy.Contracts.Hex.Files.PE {
	/// <summary>
	/// Optional header
	/// </summary>
	public abstract class PeOptionalHeaderData : StructureData {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Structure name</param>
		/// <param name="span">Span</param>
		protected PeOptionalHeaderData(string name, HexBufferSpan span)
			: base(name, span) {
		}

		/// <summary>
		/// true if it's a <see cref="PeOptionalHeader32Data"/>, false if it's a <see cref="PeOptionalHeader64Data"/>
		/// </summary>
		public abstract bool Is32Bit { get; }

		/// <summary>IMAGE_OPTIONAL_HEADER.Magic</summary>
		public abstract StructField<UInt16Data> Magic { get; }
		/// <summary>IMAGE_OPTIONAL_HEADER.MajorLinkerVersion</summary>
		public abstract StructField<ByteData> MajorLinkerVersion { get; }
		/// <summary>IMAGE_OPTIONAL_HEADER.MinorLinkerVersion</summary>
		public abstract StructField<ByteData> MinorLinkerVersion { get; }
		/// <summary>IMAGE_OPTIONAL_HEADER.SizeOfCode</summary>
		public abstract StructField<UInt32Data> SizeOfCode { get; }
		/// <summary>IMAGE_OPTIONAL_HEADER.SizeOfInitializedData</summary>
		public abstract StructField<UInt32Data> SizeOfInitializedData { get; }
		/// <summary>IMAGE_OPTIONAL_HEADER.SizeOfUninitializedData</summary>
		public abstract StructField<UInt32Data> SizeOfUninitializedData { get; }
		/// <summary>IMAGE_OPTIONAL_HEADER.AddressOfEntryPoint</summary>
		public abstract StructField<RvaData> AddressOfEntryPoint { get; }
		/// <summary>IMAGE_OPTIONAL_HEADER.BaseOfCode</summary>
		public abstract StructField<RvaData> BaseOfCode { get; }
		/// <summary>IMAGE_OPTIONAL_HEADER.SectionAlignment</summary>
		public abstract StructField<UInt32Data> SectionAlignment { get; }
		/// <summary>IMAGE_OPTIONAL_HEADER.FileAlignment</summary>
		public abstract StructField<UInt32Data> FileAlignment { get; }
		/// <summary>IMAGE_OPTIONAL_HEADER.MajorOperatingSystemVersion</summary>
		public abstract StructField<UInt16Data> MajorOperatingSystemVersion { get; }
		/// <summary>IMAGE_OPTIONAL_HEADER.MinorOperatingSystemVersion</summary>
		public abstract StructField<UInt16Data> MinorOperatingSystemVersion { get; }
		/// <summary>IMAGE_OPTIONAL_HEADER.MajorImageVersion</summary>
		public abstract StructField<UInt16Data> MajorImageVersion { get; }
		/// <summary>IMAGE_OPTIONAL_HEADER.MinorImageVersion</summary>
		public abstract StructField<UInt16Data> MinorImageVersion { get; }
		/// <summary>IMAGE_OPTIONAL_HEADER.MajorSubsystemVersion</summary>
		public abstract StructField<UInt16Data> MajorSubsystemVersion { get; }
		/// <summary>IMAGE_OPTIONAL_HEADER.MinorSubsystemVersion</summary>
		public abstract StructField<UInt16Data> MinorSubsystemVersion { get; }
		/// <summary>IMAGE_OPTIONAL_HEADER.Win32VersionValue</summary>
		public abstract StructField<UInt32Data> Win32VersionValue { get; }
		/// <summary>IMAGE_OPTIONAL_HEADER.SizeOfImage</summary>
		public abstract StructField<UInt32Data> SizeOfImage { get; }
		/// <summary>IMAGE_OPTIONAL_HEADER.SizeOfHeaders</summary>
		public abstract StructField<UInt32Data> SizeOfHeaders { get; }
		/// <summary>IMAGE_OPTIONAL_HEADER.CheckSum</summary>
		public abstract StructField<UInt32Data> CheckSum { get; }
		/// <summary>IMAGE_OPTIONAL_HEADER.Subsystem</summary>
		public abstract StructField<UInt16EnumData> Subsystem { get; }
		/// <summary>IMAGE_OPTIONAL_HEADER.DllCharacteristics</summary>
		public abstract StructField<UInt16FlagsData> DllCharacteristics { get; }
		/// <summary>IMAGE_OPTIONAL_HEADER.LoaderFlags</summary>
		public abstract StructField<UInt32Data> LoaderFlags { get; }
		/// <summary>IMAGE_OPTIONAL_HEADER.NumberOfRvaAndSizes</summary>
		public abstract StructField<UInt32Data> NumberOfRvaAndSizes { get; }
		/// <summary>IMAGE_OPTIONAL_HEADER.DataDirectory</summary>
		public abstract StructField<ArrayData<DataDirectoryData>> DataDirectory { get; }
	}

	/// <summary>
	/// 32-bit optional header
	/// </summary>
	public abstract class PeOptionalHeader32Data : PeOptionalHeaderData {
		const string NAME = "IMAGE_OPTIONAL_HEADER32";

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Span</param>
		protected PeOptionalHeader32Data(HexBufferSpan span)
			: base(NAME, span) {
		}

		/// <summary>
		/// This is true, since it's a 32-bit optional header
		/// </summary>
		public sealed override bool Is32Bit => true;

		/// <summary>IMAGE_OPTIONAL_HEADER32.BaseOfData</summary>
		public abstract StructField<RvaData> BaseOfData { get; }
		/// <summary>IMAGE_OPTIONAL_HEADER32.ImageBase</summary>
		public abstract StructField<UInt32Data> ImageBase { get; }
		/// <summary>IMAGE_OPTIONAL_HEADER32.SizeOfStackReserve</summary>
		public abstract StructField<UInt32Data> SizeOfStackReserve { get; }
		/// <summary>IMAGE_OPTIONAL_HEADER32.SizeOfStackCommit</summary>
		public abstract StructField<UInt32Data> SizeOfStackCommit { get; }
		/// <summary>IMAGE_OPTIONAL_HEADER32.SizeOfHeapReserve</summary>
		public abstract StructField<UInt32Data> SizeOfHeapReserve { get; }
		/// <summary>IMAGE_OPTIONAL_HEADER32.SizeOfHeapCommit</summary>
		public abstract StructField<UInt32Data> SizeOfHeapCommit { get; }
	}

	/// <summary>
	/// 64-bit optional header
	/// </summary>
	public abstract class PeOptionalHeader64Data : PeOptionalHeaderData {
		const string NAME = "IMAGE_OPTIONAL_HEADER64";

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Span</param>
		protected PeOptionalHeader64Data(HexBufferSpan span)
			: base(NAME, span) {
		}

		/// <summary>
		/// This is false, since it's a 64-bit optional header
		/// </summary>
		public sealed override bool Is32Bit => false;

		/// <summary>IMAGE_OPTIONAL_HEADER64.ImageBase</summary>
		public abstract StructField<UInt64Data> ImageBase { get; }
		/// <summary>IMAGE_OPTIONAL_HEADER64.SizeOfStackReserve</summary>
		public abstract StructField<UInt64Data> SizeOfStackReserve { get; }
		/// <summary>IMAGE_OPTIONAL_HEADER64.SizeOfStackCommit</summary>
		public abstract StructField<UInt64Data> SizeOfStackCommit { get; }
		/// <summary>IMAGE_OPTIONAL_HEADER64.SizeOfHeapReserve</summary>
		public abstract StructField<UInt64Data> SizeOfHeapReserve { get; }
		/// <summary>IMAGE_OPTIONAL_HEADER64.SizeOfHeapCommit</summary>
		public abstract StructField<UInt64Data> SizeOfHeapCommit { get; }
	}
}
