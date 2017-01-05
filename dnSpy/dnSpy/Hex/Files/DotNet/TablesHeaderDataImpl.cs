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

using System.Collections.Generic;
using System.Collections.ObjectModel;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Files;
using dnSpy.Contracts.Hex.Files.DotNet;

namespace dnSpy.Hex.Files.DotNet {
	sealed class TablesHeaderDataImpl : TablesHeaderData {
		public override StructField<UInt32Data> Reserved { get; }
		public override StructField<ByteData> MajorVersion { get; }
		public override StructField<ByteData> MinorVersion { get; }
		public override StructField<ByteFlagsData> Flags { get; }
		public override StructField<ByteData> Log2Rid { get; }
		public override StructField<UInt64FlagsData> ValidMask { get; }
		public override StructField<UInt64FlagsData> SortedMask { get; }
		public override StructField<UInt32Data> ExtraData { get; }
		public override StructField<ArrayData<UInt32Data>> Rows { get; }

		protected override BufferField[] Fields { get; }

		static readonly ReadOnlyCollection<FlagInfo> heapsFlagInfos = new ReadOnlyCollection<FlagInfo>(new FlagInfo[] {
			new FlagInfo(0x01, "HEAP_STRING_4"),
			new FlagInfo(0x02, "HEAP_GUID_4"),
			new FlagInfo(0x04, "HEAP_BLOB_4"),
			new FlagInfo(0x08, "PADDING_BIT"),
			new FlagInfo(0x10, "RESERVED"),
			new FlagInfo(0x20, "DELTA_ONLY"),
			new FlagInfo(0x40, "EXTRA_DATA"),
			new FlagInfo(0x80, "HAS_DELETE"),
		});

		internal static readonly ReadOnlyCollection<FlagInfo> tableFlagInfos = new ReadOnlyCollection<FlagInfo>(new FlagInfo[64] {
			new FlagInfo(0x0000000000000001, "Module"),
			new FlagInfo(0x0000000000000002, "TypeRef"),
			new FlagInfo(0x0000000000000004, "TypeDef"),
			new FlagInfo(0x0000000000000008, "FieldPtr"),
			new FlagInfo(0x0000000000000010, "Field"),
			new FlagInfo(0x0000000000000020, "MethodPtr"),
			new FlagInfo(0x0000000000000040, "Method"),
			new FlagInfo(0x0000000000000080, "ParamPtr"),
			new FlagInfo(0x0000000000000100, "Param"),
			new FlagInfo(0x0000000000000200, "InterfaceImpl"),
			new FlagInfo(0x0000000000000400, "MemberRef"),
			new FlagInfo(0x0000000000000800, "Constant"),
			new FlagInfo(0x0000000000001000, "CustomAttribute"),
			new FlagInfo(0x0000000000002000, "FieldMarshal"),
			new FlagInfo(0x0000000000004000, "DeclSecurity"),
			new FlagInfo(0x0000000000008000, "ClassLayout"),
			new FlagInfo(0x0000000000010000, "FieldLayout"),
			new FlagInfo(0x0000000000020000, "StandAloneSig"),
			new FlagInfo(0x0000000000040000, "EventMap"),
			new FlagInfo(0x0000000000080000, "EventPtr"),
			new FlagInfo(0x0000000000100000, "Event"),
			new FlagInfo(0x0000000000200000, "PropertyMap"),
			new FlagInfo(0x0000000000400000, "PropertyPtr"),
			new FlagInfo(0x0000000000800000, "Property"),
			new FlagInfo(0x0000000001000000, "MethodSemantics"),
			new FlagInfo(0x0000000002000000, "MethodImpl"),
			new FlagInfo(0x0000000004000000, "ModuleRef"),
			new FlagInfo(0x0000000008000000, "TypeSpec"),
			new FlagInfo(0x0000000010000000, "ImplMap"),
			new FlagInfo(0x0000000020000000, "FieldRVA"),
			new FlagInfo(0x0000000040000000, "ENCLog"),
			new FlagInfo(0x0000000080000000, "ENCMap"),
			new FlagInfo(0x0000000100000000, "Assembly"),
			new FlagInfo(0x0000000200000000, "AssemblyProcessor"),
			new FlagInfo(0x0000000400000000, "AssemblyOS"),
			new FlagInfo(0x0000000800000000, "AssemblyRef"),
			new FlagInfo(0x0000001000000000, "AssemblyRefProcessor"),
			new FlagInfo(0x0000002000000000, "AssemblyRefOS"),
			new FlagInfo(0x0000004000000000, "File"),
			new FlagInfo(0x0000008000000000, "ExportedType"),
			new FlagInfo(0x0000010000000000, "ManifestResource"),
			new FlagInfo(0x0000020000000000, "NestedClass"),
			new FlagInfo(0x0000040000000000, "GenericParam"),
			new FlagInfo(0x0000080000000000, "MethodSpec"),
			new FlagInfo(0x0000100000000000, "GenericParamConstraint"),
			new FlagInfo(0x0000200000000000, "Reserved2D"),
			new FlagInfo(0x0000400000000000, "Reserved2E"),
			new FlagInfo(0x0000800000000000, "Reserved2F"),
			new FlagInfo(0x0001000000000000, "Document"),
			new FlagInfo(0x0002000000000000, "MethodDebugInformation"),
			new FlagInfo(0x0004000000000000, "LocalScope"),
			new FlagInfo(0x0008000000000000, "LocalVariable"),
			new FlagInfo(0x0010000000000000, "LocalConstant"),
			new FlagInfo(0x0020000000000000, "ImportScope"),
			new FlagInfo(0x0040000000000000, "StateMachineMethod"),
			new FlagInfo(0x0080000000000000, "CustomDebugInformation"),
			new FlagInfo(0x0100000000000000, "Reserved38"),
			new FlagInfo(0x0200000000000000, "Reserved39"),
			new FlagInfo(0x0400000000000000, "Reserved3A"),
			new FlagInfo(0x0800000000000000, "Reserved3B"),
			new FlagInfo(0x1000000000000000, "Reserved3C"),
			new FlagInfo(0x2000000000000000, "Reserved3D"),
			new FlagInfo(0x4000000000000000, "Reserved3E"),
			new FlagInfo(0x8000000000000000, "Reserved3F"),
		});

