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
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnSpy.HexEditor;

namespace dnSpy.TreeNodes.Hex {
	sealed class MetaDataTableRecordVM : HexVM, IVirtualizedListItem {
		public int Index {
			get { return (int)mdToken.Rid - 1; }
		}

		public override string Name {
			get { return name; }
		}

		public string OffsetString {
			get { return string.Format("0x{0:X8}", startOffset); }
		}

		readonly string name;
		readonly HexDocument doc;
		readonly ulong startOffset;
		readonly ulong endOffset;
		MDToken mdToken;
		readonly HexField[] hexFields;

		public override IEnumerable<HexField> HexFields {
			get { return hexFields; }
		}

		public HexField Column0 {
			get { return GetField(0); }
		}

		public HexField Column1 {
			get { return GetField(1); }
		}

		public HexField Column2 {
			get { return GetField(2); }
		}

		public HexField Column3 {
			get { return GetField(3); }
		}

		public HexField Column4 {
			get { return GetField(4); }
		}

		public HexField Column5 {
			get { return GetField(5); }
		}

		public HexField Column6 {
			get { return GetField(6); }
		}

		public HexField Column7 {
			get { return GetField(7); }
		}

		public HexField Column8 {
			get { return GetField(8); }
		}

		HexField GetField(int index) {
			Debug.Assert(index < hexFields.Length);
			return hexFields[index];
		}

		public string RidString {
			get { return mdToken.Rid.ToString(); }
		}

		public string TokenString {
			get { return string.Format("0x{0:X8}", mdToken.Raw); }
		}

		public MetaDataTableRecordVM(HexDocument doc, ulong startOffset, MDToken mdToken, TableInfo tableInfo) {
			this.name = string.Format("{0}[{1:X6}]", mdToken.Table, mdToken.Rid);
			this.doc = doc;
			this.startOffset = startOffset;
			this.endOffset = startOffset + (uint)tableInfo.RowSize - 1;
			this.mdToken = mdToken;
			this.hexFields = new HexField[tableInfo.Columns.Count];
			for (int i = 0; i < this.hexFields.Length; i++)
				this.hexFields[i] = CreateField(tableInfo.Columns[i]);
		}

		HexField CreateField(ColumnInfo colInfo) {
			switch (colInfo.ColumnSize) {
			case ColumnSize.Int16: return new Int16HexField(doc, Name, colInfo.Name, startOffset + (uint)colInfo.Offset);
			case ColumnSize.Int32: return new Int32HexField(doc, Name, colInfo.Name, startOffset + (uint)colInfo.Offset);
			}

			switch (colInfo.Size) {
			case 1: return new ByteHexField(doc, Name, colInfo.Name, startOffset + (uint)colInfo.Offset);
			case 2: return new UInt16HexField(doc, Name, colInfo.Name, startOffset + (uint)colInfo.Offset);
			case 4: return new UInt32HexField(doc, Name, colInfo.Name, startOffset + (uint)colInfo.Offset);
			default: throw new InvalidOperationException();
			}
		}
	}
}
