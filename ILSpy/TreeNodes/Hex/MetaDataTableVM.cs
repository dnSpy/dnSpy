/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnSpy.HexEditor;

namespace dnSpy.TreeNodes.Hex {
	abstract class MetaDataTableVM : HexVM {
		public Table Table {
			get { return table; }
		}
		readonly Table table;

		public uint Rows {
			get { return numRows; }
		}
		readonly uint numRows;

		public TableInfo TableInfo {
			get { return tableInfo; }
		}
		readonly TableInfo tableInfo;

		public override string Name {
			get { return string.Format("{0:X2} {1}", (byte)table, table); }
		}

		public string Column0Name {
			get { return GetColumnName(0); }
		}

		public string Column1Name {
			get { return GetColumnName(1); }
		}

		public string Column2Name {
			get { return GetColumnName(2); }
		}

		public string Column3Name {
			get { return GetColumnName(3); }
		}

		public string Column4Name {
			get { return GetColumnName(4); }
		}

		public string Column5Name {
			get { return GetColumnName(5); }
		}

		public string Column6Name {
			get { return GetColumnName(6); }
		}

		public string Column7Name {
			get { return GetColumnName(7); }
		}

		public string Column8Name {
			get { return GetColumnName(8); }
		}

		public string Column9Name {
			get { return GetColumnName(9); }
		}

		string GetColumnName(int col) {
			Debug.Assert(col < tableInfo.Columns.Count);
			if (col >= tableInfo.Columns.Count)
				return string.Empty;
			return tableInfo.Columns[col].Name;
		}

		public override IEnumerable<HexField> HexFields {
			get { return hexFields; }
		}
		readonly HexField[] hexFields = new HexField[0];

		public VirtualizedList<MetaDataTableRecordVM> Collection {
			get { return virtList; }
		}
		readonly VirtualizedList<MetaDataTableRecordVM> virtList;

		public object SelectedItem {
			get { return selectedItem; }
			set {
				if (selectedItem != value) {
					selectedItem = value;
					OnPropertyChanged("SelectedItem");
				}
			}
		}
		object selectedItem;

		readonly HexDocument doc;
		readonly ulong startOffset;
		readonly ulong endOffset;
		ulong stringsStartOffset;
		ulong stringsEndOffset;

		protected MetaDataTableVM(HexDocument doc, ulong startOffset, MDTable mdTable) {
			this.doc = doc;
			this.startOffset = startOffset;
			this.endOffset = startOffset + (mdTable.Rows == 0 ? 0 : (ulong)mdTable.Rows * mdTable.RowSize - 1);
			this.table = mdTable.Table;
			this.numRows = mdTable.Rows;
			this.tableInfo = CreateTableInfo(mdTable.TableInfo);
			this.virtList = new VirtualizedList<MetaDataTableRecordVM>((int)numRows, CreateItem);
		}

		MetaDataTableRecordVM CreateItem(int index) {
			Debug.Assert(index >= 0 && (uint)index < numRows);
			ulong recordOffset = startOffset + (ulong)index * (ulong)tableInfo.RowSize;
			return new MetaDataTableRecordVM(this, doc, recordOffset, new MDToken(table, index + 1), tableInfo);
		}

		public static MetaDataTableVM Create(HexDocument doc, ulong startOffset, MDTable mdTable) {
			switch (CreateTableInfo(mdTable.TableInfo).Columns.Count) {
			case 1:		return new MetaDataTable1VM(doc, startOffset, mdTable);
			case 2:		return new MetaDataTable2VM(doc, startOffset, mdTable);
			case 3:		return new MetaDataTable3VM(doc, startOffset, mdTable);
			case 4:		return new MetaDataTable4VM(doc, startOffset, mdTable);
			case 5:		return new MetaDataTable5VM(doc, startOffset, mdTable);
			case 6:		return new MetaDataTable6VM(doc, startOffset, mdTable);
			case 7:		return new MetaDataTable7VM(doc, startOffset, mdTable);
			case 8:		return new MetaDataTable8VM(doc, startOffset, mdTable);
			case 9:		return new MetaDataTable9VM(doc, startOffset, mdTable);
			default:	throw new InvalidOperationException();
			}
		}

