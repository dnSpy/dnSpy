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

using System;
using System.IO;

namespace dnSpy.Debugger.DotNet.Metadata.Impl {
	abstract class DmdDataStream : IDisposable {
		public abstract byte ReadByte();
		public abstract uint ReadUInt32();
		public abstract void Dispose();

		public uint ReadCompressedUInt32() {
			byte b = ReadByte();
			if ((b & 0x80) == 0)
				return b;

			if ((b & 0xC0) == 0x80)
				return (uint)(((b & 0x3F) << 8) | ReadByte());

			return (uint)(((b & 0x1F) << 24) | (ReadByte() << 16) | (ReadByte() << 8) | ReadByte());
		}

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
	}
}
