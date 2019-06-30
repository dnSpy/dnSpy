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

namespace dnSpy.Debugger.DotNet.Metadata {
	/// <summary>
	/// Data stream
	/// </summary>
	public abstract class DmdDataStream : IDisposable {
		/// <summary>
		/// Gets/sets the position
		/// </summary>
		public abstract long Position { get; set; }

		/// <summary>
		/// Gets the stream length
		/// </summary>
		public abstract long Length { get; }

		/// <summary>
		/// Reads a <see cref="byte"/>
		/// </summary>
		/// <returns></returns>
		public abstract byte ReadByte();

		/// <summary>
		/// Reads a <see cref="ushort"/>
		/// </summary>
		/// <returns></returns>
		public abstract ushort ReadUInt16();

		/// <summary>
		/// Reads a <see cref="uint"/>
		/// </summary>
		/// <returns></returns>
		public abstract uint ReadUInt32();

		/// <summary>
		/// Reads a <see cref="ulong"/>
		/// </summary>
		/// <returns></returns>
		public abstract ulong ReadUInt64();

		/// <summary>
		/// Reads a <see cref="float"/>
		/// </summary>
		/// <returns></returns>
		public abstract float ReadSingle();

		/// <summary>
		/// Reads a <see cref="double"/>
		/// </summary>
		/// <returns></returns>
		public abstract double ReadDouble();

		/// <summary>
		/// Reads bytes
		/// </summary>
		/// <param name="length">Number of bytes to read</param>
		/// <returns></returns>
		public abstract byte[] ReadBytes(int length);

		/// <summary>
		/// Reads a <see cref="sbyte"/>
		/// </summary>
		/// <returns></returns>
		public sbyte ReadSByte() => (sbyte)ReadByte();

		/// <summary>
		/// Reads a <see cref="short"/>
		/// </summary>
		/// <returns></returns>
		public short ReadInt16() => (short)ReadUInt16();

		/// <summary>
		/// Reads a <see cref="int"/>
		/// </summary>
		/// <returns></returns>
		public int ReadInt32() => (int)ReadUInt32();

		/// <summary>
		/// Reads a <see cref="long"/>
		/// </summary>
		/// <returns></returns>
		public long ReadInt64() => (long)ReadUInt64();

		/// <summary>
		/// Reads a compressed <see cref="uint"/>
		/// </summary>
		/// <returns></returns>
		public uint ReadCompressedUInt32() => ReadCompressedUInt32(ReadByte());
		internal uint ReadCompressedUInt32(byte b) {
			if ((b & 0x80) == 0)
				return b;

			if ((b & 0xC0) == 0x80)
				return (uint)(((b & 0x3F) << 8) | ReadByte());

			return (uint)(((b & 0x1F) << 24) | (ReadByte() << 16) | (ReadByte() << 8) | ReadByte());
		}

		/// <summary>
		/// Reads a compressed <see cref="int"/>
		/// </summary>
		/// <returns></returns>
		public int ReadCompressedInt32() {
			byte b = ReadByte();
			if ((b & 0x80) == 0) {
				if ((b & 1) != 0)
					return -0x40 | (b >> 1);
				return b >> 1;
			}

			if ((b & 0xC0) == 0x80) {
				uint tmp = (uint)(((b & 0x3F) << 8) | ReadByte());
				if ((tmp & 1) != 0)
					return -0x2000 | (int)(tmp >> 1);
				return (int)(tmp >> 1);
			}

			if ((b & 0xE0) == 0xC0) {
				uint tmp = (uint)(((b & 0x1F) << 24) | (ReadByte() << 16) |
						(ReadByte() << 8) | ReadByte());
				if ((tmp & 1) != 0)
					return -0x10000000 | (int)(tmp >> 1);
				return (int)(tmp >> 1);
			}

			throw new IOException();
		}

		/// <summary>
		/// Disposes this instance
		/// </summary>
		public abstract void Dispose();
	}
}
