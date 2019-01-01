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

namespace dnSpy.AsmEditor.Compiler.MDEditor {
	sealed class BlobHeapWriter : MDHeapWriter {
		public override string Name => blobHeap.Name;

		readonly BlobMDHeap blobHeap;

		public BlobHeapWriter(BlobMDHeap blobHeap) => this.blobHeap = blobHeap;

		public unsafe override void Write(MDWriter mdWriter, MDWriterStream stream, byte[] tempBuffer) {
			int start = (int)blobHeap.BlobStream.StartOffset;
			int size = (int)(blobHeap.BlobStream.EndOffset - blobHeap.BlobStream.StartOffset);
			stream.Write((byte*)mdWriter.ModuleData.Pointer + start, size);
			WriteData(stream, blobHeap.NewData, tempBuffer);
		}
	}
}
