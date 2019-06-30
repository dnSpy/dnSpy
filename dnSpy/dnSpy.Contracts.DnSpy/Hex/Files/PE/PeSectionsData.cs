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

namespace dnSpy.Contracts.Hex.Files.PE {
	/// <summary>
	/// Sections array
	/// </summary>
	public class PeSectionsData : ArrayData<PeSectionData> {
		const string NAME = "Sections";

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Span</param>
		/// <param name="fields">Array elements</param>
		public PeSectionsData(HexBufferSpan span, ArrayField<PeSectionData>[] fields)
			: base(NAME, span, fields) {
		}
	}

	/// <summary>
	/// Section header
	/// </summary>
	public abstract class PeSectionData : StructureData {
		const string NAME = "IMAGE_SECTION_HEADER";

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Span</param>
		protected PeSectionData(HexBufferSpan span)
			: base(NAME, span) {
		}

		/// <summary>IMAGE_SECTION_HEADER.Name</summary>
		public abstract StructField<StringData> SectionName { get; }
		/// <summary>IMAGE_SECTION_HEADER.VirtualSize</summary>
		public abstract StructField<UInt32Data> VirtualSize { get; }
		/// <summary>IMAGE_SECTION_HEADER.VirtualAddress</summary>
		public abstract StructField<RvaData> VirtualAddress { get; }
		/// <summary>IMAGE_SECTION_HEADER.SizeOfRawData</summary>
		public abstract StructField<UInt32Data> SizeOfRawData { get; }
		/// <summary>IMAGE_SECTION_HEADER.PointerToRawData</summary>
		public abstract StructField<FileOffsetData> PointerToRawData { get; }
		/// <summary>IMAGE_SECTION_HEADER.PointerToRelocations</summary>
		public abstract StructField<FileOffsetData> PointerToRelocations { get; }
		/// <summary>IMAGE_SECTION_HEADER.PointerToLinenumbers</summary>
		public abstract StructField<FileOffsetData> PointerToLinenumbers { get; }
		/// <summary>IMAGE_SECTION_HEADER.NumberOfRelocations</summary>
		public abstract StructField<UInt16Data> NumberOfRelocations { get; }
		/// <summary>IMAGE_SECTION_HEADER.NumberOfLinenumbers</summary>
		public abstract StructField<UInt16Data> NumberOfLinenumbers { get; }
		/// <summary>IMAGE_SECTION_HEADER.Characteristics</summary>
		public abstract StructField<UInt32FlagsData> Characteristics { get; }
	}
}