		public TablesHeaderDataImpl(HexBufferSpan span, bool hasExtraData, int rowsFieldCount)
			: base(span) {
			var buffer = span.Buffer;
			var pos = span.Start.Position;
			Reserved = new StructField<UInt32Data>("m_ulReserved", new UInt32Data(buffer, pos));
			MajorVersion = new StructField<ByteData>("m_major", new ByteData(buffer, pos + 4));
			MinorVersion = new StructField<ByteData>("m_minor", new ByteData(buffer, pos + 5));
			Flags = new StructField<ByteFlagsData>("m_heaps", new ByteFlagsData(buffer, pos + 6, heapsFlagInfos));
			Log2Rid = new StructField<ByteData>("m_rid", new ByteData(buffer, pos + 7));
			ValidMask = new StructField<UInt64FlagsData>("m_maskvalid", new UInt64FlagsData(buffer, pos + 8, tableFlagInfos));
			SortedMask = new StructField<UInt64FlagsData>("m_sorted", new UInt64FlagsData(buffer, pos + 0x10, tableFlagInfos));
			pos += 0x18;
			if (hasExtraData) {
				ExtraData = new StructField<UInt32Data>("m_ulExtra", new UInt32Data(buffer, pos));
				pos += 4;
			}
			Rows = new StructField<ArrayData<UInt32Data>>("m_rows", ArrayData.CreateUInt32Array(buffer, pos, rowsFieldCount));
			var fields = new List<BufferField>(9) {
				Reserved,
				MajorVersion,
				MinorVersion,
				Flags,
				Log2Rid,
				ValidMask,
				SortedMask,
			};
			if (hasExtraData)
				fields.Add(ExtraData);
			fields.Add(Rows);
			Fields = fields.ToArray();
		}
	}
}
