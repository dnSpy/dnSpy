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
using dnlib.DotNet;
using dnlib.DotNet.MD;

namespace dnSpy.AsmEditor.Compiler.MDEditor {
	sealed class MetadataEditor {
		readonly RawModuleBytes moduleData;
		readonly Metadata metadata;
		readonly List<MDHeap> heaps;

		public Metadata RealMetadata => metadata;
		public RawModuleBytes ModuleData => moduleData;

		public BlobMDHeap BlobHeap { get; }
		public GuidMDHeap GuidHeap { get; }
		public StringsMDHeap StringsHeap { get; }
		public USMDHeap USHeap { get; }
		public TablesMDHeap TablesHeap { get; }

		public MetadataEditor(RawModuleBytes moduleData, Metadata metadata) {
			BlobHeap = null!;
			GuidHeap = null!;
			StringsHeap = null!;
			USHeap = null!;
			TablesHeap = null!;

			this.moduleData = moduleData ?? throw new ArgumentNullException(nameof(moduleData));
			this.metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));

			heaps = new List<MDHeap>(metadata.AllStreams.Count);
			foreach (var stream in metadata.AllStreams) {
				switch (stream) {
				case BlobStream blobStream:
					heaps.Add(BlobHeap = new BlobMDHeap(this, blobStream));
					break;

				case GuidStream guidStream:
					heaps.Add(GuidHeap = new GuidMDHeap(this, guidStream));
					break;

				case StringsStream stringsStream:
					heaps.Add(StringsHeap = new StringsMDHeap(this, stringsStream));
					break;

				case USStream usStream:
					heaps.Add(USHeap = new USMDHeap(this, usStream));
					break;

				case TablesStream tablesStream:
					heaps.Add(TablesHeap = new TablesMDHeap(this, tablesStream));
					break;

				default:
					heaps.Add(new UnknownMDHeap(this, stream));
					break;
				}
			}
			if (BlobHeap is null)
				heaps.Add(BlobHeap = new BlobMDHeap(this, metadata.BlobStream));
			if (GuidHeap is null)
				heaps.Add(GuidHeap = new GuidMDHeap(this, metadata.GuidStream));
			if (StringsHeap is null)
				heaps.Add(StringsHeap = new StringsMDHeap(this, metadata.StringsStream));
			if (USHeap is null)
				heaps.Add(USHeap = new USMDHeap(this, metadata.USStream));
			if (TablesHeap is null)
				throw new InvalidOperationException();
		}

		public uint CreateAssemblyRef(IAssembly assembly) {
			var rid = TablesHeap.AssemblyRefTable.Create();
			var row = new RawAssemblyRefRow((ushort)assembly.Version.Major, (ushort)assembly.Version.Minor,
				(ushort)assembly.Version.Build, (ushort)assembly.Version.Revision,
				(uint)assembly.Attributes,
				BlobHeap.Create(GetPublicKeyOrTokenBytes(assembly.PublicKeyOrToken)),
				StringsHeap.Create(assembly.Name),
				StringsHeap.Create(assembly.Culture),
				BlobHeap.Create((assembly as AssemblyRef)?.Hash));
			TablesHeap.AssemblyRefTable.Set(rid, ref row);
			return rid;
		}

		static byte[]? GetPublicKeyOrTokenBytes(PublicKeyBase pkb) {
			if (pkb is PublicKey pk)
				return pk.Data;
			if (pkb is PublicKeyToken pkt)
				return pkt.Data;
			return null;
		}

		public bool MustRewriteMetadata() {
			foreach (var heap in heaps) {
				if (heap.MustRewriteHeap())
					return true;
			}
			return false;
		}
	}
}
