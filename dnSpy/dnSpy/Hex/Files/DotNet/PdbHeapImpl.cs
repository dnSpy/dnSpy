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

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Files;
using dnSpy.Contracts.Hex.Files.DotNet;

namespace dnSpy.Hex.Files.DotNet {
	sealed class PdbHeapImpl : PdbHeap, IDotNetHeap {
		public override DotNetMetadataHeaders Metadata => metadata;
		DotNetMetadataHeaders metadata;

		public override PdbStreamHeaderData Header {
			get {
				if (!initialized)
					Initialize();
				return pdbStreamHeaderData;
			}
		}

		public override ReadOnlyCollection<byte> PdbId {
			get {
				if (!initialized)
					Initialize();
				return pdbId;
			}
		}

		public override MDToken EntryPoint {
			get {
				if (!initialized)
					Initialize();
				return entryPoint;
			}
		}

		public override ulong ReferencedTypeSystemTables {
			get {
				if (!initialized)
					Initialize();
				return referencedTypeSystemTables;
			}
		}

		public override ReadOnlyCollection<uint> TypeSystemTableRows {
			get {
				if (!initialized)
					Initialize();
				return typeSystemTableRowsReadOnly;
			}
		}

		bool initialized;
		PdbStreamHeaderData pdbStreamHeaderData;
		ReadOnlyCollection<byte> pdbId;
		MDToken entryPoint;
		ulong referencedTypeSystemTables;
		ReadOnlyCollection<uint> typeSystemTableRowsReadOnly;

		internal static int MinimumSize => 32;

		public PdbHeapImpl(HexBufferSpan span)
			: base(span) {
		}

		void Initialize() {
			if (initialized)
				return;
			initialized = true;

			var buffer = Span.Buffer;
			var pos = Span.Span.Start;
			pdbId = new ReadOnlyCollection<byte>(buffer.ReadBytes(pos, 20));
			entryPoint = new MDToken(buffer.ReadUInt32(pos + 20));
			referencedTypeSystemTables = buffer.ReadUInt64(pos + 24);
			pos += 32;
			Debug.Assert(Span.Span.Start + MinimumSize == pos);
			Debug.Assert(Span.Span.Contains(pos - 1), "Creator should've verified this");

			var typeSystemTableRows = new List<uint>();
			var end = Span.Span.End;
			var valid = referencedTypeSystemTables;
			for (int i = 0; i < 64; valid >>= 1, i++) {
				if ((valid & 1) == 0 || !Span.Span.Contains(pos + 3))
					continue;
				typeSystemTableRows.Add(buffer.ReadUInt32(pos));
				pos += 4;
			}
			var headerSpan = HexSpan.FromBounds(Span.Span.Start, pos);

			typeSystemTableRowsReadOnly = new ReadOnlyCollection<uint>(typeSystemTableRows.ToArray());
			pdbStreamHeaderData = new PdbStreamHeaderDataImpl(new HexBufferSpan(buffer, headerSpan), typeSystemTableRowsReadOnly.Count);
		}

		public override ComplexData GetStructure(HexPosition position) {
			if (!Span.Span.Contains(position))
				return null;

			if (Header.Span.Span.Contains(position))
				return Header;

			return null;
		}

		void IDotNetHeap.SetMetadata(DotNetMetadataHeaders metadata) => this.metadata = metadata;
	}
}
