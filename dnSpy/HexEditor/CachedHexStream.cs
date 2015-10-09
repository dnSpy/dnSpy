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
using System.Diagnostics;

namespace dnSpy.HexEditor {
	public sealed class CachedHexStream : IHexStream {
		const ulong DEFAULT_PAGE_SIZE = 0x1000;
		const ulong MAX_PAGE_SIZE = 0x1000;
		const ulong MIN_PAGE_SIZE = 0x200;
		const ulong CACHE_SIZE = 4 * 0x1000;

		sealed class CachedPage {
			public readonly int Index;
			public ulong Offset;
			public bool IsInitialized;
			public readonly byte[] Data;
			// Valid data size. The remaining bytes are cleared.
			public int DataSize;
			public CachedPage(int index, int size) {
				this.Index = index;
				this.IsInitialized = false;
				this.Data = new byte[size];
			}
		}

		public ulong StartOffset {
			get { return simpleHexStream.StartOffset; }
		}

		public ulong EndOffset {
			get { return simpleHexStream.EndOffset; }
		}

		public ulong Size {
			get { return simpleHexStream.Size; }
		}

		readonly ISimpleHexStream simpleHexStream;
		readonly CachedPage[] cachedPages;
		int lastHitIndex;
		readonly ulong pageSize;
		readonly ulong pageSizeMask;

		public CachedHexStream(ISimpleHexStream simpleHexStream) {
			this.simpleHexStream = simpleHexStream;
			pageSize = simpleHexStream.PageSize;
			Debug.Assert(pageSize == 0 || IsPowerOfTwo(pageSize));
			if (pageSize == 0 || !IsPowerOfTwo(pageSize))
				pageSize = DEFAULT_PAGE_SIZE;
			if (pageSize > MAX_PAGE_SIZE)
				pageSize = MAX_PAGE_SIZE;
			Debug.Assert(pageSize >= MIN_PAGE_SIZE);
			if (pageSize < MIN_PAGE_SIZE)
				pageSize = MIN_PAGE_SIZE;
			Debug.Assert(IsPowerOfTwo(pageSize));
			this.pageSizeMask = pageSize - 1;

			int numCachedPages = (int)((CACHE_SIZE + pageSizeMask) / pageSize);
			this.cachedPages = new CachedPage[numCachedPages];
			for (int i = 0; i < this.cachedPages.Length; i++)
				this.cachedPages[i] = new CachedPage(i, (int)pageSize);
		}

		static bool IsPowerOfTwo(ulong v) {
			return v != 0 && (v & (v - 1)) == 0;
		}

		public void ClearCache() {
			foreach (var cp in cachedPages)
				cp.IsInitialized = false;
			lastHitIndex = 0;
		}

		CachedPage GetCachedPage(ulong offset) {
			ulong pageOffset = offset & ~pageSizeMask;
			for (int i = 0; i < cachedPages.Length; i++) {
				var cp = cachedPages[(i + lastHitIndex) % cachedPages.Length];
				if (cp.IsInitialized && cp.Offset == pageOffset)
					return cp;
			}

			CachedPage foundCp = null;
			for (int i = 0; i < cachedPages.Length; i++) {
				var cp = cachedPages[(i + lastHitIndex) % cachedPages.Length];
				if (!cp.IsInitialized) {
					foundCp = cp;
					break;
				}
			}
			if (foundCp == null)
				foundCp = cachedPages[(lastHitIndex + 1) % cachedPages.Length];

			lastHitIndex = foundCp.Index;
			Initialize(foundCp, pageOffset);
			return foundCp;
		}

		void Initialize(CachedPage cp, ulong pageOffset) {
			Debug.Assert((pageOffset & pageSizeMask) == 0);
			int sizeRead = simpleHexStream.Read(pageOffset, cp.Data, 0, cp.Data.Length);
			cp.DataSize = sizeRead;
			cp.Offset = pageOffset;
			cp.IsInitialized = true;
			if (sizeRead != cp.Data.Length)
				Array.Clear(cp.Data, sizeRead, cp.Data.Length - sizeRead);
		}

		void Invalidate(ulong offset, int size) {
			if (size <= 0)
				return;
			ulong startPage = offset & ~pageSizeMask;
			ulong endPage = (offset + (ulong)size - 1) & ~pageSizeMask;
			for (int i = 0; i < cachedPages.Length; i++) {
				var cp = cachedPages[i];
				if (!cp.IsInitialized)
					continue;
				if (startPage <= cp.Offset && cp.Offset <= endPage) {
					cp.IsInitialized = false;
					//TODO: Perhaps we should just re-read the data. It's usually just one byte
				}
			}
		}

