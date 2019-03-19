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
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace dnSpy.AsmEditor.Compiler.MDEditor {
	unsafe sealed class MDWriterMemoryStream : MDWriterStream {
		// Use smaller buffers to prevent them from ending up on the LOH
		const int BUFFER_LENGTH = 8 * 1024;
		readonly List<byte[]> buffers;
		long position;
		long dataLength;

		public override long Position {
			get => position;
			set {
				if (value < 0)
					throw new ArgumentOutOfRangeException(nameof(value));
				position = value;
			}
		}

		public override long Length {
			get => dataLength;
			set {
				if (value < 0)
					throw new ArgumentOutOfRangeException(nameof(value));
				dataLength = value;
				EnsureLength(dataLength);
			}
		}

		public MDWriterMemoryStream() => buffers = new List<byte[]>();

		long Capacity => (long)buffers.Count * BUFFER_LENGTH;

		void EnsureLength(long minLength) {
			if (minLength <= Capacity)
				return;
			long reqCapacity = (minLength + BUFFER_LENGTH - 1) / BUFFER_LENGTH * BUFFER_LENGTH;
			int extraBuffers = (int)((reqCapacity - Capacity) / BUFFER_LENGTH);
			for (int i = 0; i < extraBuffers; i++)
				buffers.Add(new byte[BUFFER_LENGTH]);
		}

		public override void Write(byte value) {
			const int SIZE = 1;
			EnsureLength(position + SIZE);

			int listIndex = (int)(position / BUFFER_LENGTH);
			int bufferIndex = (int)(position % BUFFER_LENGTH);

			buffers[listIndex][bufferIndex] = value;
			position += SIZE;
			dataLength = Math.Max(position, dataLength);
		}

		public override void Write(ushort value) {
			const int SIZE = 2;
			EnsureLength(position + SIZE);

			int listIndex = (int)(position / BUFFER_LENGTH);
			int bufferIndex = (int)(position % BUFFER_LENGTH);

			if (bufferIndex <= BUFFER_LENGTH - SIZE) {
				var buffer = buffers[listIndex];
				buffer[bufferIndex++] = (byte)value;
				buffer[bufferIndex] = (byte)(value >> 8);
				position += SIZE;
			}
			else {
				for (int i = 0; i < SIZE; i++, position++, value >>= 8) {
					listIndex = (int)(position / BUFFER_LENGTH);
					bufferIndex = (int)(position % BUFFER_LENGTH);

					buffers[listIndex][bufferIndex] = (byte)value;
				}
			}
			dataLength = Math.Max(position, dataLength);
		}

		public override void Write(uint value) {
			const int SIZE = 4;
			EnsureLength(position + SIZE);

			int listIndex = (int)(position / BUFFER_LENGTH);
			int bufferIndex = (int)(position % BUFFER_LENGTH);

			if (bufferIndex <= BUFFER_LENGTH - SIZE) {
				var buffer = buffers[listIndex];
				buffer[bufferIndex++] = (byte)value;
				buffer[bufferIndex++] = (byte)(value >> 8);
				buffer[bufferIndex++] = (byte)(value >> 16);
				buffer[bufferIndex] = (byte)(value >> 24);
				position += SIZE;
			}
			else {
				for (int i = 0; i < SIZE; i++, position++, value >>= 8) {
					listIndex = (int)(position / BUFFER_LENGTH);
					bufferIndex = (int)(position % BUFFER_LENGTH);

					buffers[listIndex][bufferIndex] = (byte)value;
				}
			}
			dataLength = Math.Max(position, dataLength);
		}

		public override void Write(ulong value) {
			const int SIZE = 8;
			EnsureLength(position + SIZE);

			int listIndex = (int)(position / BUFFER_LENGTH);
			int bufferIndex = (int)(position % BUFFER_LENGTH);

			if (bufferIndex <= BUFFER_LENGTH - SIZE) {
				var buffer = buffers[listIndex];
				buffer[bufferIndex++] = (byte)value;
				buffer[bufferIndex++] = (byte)(value >> 8);
				buffer[bufferIndex++] = (byte)(value >> 16);
				buffer[bufferIndex++] = (byte)(value >> 24);
				buffer[bufferIndex++] = (byte)(value >> 32);
				buffer[bufferIndex++] = (byte)(value >> 40);
				buffer[bufferIndex++] = (byte)(value >> 48);
				buffer[bufferIndex] = (byte)(value >> 56);
				position += SIZE;
			}
			else {
				for (int i = 0; i < SIZE; i++, position++, value >>= 8) {
					listIndex = (int)(position / BUFFER_LENGTH);
					bufferIndex = (int)(position % BUFFER_LENGTH);

					buffers[listIndex][bufferIndex] = (byte)value;
				}
			}
			dataLength = Math.Max(position, dataLength);
		}

		public override void Write(byte* source, int length) {
			EnsureLength(position + length);

			while (length > 0) {
				int listIndex = (int)(position / BUFFER_LENGTH);
				int bufferIndex = (int)(position % BUFFER_LENGTH);

				var buffer = buffers[listIndex];
				int lengthLeft = buffer.Length - bufferIndex;
				int copyLen = Math.Min(lengthLeft, length);
				Marshal.Copy((IntPtr)source, buffer, bufferIndex, copyLen);
				source += copyLen;
				length -= copyLen;
				position += copyLen;
			}

			dataLength = Math.Max(position, dataLength);
		}

		public override void Write(byte[] source, int sourceIndex, int length) {
			EnsureLength(position + length);

			while (length > 0) {
				int listIndex = (int)(position / BUFFER_LENGTH);
				int bufferIndex = (int)(position % BUFFER_LENGTH);

				var buffer = buffers[listIndex];
				int lengthLeft = buffer.Length - bufferIndex;
				int copyLen = Math.Min(lengthLeft, length);
				Array.Copy(source, sourceIndex, buffer, bufferIndex, copyLen);
				sourceIndex += copyLen;
				length -= copyLen;
				position += copyLen;
			}

			dataLength = Math.Max(position, dataLength);
		}

		public unsafe void CopyTo(IntPtr destination, int length) {
			if (length < 0 || length > dataLength)
				throw new ArgumentOutOfRangeException(nameof(length));

			var dest = (byte*)destination;
			for (int i = 0; i < buffers.Count; i++) {
				if (length == 0)
					break;

				var buffer = buffers[i];
				int copyLen = Math.Min(buffer.Length, length);
				Marshal.Copy(buffer, 0, (IntPtr)dest, copyLen);
				dest += copyLen;
				length -= copyLen;
			}
			Debug.Assert(length == 0);
		}
	}
}
