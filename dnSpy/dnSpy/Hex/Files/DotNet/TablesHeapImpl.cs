/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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

using System.Diagnostics;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Files.DotNet;

namespace dnSpy.Hex.Files.DotNet {
	sealed class TablesHeapImpl : TablesHeap {
		public override HexSpan HeaderSpan {
			get {
				if (!initialized)
					Initialize();
				return headerSpan;
			}
		}

		public override HexSpan TablesSpan {
			get {
				if (!initialized)
					Initialize();
				return tablesSpan;
			}
		}

		public override TablesHeaderData Header {
			get {
				if (!initialized)
					Initialize();
				return tablesHeaderData;
			}
		}

		public override byte MajorVersion {
			get {
				if (!initialized)
					Initialize();
				return majorVersion;
			}
		}

		public override byte MinorVersion {
			get {
				if (!initialized)
					Initialize();
				return minorVersion;
			}
		}

		public override MDStreamFlags Flags {
			get {
				if (!initialized)
					Initialize();
				return flags;
			}
		}

		public override ulong ValidMask {
			get {
				if (!initialized)
					Initialize();
				return validMask;
			}
		}

		public override ulong SortedMask {
			get {
				if (!initialized)
					Initialize();
				return sortedMask;
			}
		}

		bool initialized;
		HexSpan headerSpan;
		HexSpan tablesSpan;
		TablesHeaderData tablesHeaderData;

		uint reserved1;
		byte majorVersion;
		byte minorVersion;
		MDStreamFlags flags;
		byte log2Rid;
		ulong validMask;
		ulong sortedMask;
		uint extraData;
		MDTable[] mdTables;

		internal static int MinimumSize => 0x18;

		public TablesHeapImpl(HexBufferSpan span, MetaDataType metaDataType)
			: base(span, metaDataType) {
		}

		void Initialize() {
			if (initialized)
				return;
			initialized = true;

			var buffer = Span.Buffer;
			var pos = Span.Start.Position;
			reserved1 = buffer.ReadUInt32(pos);
			majorVersion = buffer.ReadByte(pos + 4);
			minorVersion = buffer.ReadByte(pos + 5);
			flags = (MDStreamFlags)buffer.ReadByte(pos + 6);
			log2Rid = buffer.ReadByte(pos + 7);
			validMask = buffer.ReadUInt64(pos + 8);
			sortedMask = buffer.ReadUInt64(pos + 0x10);
			pos += 0x18;
			Debug.Assert(Span.Span.Start + MinimumSize == pos);
			Debug.Assert(Span.Span.Contains(pos - 1), "Creator should've verified this");

			int maxPresentTables;
			var dnTableSizes = new DotNetTableSizes();
			var tableInfos = dnTableSizes.CreateTables(majorVersion, minorVersion, out maxPresentTables);
			var rowsCount = new uint[tableInfos.Length];

			ulong valid = validMask;
			var sizes = new uint[64];
			int rowsFieldCount = 0;
			for (int i = 0; i < 64; valid >>= 1, i++) {
				uint rows;
				if ((valid & 1) == 0 || !Span.Span.Contains(pos + 3))
					rows = 0;
				else {
					rowsFieldCount++;
					rows = buffer.ReadUInt32(pos);
					pos += 4;
				}
				if (i >= maxPresentTables)
					rows = 0;
				sizes[i] = rows;
				if (i < rowsCount.Length)
					rowsCount[i] = rows;
			}

			bool hasExtraData = false;
			if ((flags & MDStreamFlags.ExtraData) != 0 && Span.Span.Contains(pos + 3)) {
				hasExtraData = true;
				extraData = buffer.ReadUInt32(pos);
				pos += 4;
			}

			headerSpan = HexSpan.FromBounds(Span.Span.Start, pos);

			dnTableSizes.InitializeSizes((flags & MDStreamFlags.BigStrings) != 0, (flags & MDStreamFlags.BigGUID) != 0, (flags & MDStreamFlags.BigBlob) != 0, sizes);
			mdTables = new MDTable[tableInfos.Length];
			var tablesStartPos = pos;
			bool bad = !Span.Span.Contains(pos);
			for (int i = 0; i < rowsCount.Length; i++) {
				var rows = rowsCount[i];
				var mdTable = new MDTable(pos, (Table)i, rows, tableInfos[i]);
				if (bad || mdTable.Span.End > Span.End.Position) {
					mdTable = new MDTable(pos, (Table)i, 0, tableInfos[i]);
					bad = true;
				}
				pos = mdTable.Span.End;
				mdTables[i] = mdTable;
			}

			tablesSpan = HexSpan.FromBounds(HexPosition.Min(Span.End.Position, tablesStartPos), HexPosition.Min(Span.End.Position, pos));
			tablesHeaderData = new TablesHeaderDataImpl(new HexBufferSpan(buffer, tablesSpan), hasExtraData, rowsFieldCount);
		}
	}
}
