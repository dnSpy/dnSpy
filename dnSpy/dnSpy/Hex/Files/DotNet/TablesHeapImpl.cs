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

using System.Collections.ObjectModel;
using System.Diagnostics;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Files;
using dnSpy.Contracts.Hex.Files.DotNet;

namespace dnSpy.Hex.Files.DotNet {
	sealed class TablesHeapImpl : TablesHeap, IDotNetHeap {
		public override DotNetMetadataHeaders Metadata => metadata;
		DotNetMetadataHeaders metadata;

		public override ReadOnlyCollection<MDTable> MDTables {
			get {
				if (!initialized)
					Initialize();
				return mdTablesReadOnly;
			}
		}

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
		ReadOnlyCollection<MDTable> mdTablesReadOnly;
		TableRecordDataFactory[] tableRecordDataFactories;

		internal static int MinimumSize => 0x18;

		public TablesHeapImpl(HexBufferSpan span, TablesHeapType tablesHeapType)
			: base(span, tablesHeapType) {
		}

		void Initialize() {
			if (initialized)
				return;
			initialized = true;

			var buffer = Span.Buffer;
			var pos = Span.Span.Start;
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
			mdTablesReadOnly = new ReadOnlyCollection<MDTable>(mdTables);
			tableRecordDataFactories = new TableRecordDataFactory[tableInfos.Length];
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
				tableRecordDataFactories[i] = CreateFactory(mdTable);
			}

			tablesSpan = HexSpan.FromBounds(HexPosition.Min(Span.End.Position, tablesStartPos), HexPosition.Min(Span.End.Position, pos));
			tablesHeaderData = new TablesHeaderDataImpl(new HexBufferSpan(buffer, headerSpan), hasExtraData, rowsFieldCount);
		}

		TableRecordDataFactory CreateFactory(MDTable mdTable) {
			switch (mdTable.Table) {
			case Table.TypeDef:				return new TypeDefTableRecordDataFactory(this, mdTable);
			case Table.Field:				return new FieldTableRecordDataFactory(this, mdTable);
			case Table.Method:				return new MethodTableRecordDataFactory(this, mdTable);
			case Table.Param:				return new ParamTableRecordDataFactory(this, mdTable);
			case Table.Constant:			return new ConstantTableRecordDataFactory(this, mdTable);
			case Table.DeclSecurity:		return new DeclSecurityTableRecordDataFactory(this, mdTable);
			case Table.Event:				return new EventTableRecordDataFactory(this, mdTable);
			case Table.Property:			return new PropertyTableRecordDataFactory(this, mdTable);
			case Table.MethodSemantics:		return new MethodSemanticsTableRecordDataFactory(this, mdTable);
			case Table.ImplMap:				return new ImplMapTableRecordDataFactory(this, mdTable);
			case Table.FieldRVA:			return new FieldRVATableRecordDataFactory(this, mdTable);
			case Table.ENCLog:				return new ENCLogTableRecordDataFactory(this, mdTable);
			case Table.ENCMap:				return new ENCMapTableRecordDataFactory(this, mdTable);
			case Table.Assembly:			return new AssemblyTableRecordDataFactory(this, mdTable);
			case Table.AssemblyRef:			return new AssemblyRefTableRecordDataFactory(this, mdTable);
			case Table.File:				return new FileTableRecordDataFactory(this, mdTable);
			case Table.ExportedType:		return new ExportedTypeTableRecordDataFactory(this, mdTable);
			case Table.ManifestResource:	return new ManifestResourceTableRecordDataFactory(this, mdTable);
			case Table.GenericParam:		return new GenericParamTableRecordDataFactory(this, mdTable);
			case Table.LocalVariable:		return new LocalVariableTableRecordDataFactory(this, mdTable);
			default:						return new TableRecordDataFactory(this, mdTable);
			}
		}

		public override ComplexData GetStructure(HexPosition position) {
			if (!Span.Span.Contains(position))
				return null;

			if (HeaderSpan.Contains(position))
				return Header;

			var mdTable = GetTable(position);
			if (mdTable != null)
				return GetRecord(mdTable, position);

			return null;
		}

		TableRecordData GetRecord(MDTable mdTable, HexPosition position) {
			if (!mdTable.Span.Contains(position))
				return null;
			int index = (int)((position - mdTable.Span.Start).ToUInt64() / (uint)mdTable.TableInfo.RowSize);
			return GetRecord(new MDToken(mdTable.Table, index + 1));
		}

		public override TableRecordData GetRecord(MDToken token) {
			if (!initialized)
				Initialize();
			int tableIndex = (int)token.Table;
			if ((uint)tableIndex >= (uint)tableRecordDataFactories.Length)
				return null;
			return tableRecordDataFactories[tableIndex].Create(token.Rid);
		}

		MDTable GetTable(HexPosition position) {
			if (!initialized)
				Initialize();
			var array = mdTables;
			int lo = 0, hi = array.Length - 1;
			while (lo <= hi) {
				int index = (lo + hi) / 2;

				var mdTable = array[index];
				if (position < mdTable.Span.Start)
					hi = index - 1;
				else if (position >= mdTable.Span.End)
					lo = index + 1;
				else
					return mdTable;
			}
			return null;
		}

		void IDotNetHeap.SetMetadata(DotNetMetadataHeaders metadata) => this.metadata = metadata;
	}
}
