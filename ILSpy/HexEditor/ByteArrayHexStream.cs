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

namespace dnSpy.HexEditor {
	public class ByteArrayHexStream : IHexStream {
		readonly byte[] data;

		public ulong Size {
			get { return (ulong)data.LongLength; }
		}

		public ulong EndOffset {
			get { return data.LongLength == 0 ? 0 : (ulong)data.LongLength - 1; }
		}

		public ByteArrayHexStream(byte[] data) {
			this.data = data;
		}

		public int ReadByte(ulong offset) {
			if (offset >= (ulong)data.LongLength)
				return -1;
			return data[offset];
		}

		public void Read(ulong offset, byte[] array, int index, int count) {
			if (offset >= (ulong)data.LongLength) {
				Array.Clear(array, index, count);
				return;
			}

			long bytesLeft = data.LongLength - (long)offset;
			long validBytes = count <= bytesLeft ? count : bytesLeft;
			Array.Copy(data, (long)offset, array, index, validBytes);
			count -= (int)validBytes;
			if (count > 0)
				Array.Clear(array, index + (int)validBytes, count);
		}
	}
}
