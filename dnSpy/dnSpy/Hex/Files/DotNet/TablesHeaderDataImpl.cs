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
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Files;
using dnSpy.Contracts.Hex.Files.DotNet;

namespace dnSpy.Hex.Files.DotNet {
	sealed class TablesHeaderDataImpl : TablesHeaderData {
		public override StructField<UInt32Data> Reserved        { get; }
		public override StructField<ByteData> MajorVersion      { get; }
		public override StructField<ByteData> MinorVersion      { get; }
		public override StructField<ByteFlagsData> Flags        { get; }
		public override StructField<ByteData> Log2Rid           { get; }
		public override StructField<UInt64FlagsData> ValidMask  { get; }
		public override StructField<UInt64FlagsData> SortedMask { get; }
		public override StructField<UInt32Data>? ExtraData      { get; }
		public override StructField<ArrayData<UInt32Data>> Rows { get; }

		protected override BufferField[] Fields { get; }

		static readonly ReadOnlyCollection<FlagInfo> heapsFlagInfos = new ReadOnlyCollection<FlagInfo>(new FlagInfo[] {
			new FlagInfo(0x01, "HEAP_STRING_4"),
			new FlagInfo(0x02, "HEAP_GUID_4"  ),
			new FlagInfo(0x04, "HEAP_BLOB_4"  ),
			new FlagInfo(0x08, "PADDING_BIT"  ),
			new FlagInfo(0x10, "RESERVED"     ),
			new FlagInfo(0x20, "DELTA_ONLY"   ),
			new FlagInfo(0x40, "EXTRA_DATA"   ),
			new FlagInfo(0x80, "HAS_DELETE"   ),
		});

		internal static readonly ReadOnlyCollection<FlagInfo> tableFlagInfos = new ReadOnlyCollection<FlagInfo>(new FlagInfo[64] {
			new FlagInfo(0x0000000000000001, "00_Module"                ),
			new FlagInfo(0x0000000000000002, "01_TypeRef"               ),
			new FlagInfo(0x0000000000000004, "02_TypeDef"               ),
			new FlagInfo(0x0000000000000008, "03_FieldPtr"              ),
			new FlagInfo(0x0000000000000010, "04_Field"                 ),
			new FlagInfo(0x0000000000000020, "05_MethodPtr"             ),
			new FlagInfo(0x0000000000000040, "06_Method"                ),
			new FlagInfo(0x0000000000000080, "07_ParamPtr"              ),
			new FlagInfo(0x0000000000000100, "08_Param"                 ),
			new FlagInfo(0x0000000000000200, "09_InterfaceImpl"         ),
			new FlagInfo(0x0000000000000400, "0A_MemberRef"             ),
			new FlagInfo(0x0000000000000800, "0B_Constant"              ),
			new FlagInfo(0x0000000000001000, "0C_CustomAttribute"       ),
			new FlagInfo(0x0000000000002000, "0D_FieldMarshal"          ),
			new FlagInfo(0x0000000000004000, "0E_DeclSecurity"          ),
			new FlagInfo(0x0000000000008000, "0F_ClassLayout"           ),
			new FlagInfo(0x0000000000010000, "10_FieldLayout"           ),
			new FlagInfo(0x0000000000020000, "11_StandAloneSig"         ),
			new FlagInfo(0x0000000000040000, "12_EventMap"              ),
			new FlagInfo(0x0000000000080000, "13_EventPtr"              ),
			new FlagInfo(0x0000000000100000, "14_Event"                 ),
			new FlagInfo(0x0000000000200000, "15_PropertyMap"           ),
			new FlagInfo(0x0000000000400000, "16_PropertyPtr"           ),
			new FlagInfo(0x0000000000800000, "17_Property"              ),
			new FlagInfo(0x0000000001000000, "18_MethodSemantics"       ),
			new FlagInfo(0x0000000002000000, "19_MethodImpl"            ),
			new FlagInfo(0x0000000004000000, "1A_ModuleRef"             ),
			new FlagInfo(0x0000000008000000, "1B_TypeSpec"              ),
			new FlagInfo(0x0000000010000000, "1C_ImplMap"               ),
			new FlagInfo(0x0000000020000000, "1D_FieldRVA"              ),
			new FlagInfo(0x0000000040000000, "1E_EnCLog"                ),
			new FlagInfo(0x0000000080000000, "1F_EnCMap"                ),
			new FlagInfo(0x0000000100000000, "20_Assembly"              ),
			new FlagInfo(0x0000000200000000, "21_AssemblyProcessor"     ),
			new FlagInfo(0x0000000400000000, "22_AssemblyOS"            ),
			new FlagInfo(0x0000000800000000, "23_AssemblyRef"           ),
			new FlagInfo(0x0000001000000000, "24_AssemblyRefProcessor"  ),
			new FlagInfo(0x0000002000000000, "25_AssemblyRefOS"         ),
			new FlagInfo(0x0000004000000000, "26_File"                  ),
			new FlagInfo(0x0000008000000000, "27_ExportedType"          ),
			new FlagInfo(0x0000010000000000, "28_ManifestResource"      ),
			new FlagInfo(0x0000020000000000, "29_NestedClass"           ),
			new FlagInfo(0x0000040000000000, "2A_GenericParam"          ),
			new FlagInfo(0x0000080000000000, "2B_MethodSpec"            ),
			new FlagInfo(0x0000100000000000, "2C_GenericParamConstraint"),
			new FlagInfo(0x0000200000000000, "2D_Reserved"              ),
			new FlagInfo(0x0000400000000000, "2E_Reserved"              ),
			new FlagInfo(0x0000800000000000, "2F_Reserved"              ),
			new FlagInfo(0x0001000000000000, "30_Document"              ),
			new FlagInfo(0x0002000000000000, "31_MethodDebugInformation"),
			new FlagInfo(0x0004000000000000, "32_LocalScope"            ),
			new FlagInfo(0x0008000000000000, "33_LocalVariable"         ),
			new FlagInfo(0x0010000000000000, "34_LocalConstant"         ),
			new FlagInfo(0x0020000000000000, "35_ImportScope"           ),
			new FlagInfo(0x0040000000000000, "36_StateMachineMethod"    ),
			new FlagInfo(0x0080000000000000, "37_CustomDebugInformation"),
			new FlagInfo(0x0100000000000000, "38_Reserved"              ),
			new FlagInfo(0x0200000000000000, "39_Reserved"              ),
			new FlagInfo(0x0400000000000000, "3A_Reserved"              ),
			new FlagInfo(0x0800000000000000, "3B_Reserved"              ),
			new FlagInfo(0x1000000000000000, "3C_Reserved"              ),
			new FlagInfo(0x2000000000000000, "3D_Reserved"              ),
			new FlagInfo(0x4000000000000000, "3E_Reserved"              ),
			new FlagInfo(0x8000000000000000, "3F_Reserved"              ),
		});

