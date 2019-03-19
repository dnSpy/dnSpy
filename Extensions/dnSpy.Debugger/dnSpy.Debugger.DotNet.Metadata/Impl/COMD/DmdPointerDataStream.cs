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

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace dnSpy.Debugger.DotNet.Metadata.Impl.COMD {
	sealed unsafe class DmdPointerDataStream : DmdDataStream {
		public override long Position {
			get => pos - start;
			set => pos = start + value;
		}

		public override long Length => end - start;

		readonly byte* start;
		readonly byte* end;
		byte* pos;

		public DmdPointerDataStream((IntPtr addr, uint size) info) : this(info.addr, (int)info.size) { }

		public DmdPointerDataStream(IntPtr data, int length) {
			var d = (byte*)data;
			pos = d;
			start = d;
			end = d + length;
		}

		public override byte ReadByte() {
			if (pos < start || pos >= end)
				throw new IOException();
			return *pos++;
		}

		public override ushort ReadUInt16() {
			if (pos < start || pos + 1 >= end)
				throw new IOException();
			var res = *(ushort*)pos;
			pos += 2;
			return res;
		}

		public override uint ReadUInt32() {
			if (pos < start || pos + 3 >= end)
				throw new IOException();
			var res = *(uint*)pos;
			pos += 4;
			return res;
		}

		public override ulong ReadUInt64() {
			if (pos < start || pos + 7 >= end)
				throw new IOException();
			var res = *(ulong*)pos;
			pos += 8;
			return res;
		}

		public override float ReadSingle() {
			if (pos < start || pos + 3 >= end)
				throw new IOException();
			var res = *(float*)pos;
			pos += 4;
			return res;
		}

		public override double ReadDouble() {
			if (pos < start || pos + 7 >= end)
				throw new IOException();
			var res = *(double*)pos;
			pos += 8;
			return res;
		}

		public override byte[] ReadBytes(int length) {
			if (pos < start || pos + length > end)
				throw new IOException();
			var res = new byte[length];
			Marshal.Copy(new IntPtr(pos), res, 0, length);
			pos += length;
			return res;
		}

		public override void Dispose() { }
	}
}
