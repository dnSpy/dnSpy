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

		readonly byte[] bytes;
		readonly BitArray validBytes;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="bytes">Bytes, all bytes are assumed to be valid</param>
		public HexBytes(byte[] bytes) {
			if (bytes == null)
				throw new ArgumentNullException(nameof(bytes));
			this.bytes = bytes;
			this.validBytes = null;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="bytes">Bytes</param>
		/// <param name="validBytes">Valid bytes</param>
		public HexBytes(byte[] bytes, BitArray validBytes) {
			if (bytes == null)
				throw new ArgumentNullException(nameof(bytes));
			if (validBytes == null)
				throw new ArgumentNullException(nameof(validBytes));
			if (validBytes.Length != bytes.LongLength)
				throw new ArgumentOutOfRangeException(nameof(bytes));
			this.bytes = bytes;
			this.validBytes = validBytes;
		}

		/// <summary>
		/// Checks whether the byte at <paramref name="index"/> is valid
		/// </summary>
		/// <param name="index">Index of byte</param>
		/// <returns></returns>
		public bool IsValid(int index) => validBytes == null || validBytes.Get(index);

		/// <summary>
		/// Checks whether the byte at <paramref name="index"/> is valid
		/// </summary>
		/// <param name="index">Index of byte</param>
		/// <returns></returns>
		public bool IsValid(long index) => validBytes == null || validBytes.Get(checked((int)index));

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
		/// Returns the byte at <paramref name="index"/> or a value less than 0 if the byte is invalid
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
		/// Reads the byte at <paramref name="index"/>
		/// </summary>
		/// <param name="index">Index of byte</param>
		/// <returns></returns>
		public byte ReadByte(int index) => bytes[index];

		/// <summary>
		/// Reads the byte at <paramref name="index"/>
		/// </summary>
		/// <param name="index">Index of byte</param>
		/// <returns></returns>
		public byte ReadByte(long index) => bytes[index];
	}
}
