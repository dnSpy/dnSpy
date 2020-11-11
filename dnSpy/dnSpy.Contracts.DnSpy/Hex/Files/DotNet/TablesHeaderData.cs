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

namespace dnSpy.Contracts.Hex.Files.DotNet {
	/// <summary>
	/// .NET tables stream header
	/// </summary>
	public abstract class TablesHeaderData : StructureData {
		const string NAME = "MiniMdSchema";

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Span</param>
		protected TablesHeaderData(HexBufferSpan span)
			: base(NAME, span) {
		}

		/// <summary>m_ulReserved</summary>
		public abstract StructField<UInt32Data> Reserved { get; }
		/// <summary>m_major</summary>
		public abstract StructField<ByteData> MajorVersion { get; }
		/// <summary>m_minor</summary>
		public abstract StructField<ByteData> MinorVersion { get; }
		/// <summary>m_heaps</summary>
		public abstract StructField<ByteFlagsData> Flags { get; }
		/// <summary>m_rid</summary>
		public abstract StructField<ByteData> Log2Rid { get; }
		/// <summary>m_maskvalid</summary>
		public abstract StructField<UInt64FlagsData> ValidMask { get; }
		/// <summary>m_sorted</summary>
		public abstract StructField<UInt64FlagsData> SortedMask { get; }
		/// <summary>Extra data or null if there was no extra data</summary>
		public abstract StructField<UInt32Data>? ExtraData { get; }
		/// <summary>true if <see cref="ExtraData"/> field exists in header and isn't null</summary>
		public bool HasExtraData => ExtraData is not null;
		/// <summary>Rows</summary>
		public abstract StructField<ArrayData<UInt32Data>> Rows { get; }
	}
}
