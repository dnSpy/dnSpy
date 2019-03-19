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
				WriteRow(ref sortedRows[rowIndex], RawRowColumnReader.ReadCustomAttributeColumn, newColumns, destination, destinationIndex);
		}

		protected void WriteRow<TRow>(ref TRow row, RawRowColumnReader.ReadColumnDelegate<TRow> readColumn, IList<ColumnInfo> newColumns, byte[] destination, int destinationIndex) where TRow : struct {
			var oldColumns = info.MDTable.Columns;
			for (int i = 0; i < newColumns.Count; i++) {
				uint value = readColumn(ref row, i);
				switch (newColumns[i].Size) {
				case 1:
					Debug.Assert(newColumns[i].Size == oldColumns[i].Size);
					Debug.Assert(value <= byte.MaxValue);
					destination[destinationIndex++] = (byte)value;
					break;

				case 2:
					// The old and new sizes should match, unless the metadata writer used eg. BigStrings
					// when it wasn't needed.
					//Debug.Assert(newColumns[i].Size == oldColumns[i].Size);
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