		public void Read(ulong offset, byte[] array, long index, int count) {
			while (count > 0) {
				var cp = GetCachedPage(offset);
				long srcIndex = (long)(offset - cp.Offset);
				int partSize = (int)(pageSize - (ulong)srcIndex);
				if (partSize > count)
					partSize = count;
				Array.Copy(cp.Data, srcIndex, array, index, partSize);
				offset += (ulong)partSize;
				count -= partSize;
				index += partSize;
			}
		}

		public int ReadByte(ulong offset) {
			int index = (int)(offset & pageSizeMask);
			var cp = GetCachedPage(offset);
			if (index >= cp.DataSize)
				return -1;
			return cp.Data[index];
		}

		byte[] ReadSlow(ulong offset, int size) {
			for (int i = 0; i < size; i++) {
				int b = ReadByte(offset);
				slowBuf[i] = (byte)(b < 0 ? 0 : b);
			}
			return slowBuf;
		}
		readonly byte[] slowBuf = new byte[8];

		public short ReadInt16(ulong offset) {
			int index = (int)(offset & pageSizeMask);
			var cp = GetCachedPage(offset);
			if (index + 1 < cp.Data.Length)
				return (short)(cp.Data[0] | (cp.Data[1] << 8));
			return BitConverter.ToInt16(ReadSlow(offset, 2), 0);
		}

		public int ReadInt32(ulong offset) {
			int index = (int)(offset & pageSizeMask);
			var cp = GetCachedPage(offset);
			if (index + 3 < cp.Data.Length)
				return cp.Data[0] | (cp.Data[1] << 8) | (cp.Data[2] << 16) | (cp.Data[3] << 24);
			return BitConverter.ToInt32(ReadSlow(offset, 4), 0);
		}

		public long ReadInt64(ulong offset) {
			int index = (int)(offset & pageSizeMask);
			var cp = GetCachedPage(offset);
			if (index + 7 < cp.Data.Length) {
				return ((long)cp.Data[0] | ((long)cp.Data[1] << 8) | ((long)cp.Data[2] << 16) | ((long)cp.Data[3] << 24) |
					((long)cp.Data[4] << 32) | ((long)cp.Data[5] << 40) | ((long)cp.Data[6] << 48) | ((long)cp.Data[7] << 56));
			}
			return BitConverter.ToInt64(ReadSlow(offset, 8), 0);
		}

		public ushort ReadUInt16(ulong offset) {
			int index = (int)(offset & pageSizeMask);
			var cp = GetCachedPage(offset);
			if (index + 1 < cp.Data.Length)
				return (ushort)(cp.Data[0] | (cp.Data[1] << 8));
			return BitConverter.ToUInt16(ReadSlow(offset, 2), 0);
		}

		public uint ReadUInt32(ulong offset) {
			int index = (int)(offset & pageSizeMask);
			var cp = GetCachedPage(offset);
			if (index + 3 < cp.Data.Length)
				return (uint)(cp.Data[0] | (cp.Data[1] << 8) | (cp.Data[2] << 16) | (cp.Data[3] << 24));
			return BitConverter.ToUInt32(ReadSlow(offset, 4), 0);
		}

		public ulong ReadUInt64(ulong offset) {
			int index = (int)(offset & pageSizeMask);
			var cp = GetCachedPage(offset);
			if (index + 7 < cp.Data.Length) {
				return ((ulong)cp.Data[0] | ((ulong)cp.Data[1] << 8) | ((ulong)cp.Data[2] << 16) | ((ulong)cp.Data[3] << 24) |
					((ulong)cp.Data[4] << 32) | ((ulong)cp.Data[5] << 40) | ((ulong)cp.Data[6] << 48) | ((ulong)cp.Data[7] << 56));
			}
			return BitConverter.ToUInt64(ReadSlow(offset, 8), 0);
		}

		public void Write(ulong offset, byte b) {
			byteBuf[0] = b;
			Write(offset, byteBuf, 0, 1);
		}
		readonly byte[] byteBuf = new byte[1];

		public void Write(ulong offset, byte[] array, long index, int count) {
			simpleHexStream.Write(offset, array, index, count);
			Invalidate(offset, count);
		}
	}
}
