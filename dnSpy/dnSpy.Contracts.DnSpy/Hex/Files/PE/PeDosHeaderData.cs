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
	/// DOS header
	/// </summary>
	public abstract class PeDosHeaderData : StructureData {
		const string NAME = "IMAGE_DOS_HEADER";

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Span</param>
		protected PeDosHeaderData(HexBufferSpan span)
			: base(NAME, span) {
		}

		/// <summary>IMAGE_DOS_HEADER.e_magic</summary>
		public abstract StructField<UInt16Data> Magic { get; }
		/// <summary>IMAGE_DOS_HEADER.e_cblp</summary>
		public abstract StructField<UInt16Data> Cblp { get; }
		/// <summary>IMAGE_DOS_HEADER.e_cp</summary>
		public abstract StructField<UInt16Data> Cp { get; }
		/// <summary>IMAGE_DOS_HEADER.e_crlc</summary>
		public abstract StructField<UInt16Data> Crlc { get; }
		/// <summary>IMAGE_DOS_HEADER.e_cparhdr</summary>
		public abstract StructField<UInt16Data> Cparhdr { get; }
		/// <summary>IMAGE_DOS_HEADER.e_minalloc</summary>
		public abstract StructField<UInt16Data> Minalloc { get; }
		/// <summary>IMAGE_DOS_HEADER.e_maxalloc</summary>
		public abstract StructField<UInt16Data> Maxalloc { get; }
		/// <summary>IMAGE_DOS_HEADER.e_ss</summary>
		public abstract StructField<UInt16Data> Ss { get; }
		/// <summary>IMAGE_DOS_HEADER.e_sp</summary>
		public abstract StructField<UInt16Data> Sp { get; }
		/// <summary>IMAGE_DOS_HEADER.e_csum</summary>
		public abstract StructField<UInt16Data> Csum { get; }
		/// <summary>IMAGE_DOS_HEADER.e_ip</summary>
		public abstract StructField<UInt16Data> Ip { get; }
		/// <summary>IMAGE_DOS_HEADER.e_cs</summary>
		public abstract StructField<UInt16Data> Cs { get; }
		/// <summary>IMAGE_DOS_HEADER.e_lfarlc</summary>
		public abstract StructField<UInt16Data> Lfarlc { get; }
		/// <summary>IMAGE_DOS_HEADER.e_ovno</summary>
		public abstract StructField<UInt16Data> Ovno { get; }
		/// <summary>IMAGE_DOS_HEADER.e_res[4]</summary>
		public abstract StructField<ArrayData<UInt16Data>> Res { get; }
		/// <summary>IMAGE_DOS_HEADER.e_oemid</summary>
		public abstract StructField<UInt16Data> Oemid { get; }
		/// <summary>IMAGE_DOS_HEADER.e_oeminfo</summary>
		public abstract StructField<UInt16Data> Oeminfo { get; }
		/// <summary>IMAGE_DOS_HEADER.e_res2[10]</summary>
		public abstract StructField<ArrayData<UInt16Data>> Res2 { get; }
		/// <summary>IMAGE_DOS_HEADER.e_lfanew</summary>
		public abstract StructField<FileOffsetData> Lfanew { get; }
	}
}
