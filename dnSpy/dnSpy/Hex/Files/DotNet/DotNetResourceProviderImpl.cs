/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Files;
using dnSpy.Contracts.Hex.Files.DotNet;
using dnSpy.Contracts.Hex.Files.PE;

namespace dnSpy.Hex.Files.DotNet {
	sealed class DotNetResourceProviderImpl : DotNetResourceProvider {
		public override HexSpan ResourcesSpan { get; }

		readonly PeHeaders peHeaders;
		readonly ResourceInfo[] resourceInfos;

		struct ResourceInfo {
			public uint Token { get; }
			public HexSpan Span { get; }
			public string FilteredName { get; }
			public ResourceInfo(uint token, HexSpan span, string filteredName) {
				if (filteredName == null)
					throw new ArgumentNullException(nameof(filteredName));
				Token = token;
				Span = span;
				FilteredName = filteredName;
			}
		}

		sealed class ResourceInfoComparer : IComparer<ResourceInfo> {
			public static readonly ResourceInfoComparer Instance = new ResourceInfoComparer();
			public int Compare(ResourceInfo x, ResourceInfo y) {
				int c = x.Span.Start.CompareTo(y.Span.Start);
				if (c != 0)
					return c;
				return x.Token.CompareTo(y.Token);
			}
		}

		public DotNetResourceProviderImpl(HexBufferFile file, PeHeaders peHeaders, DotNetMetadataHeaders metadataHeaders, HexSpan? resourcesSpan)
			: base(file) {
			if (peHeaders == null)
				throw new ArgumentNullException(nameof(peHeaders));
			this.peHeaders = peHeaders;
			if (metadataHeaders?.TablesStream != null && resourcesSpan != null) {
				Debug.Assert(file.Span.Contains(resourcesSpan.Value));// Verified by caller
				ResourcesSpan = resourcesSpan.Value;
				resourceInfos = CreateResourceInfos(file, metadataHeaders.TablesStream.MDTables[(int)Table.ManifestResource], metadataHeaders.StringsStream);
			}
			else
				resourceInfos = Array.Empty<ResourceInfo>();

			if (resourceInfos.Length > 0) {
				var lastEnd = resourceInfos[0].Span.Start;
				var filesToCreate = new List<BufferFileOptions>();
				foreach (var info in resourceInfos) {
					if (info.Span.Start < lastEnd)
						continue;
					filesToCreate.Add(new BufferFileOptions(HexSpan.FromBounds(info.Span.Start + 4, info.Span.End), info.FilteredName, string.Empty, defaultTags));
					lastEnd = info.Span.End;
				}
				if (filesToCreate.Count > 0)
					file.CreateFiles(filesToCreate.ToArray());
			}
		}
		static readonly string[] defaultTags = new string[] { PredefinedBufferFileTags.DotNetResources };

		ResourceInfo[] CreateResourceInfos(HexBufferFile file, MDTable resourceTable, StringsHeap stringsHeap) {
			if (resourceTable == null)
				return Array.Empty<ResourceInfo>();
			var list = new List<ResourceInfo>((int)resourceTable.Rows);

			var recordPos = resourceTable.Span.Start;
			var buffer = file.Buffer;
			for (uint rid = 1; rid <= resourceTable.Rows; rid++, recordPos += resourceTable.RowSize) {
				uint offset = buffer.ReadUInt32(recordPos);
				uint nameOffset = resourceTable.TableInfo.Columns[2].Size == 2 ?
					buffer.ReadUInt16(recordPos + 8) :
					buffer.ReadUInt32(recordPos + 8);
				uint implementation = resourceTable.TableInfo.Columns[3].Size == 2 ?
					buffer.ReadUInt16(recordPos + resourceTable.RowSize - 2) :
					buffer.ReadUInt32(recordPos + resourceTable.RowSize - 4);

				MDToken implementationToken;
				if (!CodedToken.Implementation.Decode(implementation, out implementationToken))
					continue;
				if (implementationToken.Rid != 0)
					continue;

				var resourceSpan = GetResourceSpan(file.Buffer, offset);
				if (resourceSpan == null)
					continue;

				var token = new MDToken(Table.ManifestResource, rid);
				var filteredName = NameUtils.FilterName(stringsHeap?.Read(nameOffset) ?? string.Empty);
				list.Add(new ResourceInfo(token.Raw, resourceSpan.Value, filteredName));
			}

			list.Sort(ResourceInfoComparer.Instance);
			return list.ToArray();
		}

		HexSpan? GetResourceSpan(HexBuffer buffer, uint offset) {
			var start = ResourcesSpan.Start + offset;
			if (start + 4 > ResourcesSpan.End)
				return null;
			uint size = buffer.ReadUInt32(start);
			var end = start + 4 + size;
			if (end > ResourcesSpan.End)
				return null;
			return HexSpan.FromBounds(start, end);
		}

		public override bool IsResourcePosition(HexPosition position) => ResourcesSpan.Contains(position);

		public override DotNetEmbeddedResource GetResource(HexPosition position) {
			if (!IsResourcePosition(position))
				return null;
			int index = GetIndex(position);
			if (index < 0)
				return null;
			var info = resourceInfos[index];
			return new DotNetEmbeddedResourceImpl(this, new HexBufferSpan(File.Buffer, info.Span), info.Token);
		}

		int GetIndex(HexPosition position) {
			var array = resourceInfos;
			int lo = 0, hi = array.Length - 1;
			while (lo <= hi) {
				int index = (lo + hi) / 2;

				var info = array[index];
				if (position < info.Span.Start)
					hi = index - 1;
				else if (position >= info.Span.End)
					lo = index + 1;
				else
					return index;
			}
			return -1;
		}
	}
}
