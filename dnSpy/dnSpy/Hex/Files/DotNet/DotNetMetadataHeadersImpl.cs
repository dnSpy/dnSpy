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
using System.Collections.ObjectModel;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Files;
using dnSpy.Contracts.Hex.Files.DotNet;

namespace dnSpy.Hex.Files.DotNet {
	sealed class DotNetMetadataHeadersImpl : DotNetMetadataHeaders {
		public override DotNetMetadataHeaderData MetadataHeader { get; }
		public override TablesHeap? TablesStream { get; }
		public override StringsHeap? StringsStream { get; }
		public override USHeap? USStream { get; }
		public override GUIDHeap? GUIDStream { get; }
		public override BlobHeap? BlobStream { get; }
		public override PdbHeap? PdbStream { get; }
		public override ReadOnlyCollection<DotNetHeap> Streams { get; }

		public DotNetMetadataHeadersImpl(HexSpan metadataSpan, DotNetMetadataHeaderData metadataHeader, DotNetHeap[] streams)
			: base(metadataSpan) {
			MetadataHeader = metadataHeader ?? throw new ArgumentNullException(nameof(metadataHeader));
			Streams = new ReadOnlyCollection<DotNetHeap>(streams);
			TablesStream = FindStream<TablesHeap>(streams);
			StringsStream = FindStream<StringsHeap>(streams);
			USStream = FindStream<USHeap>(streams);
			GUIDStream = FindStream<GUIDHeap>(streams);
			BlobStream = FindStream<BlobHeap>(streams);
			PdbStream = FindStream<PdbHeap>(streams);
			foreach (IDotNetHeap heap in streams)
				heap.SetMetadata(this);
		}

		T? FindStream<T>(DotNetHeap[] streams) where T : DotNetHeap {
			foreach (var stream in streams) {
				if (stream is T t)
					return t;
			}
			return null;
		}

		public override ComplexData? GetStructure(HexPosition position) {
			if (!MetadataSpan.Contains(position))
				return null;

			if (MetadataHeader.Span.Span.Contains(position))
				return MetadataHeader;

			foreach (var stream in Streams) {
				if (stream.Span.Span.Contains(position))
					return stream.GetStructure(position);
			}

			return null;
		}
	}
}
