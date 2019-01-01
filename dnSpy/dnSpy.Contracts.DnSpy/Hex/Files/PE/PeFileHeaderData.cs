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
	/// File header
	/// </summary>
	public abstract class PeFileHeaderData : StructureData {
		const string NAME = "IMAGE_FILE_HEADER";

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Span</param>
		protected PeFileHeaderData(HexBufferSpan span)
			: base(NAME, span) {
		}

		/// <summary>IMAGE_FILE_HEADER.Machine</summary>
		public abstract StructField<UInt16EnumData> Machine { get; }
		/// <summary>IMAGE_FILE_HEADER.NumberOfSections</summary>
		public abstract StructField<UInt16Data> NumberOfSections { get; }
		/// <summary>IMAGE_FILE_HEADER.TimeDateStamp</summary>
		public abstract StructField<UnixTime32Data> TimeDateStamp { get; }
		/// <summary>IMAGE_FILE_HEADER.PointerToSymbolTable</summary>
		public abstract StructField<FileOffsetData> PointerToSymbolTable { get; }
		/// <summary>IMAGE_FILE_HEADER.NumberOfSymbols</summary>
		public abstract StructField<UInt32Data> NumberOfSymbols { get; }
		/// <summary>IMAGE_FILE_HEADER.SizeOfOptionalHeader</summary>
		public abstract StructField<UInt16Data> SizeOfOptionalHeader { get; }
		/// <summary>IMAGE_FILE_HEADER.Characteristics</summary>
		public abstract StructField<UInt16FlagsData> Characteristics { get; }
	}
}
