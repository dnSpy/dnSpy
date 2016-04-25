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
using System.Diagnostics;
using dnlib.DotNet.MD;

namespace dnSpy.AsmEditor.Hex {
	static class TableSorter {
		public static bool CanSort(TableInfo table) {
			switch (table.Table) {
			case Table.InterfaceImpl:
			case Table.Constant:
			case Table.CustomAttribute:
			case Table.FieldMarshal:
			case Table.DeclSecurity:
			case Table.ClassLayout:
			case Table.FieldLayout:
			case Table.EventMap:
			case Table.PropertyMap:
			case Table.MethodSemantics:
			case Table.MethodImpl:
			case Table.ImplMap:
			case Table.FieldRVA:
			case Table.NestedClass:
			case Table.GenericParam:
			case Table.GenericParamConstraint:
				return true;

			case Table.Module:
			case Table.TypeRef:
			case Table.TypeDef:
			case Table.FieldPtr:
			case Table.Field:
			case Table.MethodPtr:
			case Table.Method:
			case Table.ParamPtr:
			case Table.Param:
			case Table.MemberRef:
			case Table.StandAloneSig:
			case Table.EventPtr:
			case Table.Event:
			case Table.PropertyPtr:
			case Table.Property:
			case Table.ModuleRef:
			case Table.TypeSpec:
			case Table.ENCLog:
			case Table.ENCMap:
			case Table.Assembly:
			case Table.AssemblyProcessor:
			case Table.AssemblyOS:
			case Table.AssemblyRef:
			case Table.AssemblyRefProcessor:
			case Table.AssemblyRefOS:
			case Table.File:
			case Table.ExportedType:
			case Table.ManifestResource:
			case Table.MethodSpec:
			default:
				return false;
			}
		}

		struct Record {
			public readonly int OrigIndex;
			public readonly byte[] Data;

			public Record(int index, byte[] data) {
				this.OrigIndex = index;
				this.Data = data;
			}
		}

		public static void Sort(TableInfo table, byte[] data) {
			Debug.Assert(data.Length % table.RowSize == 0);

			var recs = new Record[data.Length / table.RowSize];
			for (int i = 0, offs = 0; i < recs.Length; i++, offs += table.RowSize) {
				var d = new byte[table.RowSize];
				recs[i] = new Record(i, d);
				Array.Copy(data, offs, d, 0, d.Length);
			}

			switch (table.Table) {
			case Table.CustomAttribute:
			case Table.FieldMarshal:
			case Table.EventMap:
			case Table.PropertyMap:
			case Table.MethodImpl:
			case Table.NestedClass:
			case Table.GenericParamConstraint:
				Array.Sort(recs, (a, b) => {
					uint ac = Read(table, a.Data, 0);
					uint bc = Read(table, b.Data, 0);
					if (ac != bc)
						return ac.CompareTo(bc);
					return a.OrigIndex.CompareTo(b.OrigIndex);
				});
				break;

			case Table.DeclSecurity:
			case Table.FieldLayout:
			case Table.ImplMap:
			case Table.FieldRVA:
				Array.Sort(recs, (a, b) => {
					uint ac = Read(table, a.Data, 1);
					uint bc = Read(table, b.Data, 1);
					if (ac != bc)
						return ac.CompareTo(bc);
					return a.OrigIndex.CompareTo(b.OrigIndex);
				});
				break;

			case Table.Constant:
			case Table.ClassLayout:
			case Table.MethodSemantics:
				Array.Sort(recs, (a, b) => {
					// Constant: Parent column is 2 and not 1 because a 'pad' column has been inserted
					uint ac = Read(table, a.Data, 2);
					uint bc = Read(table, b.Data, 2);
					if (ac != bc)
						return ac.CompareTo(bc);
					return a.OrigIndex.CompareTo(b.OrigIndex);
				});
				break;

			case Table.InterfaceImpl:
				Array.Sort(recs, (a, b) => {
					uint ac = Read(table, a.Data, 0);
					uint bc = Read(table, b.Data, 0);
					if (ac != bc)
						return ac.CompareTo(bc);
					ac = Read(table, a.Data, 1);
					bc = Read(table, b.Data, 1);
					if (ac != bc)
						return ac.CompareTo(bc);
					return a.OrigIndex.CompareTo(b.OrigIndex);
				});
				break;

			case Table.GenericParam:
				Array.Sort(recs, (a, b) => {
					uint ac = Read(table, a.Data, 2);
					uint bc = Read(table, b.Data, 2);
					if (ac != bc)
						return ac.CompareTo(bc);
					ac = Read(table, a.Data, 0);
					bc = Read(table, b.Data, 0);
					if (ac != bc)
						return ac.CompareTo(bc);
					return a.OrigIndex.CompareTo(b.OrigIndex);
				});
				break;

			default:
				Debug.Fail("Can't sort it. Call CanSort() first");
				return;
			}

			for (int i = 0, offs = 0; i < recs.Length; i++, offs += table.RowSize) {
				var d = recs[i].Data;
				Array.Copy(d, 0, data, offs, d.Length);
			}
		}

		static uint Read(TableInfo table, byte[] rec, int colIndex) {
			var col = table.Columns[colIndex];
			if (col.Size == 2)
				return BitConverter.ToUInt16(rec, col.Offset);
			if (col.Size == 4)
				return BitConverter.ToUInt32(rec, col.Offset);
			throw new InvalidOperationException();
		}
	}
}
