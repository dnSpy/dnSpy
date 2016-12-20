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

namespace dnSpy.Contracts.Hex.Files.PE {
	/// <summary>
	/// Sections array
	/// </summary>
	public abstract class PeSectionsData : ArrayData {
		const string NAME = "Sections";

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Span</param>
		protected PeSectionsData(HexBufferSpan span)
			: base(NAME, span) {
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
		public abstract StringData SectionName { get; }
		/// <summary>IMAGE_SECTION_HEADER.VirtualSize</summary>
		public abstract UInt32Data VirtualSize { get; }
		/// <summary>IMAGE_SECTION_HEADER.VirtualAddress</summary>
		public abstract UInt32Data VirtualAddress { get; }
		/// <summary>IMAGE_SECTION_HEADER.SizeOfRawData</summary>
		public abstract UInt32Data SizeOfRawData { get; }
		/// <summary>IMAGE_SECTION_HEADER.PointerToRawData</summary>
		public abstract UInt32Data PointerToRawData { get; }
		/// <summary>IMAGE_SECTION_HEADER.PointerToRelocations</summary>
		public abstract UInt32Data PointerToRelocations { get; }
		/// <summary>IMAGE_SECTION_HEADER.PointerToLinenumbers</summary>
		public abstract UInt32Data PointerToLinenumbers { get; }
		/// <summary>IMAGE_SECTION_HEADER.NumberOfRelocations</summary>
		public abstract UInt16Data NumberOfRelocations { get; }
		/// <summary>IMAGE_SECTION_HEADER.NumberOfLinenumbers</summary>
		public abstract UInt16Data NumberOfLinenumbers { get; }
		/// <summary>IMAGE_SECTION_HEADER.Characteristics</summary>
		public abstract UInt32FlagsData Characteristics { get; }
	}
}
