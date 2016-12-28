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

namespace dnSpy.Contracts.Hex.Files.DotNet {
	/// <summary>
	/// .NET multi-file resource header
	/// </summary>
	public abstract class DotNetMultiFileResourceHeaderData : StructureData {
		const string NAME = "MultiFileResource";

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Span</param>
		protected DotNetMultiFileResourceHeaderData(HexBufferSpan span)
			: base(NAME, span) {
		}

		/// <summary>Magic number</summary>
		public abstract StructField<UInt32Data> MagicNum { get; }
		/// <summary>Header version</summary>
		public abstract StructField<UInt32Data> ResMgrHeaderVersion { get; }
		/// <summary>Header size</summary>
		public abstract StructField<UInt32Data> HeaderSize { get; }
		/// <summary>Unknown header or null if it's a known header (see <see cref="ReaderType"/> and <see cref="ResourceSetType"/>)</summary>
		public abstract StructField<VirtualArrayData<ByteData>> UnknownHeader { get; }
		/// <summary>Reader type or null if unknown header</summary>
		public abstract StructField<Bit7EncodedStringData> ReaderType { get; }
		/// <summary>ResourceSet type or null if unknown header</summary>
		public abstract StructField<Bit7EncodedStringData> ResourceSetType { get; }
		/// <summary>Version</summary>
		public abstract StructField<UInt32Data> Version { get; }
		/// <summary>NumResources</summary>
		public abstract StructField<UInt32Data> NumResources { get; }
		/// <summary>NumTypes</summary>
		public abstract StructField<UInt32Data> NumTypes { get; }
		/// <summary>TypeNames</summary>
		public abstract StructField<VariableLengthArrayData<Bit7EncodedStringData>> TypeNames { get; }
		/// <summary>8-byte alignment</summary>
		public abstract StructField<ArrayData<ByteData>> Alignment8 { get; }
		/// <summary>Name hashes</summary>
		public abstract StructField<VirtualArrayData<UInt32Data>> NameHashes { get; }
		/// <summary>Name positions</summary>
		public abstract StructField<VirtualArrayData<UInt32Data>> NamePositions { get; }
		/// <summary>DataSectionOffset</summary>
		public abstract StructField<UInt32Data> DataSectionOffset { get; }
	}
}
