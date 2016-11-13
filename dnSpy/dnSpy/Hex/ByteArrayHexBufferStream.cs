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
using System.Collections;
using System.Diagnostics;
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

		public override int TryReadByte(HexPosition position) {
			Debug.Assert(position < HexPosition.MaxEndPosition);
			var pos = position.ToUInt64();
			var d = data;
			if (pos >= (ulong)d.LongLength)
				return -1;
			return d[pos];
		}

		public override byte ReadByte(HexPosition position) {
			Debug.Assert(position < HexPosition.MaxEndPosition);
			var pos = position.ToUInt64();
			var d = data;
			if (pos >= (ulong)d.LongLength)
				return 0;
			return d[pos];
		}

		public override sbyte ReadSByte(HexPosition position) {
			Debug.Assert(position < HexPosition.MaxEndPosition);
			var pos = position.ToUInt64();
			var d = data;
			if (pos >= (ulong)d.LongLength)
				return 0;
			return (sbyte)d[pos];
		}

		public override short ReadInt16(HexPosition position) {
			Debug.Assert(position < HexPosition.MaxEndPosition);
			var pos = position.ToUInt64();
			var d = data;
			if (pos + 1 < pos || pos + 1 >= (ulong)d.LongLength)
				return pos < (ulong)d.LongLength ? d[pos] : (short)0;

			return (short)(d[pos] | (d[pos + 1] << 8));
		}

		public override ushort ReadUInt16(HexPosition position) {
			Debug.Assert(position < HexPosition.MaxEndPosition);
			var pos = position.ToUInt64();
			var d = data;
			if (pos + 1 < pos || pos + 1 >= (ulong)d.LongLength)
				return pos < (ulong)d.LongLength ? d[pos] : (ushort)0;

			return (ushort)(d[pos] | (d[pos + 1] << 8));
		}

		public override int ReadInt32(HexPosition position) {
			Debug.Assert(position < HexPosition.MaxEndPosition);
			var pos = position.ToUInt64();
			var d = data;
			if (pos + 3 < pos)
				return 0;
			if (pos + 3 >= (ulong)d.LongLength) {
				int res = 0;
				if (pos < (ulong)d.LongLength)
					res = d[pos];
				if (pos + 1 < (ulong)d.LongLength)
					res |= d[pos + 1] << 8;
				if (pos + 2 < (ulong)d.LongLength)
					res |= d[pos + 2] << 16;
				return res;
			}

			return d[pos] |
					(d[pos + 1] << 8) |
					(d[pos + 2] << 16) |
					(d[pos + 3] << 24);
		}

		public override uint ReadUInt32(HexPosition position) {
			Debug.Assert(position < HexPosition.MaxEndPosition);
			var pos = position.ToUInt64();
			var d = data;
			if (pos + 3 < pos)
				return 0;
			if (pos + 3 >= (ulong)d.LongLength) {
				int res = 0;
				if (pos < (ulong)d.LongLength)
					res = d[pos];
				if (pos + 1 < (ulong)d.LongLength)
					res |= d[pos + 1] << 8;
				if (pos + 2 < (ulong)d.LongLength)
					res |= d[pos + 2] << 16;
				return (uint)res;
			}

			return (uint)(d[pos] |
					(d[pos + 1] << 8) |
					(d[pos + 2] << 16) |
					(d[pos + 3] << 24));
		}

		public override long ReadInt64(HexPosition position) {
			Debug.Assert(position < HexPosition.MaxEndPosition);
			var pos = position.ToUInt64();
			var d = data;
			if (pos + 7 < pos)
				return 0;
			if (pos + 7 >= (ulong)d.LongLength) {
				long res = 0;
				if (pos < (ulong)d.LongLength)
					res = d[pos];
				if (pos + 1 < (ulong)d.LongLength)
					res |= (long)d[pos + 1] << 8;
				if (pos + 2 < (ulong)d.LongLength)
					res |= (long)d[pos + 2] << 16;
				if (pos + 3 < (ulong)d.LongLength)
					res |= (long)d[pos + 3] << 24;
				if (pos + 4 < (ulong)d.LongLength)
					res |= (long)d[pos + 4] << 32;
				if (pos + 5 < (ulong)d.LongLength)
					res |= (long)d[pos + 5] << 40;
				if (pos + 6 < (ulong)d.LongLength)
					res |= (long)d[pos + 6] << 48;
				return res;
			}

			return d[pos] |
					((long)d[pos + 1] << 8) |
					((long)d[pos + 2] << 16) |
					((long)d[pos + 3] << 24) |
					((long)d[pos + 4] << 32) |
					((long)d[pos + 5] << 40) |
					((long)d[pos + 6] << 48) |
					((long)d[pos + 7] << 56);
		}

		public override ulong ReadUInt64(HexPosition position) {
			Debug.Assert(position < HexPosition.MaxEndPosition);
			var pos = position.ToUInt64();
			var d = data;
			if (pos + 7 < pos)
				return 0;
			if (pos + 7 >= (ulong)d.LongLength) {
				ulong res = 0;
				if (pos < (ulong)d.LongLength)
					res = d[pos];
				if (pos + 1 < (ulong)d.LongLength)
					res |= (ulong)d[pos + 1] << 8;
				if (pos + 2 < (ulong)d.LongLength)
					res |= (ulong)d[pos + 2] << 16;
				if (pos + 3 < (ulong)d.LongLength)
					res |= (ulong)d[pos + 3] << 24;
				if (pos + 4 < (ulong)d.LongLength)
					res |= (ulong)d[pos + 4] << 32;
				if (pos + 5 < (ulong)d.LongLength)
					res |= (ulong)d[pos + 5] << 40;
				if (pos + 6 < (ulong)d.LongLength)
					res |= (ulong)d[pos + 6] << 48;
				return res;
			}

			return d[pos] |
					((ulong)d[pos + 1] << 8) |
					((ulong)d[pos + 2] << 16) |
					((ulong)d[pos + 3] << 24) |
					((ulong)d[pos + 4] << 32) |
					((ulong)d[pos + 5] << 40) |
					((ulong)d[pos + 6] << 48) |
					((ulong)d[pos + 7] << 56);
		}

		public unsafe override float ReadSingle(HexPosition position) {
			int v = ReadInt32(position);
			return *(float*)&v;
		}

		public unsafe override double ReadDouble(HexPosition position) {
			long v = ReadInt64(position);
			return *(double*)&v;
		}

		public override byte[] ReadBytes(HexPosition position, long length) {
			var res = new byte[length];
			ReadBytes(position, res, 0, res.LongLength);
			return res;
		}

		public override void ReadBytes(HexPosition position, byte[] destination, long destinationIndex, long length) {
			Debug.Assert(position < HexPosition.MaxEndPosition);
			var pos = position.ToUInt64();
			var d = data;
			if (pos >= (ulong)d.LongLength) {
				Clear(destination, destinationIndex, length);
				return;
			}

			long bytesLeft = d.LongLength - (long)pos;
			long validBytes = length <= bytesLeft ? length : bytesLeft;
			Array.Copy(d, (long)pos, destination, destinationIndex, validBytes);
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

		public override HexBytes ReadHexBytes(HexPosition position, long length) {
			Debug.Assert(position < HexPosition.MaxEndPosition);
			if (length == 0)
				return HexBytes.Empty;

			var pos = position.ToUInt64();
			var d = data;
			if (pos >= (ulong)d.LongLength)
				return new HexBytes(new byte[length], allValid: false);

			var ary = new byte[length];

			if (pos + (ulong)length > (ulong)d.LongLength) {
				var bitArray = new BitArray(length > int.MaxValue ? int.MaxValue : (int)length);

				long len = d.LongLength - (long)pos;
				Array.Copy(d, (long)pos, ary, 0, len);

				long bits = len > int.MaxValue ? int.MaxValue : len;
				for (long i = 0; i < bits; i++)
					bitArray.Set((int)i, true);

				return new HexBytes(ary, bitArray);
			}

			Array.Copy(d, (long)pos, ary, 0, length);
			return new HexBytes(ary);
		}

		public override void Write(HexPosition position, byte[] source, long sourceIndex, long length) {
			Debug.Assert(position < HexPosition.MaxEndPosition);
			var pos = position.ToUInt64();
			var d = data;
			if (pos >= (ulong)d.LongLength)
				return;

			long bytesLeft = d.LongLength - (long)pos;
			long validBytes = length <= bytesLeft ? length : bytesLeft;
			Array.Copy(source, sourceIndex, d, (long)pos, validBytes);
		}
	}
}
