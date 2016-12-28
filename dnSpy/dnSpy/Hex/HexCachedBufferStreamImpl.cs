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
	sealed class HexCachedBufferStreamImpl : HexCachedBufferStream {
		public override HexSpan Span => simpleStream.Span;
		public override string Name => simpleStream.Name;
		public override bool IsReadOnly => simpleStream.IsReadOnly;
		public override bool IsVolatile => simpleStream.IsVolatile;
		public override event EventHandler<HexBufferStreamSpanInvalidatedEventArgs> BufferStreamSpanInvalidated;

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
				Index = index;
				IsInitialized = false;
				Data = new byte[size];
			}
		}

		HexSimpleBufferStream simpleStream;
		readonly CachedPage[] cachedPages;
		int lastHitIndex;
		readonly object lockObj;
		readonly ulong pageSize;
		readonly ulong pageSizeMask;
		readonly bool disposeStream;

		public HexCachedBufferStreamImpl(HexSimpleBufferStream simpleStream, bool disposeStream) {
			if (simpleStream == null)
				throw new ArgumentNullException(nameof(simpleStream));
			lockObj = new object();
			this.simpleStream = simpleStream;
			this.disposeStream = disposeStream;
			pageSize = simpleStream.PageSize;
			Debug.Assert(pageSize == 0 || IsPowerOfTwo(pageSize));
			if (pageSize == 0 || !IsPowerOfTwo(pageSize))
				pageSize = DEFAULT_PAGE_SIZE;
			if (pageSize > MAX_PAGE_SIZE)
				pageSize = MAX_PAGE_SIZE;
			Debug.Assert(pageSize >= MIN_PAGE_SIZE);
			if (pageSize < MIN_PAGE_SIZE)
				pageSize = MIN_PAGE_SIZE;
			Debug.Assert(IsPowerOfTwo(pageSize));
			pageSizeMask = pageSize - 1;

			int numCachedPages = (int)((CACHE_SIZE + pageSizeMask) / pageSize);
			cachedPages = new CachedPage[numCachedPages];
			for (int i = 0; i < cachedPages.Length; i++)
				cachedPages[i] = new CachedPage(i, (int)pageSize);
		}

		static bool IsPowerOfTwo(ulong v) => v != 0 && (v & (v - 1)) == 0;

		CachedPage GetCachedPage_NoLock(HexPosition position) {
			ulong pageOffset = position.ToUInt64() & ~pageSizeMask;
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
			Initialize_NoLock(foundCp, pageOffset);
			return foundCp;
		}

		void Initialize_NoLock(CachedPage cp, ulong pageOffset) {
			Debug.Assert((pageOffset & pageSizeMask) == 0);
			int sizeRead = (int)simpleStream.Read(pageOffset, cp.Data, 0, cp.Data.Length).ToUInt64();
			cp.DataSize = sizeRead;
			cp.Offset = pageOffset;
			cp.IsInitialized = true;
			if (sizeRead != cp.Data.Length)
				Array.Clear(cp.Data, sizeRead, cp.Data.Length - sizeRead);
		}

		void InvalidateCore(HexSpan span) {
			if (span.IsEmpty)
				return;
			ulong startPage = span.Start.ToUInt64() & ~pageSizeMask;
			ulong endPage = (span.End.ToUInt64() - 1) & ~pageSizeMask;
			lock (lockObj) {
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
		}

		public override HexSpanInfo GetSpanInfo(HexPosition position) =>
			simpleStream.GetSpanInfo(position);

		public override int TryReadByte(HexPosition position) {
			Debug.Assert(position < HexPosition.MaxEndPosition);
			int index = (int)(position.ToUInt64() & pageSizeMask);
			lock (lockObj) {
				var cp = GetCachedPage_NoLock(position);
				if (index >= cp.DataSize)
					return -1;
				return cp.Data[index];
			}
		}

		byte[] ReadSlow(HexPosition position, int size) {
			var slowBuf = new byte[size];
			for (int i = 0; i < size; i++) {
				int b = ReadByte(position);
				position += 1;
				slowBuf[i] = (byte)(b < 0 ? 0 : b);
			}
			return slowBuf;
		}

		public override byte ReadByte(HexPosition position) {
			Debug.Assert(position < HexPosition.MaxEndPosition);
			int index = (int)(position.ToUInt64() & pageSizeMask);
			lock (lockObj) {
				var cp = GetCachedPage_NoLock(position);
				if (index >= cp.DataSize)
					return 0;
				return cp.Data[index];
			}
		}

		public override sbyte ReadSByte(HexPosition position) {
			Debug.Assert(position < HexPosition.MaxEndPosition);
			int index = (int)(position.ToUInt64() & pageSizeMask);
			lock (lockObj) {
				var cp = GetCachedPage_NoLock(position);
				if (index >= cp.DataSize)
					return 0;
				return (sbyte)cp.Data[index];
			}
		}

		public override short ReadInt16(HexPosition position) {
			Debug.Assert(position < HexPosition.MaxEndPosition);
			int index = (int)(position.ToUInt64() & pageSizeMask);
			lock (lockObj) {
				var cp = GetCachedPage_NoLock(position);
				var data = cp.Data;
				if (index + 1 < data.Length)
					return (short)(data[index] | (data[index + 1] << 8));
				return BitConverter.ToInt16(ReadSlow(position, 2), 0);
			}
		}

		public override ushort ReadUInt16(HexPosition position) {
			Debug.Assert(position < HexPosition.MaxEndPosition);
			int index = (int)(position.ToUInt64() & pageSizeMask);
			lock (lockObj) {
				var cp = GetCachedPage_NoLock(position);
				var data = cp.Data;
				if (index + 1 < data.Length)
					return (ushort)(data[index] | (data[index + 1] << 8));
				return BitConverter.ToUInt16(ReadSlow(position, 2), 0);
			}
		}

		public override int ReadInt32(HexPosition position) {
			Debug.Assert(position < HexPosition.MaxEndPosition);
			int index = (int)(position.ToUInt64() & pageSizeMask);
			lock (lockObj) {
				var cp = GetCachedPage_NoLock(position);
				var data = cp.Data;
				if (index + 3 < data.Length)
					return data[index] | (data[index + 1] << 8) | (data[index + 2] << 16) | (data[index + 3] << 24);
				return BitConverter.ToInt32(ReadSlow(position, 4), 0);
			}
		}

		public override uint ReadUInt32(HexPosition position) {
			Debug.Assert(position < HexPosition.MaxEndPosition);
			int index = (int)(position.ToUInt64() & pageSizeMask);
			lock (lockObj) {
				var cp = GetCachedPage_NoLock(position);
				var data = cp.Data;
				if (index + 3 < data.Length)
					return (uint)(data[index] | (data[index + 1] << 8) | (data[index + 2] << 16) | (data[index + 3] << 24));
				return BitConverter.ToUInt32(ReadSlow(position, 4), 0);
			}
		}

		public override long ReadInt64(HexPosition position) {
			Debug.Assert(position < HexPosition.MaxEndPosition);
			int index = (int)(position.ToUInt64() & pageSizeMask);
			lock (lockObj) {
				var cp = GetCachedPage_NoLock(position);
				var data = cp.Data;
				if (index + 7 < data.Length) {
					return ((long)data[index] | ((long)data[index + 1] << 8) | ((long)data[index + 2] << 16) | ((long)data[index + 3] << 24) |
						((long)data[index + 4] << 32) | ((long)data[index + 5] << 40) | ((long)data[index + 6] << 48) | ((long)data[index + 7] << 56));
				}
				return BitConverter.ToInt64(ReadSlow(position, 8), 0);
			}
		}

		public override ulong ReadUInt64(HexPosition position) {
			Debug.Assert(position < HexPosition.MaxEndPosition);
			int index = (int)(position.ToUInt64() & pageSizeMask);
			lock (lockObj) {
				var cp = GetCachedPage_NoLock(position);
				var data = cp.Data;
				if (index + 7 < data.Length) {
					return ((ulong)data[index] | ((ulong)data[index + 1] << 8) | ((ulong)data[index + 2] << 16) | ((ulong)data[index + 3] << 24) |
						((ulong)data[index + 4] << 32) | ((ulong)data[index + 5] << 40) | ((ulong)data[index + 6] << 48) | ((ulong)data[index + 7] << 56));
				}
				return BitConverter.ToUInt64(ReadSlow(position, 8), 0);
			}
		}

		public unsafe override float ReadSingle(HexPosition position) {
			int v = ReadInt32(position);
			return *(float*)&v;
		}

		public unsafe override double ReadDouble(HexPosition position) {
			long v = ReadInt64(position);
			return *(double*)&v;
		}

		public override short ReadInt16BigEndian(HexPosition position) {
			Debug.Assert(position < HexPosition.MaxEndPosition);
			int index = (int)(position.ToUInt64() & pageSizeMask);
			lock (lockObj) {
				var cp = GetCachedPage_NoLock(position);
				var data = cp.Data;
				if (index + 1 >= data.Length) {
					data = ReadSlow(position, 2);
					index = 0;
				}
				return (short)(data[index + 1] | (data[index] << 8));
			}
		}

		public override ushort ReadUInt16BigEndian(HexPosition position) {
			Debug.Assert(position < HexPosition.MaxEndPosition);
			int index = (int)(position.ToUInt64() & pageSizeMask);
			lock (lockObj) {
				var cp = GetCachedPage_NoLock(position);
				var data = cp.Data;
				if (index + 1 >= data.Length) {
					data = ReadSlow(position, 2);
					index = 0;
				}
				return (ushort)(data[index + 1] | (data[index] << 8));
			}
		}

		public override int ReadInt32BigEndian(HexPosition position) {
			Debug.Assert(position < HexPosition.MaxEndPosition);
			int index = (int)(position.ToUInt64() & pageSizeMask);
			lock (lockObj) {
				var cp = GetCachedPage_NoLock(position);
				var data = cp.Data;
				if (index + 3 >= data.Length) {
					data = ReadSlow(position, 4);
					index = 0;
				}
				return data[index + 3] | (data[index + 2] << 8) | (data[index + 1] << 16) | (data[index] << 24);
			}
		}

		public override uint ReadUInt32BigEndian(HexPosition position) {
			Debug.Assert(position < HexPosition.MaxEndPosition);
			int index = (int)(position.ToUInt64() & pageSizeMask);
			lock (lockObj) {
				var cp = GetCachedPage_NoLock(position);
				var data = cp.Data;
				if (index + 3 >= data.Length) {
					data = ReadSlow(position, 4);
					index = 0;
				}
				return (uint)(data[index + 3] | (data[index + 2] << 8) | (data[index + 1] << 16) | (data[index] << 24));
			}
		}

		public override long ReadInt64BigEndian(HexPosition position) {
			Debug.Assert(position < HexPosition.MaxEndPosition);
			int index = (int)(position.ToUInt64() & pageSizeMask);
			lock (lockObj) {
				var cp = GetCachedPage_NoLock(position);
				var data = cp.Data;
				if (index + 7 >= data.Length) {
					data = ReadSlow(position, 8);
					index = 0;
				}
				return ((long)data[index + 7] | ((long)data[index + 6] << 8) | ((long)data[index + 5] << 16) | ((long)data[index + 4] << 24) |
					((long)data[index + 3] << 32) | ((long)data[index + 2] << 40) | ((long)data[index + 1] << 48) | ((long)data[index] << 56));
			}
		}

		public override ulong ReadUInt64BigEndian(HexPosition position) {
			Debug.Assert(position < HexPosition.MaxEndPosition);
			int index = (int)(position.ToUInt64() & pageSizeMask);
			lock (lockObj) {
				var cp = GetCachedPage_NoLock(position);
				var data = cp.Data;
				if (index + 7 >= data.Length) {
					data = ReadSlow(position, 8);
					index = 0;
				}
				return ((ulong)data[index + 7] | ((ulong)data[index + 6] << 8) | ((ulong)data[index + 5] << 16) | ((ulong)data[index + 4] << 24) |
					((ulong)data[index + 3] << 32) | ((ulong)data[index + 2] << 40) | ((ulong)data[index + 1] << 48) | ((ulong)data[index] << 56));
			}
		}

		public unsafe override float ReadSingleBigEndian(HexPosition position) {
			int v = ReadInt32BigEndian(position);
			return *(float*)&v;
		}

		public unsafe override double ReadDoubleBigEndian(HexPosition position) {
			long v = ReadInt64BigEndian(position);
			return *(double*)&v;
		}

		public override byte[] ReadBytes(HexPosition position, long length) {
			var res = new byte[length];
			ReadBytes(position, res, 0, res.LongLength);
			return res;
		}

		public override void ReadBytes(HexPosition position, byte[] destination, long destinationIndex, long length) {
			Debug.Assert(position < HexPosition.MaxEndPosition);
			lock (lockObj) {
				while (length > 0) {
					var cp = GetCachedPage_NoLock(position);
					long srcIndex = (long)(position.ToUInt64() - cp.Offset);
					int partSize = (int)(pageSize - (ulong)srcIndex);
					if (partSize > length)
						partSize = (int)length;
					Array.Copy(cp.Data, srcIndex, destination, destinationIndex, partSize);
					position += (ulong)partSize;
					length -= partSize;
					destinationIndex += partSize;
				}
			}
		}

		public override HexBytes ReadHexBytes(HexPosition position, long length) {
			Debug.Assert(position < HexPosition.MaxEndPosition);
			if (length == 0)
				return HexBytes.Empty;

			BitArray bitArray = null;
			var destination = new byte[length];
			long destinationIndex = 0;
			long invalidBytes = 0, validBytes = 0;
			long bytesRead = 0;
			var pos = position;
			lock (lockObj) {
				while (length > 0) {
					var cp = GetCachedPage_NoLock(pos);
					long srcIndex = (long)(pos.ToUInt64() - cp.Offset);
					int partSize = (int)(pageSize - (ulong)srcIndex);
					if (partSize > length)
						partSize = (int)length;
					if (srcIndex + partSize > cp.DataSize) {
						if (srcIndex >= cp.DataSize && bytesRead == invalidBytes) {
							Debug.Assert(bitArray == null);
							invalidBytes += partSize;
						}
						else {
							if (bitArray == null)
								bitArray = CreateBitArray(destination, invalidBytes, bytesRead);
							int validCount = cp.DataSize - (int)srcIndex;
							for (int i = 0; i < validCount; i++) {
								long j = bytesRead + i;
								if (j > int.MaxValue)
									break;
								bitArray.Set((int)j, true);
							}
						}
					}
					else if (bytesRead == validBytes) {
						Debug.Assert(bitArray == null);
						validBytes += partSize;
					}
					else {
						if (bitArray == null)
							bitArray = CreateBitArray(destination, invalidBytes, bytesRead);
						for (int i = 0; i < partSize; i++) {
							long j = bytesRead + i;
							if (j > int.MaxValue)
								break;
							bitArray.Set((int)j, true);
						}
					}
					Array.Copy(cp.Data, srcIndex, destination, destinationIndex, partSize);
					pos += (ulong)partSize;
					length -= partSize;
					destinationIndex += partSize;
					bytesRead += partSize;
				}
			}

			if (bitArray != null)
				return new HexBytes(destination, bitArray);
			if (invalidBytes == bytesRead)
				return new HexBytes(destination, false);
			Debug.Assert(bytesRead == validBytes);
			return new HexBytes(destination);
		}

		static BitArray CreateBitArray(byte[] destination, long invalidBytes, long bytesRead) {
			var bitArray = new BitArray((int)Math.Min(int.MaxValue, destination.LongLength), false);
			long len = bytesRead - invalidBytes;
			for (long i = 0; i < len; i++) {
				long j = invalidBytes + i;
				if (j > int.MaxValue)
					break;
				bitArray.Set((int)j, true);
			}
			return bitArray;
		}

		public override void Write(HexPosition position, byte[] source, long sourceIndex, long length) {
			Debug.Assert(position < HexPosition.MaxEndPosition);
			simpleStream.Write(position, source, sourceIndex, length);
			InvalidateCore(new HexSpan(position, (ulong)length));
		}

		void ClearAll() {
			lock (lockObj) {
				foreach (var cp in cachedPages)
					cp.IsInitialized = false;
				lastHitIndex = 0;
			}
		}

		public override void Invalidate(HexSpan span) {
			if (span.Start <= Span.Start && span.End >= Span.End)
				ClearAll();
			else
				InvalidateCore(span);
			BufferStreamSpanInvalidated?.Invoke(this, new HexBufferStreamSpanInvalidatedEventArgs(span));
		}

		public override void ClearCache() {
			simpleStream.ClearCache();
			ClearAll();
		}

		protected override void DisposeCore() {
			if (disposeStream)
				simpleStream?.Dispose();
			simpleStream = null;
		}
	}
}
