/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using dnlib.IO;

namespace dndbg.DotNet {
	public sealed class ProcessBinaryReader : IBinaryReader {
		const int CACHE_SIZE = 0x100;
		readonly IProcessReader reader;
		readonly ulong baseAddress;
		readonly byte[] cache;
		ulong cacheAddress;
		bool cacheValid;
		ulong address;

		public ProcessBinaryReader(IProcessReader reader, ulong address) {
			this.reader = reader;
			this.baseAddress = address;
			this.cache = new byte[CACHE_SIZE];
			this.cacheAddress = 0;
			this.cacheValid = false;
			this.address = address;
		}

		public long Length {
			get { return long.MaxValue; }
		}

		public long Position {
			get { return (long)(address - baseAddress); }
			set { address = baseAddress + (ulong)value; }
		}

		public void Dispose() {
			var id = reader as IDisposable;
			if (id != null)
				id.Dispose();
		}

		public int Read(byte[] buffer, int offset, int length) {
			int sizeRead = reader.ReadBytes(address, buffer, offset, length);
			address += (ulong)sizeRead;
			return sizeRead;
		}

		public byte[] ReadBytes(int size) {
			var data = new byte[size];
			int sizeRead = reader.ReadBytes(address, data, 0, data.Length);
			address += (ulong)sizeRead;
			return data;
		}

		public byte[] ReadBytesUntilByte(byte b) {
			var list = new List<byte>();
			var origAddr = address;
			const int MAX_BYTES_TO_CHECK = 0x10000;
			for (int i = 0; ; i++) {
				if (i >= MAX_BYTES_TO_CHECK) {
					address = origAddr;
					return null;
				}
				var b2 = ReadByte();
				if (b == b2) {
					address--;
					return list.ToArray();
				}
				list.Add(b2);
			}
		}

		int InitializeCache(int size) {
			Debug.Assert(size > 0 && size <= cache.Length);
			if (!cacheValid || address < cacheAddress || address + (ulong)size > cacheAddress + (ulong)cache.Length) {
				InitializeCacheAddress(address);
				return 0;
			}
			return (int)(address - cacheAddress);
		}

		void InitializeCacheAddress(ulong addr) {
			cacheAddress = addr;
			int sizeRead = reader.ReadBytes(cacheAddress, cache, 0, cache.Length);
			Debug.Assert(sizeRead != 0);
			// Check if we tried to read non-present memory or if there was some other error.
			if (sizeRead == 0)
				throw new IOException();
			if (sizeRead != cache.Length)
				Array.Clear(cache, sizeRead, cache.Length - sizeRead);
			cacheValid = true;
		}

		public byte ReadByte() {
			const int SIZE = 1;
			int cacheIndex = InitializeCache(SIZE);
			byte val = cache[cacheIndex];
			address += SIZE;
			return val;
		}

		public ushort ReadUInt16() {
			const int SIZE = 2;
			int cacheIndex = InitializeCache(SIZE);
			ushort val = (ushort)(cache[cacheIndex] | (cache[cacheIndex + 1] << 8));
			address += SIZE;
			return val;
		}

		public uint ReadUInt32() {
			const int SIZE = 4;
			int cacheIndex = InitializeCache(SIZE);
			uint val = (uint)(cache[cacheIndex] | (cache[cacheIndex + 1] << 8) |
								(cache[cacheIndex + 2] << 16) | (cache[cacheIndex + 3] << 24));
			address += SIZE;
			return val;
		}

		public ulong ReadUInt64() {
			const int SIZE = 8;
			int cacheIndex = InitializeCache(SIZE);
			ulong val = ((ulong)cache[cacheIndex] | ((ulong)cache[cacheIndex + 1] << 8) |
						((ulong)cache[cacheIndex + 2] << 16) | ((ulong)cache[cacheIndex + 3] << 24) |
						((ulong)cache[cacheIndex + 4] << 32) | ((ulong)cache[cacheIndex + 5] << 40) |
						((ulong)cache[cacheIndex + 6] << 48) | ((ulong)cache[cacheIndex + 7] << 56));
			address += SIZE;
			return val;
		}

		public sbyte ReadSByte() {
			const int SIZE = 1;
			int cacheIndex = InitializeCache(SIZE);
			byte val = cache[cacheIndex];
			address += SIZE;
			return (sbyte)val;
		}

		public short ReadInt16() {
			const int SIZE = 2;
			int cacheIndex = InitializeCache(SIZE);
			ushort val = (ushort)(cache[cacheIndex] | (cache[cacheIndex + 1] << 8));
			address += SIZE;
			return (short)val;
		}

		public int ReadInt32() {
			const int SIZE = 4;
			int cacheIndex = InitializeCache(SIZE);
			uint val = (uint)(cache[cacheIndex] | (cache[cacheIndex + 1] << 8) |
								(cache[cacheIndex + 2] << 16) | (cache[cacheIndex + 3] << 24));
			address += SIZE;
			return (int)val;
		}

		public long ReadInt64() {
			const int SIZE = 8;
			int cacheIndex = InitializeCache(SIZE);
			ulong val = ((ulong)cache[cacheIndex] | ((ulong)cache[cacheIndex + 1] << 8) |
						((ulong)cache[cacheIndex + 2] << 16) | ((ulong)cache[cacheIndex + 3] << 24) |
						((ulong)cache[cacheIndex + 4] << 32) | ((ulong)cache[cacheIndex + 5] << 40) |
						((ulong)cache[cacheIndex + 6] << 48) | ((ulong)cache[cacheIndex + 7] << 56));
			address += SIZE;
			return (long)val;
		}

		public float ReadSingle() {
			const int SIZE = 4;
			int cacheIndex = InitializeCache(SIZE);
			float val = BitConverter.ToSingle(cache, cacheIndex);
			address += SIZE;
			return val;
		}

		public double ReadDouble() {
			const int SIZE = 8;
			int cacheIndex = InitializeCache(SIZE);
			double val = BitConverter.ToDouble(cache, cacheIndex);
			address += SIZE;
			return val;
		}

		public string ReadString(int chars) {
			var buf = new char[chars];
			for (int i = 0; i < buf.Length; i++)
				buf[i] = (char)ReadUInt16();
			return new string(buf);
		}
	}
}
