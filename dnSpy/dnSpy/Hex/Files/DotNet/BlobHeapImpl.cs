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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Files;
using dnSpy.Contracts.Hex.Files.DotNet;

namespace dnSpy.Hex.Files.DotNet {
	sealed class BlobHeapImpl : BlobHeap, IDotNetHeap {
		public override DotNetMetadataHeaders Metadata => metadata;
		DotNetMetadataHeaders metadata;
		BlobDataInfo[] blobDataInfos;

		enum BlobDataKind {
			None,
			TypeSignature,
			Signature,
			Constant,
			CustomAttribute,
			NativeType,
			PermissionSet,
			PublicKey,
			PublicKeyOrToken,
			HashValue,
			Utf8Name,
			Name,
			SequencePoints,
			LocalConstantSig,
			Imports,
			CustomDebugInformationValue,
		}

		struct BlobDataInfo {
			public HexSpan Span { get; }
			public ReadOnlyCollection<uint> Tokens { get; }
			public BlobDataKind Kind { get; }

			public BlobDataInfo(HexSpan span, uint[] tokens, BlobDataKind kind) {
				Span = span;
				Tokens = new ReadOnlyCollection<uint>(tokens);
				Kind = kind;
			}
		}

		struct BlobDataInfoPosition {
			public HexPosition Position { get; }
			public List<uint> Tokens { get; }
			public BlobDataKind Kind { get; }

			public BlobDataInfoPosition(HexPosition position, BlobDataKind kind) {
				Position = position;
				Tokens = new List<uint>();
				Kind = kind;
			}
		}

		public BlobHeapImpl(HexBufferSpan span)
			: base(span) {
		}

		void Initialize() {
			if (blobDataInfos != null)
				return;
			if (metadata == null)
				return;
			blobDataInfos = CreateBlobDataInfos(metadata.TablesStream);
		}

		public override ComplexData GetStructure(HexPosition position) {
			var info = GetBlobDataInfo(position);
			if (info != null)
				return GetStructure(info.Value, position);

			return null;
		}

		ComplexData GetStructure(BlobDataInfo info, HexPosition position) {
			var pos = info.Span.Start;
			var lengthStart = pos;
			var len = ReadCompressedUInt32(ref pos) ?? -1;
			if (len < 0)
				return null;
			if (pos + len > Span.Span.End)
				return null;
			var lengthSpan = HexSpan.FromBounds(lengthStart, pos);
			var dataSpan = new HexSpan(lengthSpan.End, (ulong)len);
			var fullSpan = HexSpan.FromBounds(lengthSpan.Start, dataSpan.End);
			if (!fullSpan.Contains(position))
				return null;

			switch (info.Kind) {
			case BlobDataKind.None:
			case BlobDataKind.TypeSignature:
			case BlobDataKind.Signature:
			case BlobDataKind.Constant:
			case BlobDataKind.CustomAttribute:
			case BlobDataKind.NativeType:
			case BlobDataKind.PermissionSet:
			case BlobDataKind.PublicKey:
			case BlobDataKind.PublicKeyOrToken:
			case BlobDataKind.HashValue:
			case BlobDataKind.Utf8Name:
			case BlobDataKind.Name:
			case BlobDataKind.SequencePoints:
			case BlobDataKind.LocalConstantSig:
			case BlobDataKind.Imports:
			case BlobDataKind.CustomDebugInformationValue:
				var varray = ArrayData.CreateVirtualByteArray(new HexBufferSpan(Span.Buffer, dataSpan));
				return new BlobHeapRecordData(Span.Buffer, fullSpan, lengthSpan, varray, info.Tokens, this);

			default:
				throw new InvalidOperationException();
			}
		}

		BlobDataInfo? GetBlobDataInfo(HexPosition position) {
			if (!Span.Contains(position))
				return null;
			var index = GetIndex(position);
			if (index < 0)
				return null;
			return blobDataInfos[index];
		}

