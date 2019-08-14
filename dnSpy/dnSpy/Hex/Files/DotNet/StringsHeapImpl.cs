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
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Files;
using dnSpy.Contracts.Hex.Files.DotNet;

namespace dnSpy.Hex.Files.DotNet {
	sealed class StringsHeapImpl : StringsHeap, IDotNetHeap {
		public override DotNetMetadataHeaders Metadata => metadata!;
		DotNetMetadataHeaders? metadata;
		KnownStringInfo[]? knownStringInfos;

		readonly struct KnownStringInfo {
			public HexSpan Span { get; }
			public uint[] Tokens { get; }

			public KnownStringInfo(HexSpan span, uint[] tokens) {
				Span = span;
				Tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
			}
		}

		public StringsHeapImpl(HexBufferSpan span)
			: base(span) {
		}

		readonly struct StringInfo {
			public StringZ String { get; }
			public uint[] Tokens { get; }
			public StringInfo(StringZ span, uint[] tokens) {
				String = span;
				Tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
			}
		}

		readonly struct StringZ {
			public bool HasTerminator => StringSpan.End < FullSpan.End;
			public HexSpan StringSpan { get; }
			public HexSpan FullSpan { get; }
			public StringZ(HexSpan stringSpan, int terminatorLength) {
				StringSpan = stringSpan;
				FullSpan = HexSpan.FromBounds(stringSpan.Start, stringSpan.End + terminatorLength);
			}
		}

		public override ComplexData? GetStructure(HexPosition position) {
			var info = GetStringInfo(position);
			if (!(info is null))
				return new StringsHeapRecordData(Span.Buffer, info.Value.String.StringSpan, info.Value.String.HasTerminator, this, info.Value.Tokens);

			return null;
		}

		StringInfo? GetStringInfo(HexPosition position) {
			if (!Span.Contains(position))
				return null;
			var index = GetIndex(position);
			if (index < 0)
				return null;
			Debug2.Assert(!(knownStringInfos is null));

			var pos = knownStringInfos[index].Span.Start;
			var end = HexPosition.Min(Span.Span.End, pos + 0x1000);
			while (pos < end) {
				if (pos > position)
					return null;
				var stringSpan = GetStringSpan(pos, end);
				if (stringSpan.FullSpan.Contains(position)) {
					index = GetIndex(stringSpan.FullSpan.Start);
					var tokens = index >= 0 && knownStringInfos[index].Span.Start == stringSpan.FullSpan.Start ?
						knownStringInfos[index].Tokens : Array.Empty<uint>();
					return new StringInfo(stringSpan, tokens);
				}
				pos = stringSpan.FullSpan.End;
			}

			return null;
		}

		StringZ GetStringSpan(HexPosition position, HexPosition end) {
			var pos = position;
			var buffer = Span.Buffer;
			while (pos < end) {
				if (buffer.ReadByte(pos) == 0)
					return new StringZ(HexSpan.FromBounds(position, pos), 1);
				pos++;
			}
			return new StringZ(HexSpan.FromBounds(position, pos), 0);
		}

		void Initialize() {
			if (!(knownStringInfos is null))
				return;
			if (metadata is null)
				return;
			knownStringInfos = CreateKnownStringInfos(metadata.TablesStream);
		}

		KnownStringInfo[] CreateKnownStringInfos(TablesHeap? tables) {
			if (tables is null)
				return Array.Empty<KnownStringInfo>();

			var dict = new Dictionary<uint, List<uint>>();
			dict[0] = new List<uint>();
			foreach (var info in tableInitInfos) {
				if (info.Column2 >= 0)
					Add(dict, tables.MDTables[(int)info.Table], info.Column1, info.Column2);
				else
					Add(dict, tables.MDTables[(int)info.Table], info.Column1);
			}

			var list = new List<(uint pos, uint[] tokens)>(dict.Count);
			foreach (var kv in dict)
				list.Add((kv.Key, kv.Value.ToArray()));
			list.Sort((a, b) => a.pos.CompareTo(b.pos));

			var infos = new KnownStringInfo[list.Count];
			var start = Span.Span.Start;
			var end = Span.Span.End;
			int i;
			for (i = 0; i < infos.Length; i++) {
				var kv = list[i];
				var pos = start + kv.pos;
				if (pos >= end)
					break;
				var span = HexSpan.FromBounds(pos, i + 1 < list.Count ? start + list[i + 1].pos : end);
				infos[i] = new KnownStringInfo(span, kv.tokens);
			}
			if (i != infos.Length)
				Array.Resize(ref infos, i);

			return infos;
		}
		// Sorted on Table and column position so we read from increasing positions
		static readonly TableInitInfo[] tableInitInfos = new TableInitInfo[] {
			new TableInitInfo(Table.Module, 1),// Name
			new TableInitInfo(Table.TypeRef, 1, 2),// Name, Namespace
			new TableInitInfo(Table.TypeDef, 1, 2),// Name, Namespace
			new TableInitInfo(Table.Field, 1),// Name
			new TableInitInfo(Table.Method, 3),// Name
			new TableInitInfo(Table.Param, 2),// Name
			new TableInitInfo(Table.MemberRef, 1),// Name
			new TableInitInfo(Table.Event, 1),// Name
			new TableInitInfo(Table.Property, 1),// Name
			new TableInitInfo(Table.ModuleRef, 0),// Name
			new TableInitInfo(Table.ImplMap, 2),// ImportName
			new TableInitInfo(Table.Assembly, 7, 8),// Name, Locale
			new TableInitInfo(Table.AssemblyRef, 6, 7),// Name, Locale
			new TableInitInfo(Table.File, 1),// Name
			new TableInitInfo(Table.ExportedType, 2, 3),// TypeName, TypeNamespace
			new TableInitInfo(Table.ManifestResource, 2),// Name
			new TableInitInfo(Table.GenericParam, 3),// Name
			new TableInitInfo(Table.LocalVariable, 2),// Name
			new TableInitInfo(Table.LocalConstant, 0),// Name
		};

