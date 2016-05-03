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

namespace dnSpy.Shared.HexEditor {
	public class ByteArrayHexStream : IHexStream {
		readonly byte[] data;

		public ulong Size => (ulong)data.LongLength;
		public ulong StartOffset => 0;
		public ulong EndOffset => data.LongLength == 0 ? 0 : (ulong)data.LongLength - 1;

		public ByteArrayHexStream(byte[] data) {
			this.data = data;
		}

		public int ReadByte(ulong offset) {
			if (offset >= (ulong)data.LongLength)
				return -1;
			return data[offset];
		}

		public short ReadInt16(ulong offset) {
			if (offset + 1 < offset || offset + 1 >= (ulong)data.LongLength)
				return offset < (ulong)data.LongLength ? data[offset] : (short)0;

			return (short)(data[offset] | (data[offset + 1] << 8));
		}

		public ushort ReadUInt16(ulong offset) {
			if (offset + 1 < offset || offset + 1 >= (ulong)data.LongLength)
				return offset < (ulong)data.LongLength ? data[offset] : (ushort)0;

			return (ushort)(data[offset] | (data[offset + 1] << 8));
		}

		public int ReadInt32(ulong offset) {
			if (offset + 3 < offset)
				return 0;
			if (offset + 3 >= (ulong)data.LongLength) {
				int res = 0;
				if (offset < (ulong)data.LongLength)
					res = data[offset];
				if (offset + 1 < (ulong)data.LongLength)
					res |= data[offset + 1] << 8;
				if (offset + 2 < (ulong)data.LongLength)
					res |= data[offset + 2] << 16;
				return res;
			}

			return data[offset] |
					(data[offset + 1] << 8) |
					(data[offset + 2] << 16) |
					(data[offset + 3] << 24);
		}

		public uint ReadUInt32(ulong offset) {
			if (offset + 3 < offset)
				return 0;
			if (offset + 3 >= (ulong)data.LongLength) {
				int res = 0;
				if (offset < (ulong)data.LongLength)
					res = data[offset];
				if (offset + 1 < (ulong)data.LongLength)
					res |= data[offset + 1] << 8;
				if (offset + 2 < (ulong)data.LongLength)
					res |= data[offset + 2] << 16;
				return (uint)res;
			}

			return (uint)(data[offset] |
					(data[offset + 1] << 8) |
					(data[offset + 2] << 16) |
					(data[offset + 3] << 24));
		}

		public long ReadInt64(ulong offset) {
			if (offset + 7 < offset)
				return 0;
			if (offset + 7 >= (ulong)data.LongLength) {
				long res = 0;
				if (offset < (ulong)data.LongLength)
					res = data[offset];
				if (offset + 1 < (ulong)data.LongLength)
					res |= (long)data[offset + 1] << 8;
				if (offset + 2 < (ulong)data.LongLength)
					res |= (long)data[offset + 2] << 16;
				if (offset + 3 < (ulong)data.LongLength)
					res |= (long)data[offset + 3] << 24;
				if (offset + 4 < (ulong)data.LongLength)
					res |= (long)data[offset + 4] << 32;
				if (offset + 5 < (ulong)data.LongLength)
					res |= (long)data[offset + 5] << 40;
				if (offset + 6 < (ulong)data.LongLength)
					res |= (long)data[offset + 6] << 48;
				return res;
			}

			return data[offset] |
					((long)data[offset + 1] << 8) |
					((long)data[offset + 2] << 16) |
					((long)data[offset + 3] << 24) |
					((long)data[offset + 4] << 32) |
					((long)data[offset + 5] << 40) |
					((long)data[offset + 6] << 48) |
					((long)data[offset + 7] << 56);
		}

		public ulong ReadUInt64(ulong offset) {
			if (offset + 7 < offset)
				return 0;
			if (offset + 7 >= (ulong)data.LongLength) {
				ulong res = 0;
				if (offset < (ulong)data.LongLength)
					res = data[offset];
				if (offset + 1 < (ulong)data.LongLength)
					res |= (ulong)data[offset + 1] << 8;
				if (offset + 2 < (ulong)data.LongLength)
					res |= (ulong)data[offset + 2] << 16;
				if (offset + 3 < (ulong)data.LongLength)
					res |= (ulong)data[offset + 3] << 24;
				if (offset + 4 < (ulong)data.LongLength)
					res |= (ulong)data[offset + 4] << 32;
				if (offset + 5 < (ulong)data.LongLength)
					res |= (ulong)data[offset + 5] << 40;
				if (offset + 6 < (ulong)data.LongLength)
					res |= (ulong)data[offset + 6] << 48;
				return res;
			}

			return data[offset] |
					((ulong)data[offset + 1] << 8) |
					((ulong)data[offset + 2] << 16) |
					((ulong)data[offset + 3] << 24) |
					((ulong)data[offset + 4] << 32) |
					((ulong)data[offset + 5] << 40) |
					((ulong)data[offset + 6] << 48) |
					((ulong)data[offset + 7] << 56);
		}

		public void Read(ulong offset, byte[] array, long index, int count) {
			if (offset >= (ulong)data.LongLength) {
				Clear(array, index, count);
				return;
			}

			long bytesLeft = data.LongLength - (long)offset;
			long validBytes = count <= bytesLeft ? count : bytesLeft;
			Array.Copy(data, (long)offset, array, index, validBytes);
			count -= (int)validBytes;
			if (count > 0)
				Clear(array, index + (int)validBytes, count);
		}

		public void Write(ulong offset, byte b) {
			if (offset >= (ulong)data.LongLength)
				return;

			data[offset] = b;
		}

		public void Write(ulong offset, byte[] array, long index, int count) {
			if (offset >= (ulong)data.LongLength)
				return;

			long bytesLeft = data.LongLength - (long)offset;
			long validBytes = count <= bytesLeft ? count : bytesLeft;
			Array.Copy(array, index, data, (long)offset, validBytes);
		}

		void Clear(byte[] array, long index, int count) {
			if (count == 0)
				return;
			if (index <= int.MaxValue && index + count - 1 <= int.MaxValue) {
				Array.Clear(array, (int)index, count);
				return;
			}

			long i = index;
			while (count-- > 0)
				array[i++] = 0;
		}
	}
}