		BlobDataInfo[] CreateBlobDataInfos(TablesHeap tables) {
			if (tables == null || Span.IsEmpty)
				return Array.Empty<BlobDataInfo>();

			var dict = new Dictionary<uint, BlobDataInfoPosition>();
			foreach (var info in tableInitInfos) {
				if (info.Column2 >= 0)
					Add(dict, tables.MDTables[(int)info.Table], info.Column1, info.Kind1, info.Column2, info.Kind2);
				else
					Add(dict, tables.MDTables[(int)info.Table], info.Column1, info.Kind1);
			}
			AddNameIndexes(dict, tables, Table.Document, 0);
			AddImportScopeIndexes(dict, tables, Table.ImportScope, 1);
			dict[0] = new BlobDataInfoPosition(Span.Span.Start, BlobDataKind.None);

			var infos = dict.Values.ToArray();
			var res = new BlobDataInfo[infos.Length];
			Array.Sort(infos, (a, b) => a.Position.CompareTo(b.Position));
			for (int i = 0; i < infos.Length; i++) {
				var info = infos[i];
				var end = i + 1 < infos.Length ? infos[i + 1].Position : Span.Span.End;
				res[i] = new BlobDataInfo(HexSpan.FromBounds(info.Position, end), info.Tokens.ToArray(), info.Kind);
			}

			return res;
		}
		// Sorted on Table and column position so we read from increasing positions
		static readonly TableInitInfo[] tableInitInfos = new TableInitInfo[] {
			new TableInitInfo(Table.Field, 2, BlobDataKind.Signature),// Signature
			new TableInitInfo(Table.Method, 4, BlobDataKind.Signature),// Signature
			new TableInitInfo(Table.MemberRef, 2, BlobDataKind.Signature),// Signature
			new TableInitInfo(Table.Constant, 3, BlobDataKind.Constant),// Value
			new TableInitInfo(Table.CustomAttribute, 2, BlobDataKind.CustomAttribute),// Value
			new TableInitInfo(Table.FieldMarshal, 1, BlobDataKind.NativeType),// NativeType
			new TableInitInfo(Table.DeclSecurity, 2, BlobDataKind.PermissionSet),// PermissionSet
			new TableInitInfo(Table.StandAloneSig, 0, BlobDataKind.Signature),// Signature
			new TableInitInfo(Table.Property, 2, BlobDataKind.Signature),// Type
			new TableInitInfo(Table.TypeSpec, 0, BlobDataKind.TypeSignature),// Signature
			new TableInitInfo(Table.Assembly, 6, BlobDataKind.PublicKey),// PublicKey
			new TableInitInfo(Table.AssemblyRef, 5, BlobDataKind.PublicKeyOrToken, 8, BlobDataKind.HashValue),// PublicKeyOrToken, HashValue
			new TableInitInfo(Table.File, 2, BlobDataKind.HashValue),// HashValue
			new TableInitInfo(Table.MethodSpec, 1, BlobDataKind.Signature),// Instantiation
			new TableInitInfo(Table.Document, 0, BlobDataKind.Name, 2, BlobDataKind.HashValue),// Name, Hash
			new TableInitInfo(Table.MethodDebugInformation, 1, BlobDataKind.SequencePoints),// SequencePoints
			new TableInitInfo(Table.LocalConstant, 1, BlobDataKind.LocalConstantSig),// Signature
			new TableInitInfo(Table.ImportScope, 1, BlobDataKind.Imports),// Imports
			new TableInitInfo(Table.CustomDebugInformation, 2, BlobDataKind.CustomDebugInformationValue),// Value
		};

		struct TableInitInfo {
			public Table Table { get; }
			public byte Column1 { get; }
			public BlobDataKind Kind1 { get; }
			public sbyte Column2 { get; }
			public BlobDataKind Kind2 { get; }

			public TableInitInfo(Table table, byte column1, BlobDataKind kind1) {
				Table = table;
				Column1 = column1;
				Kind1 = kind1;
				Column2 = -1;
				Kind2 = BlobDataKind.None;
			}

			public TableInitInfo(Table table, byte column1, BlobDataKind kind1, sbyte column2, BlobDataKind kind2) {
				Table = table;
				Column1 = column1;
				Kind1 = kind1;
				Column2 = column2;
				Kind2 = kind2;
			}
		}

		void Add(Dictionary<uint, BlobDataInfoPosition> dict, MDTable mdTable, int column1, BlobDataKind kind1) {
			var heapStart = Span.Span.Start;
			var heapEnd = Span.Span.End;
			var buffer = Span.Buffer;
			var recPos = mdTable.Span.Start;
			var rows = mdTable.Rows;
			var colInfo1 = mdTable.Columns[column1];
			Debug.Assert(colInfo1.ColumnSize == ColumnSize.Blob);
			uint recSize = mdTable.RowSize;
			bool bigBlob = colInfo1.Size == 4;
			uint tokenBase = new MDToken(mdTable.Table, 0).Raw;
			for (uint rid = 1; rid <= rows; rid++, recPos += recSize) {
				uint offs1 = bigBlob ? buffer.ReadUInt32(recPos + colInfo1.Offset) : buffer.ReadUInt16(recPos + colInfo1.Offset);
				if (offs1 == 0)
					continue;

				List<uint> tokens;
				BlobDataInfoPosition info;
				if (dict.TryGetValue(offs1, out info))
					tokens = info.Tokens;
				else {
					var pos = heapStart + offs1;
					if (pos < heapEnd) {
						dict[offs1] = info = new BlobDataInfoPosition(pos, kind1);
						tokens = info.Tokens;
					}
					else
						tokens = null;
				}
				tokens?.Add(tokenBase + rid);
			}
		}