		readonly struct TableInitInfo {
			public Table Table { get; }
			public byte Column1 { get; }
			public sbyte Column2 { get; }

			public TableInitInfo(Table table, byte column1) {
				Table = table;
				Column1 = column1;
				Column2 = -1;
			}

			public TableInitInfo(Table table, byte column1, sbyte column2) {
				Table = table;
				Column1 = column1;
				Column2 = column2;
			}
		}

		void Add(Dictionary<uint, List<uint>> dict, MDTable mdTable, int column1) {
			var buffer = Span.Buffer;
			var recPos = mdTable.Span.Start;
			var rows = mdTable.Rows;
			var colInfo1 = mdTable.Columns[column1];
			Debug.Assert(colInfo1.ColumnSize == ColumnSize.Strings);
			uint recSize = mdTable.RowSize;
			bool bigStrings = colInfo1.Size == 4;
			uint tokenBase = new MDToken(mdTable.Table, 0).Raw;
			for (uint rid = 1; rid <= rows; rid++, recPos += recSize) {
				uint offs1 = bigStrings ? buffer.ReadUInt32(recPos + colInfo1.Offset) : buffer.ReadUInt16(recPos + colInfo1.Offset);
				if (offs1 == 0)
					continue;
				if (!dict.TryGetValue(offs1, out var list))
					dict[offs1] = list = new List<uint>();
				list.Add(tokenBase + rid);
			}
		}

		void Add(Dictionary<uint, List<uint>> dict, MDTable mdTable, int column1, int column2) {
			var buffer = Span.Buffer;
			var recPos = mdTable.Span.Start;
			var rows = mdTable.Rows;
			var colInfo1 = mdTable.Columns[column1];
			var colInfo2 = mdTable.Columns[column2];
			Debug.Assert(colInfo1.ColumnSize == ColumnSize.Strings);
			Debug.Assert(colInfo2.ColumnSize == ColumnSize.Strings);
			uint recSize = mdTable.RowSize;
			bool bigStrings = colInfo1.Size == 4;
			uint tokenBase = new MDToken(mdTable.Table, 0).Raw;
			List<uint>? list;
			for (uint rid = 1; rid <= rows; rid++, recPos += recSize) {
				uint offs1, offs2;
				if (bigStrings) {
					offs1 = buffer.ReadUInt32(recPos + colInfo1.Offset);
					offs2 = buffer.ReadUInt32(recPos + colInfo2.Offset);
				}
				else {
					offs1 = buffer.ReadUInt16(recPos + colInfo1.Offset);
					offs2 = buffer.ReadUInt16(recPos + colInfo2.Offset);
				}

				if (offs1 != 0) {
					if (!dict.TryGetValue(offs1, out list))
						dict[offs1] = list = new List<uint>();
					list.Add(tokenBase + rid);
				}

				if (offs2 != 0 && offs1 != offs2) {
					if (!dict.TryGetValue(offs2, out list))
						dict[offs2] = list = new List<uint>();
					list.Add(tokenBase + rid);
				}
			}
		}

		int GetIndex(HexPosition position) {
			var array = knownStringInfos;
			if (array is null) {
				Initialize();
				array = knownStringInfos;
				if (array is null)
					return -1;
			}
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

		void IDotNetHeap.SetMetadata(DotNetMetadataHeaders metadata) => this.metadata = metadata;
	}
}
