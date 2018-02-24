/*
    Copyright (C) 2014-2018 de4dot@gmail.com

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
using dnlib.DotNet.MD;

namespace dnSpy.AsmEditor.Compiler.MDEditor {
	abstract class TableWriter {
		public uint Rows => info.Rows;
		public virtual uint FirstModifiedRowId => 1;
		protected readonly TablesMDHeap.TableInfo info;

		protected TableWriter(TablesMDHeap.TableInfo info) => this.info = info;

		sealed class NormalTableWriter : TableWriter {
			public override uint FirstModifiedRowId => info.FirstModifiedRowId;
			public NormalTableWriter(TablesMDHeap.TableInfo info) : base(info) {
			}

			public override void WriteRow(uint rowIndex, IList<ColumnInfo> newColumns, byte[] destination, int destinationIndex) =>
				info.WriteRow(rowIndex, newColumns, destination, destinationIndex);
		}

		sealed class CustomAttributeTableWriter : TableWriter {
			readonly RawCustomAttributeRow[] sortedRows;

			public CustomAttributeTableWriter(TablesMDHeap.TableInfo<RawCustomAttributeRow> info) : base(info) {
				this.sortedRows = new RawCustomAttributeRow[info.Rows];
				var sortedRows = this.sortedRows;
				for (int i = 0; i < sortedRows.Length; i++)
					sortedRows[i] = info.Get((uint)i + 1);
				Array.Sort(sortedRows, (a, b) => a.Parent.CompareTo(b.Parent));
			}

			public override void WriteRow(uint rowIndex, IList<ColumnInfo> newColumns, byte[] destination, int destinationIndex) =>
				WriteRow(sortedRows[rowIndex], newColumns, destination, destinationIndex);
		}

		protected void WriteRow<TRow>(TRow row, IList<ColumnInfo> newColumns, byte[] destination, int destinationIndex) where TRow : IRawRow {
			var oldColumns = info.MDTable.Columns;
			for (int i = 0; i < newColumns.Count; i++) {
				uint value = row.Read(i);
				switch (newColumns[i].Size) {
				case 1:
					Debug.Assert(i + 1 < newColumns.Count && newColumns[i + 1].Offset == newColumns[i].Offset + 2);
					Debug.Assert(value <= byte.MaxValue);
					destination[destinationIndex++] = (byte)value;
					destination[destinationIndex++] = 0;
					break;

				case 2:
					// The tables can only grow in size so it's not possible for a column to change
					// from 4 bytes in size to 2 bytes in size. Since this column is 2 bytes in size,
					// the old column is also 2 bytes in size.
					Debug.Assert(newColumns[i].Size == oldColumns[i].Size);
					Debug.Assert(value <= ushort.MaxValue);
					destination[destinationIndex++] = (byte)value;
					destination[destinationIndex++] = (byte)(value >> 8);
					break;

				case 4:
					destination[destinationIndex++] = (byte)value;
					destination[destinationIndex++] = (byte)(value >> 8);
					destination[destinationIndex++] = (byte)(value >> 16);
					destination[destinationIndex++] = (byte)(value >> 24);
					break;

				default:
					throw new InvalidOperationException();
				}
			}
		}

		public static TableWriter Create(TablesMDHeap.TableInfo info) {
			if (info.HasNewRows) {
				switch (info.MDTable.Table) {
				case Table.CustomAttribute:
					return new CustomAttributeTableWriter((TablesMDHeap.TableInfo<RawCustomAttributeRow>)info);

				case Table.InterfaceImpl:
				case Table.Constant:
				case Table.FieldMarshal:
				case Table.DeclSecurity:
				case Table.ClassLayout:
				case Table.FieldLayout:
				case Table.MethodSemantics:
				case Table.MethodImpl:
				case Table.ImplMap:
				case Table.FieldRVA:
				case Table.NestedClass:
				case Table.GenericParam:
				case Table.GenericParamConstraint:
				case Table.LocalScope:
				case Table.StateMachineMethod:
				case Table.CustomDebugInformation:
					throw new NotImplementedException();

				default:
					break;
				}
			}
			return new NormalTableWriter(info);
		}

		public abstract void WriteRow(uint rowIndex, IList<ColumnInfo> newColumns, byte[] destination, int destinationIndex);
	}
}
