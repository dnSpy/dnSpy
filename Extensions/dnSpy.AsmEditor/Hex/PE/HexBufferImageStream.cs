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
using System.IO;
using dnlib.IO;
using dnSpy.Contracts.Hex;

namespace dnSpy.AsmEditor.Hex.PE {
	sealed class HexBufferImageStreamCreator : IImageStreamCreator {
		public string FileName { get; }
		public long Length { get; }

		readonly HexBuffer buffer;
		readonly HexSpan dataSpan;
		bool disposed;

		public HexBufferImageStreamCreator(HexBuffer buffer, HexSpan dataSpan) {
			if (buffer == null)
				throw new ArgumentNullException(nameof(buffer));
			if (disposed)
				throw new ObjectDisposedException(nameof(HexBufferImageStreamCreator));
			this.buffer = buffer;
			FileName = buffer.IsMemory ? null : buffer.Name;
			Length = buffer.Span.Length > long.MaxValue ? long.MaxValue : (long)buffer.Span.Length.ToUInt64();
		}

		public IImageStream Create(FileOffset offset, long length) {
			if (disposed)
				throw new ObjectDisposedException(nameof(HexBufferImageStreamCreator));
			if (offset < 0 || length < 0)
				return MemoryImageStream.CreateEmpty();

			long offs = Math.Min(Length, (long)offset);
			long len = Math.Min(Length - offs, length);
			return new HexBufferImageStream(buffer, dataSpan.Start, offset, offs, len);
		}

		public IImageStream CreateFull() => Create(0, Length);
		public void Dispose() => disposed = true;
	}

	sealed class HexBufferImageStream : IImageStream {
		public FileOffset FileOffset { get; }
		public long Length => endAddr - startAddr;
		public long Position {
			get { return currentAddr - startAddr; }
			set {
				long newAddr = startAddr + value;
				if (newAddr < startAddr)
					newAddr = endAddr;
				currentAddr = newAddr;
			}
		}

		readonly HexBuffer buffer;
		readonly HexPosition bufferDataStart;
		readonly long startAddr;
		readonly long endAddr;
		long currentAddr;

		public HexBufferImageStream(HexBuffer buffer, HexPosition bufferDataStart, FileOffset fileOffset, long baseAddr, long length) {
			if (buffer == null)
				throw new ArgumentNullException(nameof(buffer));
			this.buffer = buffer;
			this.bufferDataStart = bufferDataStart;
			FileOffset = fileOffset;
			startAddr = baseAddr;
			endAddr = baseAddr + length;
			currentAddr = startAddr;
		}

		public IImageStream Create(FileOffset offset, long length) {
			if (offset < 0 || length < 0)
				return MemoryImageStream.CreateEmpty();

			long offs = Math.Min(Length, (long)offset);
			long len = Math.Min(Length - offs, length);
			return new HexBufferImageStream(buffer, bufferDataStart, (FileOffset)((long)FileOffset + (long)offset), startAddr + (long)offs, len);
		}

		public byte[] ReadBytes(int size) {
			if (size < 0)
				throw new IOException("Invalid size");
			size = (int)Math.Min(size, Length - Math.Min(Length, Position));
			var newData = new byte[size];
			buffer.ReadBytes(bufferDataStart + currentAddr, newData, 0, newData.Length);
			currentAddr += size;
			return newData;
		}

		public int Read(byte[] buffer, int offset, int length) {
			if (length < 0)
				throw new IOException("Invalid size");
			length = (int)Math.Min(length, Length - Math.Min(Length, Position));
			this.buffer.ReadBytes(bufferDataStart + currentAddr, buffer, offset, length);
			currentAddr += length;
			return length;
		}

		public byte[] ReadBytesUntilByte(byte b) {
			long pos = GetPositionOf(b);
			if (pos == -1)
				return null;
			return ReadBytes((int)(pos - currentAddr));
		}

		long GetPositionOf(byte b) {
			var pos = bufferDataStart + currentAddr;
			var posStart = pos;
			var endPos = bufferDataStart + endAddr;
			while (pos < endPos) {
				if (buffer.ReadByte(pos) == b)
					return currentAddr + (long)(pos - posStart).ToUInt64();
				pos++;
			}
			return -1;
		}

		public sbyte ReadSByte() {
			if (currentAddr >= endAddr)
				throw new IOException("Can't read one SByte");
			return buffer.ReadSByte(bufferDataStart + currentAddr++);
		}

		public byte ReadByte() {
			if (currentAddr >= endAddr)
				throw new IOException("Can't read one Byte");
			return buffer.ReadByte(bufferDataStart + currentAddr++);
		}

		public short ReadInt16() {
			if (currentAddr + 1 >= endAddr)
				throw new IOException("Can't read one Int16");
			short val = buffer.ReadInt16(bufferDataStart + currentAddr);
			currentAddr += 2;
			return val;
		}

		public ushort ReadUInt16() {
			if (currentAddr + 1 >= endAddr)
				throw new IOException("Can't read one UInt16");
			ushort val = buffer.ReadUInt16(bufferDataStart + currentAddr);
			currentAddr += 2;
			return val;
		}

		public int ReadInt32() {
			if (currentAddr + 3 >= endAddr)
				throw new IOException("Can't read one Int32");
			int val = buffer.ReadInt32(bufferDataStart + currentAddr);
			currentAddr += 4;
			return val;
		}

		public uint ReadUInt32() {
			if (currentAddr + 3 >= endAddr)
				throw new IOException("Can't read one UInt32");
			uint val = buffer.ReadUInt32(bufferDataStart + currentAddr);
			currentAddr += 4;
			return val;
		}

		public long ReadInt64() {
			if (currentAddr + 7 >= endAddr)
				throw new IOException("Can't read one Int64");
			long val = buffer.ReadInt64(bufferDataStart + currentAddr);
			currentAddr += 8;
			return val;
		}

		public ulong ReadUInt64() {
			if (currentAddr + 7 >= endAddr)
				throw new IOException("Can't read one UInt64");
			ulong val = buffer.ReadUInt64(bufferDataStart + currentAddr);
			currentAddr += 8;
			return val;
		}

		public float ReadSingle() {
			if (currentAddr + 3 >= endAddr)
				throw new IOException("Can't read one Single");
			var val = buffer.ReadSingle(bufferDataStart + currentAddr);
			currentAddr += 4;
			return val;
		}

		public double ReadDouble() {
			if (currentAddr + 7 >= endAddr)
				throw new IOException("Can't read one Double");
			var val = buffer.ReadDouble(bufferDataStart + currentAddr);
			currentAddr += 8;
			return val;
		}

		public string ReadString(int chars) {
			if (currentAddr + chars * 2 < currentAddr || (chars != 0 && currentAddr + chars * 2 - 1 >= endAddr))
				throw new IOException("Not enough space to read the string");
			var s = new string(ReadChars(chars), 0, chars);
			currentAddr += chars * 2;
			return s;
		}

		char[] ReadChars(int count) {
			var chars = new char[count];
			var addr = bufferDataStart + currentAddr;
			for (int i = 0; i < count; i++)
				chars[i] = (char)buffer.ReadUInt16(addr++);
			return chars;
		}

		public void Dispose() { }
	}
}
