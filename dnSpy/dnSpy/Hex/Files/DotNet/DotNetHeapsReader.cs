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
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Files;
using dnSpy.Contracts.Hex.Files.DotNet;

namespace dnSpy.Hex.Files.DotNet {
	sealed class DotNetHeapsReader {
		public DotNetHeap[] Streams { get; private set; }

		readonly HexBufferFile file;
		readonly DotNetMetadataHeaderData mdHeader;
		readonly StorageStreamHeader[] storageStreamHeaders;

		public DotNetHeapsReader(HexBufferFile file, DotNetMetadataHeaderData mdHeader, StorageStreamHeader[] storageStreamHeaders) {
			this.file = file ?? throw new ArgumentNullException(nameof(file));
			this.mdHeader = mdHeader ?? throw new ArgumentNullException(nameof(mdHeader));
			this.storageStreamHeaders = storageStreamHeaders ?? throw new ArgumentNullException(nameof(storageStreamHeaders));
		}

		public bool Read() {
			var metaDataType = GetTablesHeapType(storageStreamHeaders);
			Streams = CreateHeaps(metaDataType);
			return true;
		}

		DotNetHeap[] CreateHeaps(TablesHeapType metaDataType) {
			switch (metaDataType) {
			case TablesHeapType.Compressed:	return CreateCompressedHeaps();
			case TablesHeapType.ENC:		return CreateENCHeaps();
			default:						throw new InvalidOperationException();
			}
		}

		DotNetHeap[] CreateCompressedHeaps() {
			var list = new List<DotNetHeap>();
			StringsHeap stringsHeap = null;
			USHeap usHeap = null;
			BlobHeap blobHeap = null;
			GUIDHeap guidHeap = null;
			TablesHeap tablesHeap = null;
			PdbHeap pdbHeap = null;
			for (int i = storageStreamHeaders.Length - 1; i >= 0; i--) {
				var ssh = storageStreamHeaders[i];
				var span = new HexBufferSpan(file.Buffer, ssh.DataSpan);

				switch (ssh.Name) {
				case "#Strings":
					if (stringsHeap == null) {
						stringsHeap = new StringsHeapImpl(span);
						list.Add(stringsHeap);
						continue;
					}
					break;

				case "#US":
					if (usHeap == null) {
						usHeap = new USHeapImpl(span);
						list.Add(usHeap);
						continue;
					}
					break;

				case "#Blob":
					if (blobHeap == null) {
						blobHeap = new BlobHeapImpl(span);
						list.Add(blobHeap);
						continue;
					}
					break;

				case "#GUID":
					if (guidHeap == null) {
						guidHeap = new GUIDHeapImpl(span);
						list.Add(guidHeap);
						continue;
					}
					break;

				case "#~":
					if (tablesHeap == null && span.Length >= TablesHeapImpl.MinimumSize) {
						tablesHeap = new TablesHeapImpl(span, TablesHeapType.Compressed);
						list.Add(tablesHeap);
						continue;
					}
					break;

				case "#!":
					list.Add(new HotHeapImpl(span));
					continue;

				case "#Pdb":
					if (pdbHeap == null && span.Length >= PdbHeapImpl.MinimumSize) {
						pdbHeap = new PdbHeapImpl(span);
						list.Add(pdbHeap);
						continue;
					}
					break;
				}
				list.Add(new UnknownHeapImpl(span));
			}

			list.Reverse();
			return list.ToArray();
		}

		DotNetHeap[] CreateENCHeaps() {
			var list = new List<DotNetHeap>();
			StringsHeap stringsHeap = null;
			USHeap usHeap = null;
			BlobHeap blobHeap = null;
			GUIDHeap guidHeap = null;
			TablesHeap tablesHeap = null;
			foreach (var ssh in storageStreamHeaders) {
				var span = new HexBufferSpan(file.Buffer, ssh.DataSpan);

				switch (ssh.Name.ToUpperInvariant()) {
				case "#STRINGS":
					if (stringsHeap == null) {
						stringsHeap = new StringsHeapImpl(span);
						list.Add(stringsHeap);
						continue;
					}
					break;

				case "#US":
					if (usHeap == null) {
						usHeap = new USHeapImpl(span);
						list.Add(usHeap);
						continue;
					}
					break;

				case "#BLOB":
					if (blobHeap == null) {
						blobHeap = new BlobHeapImpl(span);
						list.Add(blobHeap);
						continue;
					}
					break;

				case "#GUID":
					if (guidHeap == null) {
						guidHeap = new GUIDHeapImpl(span);
						list.Add(guidHeap);
						continue;
					}
					break;

				case "#~":	// Only if #Schema is used
				case "#-":
					if (tablesHeap == null && span.Length >= TablesHeapImpl.MinimumSize) {
						tablesHeap = new TablesHeapImpl(span, TablesHeapType.ENC);
						list.Add(tablesHeap);
						continue;
					}
					break;
				}
				list.Add(new UnknownHeapImpl(span));
			}
			return list.ToArray();
		}

		static TablesHeapType GetTablesHeapType(StorageStreamHeader[] storageStreamHeaders) {
			TablesHeapType? thType = null;
			foreach (var sh in storageStreamHeaders) {
				if (thType == null) {
					if (sh.Name == "#~")
						thType = TablesHeapType.Compressed;
					else if (sh.Name == "#-")
						thType = TablesHeapType.ENC;
				}
				if (sh.Name == "#Schema")
					thType = TablesHeapType.ENC;
			}
			return thType ?? TablesHeapType.Compressed;
		}
	}
}