		static TableInfo CreateTableInfo(TableInfo info) {
			var newCols = new List<ColumnInfo>(info.Columns.Count + 1);
			int offs = 0;
			for (int i = 0, coli = 0; i < info.Columns.Count; i++, coli++) {
				var col = info.Columns[i];
				newCols.Add(new ColumnInfo((byte)coli, col.Name, col.ColumnSize, (byte)offs, (byte)col.Size));
				offs += col.Size;

				int nextOffs = i + 1 >= info.Columns.Count ? info.RowSize : info.Columns[i + 1].Offset;
				int padding = nextOffs - (col.Offset + col.Size);
				for (int j = 0; j < padding; j++) {
					newCols.Add(new ColumnInfo((byte)(coli++ + 1), "pad", ColumnSize.Byte, (byte)offs, 1));
					offs++;
				}
			}

			return new TableInfo(info.Table, info.Name, newCols.ToArray(), offs);
		}

		public override void OnDocumentModified(ulong modifiedStart, ulong modifiedEnd) {
			if (!HexUtils.IsModified(startOffset, endOffset, modifiedStart, modifiedEnd))
				return;

			ulong start = Math.Max(startOffset, modifiedStart);
			ulong end = Math.Min(endOffset, modifiedEnd);
			int i = (int)((start - startOffset) / (ulong)tableInfo.RowSize);
			int endi = (int)((end - startOffset) / (ulong)tableInfo.RowSize);
			Debug.Assert(0 <= i && i <= endi && endi < virtList.Count);
			while (i <= endi) {
				var obj = virtList.TryGet(i);
				if (obj != null)
					obj.OnDocumentModified(modifiedStart, modifiedEnd);
				i++;
			}
		}

		public MetaDataTableRecordVM Get(int index) {
			return virtList[index];
		}

		public MetaDataTableRecordVM TryGet(int index) {
			return virtList.TryGet(index);
		}

		internal void InitializeHeapOffsets(ulong stringsStartOffset, ulong stringsEndOffset) {
			this.stringsStartOffset = stringsStartOffset;
			this.stringsEndOffset = stringsEndOffset;
		}

		public string ReadStringsHeap(uint offset) {
			if (offset == 0)
				return string.Empty;
			ulong offs = stringsStartOffset + offset;
			var bytes = new List<byte>();
			while (offs <= stringsEndOffset) {
				int b = doc.ReadByte(offs);
				if (b <= 0)
					break;
				bytes.Add((byte)b);
				offs++;
			}
			return Encoding.UTF8.GetString(bytes.ToArray());
		}
	}

	sealed class MetaDataTable1VM : MetaDataTableVM {
		public MetaDataTable1VM(HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(doc, startOffset, mdTable) {
		}
	}

	sealed class MetaDataTable2VM : MetaDataTableVM {
		public MetaDataTable2VM(HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(doc, startOffset, mdTable) {
		}
	}

	sealed class MetaDataTable3VM : MetaDataTableVM {
		public MetaDataTable3VM(HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(doc, startOffset, mdTable) {
		}
	}

	sealed class MetaDataTable4VM : MetaDataTableVM {
		public MetaDataTable4VM(HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(doc, startOffset, mdTable) {
		}
	}

	sealed class MetaDataTable5VM : MetaDataTableVM {
		public MetaDataTable5VM(HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(doc, startOffset, mdTable) {
		}
	}

	sealed class MetaDataTable6VM : MetaDataTableVM {
		public MetaDataTable6VM(HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(doc, startOffset, mdTable) {
		}
	}

	sealed class MetaDataTable7VM : MetaDataTableVM {
		public MetaDataTable7VM(HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(doc, startOffset, mdTable) {
		}
	}

	sealed class MetaDataTable8VM : MetaDataTableVM {
		public MetaDataTable8VM(HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(doc, startOffset, mdTable) {
		}
	}

	sealed class MetaDataTable9VM : MetaDataTableVM {
		public MetaDataTable9VM(HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(doc, startOffset, mdTable) {
		}
	}
}
