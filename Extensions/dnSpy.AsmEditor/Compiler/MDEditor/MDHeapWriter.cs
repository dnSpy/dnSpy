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

namespace dnSpy.AsmEditor.Compiler.MDEditor {
	abstract class MDHeapWriter {
		public abstract string Name { get; }
		public abstract void Write(MDWriter mdWriter, MDWriterStream stream, byte[] tempBuffer);

		protected void WriteData(MDWriterStream stream, List<byte[]> newData, byte[] tempBuffer) {
			if (newData.Count == 0)
				return;
			int tempBufferIndex = 0;
			foreach (var data in newData) {
				int bytesLeft = tempBuffer.Length - tempBufferIndex;
				if (data.Length > bytesLeft) {
					stream.Write(tempBuffer, 0, tempBufferIndex);
					tempBufferIndex = 0;
				}
				if (data.Length > tempBuffer.Length) {
					Debug.Assert(tempBufferIndex == 0);
					stream.Write(data);
				}
				else {
					Array.Copy(data, 0, tempBuffer, tempBufferIndex, data.Length);
					tempBufferIndex += data.Length;
				}
			}
			if (tempBufferIndex > 0)
				stream.Write(tempBuffer, 0, tempBufferIndex);
		}
	}
}
