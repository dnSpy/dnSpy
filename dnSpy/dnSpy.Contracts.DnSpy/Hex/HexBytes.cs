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

namespace dnSpy.Contracts.Hex {
	/// <summary>
	/// Contains bytes and information about whether a byte exists in the stream
	/// </summary>
	public struct HexBytes {
		/// <summary>
		/// Gets the empty instance
		/// </summary>
		public static readonly HexBytes Empty = new HexBytes(Array.Empty<byte>());

		/// <summary>
		/// true if this is a default instance that hasn't been initialized
		/// </summary>
		public bool IsDefault => bytes == null;

		/// <summary>
		/// Gets the length in bytes
		/// </summary>
		public int Length => bytes.Length;

		/// <summary>
		/// Returns true if all bytes are valid, false if all bytes are invalid, or null
		/// if it's not known (use <see cref="IsValid(int)"/>)
		/// </summary>
		public bool? AllValid => validBytes != null ? (bool?)null : allValid;

		readonly byte[] bytes;
		readonly bool allValid;// Only used if the bit array is null
		readonly BitArray validBytes;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="bytes">All bytes and all of them are valid</param>
		public HexBytes(byte[] bytes) {
			if (bytes == null)
				throw new ArgumentNullException(nameof(bytes));
			this.bytes = bytes;
			validBytes = null;
			allValid = true;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="bytes">All bytes</param>
		/// <param name="allValid">true if all bytes are valid, false if all bytes are invalid</param>
		public HexBytes(byte[] bytes, bool allValid) {
			if (bytes == null)
				throw new ArgumentNullException(nameof(bytes));
			this.bytes = bytes;
			validBytes = null;
			this.allValid = allValid;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="bytes">All bytes. All invalid bytes should be cleared</param>
		/// <param name="validBytes">Valid bytes</param>
		public HexBytes(byte[] bytes, BitArray validBytes) {
			if (bytes == null)
				throw new ArgumentNullException(nameof(bytes));
			if (validBytes == null)
				throw new ArgumentNullException(nameof(validBytes));
			if (validBytes.Length != bytes.LongLength)
				throw new ArgumentOutOfRangeException(nameof(bytes));
			this.bytes = bytes;
			allValid = false;// This field isn't used if the bit array is non-null
			this.validBytes = validBytes;
		}

		/// <summary>
		/// Checks whether the byte at <paramref name="index"/> is valid
		/// </summary>
		/// <param name="index">Index of byte</param>
		/// <returns></returns>
		public bool IsValid(int index) {
			if (validBytes == null)
				return allValid;
			return index < validBytes.Length && validBytes.Get(index);
		}

		/// <summary>
		/// Checks whether the byte at <paramref name="index"/> is valid
		/// </summary>
		/// <param name="index">Index of byte</param>
		/// <returns></returns>
		public bool IsValid(long index) {
			if (validBytes == null)
				return allValid;
			return index <= int.MaxValue && (int)index < validBytes.Length ? validBytes.Get((int)index) : true;
		}

		/// <summary>
		/// Returns the byte at <paramref name="index"/> or a value less than 0 if the byte is invalid
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public int TryReadByte(int index) {
			if (!IsValid(index))
				return -1;
			return bytes[index];
		}

		/// <summary>
		/// Returns the <see cref="byte"/> at <paramref name="index"/> or a value less than 0 if the byte is invalid
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public int TryReadByte(long index) {
			if (!IsValid(index))
				return -1;
			return bytes[index];
		}

		/// <summary>
		/// Reads the byte at <paramref name="index"/>
		/// </summary>
		/// <param name="index">Index of byte</param>
		/// <returns></returns>
		public byte this[int index] => bytes[index];

		/// <summary>
		/// Reads the byte at <paramref name="index"/>
		/// </summary>
		/// <param name="index">Index of byte</param>
		/// <returns></returns>
		public byte this[long index] => bytes[index];

		/// <summary>
		/// Returns the <see cref="sbyte"/> at <paramref name="index"/> or null if the byte is invalid
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public sbyte? TryReadSByte(long index) {
			if (!IsValid(index))
				return null;
			return (sbyte)bytes[index];
		}

		/// <summary>
		/// Returns the <see cref="ushort"/> at <paramref name="index"/> or null if all bytes are invalid
		/// </summary>
		/// <param name="index">Index of value</param>
		/// <returns></returns>
		public ushort? TryReadUInt16(long index) {
			if (!IsValid(index) && !IsValid(index + 1))
				return null;
			var d = bytes;
			if ((ulong)index + 1 >= (ulong)d.LongLength)
				return null;
			return (ushort)(d[index] | (d[index + 1] << 8));
		}

		/// <summary>
		/// Returns the <see cref="short"/> at <paramref name="index"/> or null if all bytes are invalid
		/// </summary>
		/// <param name="index">Index of value</param>
		/// <returns></returns>
		public short? TryReadInt16(long index) {
			if (!IsValid(index) && !IsValid(index + 1))
				return null;
			var d = bytes;
			if ((ulong)index + 1 >= (ulong)d.LongLength)
				return null;
			return (short)(d[index] | (d[index + 1] << 8));
		}

		/// <summary>
		/// Returns the <see cref="uint"/> at <paramref name="index"/> or null if all bytes are invalid
		/// </summary>
		/// <param name="index">Index of value</param>
		/// <returns></returns>
		public uint? TryReadUInt32(long index) {
			if (!IsValid(index) && !IsValid(index + 1) && !IsValid(index + 2) && !IsValid(index + 3))
				return null;
			var d = bytes;
			if ((ulong)index + 3 >= (ulong)d.LongLength)
				return null;
			return (uint)(d[index] | (d[index + 1] << 8) | (d[index + 2] << 16) | (d[index + 3] << 24));
		}

		/// <summary>
		/// Returns the <see cref="int"/> at <paramref name="index"/> or null if all bytes are invalid
		/// </summary>
		/// <param name="index">Index of value</param>
		/// <returns></returns>
		public int? TryReadInt32(long index) {
			if (!IsValid(index) && !IsValid(index + 1) && !IsValid(index + 2) && !IsValid(index + 3))
				return null;
			var d = bytes;
			if ((ulong)index + 3 >= (ulong)d.LongLength)
				return null;
			return d[index] | (d[index + 1] << 8) | (d[index + 2] << 16) | (d[index + 3] << 24);
		}

		/// <summary>
		/// Returns the <see cref="ulong"/> at <paramref name="index"/> or null if all bytes are invalid
		/// </summary>
		/// <param name="index">Index of value</param>
		/// <returns></returns>
		public ulong? TryReadUInt64(long index) {
			if (!IsValid(index) && !IsValid(index + 1) && !IsValid(index + 2) && !IsValid(index + 3) && !IsValid(index + 4) && !IsValid(index + 5) && !IsValid(index + 6) && !IsValid(index + 7))
				return null;
			var d = bytes;
			if ((ulong)index + 7 >= (ulong)d.LongLength)
				return null;
			return d[index] |
					((ulong)d[index + 1] << 8) |
					((ulong)d[index + 2] << 16) |
					((ulong)d[index + 3] << 24) |
					((ulong)d[index + 4] << 32) |
					((ulong)d[index + 5] << 40) |
					((ulong)d[index + 6] << 48) |
					((ulong)d[index + 7] << 56);
		}

		/// <summary>
		/// Returns the <see cref="long"/> at <paramref name="index"/> or null if all bytes are invalid
		/// </summary>
		/// <param name="index">Index of value</param>
		/// <returns></returns>
		public long? TryReadInt64(long index) {
			if (!IsValid(index) && !IsValid(index + 1) && !IsValid(index + 2) && !IsValid(index + 3) && !IsValid(index + 4) && !IsValid(index + 5) && !IsValid(index + 6) && !IsValid(index + 7))
				return null;
			var d = bytes;
			if ((ulong)index + 7 >= (ulong)d.LongLength)
				return null;
			return d[index] |
					((long)d[index + 1] << 8) |
					((long)d[index + 2] << 16) |
					((long)d[index + 3] << 24) |
					((long)d[index + 4] << 32) |
					((long)d[index + 5] << 40) |
					((long)d[index + 6] << 48) |
					((long)d[index + 7] << 56);
		}

		/// <summary>
		/// Returns the <see cref="float"/> at <paramref name="index"/> or null if all bytes are invalid
		/// </summary>
		/// <param name="index">Index of value</param>
		/// <returns></returns>
		public unsafe float? TryReadSingle(long index) {
			var v = TryReadInt32(index);
			if (v == null)
				return null;
			int v2 = v.Value;
			return *(float*)&v2;
		}

		/// <summary>
		/// Returns the <see cref="double"/> at <paramref name="index"/> or null if all bytes are invalid
		/// </summary>
		/// <param name="index">Index of value</param>
		/// <returns></returns>
		public unsafe double? TryReadDouble(long index) {
			var v = TryReadInt64(index);
			if (v == null)
				return null;
			long v2 = v.Value;
			return *(double*)&v2;
		}

		/// <summary>
		/// Returns the <see cref="ushort"/> at <paramref name="index"/> or null if all bytes are invalid
		/// </summary>
		/// <param name="index">Index of value</param>
		/// <returns></returns>
		public ushort? TryReadUInt16BigEndian(long index) {
			if (!IsValid(index) && !IsValid(index + 1))
				return null;
			var d = bytes;
			if ((ulong)index + 1 >= (ulong)d.LongLength)
				return null;
			return (ushort)(d[index + 1] | (d[index] << 8));
		}

		/// <summary>
		/// Returns the <see cref="short"/> at <paramref name="index"/> or null if all bytes are invalid
		/// </summary>
		/// <param name="index">Index of value</param>
		/// <returns></returns>
		public short? TryReadInt16BigEndian(long index) {
			if (!IsValid(index) && !IsValid(index + 1))
				return null;
			var d = bytes;
			if ((ulong)index + 1 >= (ulong)d.LongLength)
				return null;
			return (short)(d[index + 1] | (d[index] << 8));
		}

		/// <summary>
		/// Returns the <see cref="uint"/> at <paramref name="index"/> or null if all bytes are invalid
		/// </summary>
		/// <param name="index">Index of value</param>
		/// <returns></returns>
		public uint? TryReadUInt32BigEndian(long index) {
			if (!IsValid(index) && !IsValid(index + 1) && !IsValid(index + 2) && !IsValid(index + 3))
				return null;
			var d = bytes;
			if ((ulong)index + 3 >= (ulong)d.LongLength)
				return null;
			return (uint)(d[index + 3] | (d[index + 2] << 8) | (d[index + 1] << 16) | (d[index] << 24));
		}

		/// <summary>
		/// Returns the <see cref="int"/> at <paramref name="index"/> or null if all bytes are invalid
		/// </summary>
		/// <param name="index">Index of value</param>
		/// <returns></returns>
		public int? TryReadInt32BigEndian(long index) {
			if (!IsValid(index) && !IsValid(index + 1) && !IsValid(index + 2) && !IsValid(index + 3))
				return null;
			var d = bytes;
			if ((ulong)index + 3 >= (ulong)d.LongLength)
				return null;
			return d[index + 3] | (d[index + 2] << 8) | (d[index + 1] << 16) | (d[index] << 24);
		}

		/// <summary>
		/// Returns the <see cref="ulong"/> at <paramref name="index"/> or null if all bytes are invalid
		/// </summary>
		/// <param name="index">Index of value</param>
		/// <returns></returns>
		public ulong? TryReadUInt64BigEndian(long index) {
			if (!IsValid(index) && !IsValid(index + 1) && !IsValid(index + 2) && !IsValid(index + 3) && !IsValid(index + 4) && !IsValid(index + 5) && !IsValid(index + 6) && !IsValid(index + 7))
				return null;
			var d = bytes;
			if ((ulong)index + 7 >= (ulong)d.LongLength)
				return null;
			return d[index + 7] |
					((ulong)d[index + 6] << 8) |
					((ulong)d[index + 5] << 16) |
					((ulong)d[index + 4] << 24) |
					((ulong)d[index + 3] << 32) |
					((ulong)d[index + 2] << 40) |
					((ulong)d[index + 1] << 48) |
					((ulong)d[index] << 56);
		}

		/// <summary>
		/// Returns the <see cref="long"/> at <paramref name="index"/> or null if all bytes are invalid
		/// </summary>
		/// <param name="index">Index of value</param>
		/// <returns></returns>
		public long? TryReadInt64BigEndian(long index) {
			if (!IsValid(index) && !IsValid(index + 1) && !IsValid(index + 2) && !IsValid(index + 3) && !IsValid(index + 4) && !IsValid(index + 5) && !IsValid(index + 6) && !IsValid(index + 7))
				return null;
			var d = bytes;
			if ((ulong)index + 7 >= (ulong)d.LongLength)
				return null;
			return d[index + 7] |
					((long)d[index + 6] << 8) |
					((long)d[index + 5] << 16) |
					((long)d[index + 4] << 24) |
					((long)d[index + 3] << 32) |
					((long)d[index + 2] << 40) |
					((long)d[index + 1] << 48) |
					((long)d[index] << 56);
		}

		/// <summary>
		/// Returns the <see cref="float"/> at <paramref name="index"/> or null if all bytes are invalid
		/// </summary>
		/// <param name="index">Index of value</param>
		/// <returns></returns>
		public unsafe float? TryReadSingleBigEndian(long index) {
			var v = TryReadInt32BigEndian(index);
			if (v == null)
				return null;
			int v2 = v.Value;
			return *(float*)&v2;
		}

		/// <summary>
		/// Returns the <see cref="double"/> at <paramref name="index"/> or null if all bytes are invalid
		/// </summary>
		/// <param name="index">Index of value</param>
		/// <returns></returns>
		public unsafe double? TryReadDoubleBigEndian(long index) {
			var v = TryReadInt64BigEndian(index);
			if (v == null)
				return null;
			long v2 = v.Value;
			return *(double*)&v2;
		}

		/// <summary>
		/// Reads bytes
		/// </summary>
		/// <param name="index">Index of data</param>
		/// <param name="destination">Destination array</param>
		/// <param name="destinationIndex">Index in <paramref name="destination"/></param>
		/// <param name="length">Number of bytes to copy</param>
		public void ReadBytes(long index, byte[] destination, long destinationIndex, long length) {
			if (index >= bytes.LongLength)
				throw new ArgumentOutOfRangeException(nameof(index));
			long bytesToRead = length;
			if (index + bytesToRead > bytes.LongLength)
				bytesToRead = bytes.LongLength - index;
			Array.Copy(bytes, index, destination, destinationIndex, bytesToRead);
			while (bytesToRead < length)
				destination[bytesToRead++] = 0;
		}
	}
}
