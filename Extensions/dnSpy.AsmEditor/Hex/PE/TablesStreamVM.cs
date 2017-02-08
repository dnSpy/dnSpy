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

using System.Collections.Generic;
using System.Diagnostics;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Files.DotNet;

namespace dnSpy.AsmEditor.Hex.PE {
	sealed class TablesStreamVM : HexVM {
		public override string Name { get; }

		public UInt32HexField M_ulReservedVM { get; }
		public ByteHexField M_majorVM { get; }
		public ByteHexField M_minorVM { get; }
		public ByteFlagsHexField M_heapsVM { get; }
		public ByteHexField M_ridVM { get; }
		public UInt64FlagsHexField M_maskvalidVM { get; }
		public UInt64FlagsHexField M_sortedVM { get; }
		public UInt32HexField Rows00VM => rowsVM[0x00];
		public UInt32HexField Rows01VM => rowsVM[0x01];
		public UInt32HexField Rows02VM => rowsVM[0x02];
		public UInt32HexField Rows03VM => rowsVM[0x03];
		public UInt32HexField Rows04VM => rowsVM[0x04];
		public UInt32HexField Rows05VM => rowsVM[0x05];
		public UInt32HexField Rows06VM => rowsVM[0x06];
		public UInt32HexField Rows07VM => rowsVM[0x07];
		public UInt32HexField Rows08VM => rowsVM[0x08];
		public UInt32HexField Rows09VM => rowsVM[0x09];
		public UInt32HexField Rows0AVM => rowsVM[0x0A];
		public UInt32HexField Rows0BVM => rowsVM[0x0B];
		public UInt32HexField Rows0CVM => rowsVM[0x0C];
		public UInt32HexField Rows0DVM => rowsVM[0x0D];
		public UInt32HexField Rows0EVM => rowsVM[0x0E];
		public UInt32HexField Rows0FVM => rowsVM[0x0F];
		public UInt32HexField Rows10VM => rowsVM[0x10];
		public UInt32HexField Rows11VM => rowsVM[0x11];
		public UInt32HexField Rows12VM => rowsVM[0x12];
		public UInt32HexField Rows13VM => rowsVM[0x13];
		public UInt32HexField Rows14VM => rowsVM[0x14];
		public UInt32HexField Rows15VM => rowsVM[0x15];
		public UInt32HexField Rows16VM => rowsVM[0x16];
		public UInt32HexField Rows17VM => rowsVM[0x17];
		public UInt32HexField Rows18VM => rowsVM[0x18];
		public UInt32HexField Rows19VM => rowsVM[0x19];
		public UInt32HexField Rows1AVM => rowsVM[0x1A];
		public UInt32HexField Rows1BVM => rowsVM[0x1B];
		public UInt32HexField Rows1CVM => rowsVM[0x1C];
		public UInt32HexField Rows1DVM => rowsVM[0x1D];
		public UInt32HexField Rows1EVM => rowsVM[0x1E];
		public UInt32HexField Rows1FVM => rowsVM[0x1F];
		public UInt32HexField Rows20VM => rowsVM[0x20];
		public UInt32HexField Rows21VM => rowsVM[0x21];
		public UInt32HexField Rows22VM => rowsVM[0x22];
		public UInt32HexField Rows23VM => rowsVM[0x23];
		public UInt32HexField Rows24VM => rowsVM[0x24];
		public UInt32HexField Rows25VM => rowsVM[0x25];
		public UInt32HexField Rows26VM => rowsVM[0x26];
		public UInt32HexField Rows27VM => rowsVM[0x27];
		public UInt32HexField Rows28VM => rowsVM[0x28];
		public UInt32HexField Rows29VM => rowsVM[0x29];
		public UInt32HexField Rows2AVM => rowsVM[0x2A];
		public UInt32HexField Rows2BVM => rowsVM[0x2B];
		public UInt32HexField Rows2CVM => rowsVM[0x2C];
		public UInt32HexField Rows2DVM => rowsVM[0x2D];
		public UInt32HexField Rows2EVM => rowsVM[0x2E];
		public UInt32HexField Rows2FVM => rowsVM[0x2F];
		public UInt32HexField Rows30VM => rowsVM[0x30];
		public UInt32HexField Rows31VM => rowsVM[0x31];
		public UInt32HexField Rows32VM => rowsVM[0x32];
		public UInt32HexField Rows33VM => rowsVM[0x33];
		public UInt32HexField Rows34VM => rowsVM[0x34];
		public UInt32HexField Rows35VM => rowsVM[0x35];
		public UInt32HexField Rows36VM => rowsVM[0x36];
		public UInt32HexField Rows37VM => rowsVM[0x37];
		public UInt32HexField Rows38VM => rowsVM[0x38];
		public UInt32HexField Rows39VM => rowsVM[0x39];
		public UInt32HexField Rows3AVM => rowsVM[0x3A];
		public UInt32HexField Rows3BVM => rowsVM[0x3B];
		public UInt32HexField Rows3CVM => rowsVM[0x3C];
		public UInt32HexField Rows3DVM => rowsVM[0x3D];
		public UInt32HexField Rows3EVM => rowsVM[0x3E];
		public UInt32HexField Rows3FVM => rowsVM[0x3F];