		void Add(Dictionary<uint, BlobDataInfoPosition> dict, MDTable mdTable, int column1, BlobDataKind kind1, int column2, BlobDataKind kind2) {
			var heapStart = Span.Span.Start;
			var heapEnd = Span.Span.End;
			var buffer = Span.Buffer;
			var recPos = mdTable.Span.Start;
			var rows = mdTable.Rows;
			var colInfo1 = mdTable.Columns[column1];
			var colInfo2 = mdTable.Columns[column2];
			Debug.Assert(colInfo1.ColumnSize == ColumnSize.Blob);
			Debug.Assert(colInfo2.ColumnSize == ColumnSize.Blob);
			uint recSize = mdTable.RowSize;
			bool bigBlob = colInfo1.Size == 4;
			uint tokenBase = new MDToken(mdTable.Table, 0).Raw;
			for (uint rid = 1; rid <= rows; rid++, recPos += recSize) {
				uint offs1 = bigBlob ? buffer.ReadUInt32(recPos + colInfo1.Offset) : buffer.ReadUInt16(recPos + colInfo1.Offset);
				uint offs2 = bigBlob ? buffer.ReadUInt32(recPos + colInfo2.Offset) : buffer.ReadUInt16(recPos + colInfo2.Offset);

				{
					List<uint> tokens;
					BlobDataInfoPosition info;
					if (offs1 == 0)
						tokens = null;
					else if (dict.TryGetValue(offs1, out info))
						tokens = info.Tokens;
					else {
						var pos = heapStart + offs1;
						if (pos < heapEnd) {
							dict[offs1] = info = new BlobDataInfoPosition(pos, kind1);
							tokens = info.Tokens;
						}
						else
							tokens = null;
					}
					tokens?.Add(tokenBase + rid);
				}

				{
					List<uint> tokens;
					BlobDataInfoPosition info;
					if (offs2 == 0)
						tokens = null;
					else if (dict.TryGetValue(offs2, out info))
						tokens = info.Tokens;
					else {
						var pos = heapStart + offs2;
						if (pos < heapEnd) {
							dict[offs2] = info = new BlobDataInfoPosition(pos, kind2);
							tokens = info.Tokens;
						}
						else
							tokens = null;
					}
					tokens?.Add(tokenBase + rid);
				}
			}
		}

		void AddNameIndexes(Dictionary<uint, BlobDataInfoPosition> dict, TablesHeap tables, Table table, int column) {
			var mdTable = tables.MDTables[(int)table];
			if (mdTable.Rows == 0)
				return;

			var heapStart = Span.Span.Start;
			var heapEnd = Span.Span.End;
			var buffer = Span.Buffer;
			var recPos = mdTable.Span.Start;
			var rows = mdTable.Rows;
			var colInfo = mdTable.Columns[column];
			uint recSize = mdTable.RowSize;
			bool bigBlob = colInfo.Size == 4;
			for (uint rid = 1; rid <= rows; rid++, recPos += recSize) {
				uint offs = bigBlob ? buffer.ReadUInt32(recPos + colInfo.Offset) : buffer.ReadUInt16(recPos + colInfo.Offset);
				if (offs == 0)
					continue;

				var pos = heapStart + offs;
				int len = ReadCompressedUInt32(ref pos) ?? -1;
				if (len <= 0)
					continue;
				var end = HexPosition.Min(heapEnd, pos + len);
				pos = SkipUtf8Char(pos, end);
				while (pos < end) {
					var nameOffs = ReadCompressedUInt32(ref pos) ?? -1;
					if (nameOffs < 0)
						break;
					if (nameOffs != 0 && !dict.ContainsKey((uint)nameOffs)) {
						var namePos = heapStart + nameOffs;
						if (namePos < heapEnd)
							dict[(uint)nameOffs] = new BlobDataInfoPosition(namePos, BlobDataKind.Utf8Name);
					}
				}
			}
		}

		HexPosition SkipUtf8Char(HexPosition pos, HexPosition end) {
			var decoder = Encoding.UTF8.GetDecoder();
			var bytes = new byte[1];
			var chars = new char[2];
			while (pos < end) {
				byte b = Span.Buffer.ReadByte(pos++);
				int bytesUsed;
				int charsUsed;
				bool completed;
				decoder.Convert(bytes, 0, 1, chars, 0, 2, pos == end, out bytesUsed, out charsUsed, out completed);
				if (charsUsed > 0)
					break;
			}
			return pos;
		}

