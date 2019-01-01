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
using System.Text;

namespace dnSpy.AsmEditor.Compiler.MDEditor {
	sealed class DotNetMetadataSectionData : PESectionData {
		public override uint Alignment => 8;

		public uint RVA { get; private set; }
		public uint Size { get; private set; }

		readonly MetadataEditor mdEditor;
		// PERF: Write as many rows as possible to this array, followed by a call to stream.Write(byte[]).
		// This is much faster than writing one column at a time.
		readonly byte[] buffer;
		const int BUFFER_SIZE = 0x1000;

		public DotNetMetadataSectionData(MetadataEditor mdEditor) {
			this.mdEditor = mdEditor;
			buffer = new byte[BUFFER_SIZE];
		}

		public override void Write(MDWriter mdWriter, uint rva, MDWriterStream stream) {
			RVA = rva;
			var startPos = stream.Position;

			// PERF: We only write the known needed heaps. The #US heap isn't written since
			// it's only used by method bodies and we don't write method bodies.
			var heapWriters = new List<MDHeapWriter>(4);
			heapWriters.Add(new TablesHeapWriter(mdEditor.TablesHeap, mdEditor.StringsHeap, mdEditor.GuidHeap, mdEditor.BlobHeap));
			if (mdEditor.StringsHeap.ExistsInMetadata || mdEditor.StringsHeap.MustRewriteHeap())
				heapWriters.Add(new StringsHeapWriter(mdEditor.StringsHeap));
			if (mdEditor.GuidHeap.ExistsInMetadata || mdEditor.GuidHeap.MustRewriteHeap())
				heapWriters.Add(new GuidHeapWriter(mdEditor.GuidHeap));
			if (mdEditor.BlobHeap.ExistsInMetadata || mdEditor.BlobHeap.MustRewriteHeap())
				heapWriters.Add(new BlobHeapWriter(mdEditor.BlobHeap));

			var heapInfos = new(long offsetAndSizePosition, long dataPosition, long dataEndPosition)[heapWriters.Count];

			// Write the metadata header
			var mdHeader = mdWriter.MetadataEditor.RealMetadata.MetadataHeader;
			stream.Write(0x424A5342);// BSJB
			stream.Write(mdHeader.MajorVersion);
			stream.Write(mdHeader.MinorVersion);
			stream.Write(mdHeader.Reserved1);
			var s = Encoding.UTF8.GetBytes(mdHeader.VersionString + "\0");
			stream.Write(AlignUp((uint)s.Length, 4));
			stream.Write(s);
			stream.Position += AlignUp((uint)s.Length, 4) - s.Length;
			stream.Write((byte)mdHeader.Flags);
			stream.Write(mdHeader.Reserved2);
			stream.Write((ushort)heapWriters.Count);

			for (int i = 0; i < heapWriters.Count; i++) {
				var heapWriter = heapWriters[i];
				heapInfos[i].offsetAndSizePosition = stream.Position;
				// We'll fix this later (offset + length)
				stream.Position += 8;
				stream.Write(s = Encoding.ASCII.GetBytes(heapWriter.Name + "\0"));
				if (s.Length > 32)
					throw new InvalidOperationException();
				stream.Position += AlignUp((uint)s.Length, 4) - s.Length;
			}

			const uint HEAP_ALIGNMENT = 4;
			for (int i = 0; i < heapWriters.Count; i++) {
				var heapWriter = heapWriters[i];
				stream.Position = (stream.Position + HEAP_ALIGNMENT - 1) & ~(HEAP_ALIGNMENT - 1);
				heapInfos[i].dataPosition = stream.Position;
				heapWriter.Write(mdWriter, stream, buffer);
				heapInfos[i].dataEndPosition = stream.Position;
			}

			// Write the now known heap offsets and sizes
			var currPos = stream.Position;
			for (int i = 0; i < heapInfos.Length; i++) {
				ref var info = ref heapInfos[i];
				stream.Position = info.offsetAndSizePosition;
				stream.Write((uint)(info.dataPosition - startPos));
				stream.Write((uint)(info.dataEndPosition - info.dataPosition));
			}
			stream.Position = currPos;

			Size = (uint)(stream.Position - startPos);
		}

		static uint AlignUp(uint offset, uint alignment) => (offset + alignment - 1) & ~(alignment - 1);
	}
}