		public TablesHeaderDataImpl(HexBufferSpan span, bool hasExtraData, int rowsFieldCount)
			: base(span) {
			var buffer = span.Buffer;
			var pos    = span.Start.Position;

			Reserved      = new StructField<UInt32Data>     ( "Reserved"  , new UInt32Data     (buffer, pos       ));
			MajorVersion  = new StructField<ByteData>       ( "MajorVer"  , new ByteData       (buffer, pos +    4));
			MinorVersion  = new StructField<ByteData>       ( "MinorVer"  , new ByteData       (buffer, pos +    5));
			Flags         = new StructField<ByteFlagsData>  ( "HeapSizes" , new ByteFlagsData  (buffer, pos +    6, heapsFlagInfos)); // a bitmask-byte that encodes how wide(=16 or 32 bit) offsets into the various heaps are
			Log2Rid       = new StructField<ByteData>       ( "Rid"       , new ByteData       (buffer, pos +    7));                 // Reserved Byte, always 1
			ValidMask     = new StructField<UInt64FlagsData>( "MaskValid" , new UInt64FlagsData(buffer, pos +    8, tableFlagInfos)); // a bitmask-qword telling which MetaData tables are present in the assembly.
			SortedMask    = new StructField<UInt64FlagsData>( "MaskSorted", new UInt64FlagsData(buffer, pos + 0x10, tableFlagInfos)); // a bitmask-qword telling which MetaData tables  are sorted.
			pos += 0x18;

			Rows          = new StructField<ArrayData<UInt32Data>>("RowsCounts" , ArrayData.CreateUInt32Array(buffer, pos, rowsFieldCount));

			if (hasExtraData) {
				ExtraData = new StructField<UInt32Data>(      "Extra"   , new UInt32Data(buffer, pos));
				pos += 4;
			}

			var fields = new List<BufferField>(9) {
				Reserved,
				MajorVersion,
				MinorVersion,
				Flags,
				Log2Rid,
				ValidMask,
				SortedMask,
			};
			fields.Add(Rows);

			if (hasExtraData)
				fields.Add(ExtraData!);
			Fields = fields.ToArray();
		}
	}
}