		readonly UInt32HexField[] rowsVM;

		public UInt32HexField M_ulExtraVM { get; }

		public override IEnumerable<HexField> HexFields => hexFields;
		readonly HexField[] hexFields;

		public MetaDataTableVM[] MetaDataTables => metaDataTables;
		readonly MetaDataTableVM[] metaDataTables;

		public TablesStreamVM(HexBuffer buffer, TablesHeap tablesHeap, MetaDataTableVM[] metaDataTables)
			: base(tablesHeap.HeaderSpan) {
			Debug.Assert(metaDataTables.Length == 0x40);
			this.metaDataTables = metaDataTables;
			Name = tablesHeap.Header.Name;
			M_ulReservedVM = new UInt32HexField(tablesHeap.Header.Reserved);
			M_majorVM = new ByteHexField(tablesHeap.Header.MajorVersion, true);
			M_minorVM = new ByteHexField(tablesHeap.Header.MinorVersion, true);
			M_heapsVM = new ByteFlagsHexField(tablesHeap.Header.Flags);
			M_heapsVM.Add(new BooleanHexBitField("BigStrings", 0));
			M_heapsVM.Add(new BooleanHexBitField("BigGUID", 1));
			M_heapsVM.Add(new BooleanHexBitField("BigBlob", 2));
			M_heapsVM.Add(new BooleanHexBitField("Padding", 3));
			M_heapsVM.Add(new BooleanHexBitField("Reserved", 4));
			M_heapsVM.Add(new BooleanHexBitField("DeltaOnly", 5));
			M_heapsVM.Add(new BooleanHexBitField("ExtraData", 6));
			M_heapsVM.Add(new BooleanHexBitField("HasDelete", 7));
			M_ridVM = new ByteHexField(tablesHeap.Header.Log2Rid);
			M_maskvalidVM = new UInt64FlagsHexField(tablesHeap.Header.ValidMask);
			AddTableFlags(M_maskvalidVM);
			M_sortedVM = new UInt64FlagsHexField(tablesHeap.Header.SortedMask);
			AddTableFlags(M_sortedVM);

			var list = new List<HexField> {
				M_ulReservedVM,
				M_majorVM,
				M_minorVM,
				M_heapsVM,
				M_ridVM,
				M_maskvalidVM,
				M_sortedVM,
			};

			rowsVM = new UInt32HexField[64];
			ulong valid = tablesHeap.ValidMask;
			for (int i = 0, rowIndex = 0; i < rowsVM.Length; i++) {
				UInt32HexField field;
				if ((valid & 1) != 0 && rowIndex < tablesHeap.Header.Rows.Data.FieldCount) {
					var row = tablesHeap.Header.Rows.Data[rowIndex++].Data;
					field = new UInt32HexField(row, tablesHeap.Header.Rows.Name + "[" + i.ToString("X2") + "]");
					list.Add(field);
				}
				else
					field = UInt32HexField.TryCreate(null);

				rowsVM[i] = field;
				valid >>= 1;
			}

			M_ulExtraVM = UInt32HexField.TryCreate(tablesHeap.Header.ExtraData);
			M_ulExtraVM.IsVisible = tablesHeap.Header.HasExtraData;
			if (tablesHeap.Header.HasExtraData)
				list.Add(M_ulExtraVM);

			hexFields = list.ToArray();
		}

