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
			get { return TableInfo.Table; }
		}

		public uint Rows {
			get { return numRows; }
		}
		readonly uint numRows;

		public TableInfo TableInfo {
			get { return tableInfo; }
		}
		readonly TableInfo tableInfo;

		public override string Name {
			get { return string.Format("{0:X2} {1}", (byte)TableInfo.Table, TableInfo.Table); }
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
			this.numRows = mdTable.Rows;
			this.tableInfo = CreateTableInfo(mdTable.TableInfo);
			this.virtList = new VirtualizedList<MetaDataTableRecordVM>((int)numRows, CreateItem);
		}

		MetaDataTableRecordVM CreateItem(int index) {
			Debug.Assert(index >= 0 && (uint)index < numRows);
			ulong recordOffset = startOffset + (ulong)index * (ulong)tableInfo.RowSize;
			switch (tableInfo.Table) {
			case Table.Module:					return new ModuleMetaDataTableRecordVM(this, doc, recordOffset, new MDToken(TableInfo.Table, index + 1), tableInfo);
			case Table.TypeRef:					return new TypeRefMetaDataTableRecordVM(this, doc, recordOffset, new MDToken(TableInfo.Table, index + 1), tableInfo);
			case Table.TypeDef:					return new TypeDefMetaDataTableRecordVM(this, doc, recordOffset, new MDToken(TableInfo.Table, index + 1), tableInfo);
			case Table.FieldPtr:				return new FieldPtrMetaDataTableRecordVM(this, doc, recordOffset, new MDToken(TableInfo.Table, index + 1), tableInfo);
			case Table.Field:					return new FieldMetaDataTableRecordVM(this, doc, recordOffset, new MDToken(TableInfo.Table, index + 1), tableInfo);
			case Table.MethodPtr:				return new MethodPtrMetaDataTableRecordVM(this, doc, recordOffset, new MDToken(TableInfo.Table, index + 1), tableInfo);
			case Table.Method:					return new MethodMetaDataTableRecordVM(this, doc, recordOffset, new MDToken(TableInfo.Table, index + 1), tableInfo);
			case Table.ParamPtr:				return new ParamPtrMetaDataTableRecordVM(this, doc, recordOffset, new MDToken(TableInfo.Table, index + 1), tableInfo);
			case Table.Param:					return new ParamMetaDataTableRecordVM(this, doc, recordOffset, new MDToken(TableInfo.Table, index + 1), tableInfo);
			case Table.InterfaceImpl:			return new InterfaceImplMetaDataTableRecordVM(this, doc, recordOffset, new MDToken(TableInfo.Table, index + 1), tableInfo);
			case Table.MemberRef:				return new MemberRefMetaDataTableRecordVM(this, doc, recordOffset, new MDToken(TableInfo.Table, index + 1), tableInfo);
			case Table.Constant:				return new ConstantMetaDataTableRecordVM(this, doc, recordOffset, new MDToken(TableInfo.Table, index + 1), tableInfo);
			case Table.CustomAttribute:			return new CustomAttributeMetaDataTableRecordVM(this, doc, recordOffset, new MDToken(TableInfo.Table, index + 1), tableInfo);
			case Table.FieldMarshal:			return new FieldMarshalMetaDataTableRecordVM(this, doc, recordOffset, new MDToken(TableInfo.Table, index + 1), tableInfo);
			case Table.DeclSecurity:			return new DeclSecurityMetaDataTableRecordVM(this, doc, recordOffset, new MDToken(TableInfo.Table, index + 1), tableInfo);
			case Table.ClassLayout:				return new ClassLayoutMetaDataTableRecordVM(this, doc, recordOffset, new MDToken(TableInfo.Table, index + 1), tableInfo);
			case Table.FieldLayout:				return new FieldLayoutMetaDataTableRecordVM(this, doc, recordOffset, new MDToken(TableInfo.Table, index + 1), tableInfo);
			case Table.StandAloneSig:			return new StandAloneSigMetaDataTableRecordVM(this, doc, recordOffset, new MDToken(TableInfo.Table, index + 1), tableInfo);
			case Table.EventMap:				return new EventMapMetaDataTableRecordVM(this, doc, recordOffset, new MDToken(TableInfo.Table, index + 1), tableInfo);
			case Table.EventPtr:				return new EventPtrMetaDataTableRecordVM(this, doc, recordOffset, new MDToken(TableInfo.Table, index + 1), tableInfo);
			case Table.Event:					return new EventMetaDataTableRecordVM(this, doc, recordOffset, new MDToken(TableInfo.Table, index + 1), tableInfo);
			case Table.PropertyMap:				return new PropertyMapMetaDataTableRecordVM(this, doc, recordOffset, new MDToken(TableInfo.Table, index + 1), tableInfo);
			case Table.PropertyPtr:				return new PropertyPtrMetaDataTableRecordVM(this, doc, recordOffset, new MDToken(TableInfo.Table, index + 1), tableInfo);
			case Table.Property:				return new PropertyMetaDataTableRecordVM(this, doc, recordOffset, new MDToken(TableInfo.Table, index + 1), tableInfo);
			case Table.MethodSemantics:			return new MethodSemanticsMetaDataTableRecordVM(this, doc, recordOffset, new MDToken(TableInfo.Table, index + 1), tableInfo);
			case Table.MethodImpl:				return new MethodImplMetaDataTableRecordVM(this, doc, recordOffset, new MDToken(TableInfo.Table, index + 1), tableInfo);
			case Table.ModuleRef:				return new ModuleRefMetaDataTableRecordVM(this, doc, recordOffset, new MDToken(TableInfo.Table, index + 1), tableInfo);
			case Table.TypeSpec:				return new TypeSpecMetaDataTableRecordVM(this, doc, recordOffset, new MDToken(TableInfo.Table, index + 1), tableInfo);
			case Table.ImplMap:					return new ImplMapMetaDataTableRecordVM(this, doc, recordOffset, new MDToken(TableInfo.Table, index + 1), tableInfo);
			case Table.FieldRVA:				return new FieldRVAMetaDataTableRecordVM(this, doc, recordOffset, new MDToken(TableInfo.Table, index + 1), tableInfo);
			case Table.ENCLog:					return new ENCLogMetaDataTableRecordVM(this, doc, recordOffset, new MDToken(TableInfo.Table, index + 1), tableInfo);
			case Table.ENCMap:					return new ENCMapMetaDataTableRecordVM(this, doc, recordOffset, new MDToken(TableInfo.Table, index + 1), tableInfo);
			case Table.Assembly:				return new AssemblyMetaDataTableRecordVM(this, doc, recordOffset, new MDToken(TableInfo.Table, index + 1), tableInfo);
			case Table.AssemblyProcessor:		return new AssemblyProcessorMetaDataTableRecordVM(this, doc, recordOffset, new MDToken(TableInfo.Table, index + 1), tableInfo);
			case Table.AssemblyOS:				return new AssemblyOSMetaDataTableRecordVM(this, doc, recordOffset, new MDToken(TableInfo.Table, index + 1), tableInfo);
			case Table.AssemblyRef:				return new AssemblyRefMetaDataTableRecordVM(this, doc, recordOffset, new MDToken(TableInfo.Table, index + 1), tableInfo);
			case Table.AssemblyRefProcessor:	return new AssemblyRefProcessorMetaDataTableRecordVM(this, doc, recordOffset, new MDToken(TableInfo.Table, index + 1), tableInfo);
			case Table.AssemblyRefOS:			return new AssemblyRefOSMetaDataTableRecordVM(this, doc, recordOffset, new MDToken(TableInfo.Table, index + 1), tableInfo);
			case Table.File:					return new FileMetaDataTableRecordVM(this, doc, recordOffset, new MDToken(TableInfo.Table, index + 1), tableInfo);
			case Table.ExportedType:			return new ExportedTypeMetaDataTableRecordVM(this, doc, recordOffset, new MDToken(TableInfo.Table, index + 1), tableInfo);
			case Table.ManifestResource:		return new ManifestResourceMetaDataTableRecordVM(this, doc, recordOffset, new MDToken(TableInfo.Table, index + 1), tableInfo);
			case Table.NestedClass:				return new NestedClassMetaDataTableRecordVM(this, doc, recordOffset, new MDToken(TableInfo.Table, index + 1), tableInfo);
			case Table.GenericParam:			return TableInfo.Columns.Count == 5 ? (MetaDataTableRecordVM)new GenericParamMetaDataTableRecordV11VM(this, doc, recordOffset, new MDToken(TableInfo.Table, index + 1), tableInfo) : new GenericParamMetaDataTableRecordVM(this, doc, recordOffset, new MDToken(TableInfo.Table, index + 1), tableInfo);
			case Table.MethodSpec:				return new MethodSpecMetaDataTableRecordVM(this, doc, recordOffset, new MDToken(TableInfo.Table, index + 1), tableInfo);
			case Table.GenericParamConstraint:	return new GenericParamConstraintMetaDataTableRecordVM(this, doc, recordOffset, new MDToken(TableInfo.Table, index + 1), tableInfo);
			default:							throw new InvalidOperationException();
			}
		}

		public static MetaDataTableVM Create(HexDocument doc, ulong startOffset, MDTable mdTable) {
			switch (mdTable.Table) {
			case Table.Module:					return new ModuleMetaDataTableVM(doc, startOffset, mdTable);
			case Table.TypeRef:					return new TypeRefMetaDataTableVM(doc, startOffset, mdTable);
			case Table.TypeDef:					return new TypeDefMetaDataTableVM(doc, startOffset, mdTable);
			case Table.FieldPtr:				return new FieldPtrMetaDataTableVM(doc, startOffset, mdTable);
			case Table.Field:					return new FieldMetaDataTableVM(doc, startOffset, mdTable);
			case Table.MethodPtr:				return new MethodPtrMetaDataTableVM(doc, startOffset, mdTable);
			case Table.Method:					return new MethodMetaDataTableVM(doc, startOffset, mdTable);
			case Table.ParamPtr:				return new ParamPtrMetaDataTableVM(doc, startOffset, mdTable);
			case Table.Param:					return new ParamMetaDataTableVM(doc, startOffset, mdTable);
			case Table.InterfaceImpl:			return new InterfaceImplMetaDataTableVM(doc, startOffset, mdTable);
			case Table.MemberRef:				return new MemberRefMetaDataTableVM(doc, startOffset, mdTable);
			case Table.Constant:				return new ConstantMetaDataTableVM(doc, startOffset, mdTable);
			case Table.CustomAttribute:			return new CustomAttributeMetaDataTableVM(doc, startOffset, mdTable);
			case Table.FieldMarshal:			return new FieldMarshalMetaDataTableVM(doc, startOffset, mdTable);
			case Table.DeclSecurity:			return new DeclSecurityMetaDataTableVM(doc, startOffset, mdTable);
			case Table.ClassLayout:				return new ClassLayoutMetaDataTableVM(doc, startOffset, mdTable);
			case Table.FieldLayout:				return new FieldLayoutMetaDataTableVM(doc, startOffset, mdTable);
			case Table.StandAloneSig:			return new StandAloneSigMetaDataTableVM(doc, startOffset, mdTable);
			case Table.EventMap:				return new EventMapMetaDataTableVM(doc, startOffset, mdTable);
			case Table.EventPtr:				return new EventPtrMetaDataTableVM(doc, startOffset, mdTable);
			case Table.Event:					return new EventMetaDataTableVM(doc, startOffset, mdTable);
			case Table.PropertyMap:				return new PropertyMapMetaDataTableVM(doc, startOffset, mdTable);
			case Table.PropertyPtr:				return new PropertyPtrMetaDataTableVM(doc, startOffset, mdTable);
			case Table.Property:				return new PropertyMetaDataTableVM(doc, startOffset, mdTable);
			case Table.MethodSemantics:			return new MethodSemanticsMetaDataTableVM(doc, startOffset, mdTable);
			case Table.MethodImpl:				return new MethodImplMetaDataTableVM(doc, startOffset, mdTable);
			case Table.ModuleRef:				return new ModuleRefMetaDataTableVM(doc, startOffset, mdTable);
			case Table.TypeSpec:				return new TypeSpecMetaDataTableVM(doc, startOffset, mdTable);
			case Table.ImplMap:					return new ImplMapMetaDataTableVM(doc, startOffset, mdTable);
			case Table.FieldRVA:				return new FieldRVAMetaDataTableVM(doc, startOffset, mdTable);
			case Table.ENCLog:					return new ENCLogMetaDataTableVM(doc, startOffset, mdTable);
			case Table.ENCMap:					return new ENCMapMetaDataTableVM(doc, startOffset, mdTable);
			case Table.Assembly:				return new AssemblyMetaDataTableVM(doc, startOffset, mdTable);
			case Table.AssemblyProcessor:		return new AssemblyProcessorMetaDataTableVM(doc, startOffset, mdTable);
			case Table.AssemblyOS:				return new AssemblyOSMetaDataTableVM(doc, startOffset, mdTable);
			case Table.AssemblyRef:				return new AssemblyRefMetaDataTableVM(doc, startOffset, mdTable);
			case Table.AssemblyRefProcessor:	return new AssemblyRefProcessorMetaDataTableVM(doc, startOffset, mdTable);
			case Table.AssemblyRefOS:			return new AssemblyRefOSMetaDataTableVM(doc, startOffset, mdTable);
			case Table.File:					return new FileMetaDataTableVM(doc, startOffset, mdTable);
			case Table.ExportedType:			return new ExportedTypeMetaDataTableVM(doc, startOffset, mdTable);
			case Table.ManifestResource:		return new ManifestResourceMetaDataTableVM(doc, startOffset, mdTable);
			case Table.NestedClass:				return new NestedClassMetaDataTableVM(doc, startOffset, mdTable);
			case Table.GenericParam:			return mdTable.Columns.Count == 5 ? (MetaDataTableVM)new GenericParamMetaDataTableV11VM(doc, startOffset, mdTable) : new GenericParamMetaDataTableVM(doc, startOffset, mdTable);
			case Table.MethodSpec:				return new MethodSpecMetaDataTableVM(doc, startOffset, mdTable);
			case Table.GenericParamConstraint:	return new GenericParamConstraintMetaDataTableVM(doc, startOffset, mdTable);
			default:							throw new InvalidOperationException();
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

		public string ReadStringsHeap(uint offset, uint maxLen = 0x200) {
			if (offset == 0)
				return string.Empty;
			ulong offs = stringsStartOffset + offset;
			var bytes = new List<byte>();
			bool tooLongString = false;
			while (offs <= stringsEndOffset) {
				int b = doc.ReadByte(offs);
				if (b <= 0)
					break;
				if (bytes.Count >= maxLen) {
					tooLongString = true;
					break;
				}
				bytes.Add((byte)b);
				offs++;
			}
			var s = Encoding.UTF8.GetString(bytes.ToArray());
			return tooLongString ? s + "…" : s;
		}
	}

	abstract class MetaDataTable1VM : MetaDataTableVM {
		public MetaDataTable1VM(HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(doc, startOffset, mdTable) {
		}
	}

	abstract class MetaDataTable2VM : MetaDataTableVM {
		public MetaDataTable2VM(HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(doc, startOffset, mdTable) {
		}
	}

	abstract class MetaDataTable3VM : MetaDataTableVM {
		public MetaDataTable3VM(HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(doc, startOffset, mdTable) {
		}
	}

	abstract class MetaDataTable4VM : MetaDataTableVM {
		public MetaDataTable4VM(HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(doc, startOffset, mdTable) {
		}
	}

	abstract class MetaDataTable5VM : MetaDataTableVM {
		public MetaDataTable5VM(HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(doc, startOffset, mdTable) {
		}
	}

	abstract class MetaDataTable6VM : MetaDataTableVM {
		public MetaDataTable6VM(HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(doc, startOffset, mdTable) {
		}
	}

	abstract class MetaDataTable9VM : MetaDataTableVM {
		public MetaDataTable9VM(HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(doc, startOffset, mdTable) {
		}
	}

	sealed class ModuleMetaDataTableVM : MetaDataTable5VM {
		public ModuleMetaDataTableVM(HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(doc, startOffset, mdTable) {
		}
	}

	sealed class TypeRefMetaDataTableVM : MetaDataTable3VM {
		public TypeRefMetaDataTableVM(HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(doc, startOffset, mdTable) {
		}
	}

	sealed class TypeDefMetaDataTableVM : MetaDataTable6VM {
		public TypeDefMetaDataTableVM(HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(doc, startOffset, mdTable) {
		}
	}

	sealed class FieldPtrMetaDataTableVM : MetaDataTable1VM {
		public FieldPtrMetaDataTableVM(HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(doc, startOffset, mdTable) {
		}
	}

	sealed class FieldMetaDataTableVM : MetaDataTable3VM {
		public FieldMetaDataTableVM(HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(doc, startOffset, mdTable) {
		}
	}

	sealed class MethodPtrMetaDataTableVM : MetaDataTable1VM {
		public MethodPtrMetaDataTableVM(HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(doc, startOffset, mdTable) {
		}
	}

	sealed class MethodMetaDataTableVM : MetaDataTable6VM {
		public MethodMetaDataTableVM(HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(doc, startOffset, mdTable) {
		}
	}

	sealed class ParamPtrMetaDataTableVM : MetaDataTable1VM {
		public ParamPtrMetaDataTableVM(HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(doc, startOffset, mdTable) {
		}
	}

	sealed class ParamMetaDataTableVM : MetaDataTable3VM {
		public ParamMetaDataTableVM(HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(doc, startOffset, mdTable) {
		}
	}

	sealed class InterfaceImplMetaDataTableVM : MetaDataTable2VM {
		public InterfaceImplMetaDataTableVM(HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(doc, startOffset, mdTable) {
		}
	}

	sealed class MemberRefMetaDataTableVM : MetaDataTable3VM {
		public MemberRefMetaDataTableVM(HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(doc, startOffset, mdTable) {
		}
	}

	sealed class ConstantMetaDataTableVM : MetaDataTable4VM {
		public ConstantMetaDataTableVM(HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(doc, startOffset, mdTable) {
		}
	}

	sealed class CustomAttributeMetaDataTableVM : MetaDataTable3VM {
		public CustomAttributeMetaDataTableVM(HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(doc, startOffset, mdTable) {
		}
	}

	sealed class FieldMarshalMetaDataTableVM : MetaDataTable2VM {
		public FieldMarshalMetaDataTableVM(HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(doc, startOffset, mdTable) {
		}
	}

	sealed class DeclSecurityMetaDataTableVM : MetaDataTable3VM {
		public DeclSecurityMetaDataTableVM(HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(doc, startOffset, mdTable) {
		}
	}

	sealed class ClassLayoutMetaDataTableVM : MetaDataTable3VM {
		public ClassLayoutMetaDataTableVM(HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(doc, startOffset, mdTable) {
		}
	}

	sealed class FieldLayoutMetaDataTableVM : MetaDataTable2VM {
		public FieldLayoutMetaDataTableVM(HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(doc, startOffset, mdTable) {
		}
	}

	sealed class StandAloneSigMetaDataTableVM : MetaDataTable1VM {
		public StandAloneSigMetaDataTableVM(HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(doc, startOffset, mdTable) {
		}
	}

	sealed class EventMapMetaDataTableVM : MetaDataTable2VM {
		public EventMapMetaDataTableVM(HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(doc, startOffset, mdTable) {
		}
	}

	sealed class EventPtrMetaDataTableVM : MetaDataTable1VM {
		public EventPtrMetaDataTableVM(HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(doc, startOffset, mdTable) {
		}
	}

	sealed class EventMetaDataTableVM : MetaDataTable3VM {
		public EventMetaDataTableVM(HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(doc, startOffset, mdTable) {
		}
	}

	sealed class PropertyMapMetaDataTableVM : MetaDataTable2VM {
		public PropertyMapMetaDataTableVM(HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(doc, startOffset, mdTable) {
		}
	}

	sealed class PropertyPtrMetaDataTableVM : MetaDataTable1VM {
		public PropertyPtrMetaDataTableVM(HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(doc, startOffset, mdTable) {
		}
	}

	sealed class PropertyMetaDataTableVM : MetaDataTable3VM {
		public PropertyMetaDataTableVM(HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(doc, startOffset, mdTable) {
		}
	}

	sealed class MethodSemanticsMetaDataTableVM : MetaDataTable3VM {
		public MethodSemanticsMetaDataTableVM(HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(doc, startOffset, mdTable) {
		}
	}

	sealed class MethodImplMetaDataTableVM : MetaDataTable3VM {
		public MethodImplMetaDataTableVM(HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(doc, startOffset, mdTable) {
		}
	}

	sealed class ModuleRefMetaDataTableVM : MetaDataTable1VM {
		public ModuleRefMetaDataTableVM(HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(doc, startOffset, mdTable) {
		}
	}

	sealed class TypeSpecMetaDataTableVM : MetaDataTable1VM {
		public TypeSpecMetaDataTableVM(HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(doc, startOffset, mdTable) {
		}
	}

	sealed class ImplMapMetaDataTableVM : MetaDataTable4VM {
		public ImplMapMetaDataTableVM(HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(doc, startOffset, mdTable) {
		}
	}

	sealed class FieldRVAMetaDataTableVM : MetaDataTable2VM {
		public FieldRVAMetaDataTableVM(HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(doc, startOffset, mdTable) {
		}
	}

	sealed class ENCLogMetaDataTableVM : MetaDataTable2VM {
		public ENCLogMetaDataTableVM(HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(doc, startOffset, mdTable) {
		}
	}

	sealed class ENCMapMetaDataTableVM : MetaDataTable1VM {
		public ENCMapMetaDataTableVM(HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(doc, startOffset, mdTable) {
		}
	}

	sealed class AssemblyMetaDataTableVM : MetaDataTable9VM {
		public AssemblyMetaDataTableVM(HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(doc, startOffset, mdTable) {
		}
	}

	sealed class AssemblyProcessorMetaDataTableVM : MetaDataTable1VM {
		public AssemblyProcessorMetaDataTableVM(HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(doc, startOffset, mdTable) {
		}
	}

	sealed class AssemblyOSMetaDataTableVM : MetaDataTable3VM {
		public AssemblyOSMetaDataTableVM(HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(doc, startOffset, mdTable) {
		}
	}

	sealed class AssemblyRefMetaDataTableVM : MetaDataTable9VM {
		public AssemblyRefMetaDataTableVM(HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(doc, startOffset, mdTable) {
		}
	}

	sealed class AssemblyRefProcessorMetaDataTableVM : MetaDataTable2VM {
		public AssemblyRefProcessorMetaDataTableVM(HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(doc, startOffset, mdTable) {
		}
	}

	sealed class AssemblyRefOSMetaDataTableVM : MetaDataTable4VM {
		public AssemblyRefOSMetaDataTableVM(HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(doc, startOffset, mdTable) {
		}
	}

	sealed class FileMetaDataTableVM : MetaDataTable3VM {
		public FileMetaDataTableVM(HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(doc, startOffset, mdTable) {
		}
	}

	sealed class ExportedTypeMetaDataTableVM : MetaDataTable5VM {
		public ExportedTypeMetaDataTableVM(HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(doc, startOffset, mdTable) {
		}
	}

	sealed class ManifestResourceMetaDataTableVM : MetaDataTable4VM {
		public ManifestResourceMetaDataTableVM(HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(doc, startOffset, mdTable) {
		}
	}

	sealed class NestedClassMetaDataTableVM : MetaDataTable2VM {
		public NestedClassMetaDataTableVM(HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(doc, startOffset, mdTable) {
		}
	}

	sealed class GenericParamMetaDataTableV11VM : MetaDataTable5VM {
		public GenericParamMetaDataTableV11VM(HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(doc, startOffset, mdTable) {
		}
	}

	sealed class GenericParamMetaDataTableVM : MetaDataTable4VM {
		public GenericParamMetaDataTableVM(HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(doc, startOffset, mdTable) {
		}
	}

	sealed class MethodSpecMetaDataTableVM : MetaDataTable2VM {
		public MethodSpecMetaDataTableVM(HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(doc, startOffset, mdTable) {
		}
	}

	sealed class GenericParamConstraintMetaDataTableVM : MetaDataTable2VM {
		public GenericParamConstraintMetaDataTableVM(HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(doc, startOffset, mdTable) {
		}
	}
}