		void AddImportScopeIndexes(Dictionary<uint, BlobDataInfoPosition> dict, TablesHeap tables, Table table, int column) {
			var mdTable = tables.MDTables[(int)table];
			if (mdTable.Rows == 0)
				return;

			var heapStart = Span.Span.Start;
			var heapEnd = Span.Span.End;
			var buffer = Span.Buffer;
			var recPos = mdTable.Span.Start;
			var rows = mdTable.Rows;
			var colInfo = mdTable.Columns[column];
			uint recSize = mdTable.RowSize;
			bool bigBlob = colInfo.Size == 4;
			for (uint rid = 1; rid <= rows; rid++, recPos += recSize) {
				uint offs = bigBlob ? buffer.ReadUInt32(recPos + colInfo.Offset) : buffer.ReadUInt16(recPos + colInfo.Offset);
				if (offs == 0)
					continue;

				var pos = heapStart + offs;
				int len = ReadCompressedUInt32(ref pos) ?? -1;
				if (len <= 0)
					continue;
				var end = HexPosition.Min(heapEnd, pos + len);
				while (pos < end) {
					var kind = ReadCompressedUInt32(ref pos);
					if (kind == null || pos > end)
						break;
					var flags = GetImportFlags(kind.Value);
					Debug.Assert(flags != 0);
					if (flags == 0)
						break;

					if ((flags & ImportFlags.Alias) != 0) {
						var valueTmp = ReadCompressedUInt32(ref pos);
						if (valueTmp == null || pos > end)
							break;
						var value = valueTmp.Value;
						if (value != 0 && !dict.ContainsKey((uint)value)) {
							var namePos = heapStart + value;
							if (namePos < heapEnd)
								dict[(uint)value] = new BlobDataInfoPosition(namePos, BlobDataKind.Utf8Name);
						}
					}

					if ((flags & ImportFlags.TargetAssembly) != 0) {
						var valueTmp = ReadCompressedUInt32(ref pos);
						if (valueTmp == null || pos > end)
							break;
					}

					if ((flags & ImportFlags.TargetNamespace) != 0) {
						var valueTmp = ReadCompressedUInt32(ref pos);
						if (valueTmp == null || pos > end)
							break;
						var value = valueTmp.Value;
						if (value != 0 && !dict.ContainsKey((uint)value)) {
							var namePos = heapStart + value;
							if (namePos < heapEnd)
								dict[(uint)value] = new BlobDataInfoPosition(namePos, BlobDataKind.Utf8Name);
						}
					}

					if ((flags & ImportFlags.TargetType) != 0) {
						var valueTmp = ReadCompressedUInt32(ref pos);
						if (valueTmp == null || pos > end)
							break;
					}
				}
			}
		}

		static ImportFlags GetImportFlags(int kind) {
			switch (kind) {
			// Comments from Roslyn's MetadataWriter.PortablePdb.cs
			// <import> ::= ImportNamespace <target-namespace>
			case 1:		return ImportFlags.TargetNamespace;
			// <import> ::= ImportAssemblyNamespace <target-assembly> <target-namespace>
			case 2:		return ImportFlags.TargetAssembly | ImportFlags.TargetNamespace;
			// <import> ::= ImportType <target-type>
			case 3:		return ImportFlags.TargetType;
			// <import> ::= ImportXmlNamespace <alias> <target-namespace>
			case 4:		return ImportFlags.Alias | ImportFlags.TargetNamespace;
			// <import> ::= ImportReferenceAlias <alias>
			case 5:		return ImportFlags.Alias;
			// <import> ::= AliasAssemblyReference <alias> <target-assembly>
			case 6:		return ImportFlags.Alias | ImportFlags.TargetAssembly;
			// <import> ::= AliasNamespace <alias> <target-namespace>
			case 7:		return ImportFlags.Alias | ImportFlags.TargetNamespace;
			// <import> ::= AliasAssemblyNamespace <alias> <target-assembly> <target-namespace>
			case 8:		return ImportFlags.Alias | ImportFlags.TargetAssembly | ImportFlags.TargetNamespace;
			// <import> ::= AliasType <alias> <target-type>
			case 9:		return ImportFlags.Alias | ImportFlags.TargetType;
			default:	return 0;
			}
		}

		[Flags]
		enum ImportFlags {
			None				= 0,
			Alias				= 0x01,
			TargetAssembly		= 0x02,
			TargetNamespace		= 0x04,
			TargetType			= 0x08,
		}

		int GetIndex(HexPosition position) {
			var array = blobDataInfos;
			if (array == null) {
				Initialize();
				array = blobDataInfos;
				if (array == null)
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