		static void AddTableFlags(UInt64FlagsHexField field) {
			field.Add(new BooleanHexBitField("Module", 0));
			field.Add(new BooleanHexBitField("TypeRef", 1));
			field.Add(new BooleanHexBitField("TypeDef", 2));
			field.Add(new BooleanHexBitField("FieldPtr", 3));
			field.Add(new BooleanHexBitField("Field", 4));
			field.Add(new BooleanHexBitField("MethodPtr", 5));
			field.Add(new BooleanHexBitField("Method", 6));
			field.Add(new BooleanHexBitField("ParamPtr", 7));
			field.Add(new BooleanHexBitField("Param", 8));
			field.Add(new BooleanHexBitField("InterfaceImpl", 9));
			field.Add(new BooleanHexBitField("MemberRef", 10));
			field.Add(new BooleanHexBitField("Constant", 11));
			field.Add(new BooleanHexBitField("CustomAttribute", 12));
			field.Add(new BooleanHexBitField("FieldMarshal", 13));
			field.Add(new BooleanHexBitField("DeclSecurity", 14));
			field.Add(new BooleanHexBitField("ClassLayout", 15));
			field.Add(new BooleanHexBitField("FieldLayout", 16));
			field.Add(new BooleanHexBitField("StandAloneSig", 17));
			field.Add(new BooleanHexBitField("EventMap", 18));
			field.Add(new BooleanHexBitField("EventPtr", 19));
			field.Add(new BooleanHexBitField("Event", 20));
			field.Add(new BooleanHexBitField("PropertyMap", 21));
			field.Add(new BooleanHexBitField("PropertyPtr", 22));
			field.Add(new BooleanHexBitField("Property", 23));
			field.Add(new BooleanHexBitField("MethodSemantics", 24));
			field.Add(new BooleanHexBitField("MethodImpl", 25));
			field.Add(new BooleanHexBitField("ModuleRef", 26));
			field.Add(new BooleanHexBitField("TypeSpec", 27));
			field.Add(new BooleanHexBitField("ImplMap", 28));
			field.Add(new BooleanHexBitField("FieldRVA", 29));
			field.Add(new BooleanHexBitField("ENCLog", 30));
			field.Add(new BooleanHexBitField("ENCMap", 31));
			field.Add(new BooleanHexBitField("Assembly", 32));
			field.Add(new BooleanHexBitField("AssemblyProcessor", 33));
			field.Add(new BooleanHexBitField("AssemblyOS", 34));
			field.Add(new BooleanHexBitField("AssemblyRef", 35));
			field.Add(new BooleanHexBitField("AssemblyRefProcessor", 36));
			field.Add(new BooleanHexBitField("AssemblyRefOS", 37));
			field.Add(new BooleanHexBitField("File", 38));
			field.Add(new BooleanHexBitField("ExportedType", 39));
			field.Add(new BooleanHexBitField("ManifestResource", 40));
			field.Add(new BooleanHexBitField("NestedClass", 41));
			field.Add(new BooleanHexBitField("GenericParam", 42));
			field.Add(new BooleanHexBitField("MethodSpec", 43));
			field.Add(new BooleanHexBitField("GenericParamConstraint", 44));
			field.Add(new BooleanHexBitField("Reserved 2D", 45));
			field.Add(new BooleanHexBitField("Reserved 2E", 46));
			field.Add(new BooleanHexBitField("Reserved 2F", 47));
			field.Add(new BooleanHexBitField("Document", 48));
			field.Add(new BooleanHexBitField("MethodDebugInformation", 49));
			field.Add(new BooleanHexBitField("LocalScope", 50));
			field.Add(new BooleanHexBitField("LocalVariable", 51));
			field.Add(new BooleanHexBitField("LocalConstant", 52));
			field.Add(new BooleanHexBitField("ImportScope", 53));
			field.Add(new BooleanHexBitField("StateMachineMethod", 54));
			field.Add(new BooleanHexBitField("CustomDebugInformation", 55));
			field.Add(new BooleanHexBitField("Reserved 38", 56));
			field.Add(new BooleanHexBitField("Reserved 39", 57));
			field.Add(new BooleanHexBitField("Reserved 3A", 58));
			field.Add(new BooleanHexBitField("Reserved 3B", 59));
			field.Add(new BooleanHexBitField("Reserved 3C", 60));
			field.Add(new BooleanHexBitField("Reserved 3D", 61));
			field.Add(new BooleanHexBitField("Reserved 3E", 62));
			field.Add(new BooleanHexBitField("Reserved 3F", 63));
		}

		public MetaDataTableVM TryGetMetaDataTable(Table table) {
			if ((uint)table >= (uint)metaDataTables.Length)
				return null;
			return metaDataTables[(int)table];
		}
	}
}
