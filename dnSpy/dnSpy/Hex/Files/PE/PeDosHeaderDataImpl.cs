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
	sealed class PeDosHeaderDataImpl : PeDosHeaderData {
		public override StructField<UInt16Data> Magic { get; }
		public override StructField<UInt16Data> Cblp { get; }
		public override StructField<UInt16Data> Cp { get; }
		public override StructField<UInt16Data> Crlc { get; }
		public override StructField<UInt16Data> Cparhdr { get; }
		public override StructField<UInt16Data> Minalloc { get; }
		public override StructField<UInt16Data> Maxalloc { get; }
		public override StructField<UInt16Data> Ss { get; }
		public override StructField<UInt16Data> Sp { get; }
		public override StructField<UInt16Data> Csum { get; }
		public override StructField<UInt16Data> Ip { get; }
		public override StructField<UInt16Data> Cs { get; }
		public override StructField<UInt16Data> Lfarlc { get; }
		public override StructField<UInt16Data> Ovno { get; }
		public override StructField<ArrayData<UInt16Data>> Res { get; }
		public override StructField<UInt16Data> Oemid { get; }
		public override StructField<UInt16Data> Oeminfo { get; }
		public override StructField<ArrayData<UInt16Data>> Res2 { get; }
		public override StructField<FileOffsetData> Lfanew { get; }

		protected override BufferField[] Fields { get; }

		PeDosHeaderDataImpl(HexBufferSpan span)
			: base(span) {
			var buffer = span.Buffer;
			var pos = span.Start.Position;
			Magic = new StructField<UInt16Data>("e_magic", new UInt16Data(buffer, pos + 0));
			Cblp = new StructField<UInt16Data>("e_cblp", new UInt16Data(buffer, pos + 2));
			Cp = new StructField<UInt16Data>("e_cp", new UInt16Data(buffer, pos + 4));
			Crlc = new StructField<UInt16Data>("e_crlc", new UInt16Data(buffer, pos + 6));
			Cparhdr = new StructField<UInt16Data>("e_cparhdr", new UInt16Data(buffer, pos + 8));
			Minalloc = new StructField<UInt16Data>("e_minalloc", new UInt16Data(buffer, pos + 0x0A));
			Maxalloc = new StructField<UInt16Data>("e_maxalloc", new UInt16Data(buffer, pos + 0x0C));
			Ss = new StructField<UInt16Data>("e_ss", new UInt16Data(buffer, pos + 0x0E));
			Sp = new StructField<UInt16Data>("e_sp", new UInt16Data(buffer, pos + 0x10));
			Csum = new StructField<UInt16Data>("e_csum", new UInt16Data(buffer, pos + 0x12));
			Ip = new StructField<UInt16Data>("e_ip", new UInt16Data(buffer, pos + 0x14));
			Cs = new StructField<UInt16Data>("e_cs", new UInt16Data(buffer, pos + 0x16));
			Lfarlc = new StructField<UInt16Data>("e_lfarlc", new UInt16Data(buffer, pos + 0x18));
			Ovno = new StructField<UInt16Data>("e_ovno", new UInt16Data(buffer, pos + 0x1A));
			Res = new StructField<ArrayData<UInt16Data>>("e_res", ArrayData.CreateUInt16Array(buffer, pos + 0x1C, 4));
			Oemid = new StructField<UInt16Data>("e_oemid", new UInt16Data(buffer, pos + 0x24));
			Oeminfo = new StructField<UInt16Data>("e_oeminfo", new UInt16Data(buffer, pos + 0x26));
			Res2 = new StructField<ArrayData<UInt16Data>>("e_res2", ArrayData.CreateUInt16Array(buffer, pos + 0x28, 10));
			Lfanew = new StructField<FileOffsetData>("e_lfanew", new FileOffsetData(buffer, pos + 0x3C));
			Fields = new StructField[] {
				Magic,
				Cblp,
				Cp,
				Crlc,
				Cparhdr,
				Minalloc,
				Maxalloc,
				Ss,
				Sp,
				Csum,
				Ip,
				Cs,
				Lfarlc,
				Ovno,
				Res,
				Oemid,
				Oeminfo,
				Res2,
				Lfanew,
			};
		}

		public static PeDosHeaderData TryCreate(HexBufferFile file, HexPosition position) {
			if (file == null)
				throw new ArgumentNullException(nameof(file));
			if (!file.Span.Contains(position) || !file.Span.Contains(position + 0x40 - 1))
				return null;
			if (file.Buffer.ReadUInt16(position) != 0x5A4D)
				return null;
			return new PeDosHeaderDataImpl(new HexBufferSpan(file.Buffer, new HexSpan(position, 0x40)));
		}
	}
}
