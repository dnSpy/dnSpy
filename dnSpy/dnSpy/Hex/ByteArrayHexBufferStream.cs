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

namespace dnSpy.Hex {
	sealed class ByteArrayHexBufferStream : HexBufferStream {
		public override HexSpan Span { get; }
		public override string Name { get; }

		readonly byte[] data;

		public ByteArrayHexBufferStream(byte[] data, string name) {
			if (data == null)
				throw new ArgumentNullException(nameof(data));
			if (name == null)
				throw new ArgumentNullException(nameof(name));
			Span = new HexSpan(0, (ulong)data.LongLength);
			Name = name;
			this.data = data;
		}

		public override int TryReadByte(ulong position) {
			var d = data;
			if (position >= (ulong)d.LongLength)
				return -1;
			return d[position];
		}

		public override byte ReadByte(ulong position) {
			var d = data;
			if (position >= (ulong)d.LongLength)
				return 0;
			return d[position];
		}

		public override sbyte ReadSByte(ulong position) {
			var d = data;
			if (position >= (ulong)d.LongLength)
				return 0;
			return (sbyte)d[position];
		}

		public override short ReadInt16(ulong position) {
			var d = data;
			if (position + 1 < position || position + 1 >= (ulong)d.LongLength)
				return position < (ulong)d.LongLength ? d[position] : (short)0;

			return (short)(d[position] | (d[position + 1] << 8));
		}

		public override ushort ReadUInt16(ulong position) {
			var d = data;
			if (position + 1 < position || position + 1 >= (ulong)d.LongLength)
				return position < (ulong)d.LongLength ? d[position] : (ushort)0;

			return (ushort)(d[position] | (d[position + 1] << 8));
		}

		public override int ReadInt32(ulong position) {
			var d = data;
			if (position + 3 < position)
				return 0;
			if (position + 3 >= (ulong)d.LongLength) {
				int res = 0;
				if (position < (ulong)d.LongLength)
					res = d[position];
				if (position + 1 < (ulong)d.LongLength)
					res |= d[position + 1] << 8;
				if (position + 2 < (ulong)d.LongLength)
					res |= d[position + 2] << 16;
				return res;
			}

			return d[position] |
					(d[position + 1] << 8) |
					(d[position + 2] << 16) |
					(d[position + 3] << 24);
		}

		public override uint ReadUInt32(ulong position) {
			var d = data;
			if (position + 3 < position)
				return 0;
			if (position + 3 >= (ulong)d.LongLength) {
				int res = 0;
				if (position < (ulong)d.LongLength)
					res = d[position];
				if (position + 1 < (ulong)d.LongLength)
					res |= d[position + 1] << 8;
				if (position + 2 < (ulong)d.LongLength)
					res |= d[position + 2] << 16;
				return (uint)res;
			}

			return (uint)(d[position] |
					(d[position + 1] << 8) |
					(d[position + 2] << 16) |
					(d[position + 3] << 24));
		}

		public override long ReadInt64(ulong position) {
			var d = data;
			if (position + 7 < position)
				return 0;
			if (position + 7 >= (ulong)d.LongLength) {
				long res = 0;
				if (position < (ulong)d.LongLength)
					res = d[position];
				if (position + 1 < (ulong)d.LongLength)
					res |= (long)d[position + 1] << 8;
				if (position + 2 < (ulong)d.LongLength)
					res |= (long)d[position + 2] << 16;
				if (position + 3 < (ulong)d.LongLength)
					res |= (long)d[position + 3] << 24;
				if (position + 4 < (ulong)d.LongLength)
					res |= (long)d[position + 4] << 32;
				if (position + 5 < (ulong)d.LongLength)
					res |= (long)d[position + 5] << 40;
				if (position + 6 < (ulong)d.LongLength)
					res |= (long)d[position + 6] << 48;
				return res;
			}

			return d[position] |
					((long)d[position + 1] << 8) |
					((long)d[position + 2] << 16) |
					((long)d[position + 3] << 24) |
					((long)d[position + 4] << 32) |
					((long)d[position + 5] << 40) |
					((long)d[position + 6] << 48) |
					((long)d[position + 7] << 56);
		}

		public override ulong ReadUInt64(ulong position) {
			var d = data;
			if (position + 7 < position)
				return 0;
			if (position + 7 >= (ulong)d.LongLength) {
				ulong res = 0;
				if (position < (ulong)d.LongLength)
					res = d[position];
				if (position + 1 < (ulong)d.LongLength)
					res |= (ulong)d[position + 1] << 8;
				if (position + 2 < (ulong)d.LongLength)
					res |= (ulong)d[position + 2] << 16;
				if (position + 3 < (ulong)d.LongLength)
					res |= (ulong)d[position + 3] << 24;
				if (position + 4 < (ulong)d.LongLength)
					res |= (ulong)d[position + 4] << 32;
				if (position + 5 < (ulong)d.LongLength)
					res |= (ulong)d[position + 5] << 40;
				if (position + 6 < (ulong)d.LongLength)
					res |= (ulong)d[position + 6] << 48;
				return res;
			}

			return d[position] |
					((ulong)d[position + 1] << 8) |
					((ulong)d[position + 2] << 16) |
					((ulong)d[position + 3] << 24) |
					((ulong)d[position + 4] << 32) |
					((ulong)d[position + 5] << 40) |
					((ulong)d[position + 6] << 48) |
					((ulong)d[position + 7] << 56);
		}

		public unsafe override float ReadSingle(ulong position) {
			int v = ReadInt32(position);
			return *(float*)&v;
		}

		public unsafe override double ReadDouble(ulong position) {
			long v = ReadInt64(position);
			return *(double*)&v;
		}

		public override byte[] ReadBytes(ulong position, long length) {
			var res = new byte[length];
			ReadBytes(position, res, 0, res.LongLength);
			return res;
		}

		public override void ReadBytes(ulong position, byte[] destination, long destinationIndex, long length) {
			var d = data;
			if (position >= (ulong)d.LongLength) {
				Clear(destination, destinationIndex, length);
				return;
			}

			long bytesLeft = d.LongLength - (long)position;
			long validBytes = length <= bytesLeft ? length : bytesLeft;
			Array.Copy(d, (long)position, destination, destinationIndex, validBytes);
			length -= validBytes;
			if (length > 0)
				Clear(destination, destinationIndex + validBytes, length);
		}

		static void Clear(byte[] array, long index, long length) {
			if (length == 0)
				return;
			if (index <= int.MaxValue && length <= int.MaxValue && index + length - 1 <= int.MaxValue) {
				Array.Clear(array, (int)index, (int)length);
				return;
			}

			long i = index;
			while (length-- > 0)
				array[i++] = 0;
		}

		public override HexBytes ReadHexBytes(ulong position, long length) {
			if (length == 0)
				return HexBytes.Empty;
			throw new NotImplementedException();//TODO:
		}

		public override void Write(ulong position, byte[] source, long sourceIndex, long length) {
			var d = data;
			if (position >= (ulong)d.LongLength)
				return;

			long bytesLeft = d.LongLength - (long)position;
			long validBytes = length <= bytesLeft ? length : bytesLeft;
			Array.Copy(source, sourceIndex, d, (long)position, validBytes);
		}
	}
}
