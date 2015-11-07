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
using dnSpy.Shared.UI.HexEditor;

namespace dnSpy.TreeNodes.Hex {
	public abstract class MetaDataTableVM : HexVM {
		public Func<Table, MetaDataTableVM> FindMetaDataTable {
			get { return findMetaDataTable; }
			set { findMetaDataTable = value; }
		}
		Func<Table, MetaDataTableVM> findMetaDataTable;

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

		public string GetColumnName(int col) {
			Debug.Assert(col < tableInfo.Columns.Count);
			if (col >= tableInfo.Columns.Count)
				return string.Empty;
			return tableInfo.Columns[col].Name;
		}

		public string InfoName {
			get { return "Info"; }
		}

		public virtual bool HasInfo {
			get { return false; }
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

		public HexDocument Document {
			get { return doc; }
		}

		public ulong StartOffset {
			get { return startOffset; }
		}

		public ulong EndOffset {
			get { return endOffset; }
		}

		readonly HexDocument doc;
		readonly ulong startOffset;
		readonly ulong endOffset;
		ulong stringsStartOffset;
		ulong stringsEndOffset;

		protected MetaDataTableVM(object owner, HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(owner) {
			this.doc = doc;
			this.startOffset = startOffset;
			this.endOffset = startOffset + (mdTable.Rows == 0 ? 0 : (ulong)mdTable.Rows * mdTable.RowSize - 1);
			this.numRows = mdTable.Rows;
			this.tableInfo = CreateTableInfo(mdTable.TableInfo);
			this.virtList = new VirtualizedList<MetaDataTableRecordVM>((int)numRows, CreateItem);
		}

		MetaDataTableRecordVM CreateItem(int index) {
			Debug.Assert(index >= 0 && (uint)index < numRows);
			switch (tableInfo.Table) {
			case Table.Module:					return new ModuleMetaDataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.TypeRef:					return new TypeRefMetaDataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.TypeDef:					return new TypeDefMetaDataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.FieldPtr:				return new FieldPtrMetaDataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.Field:					return new FieldMetaDataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.MethodPtr:				return new MethodPtrMetaDataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.Method:					return new MethodMetaDataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.ParamPtr:				return new ParamPtrMetaDataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.Param:					return new ParamMetaDataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.InterfaceImpl:			return new InterfaceImplMetaDataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.MemberRef:				return new MemberRefMetaDataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.Constant:				return new ConstantMetaDataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.CustomAttribute:			return new CustomAttributeMetaDataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.FieldMarshal:			return new FieldMarshalMetaDataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.DeclSecurity:			return new DeclSecurityMetaDataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.ClassLayout:				return new ClassLayoutMetaDataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.FieldLayout:				return new FieldLayoutMetaDataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.StandAloneSig:			return new StandAloneSigMetaDataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.EventMap:				return new EventMapMetaDataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.EventPtr:				return new EventPtrMetaDataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.Event:					return new EventMetaDataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.PropertyMap:				return new PropertyMapMetaDataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.PropertyPtr:				return new PropertyPtrMetaDataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.Property:				return new PropertyMetaDataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.MethodSemantics:			return new MethodSemanticsMetaDataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.MethodImpl:				return new MethodImplMetaDataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.ModuleRef:				return new ModuleRefMetaDataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.TypeSpec:				return new TypeSpecMetaDataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.ImplMap:					return new ImplMapMetaDataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.FieldRVA:				return new FieldRVAMetaDataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.ENCLog:					return new ENCLogMetaDataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.ENCMap:					return new ENCMapMetaDataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.Assembly:				return new AssemblyMetaDataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.AssemblyProcessor:		return new AssemblyProcessorMetaDataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.AssemblyOS:				return new AssemblyOSMetaDataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.AssemblyRef:				return new AssemblyRefMetaDataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.AssemblyRefProcessor:	return new AssemblyRefProcessorMetaDataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.AssemblyRefOS:			return new AssemblyRefOSMetaDataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.File:					return new FileMetaDataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.ExportedType:			return new ExportedTypeMetaDataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.ManifestResource:		return new ManifestResourceMetaDataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.NestedClass:				return new NestedClassMetaDataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.GenericParam:			return TableInfo.Columns.Count == 5 ? (MetaDataTableRecordVM)new GenericParamMetaDataTableRecordV11VM(this, new MDToken(TableInfo.Table, index + 1)) : new GenericParamMetaDataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.MethodSpec:				return new MethodSpecMetaDataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.GenericParamConstraint:	return new GenericParamConstraintMetaDataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			default:							throw new InvalidOperationException();
			}
		}

		public static MetaDataTableVM Create(object owner, HexDocument doc, ulong startOffset, MDTable mdTable) {
			switch (mdTable.Table) {
			case Table.Module:					return new ModuleMetaDataTableVM(owner, doc, startOffset, mdTable);
			case Table.TypeRef:					return new TypeRefMetaDataTableVM(owner, doc, startOffset, mdTable);
			case Table.TypeDef:					return new TypeDefMetaDataTableVM(owner, doc, startOffset, mdTable);
			case Table.FieldPtr:				return new FieldPtrMetaDataTableVM(owner, doc, startOffset, mdTable);
			case Table.Field:					return new FieldMetaDataTableVM(owner, doc, startOffset, mdTable);
			case Table.MethodPtr:				return new MethodPtrMetaDataTableVM(owner, doc, startOffset, mdTable);
			case Table.Method:					return new MethodMetaDataTableVM(owner, doc, startOffset, mdTable);
			case Table.ParamPtr:				return new ParamPtrMetaDataTableVM(owner, doc, startOffset, mdTable);
			case Table.Param:					return new ParamMetaDataTableVM(owner, doc, startOffset, mdTable);
			case Table.InterfaceImpl:			return new InterfaceImplMetaDataTableVM(owner, doc, startOffset, mdTable);
			case Table.MemberRef:				return new MemberRefMetaDataTableVM(owner, doc, startOffset, mdTable);
			case Table.Constant:				return new ConstantMetaDataTableVM(owner, doc, startOffset, mdTable);
			case Table.CustomAttribute:			return new CustomAttributeMetaDataTableVM(owner, doc, startOffset, mdTable);
			case Table.FieldMarshal:			return new FieldMarshalMetaDataTableVM(owner, doc, startOffset, mdTable);
			case Table.DeclSecurity:			return new DeclSecurityMetaDataTableVM(owner, doc, startOffset, mdTable);
			case Table.ClassLayout:				return new ClassLayoutMetaDataTableVM(owner, doc, startOffset, mdTable);
			case Table.FieldLayout:				return new FieldLayoutMetaDataTableVM(owner, doc, startOffset, mdTable);
			case Table.StandAloneSig:			return new StandAloneSigMetaDataTableVM(owner, doc, startOffset, mdTable);
			case Table.EventMap:				return new EventMapMetaDataTableVM(owner, doc, startOffset, mdTable);
			case Table.EventPtr:				return new EventPtrMetaDataTableVM(owner, doc, startOffset, mdTable);
			case Table.Event:					return new EventMetaDataTableVM(owner, doc, startOffset, mdTable);
			case Table.PropertyMap:				return new PropertyMapMetaDataTableVM(owner, doc, startOffset, mdTable);
			case Table.PropertyPtr:				return new PropertyPtrMetaDataTableVM(owner, doc, startOffset, mdTable);
			case Table.Property:				return new PropertyMetaDataTableVM(owner, doc, startOffset, mdTable);
			case Table.MethodSemantics:			return new MethodSemanticsMetaDataTableVM(owner, doc, startOffset, mdTable);
			case Table.MethodImpl:				return new MethodImplMetaDataTableVM(owner, doc, startOffset, mdTable);
			case Table.ModuleRef:				return new ModuleRefMetaDataTableVM(owner, doc, startOffset, mdTable);
			case Table.TypeSpec:				return new TypeSpecMetaDataTableVM(owner, doc, startOffset, mdTable);
			case Table.ImplMap:					return new ImplMapMetaDataTableVM(owner, doc, startOffset, mdTable);
			case Table.FieldRVA:				return new FieldRVAMetaDataTableVM(owner, doc, startOffset, mdTable);
			case Table.ENCLog:					return new ENCLogMetaDataTableVM(owner, doc, startOffset, mdTable);
			case Table.ENCMap:					return new ENCMapMetaDataTableVM(owner, doc, startOffset, mdTable);
			case Table.Assembly:				return new AssemblyMetaDataTableVM(owner, doc, startOffset, mdTable);
			case Table.AssemblyProcessor:		return new AssemblyProcessorMetaDataTableVM(owner, doc, startOffset, mdTable);
			case Table.AssemblyOS:				return new AssemblyOSMetaDataTableVM(owner, doc, startOffset, mdTable);
			case Table.AssemblyRef:				return new AssemblyRefMetaDataTableVM(owner, doc, startOffset, mdTable);
			case Table.AssemblyRefProcessor:	return new AssemblyRefProcessorMetaDataTableVM(owner, doc, startOffset, mdTable);
			case Table.AssemblyRefOS:			return new AssemblyRefOSMetaDataTableVM(owner, doc, startOffset, mdTable);
			case Table.File:					return new FileMetaDataTableVM(owner, doc, startOffset, mdTable);
			case Table.ExportedType:			return new ExportedTypeMetaDataTableVM(owner, doc, startOffset, mdTable);
			case Table.ManifestResource:		return new ManifestResourceMetaDataTableVM(owner, doc, startOffset, mdTable);
			case Table.NestedClass:				return new NestedClassMetaDataTableVM(owner, doc, startOffset, mdTable);
			case Table.GenericParam:			return mdTable.Columns.Count == 5 ? (MetaDataTableVM)new GenericParamMetaDataTableV11VM(owner, doc, startOffset, mdTable) : new GenericParamMetaDataTableVM(owner, doc, startOffset, mdTable);
			case Table.MethodSpec:				return new MethodSpecMetaDataTableVM(owner, doc, startOffset, mdTable);
			case Table.GenericParamConstraint:	return new GenericParamConstraintMetaDataTableVM(owner, doc, startOffset, mdTable);
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
			return tooLongString ? s + "..." : s;
		}
	}

	abstract class MetaDataTable1VM : MetaDataTableVM {
		public MetaDataTable1VM(object owner, HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(owner, doc, startOffset, mdTable) {
		}
	}

	abstract class MetaDataTable2VM : MetaDataTableVM {
		public MetaDataTable2VM(object owner, HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(owner, doc, startOffset, mdTable) {
		}
	}

	abstract class MetaDataTable3VM : MetaDataTableVM {
		public MetaDataTable3VM(object owner, HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(owner, doc, startOffset, mdTable) {
		}
	}

	abstract class MetaDataTable4VM : MetaDataTableVM {
		public MetaDataTable4VM(object owner, HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(owner, doc, startOffset, mdTable) {
		}
	}

	abstract class MetaDataTable5VM : MetaDataTableVM {
		public MetaDataTable5VM(object owner, HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(owner, doc, startOffset, mdTable) {
		}
	}

	abstract class MetaDataTable6VM : MetaDataTableVM {
		public MetaDataTable6VM(object owner, HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(owner, doc, startOffset, mdTable) {
		}
	}

	abstract class MetaDataTable9VM : MetaDataTableVM {
		public MetaDataTable9VM(object owner, HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(owner, doc, startOffset, mdTable) {
		}
	}

	abstract class MetaDataTable1InfoVM : MetaDataTable1VM {
		public override bool HasInfo {
			get { return true; }
		}

		public MetaDataTable1InfoVM(object owner, HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(owner, doc, startOffset, mdTable) {
		}
	}

	abstract class MetaDataTable2InfoVM : MetaDataTable2VM {
		public override bool HasInfo {
			get { return true; }
		}

		public MetaDataTable2InfoVM(object owner, HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(owner, doc, startOffset, mdTable) {
		}
	}

	abstract class MetaDataTable3InfoVM : MetaDataTable3VM {
		public override bool HasInfo {
			get { return true; }
		}

		public MetaDataTable3InfoVM(object owner, HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(owner, doc, startOffset, mdTable) {
		}
	}

	abstract class MetaDataTable4InfoVM : MetaDataTable4VM {
		public override bool HasInfo {
			get { return true; }
		}

		public MetaDataTable4InfoVM(object owner, HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(owner, doc, startOffset, mdTable) {
		}
	}

	abstract class MetaDataTable5InfoVM : MetaDataTable5VM {
		public override bool HasInfo {
			get { return true; }
		}

		public MetaDataTable5InfoVM(object owner, HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(owner, doc, startOffset, mdTable) {
		}
	}

	abstract class MetaDataTable6InfoVM : MetaDataTable6VM {
		public override bool HasInfo {
			get { return true; }
		}

		public MetaDataTable6InfoVM(object owner, HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(owner, doc, startOffset, mdTable) {
		}
	}

	abstract class MetaDataTable9InfoVM : MetaDataTable9VM {
		public override bool HasInfo {
			get { return true; }
		}

		public MetaDataTable9InfoVM(object owner, HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(owner, doc, startOffset, mdTable) {
		}
	}

	sealed class ModuleMetaDataTableVM : MetaDataTable5InfoVM {
		public ModuleMetaDataTableVM(object owner, HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(owner, doc, startOffset, mdTable) {
		}
	}

	sealed class TypeRefMetaDataTableVM : MetaDataTable3InfoVM {
		public TypeRefMetaDataTableVM(object owner, HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(owner, doc, startOffset, mdTable) {
		}
	}

	sealed class TypeDefMetaDataTableVM : MetaDataTable6InfoVM {
		public TypeDefMetaDataTableVM(object owner, HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(owner, doc, startOffset, mdTable) {
		}
	}

	sealed class FieldPtrMetaDataTableVM : MetaDataTable1VM {
		public FieldPtrMetaDataTableVM(object owner, HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(owner, doc, startOffset, mdTable) {
		}
	}

	sealed class FieldMetaDataTableVM : MetaDataTable3InfoVM {
		public FieldMetaDataTableVM(object owner, HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(owner, doc, startOffset, mdTable) {
		}
	}

	sealed class MethodPtrMetaDataTableVM : MetaDataTable1VM {
		public MethodPtrMetaDataTableVM(object owner, HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(owner, doc, startOffset, mdTable) {
		}
	}

	sealed class MethodMetaDataTableVM : MetaDataTable6InfoVM {
		public MethodMetaDataTableVM(object owner, HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(owner, doc, startOffset, mdTable) {
		}
	}

	sealed class ParamPtrMetaDataTableVM : MetaDataTable1VM {
		public ParamPtrMetaDataTableVM(object owner, HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(owner, doc, startOffset, mdTable) {
		}
	}

	sealed class ParamMetaDataTableVM : MetaDataTable3InfoVM {
		public ParamMetaDataTableVM(object owner, HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(owner, doc, startOffset, mdTable) {
		}
	}

	sealed class InterfaceImplMetaDataTableVM : MetaDataTable2VM {
		public InterfaceImplMetaDataTableVM(object owner, HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(owner, doc, startOffset, mdTable) {
		}
	}

	sealed class MemberRefMetaDataTableVM : MetaDataTable3InfoVM {
		public MemberRefMetaDataTableVM(object owner, HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(owner, doc, startOffset, mdTable) {
		}
	}

	sealed class ConstantMetaDataTableVM : MetaDataTable4VM {
		public ConstantMetaDataTableVM(object owner, HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(owner, doc, startOffset, mdTable) {
		}
	}

	sealed class CustomAttributeMetaDataTableVM : MetaDataTable3VM {
		public CustomAttributeMetaDataTableVM(object owner, HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(owner, doc, startOffset, mdTable) {
		}
	}

	sealed class FieldMarshalMetaDataTableVM : MetaDataTable2VM {
		public FieldMarshalMetaDataTableVM(object owner, HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(owner, doc, startOffset, mdTable) {
		}
	}

	sealed class DeclSecurityMetaDataTableVM : MetaDataTable3VM {
		public DeclSecurityMetaDataTableVM(object owner, HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(owner, doc, startOffset, mdTable) {
		}
	}

	sealed class ClassLayoutMetaDataTableVM : MetaDataTable3VM {
		public ClassLayoutMetaDataTableVM(object owner, HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(owner, doc, startOffset, mdTable) {
		}
	}

	sealed class FieldLayoutMetaDataTableVM : MetaDataTable2VM {
		public FieldLayoutMetaDataTableVM(object owner, HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(owner, doc, startOffset, mdTable) {
		}
	}

	sealed class StandAloneSigMetaDataTableVM : MetaDataTable1VM {
		public StandAloneSigMetaDataTableVM(object owner, HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(owner, doc, startOffset, mdTable) {
		}
	}

	sealed class EventMapMetaDataTableVM : MetaDataTable2VM {
		public EventMapMetaDataTableVM(object owner, HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(owner, doc, startOffset, mdTable) {
		}
	}

	sealed class EventPtrMetaDataTableVM : MetaDataTable1VM {
		public EventPtrMetaDataTableVM(object owner, HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(owner, doc, startOffset, mdTable) {
		}
	}

	sealed class EventMetaDataTableVM : MetaDataTable3InfoVM {
		public EventMetaDataTableVM(object owner, HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(owner, doc, startOffset, mdTable) {
		}
	}

	sealed class PropertyMapMetaDataTableVM : MetaDataTable2VM {
		public PropertyMapMetaDataTableVM(object owner, HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(owner, doc, startOffset, mdTable) {
		}
	}

	sealed class PropertyPtrMetaDataTableVM : MetaDataTable1VM {
		public PropertyPtrMetaDataTableVM(object owner, HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(owner, doc, startOffset, mdTable) {
		}
	}

	sealed class PropertyMetaDataTableVM : MetaDataTable3InfoVM {
		public PropertyMetaDataTableVM(object owner, HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(owner, doc, startOffset, mdTable) {
		}
	}

	sealed class MethodSemanticsMetaDataTableVM : MetaDataTable3VM {
		public MethodSemanticsMetaDataTableVM(object owner, HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(owner, doc, startOffset, mdTable) {
		}
	}

	sealed class MethodImplMetaDataTableVM : MetaDataTable3VM {
		public MethodImplMetaDataTableVM(object owner, HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(owner, doc, startOffset, mdTable) {
		}
	}

	sealed class ModuleRefMetaDataTableVM : MetaDataTable1InfoVM {
		public ModuleRefMetaDataTableVM(object owner, HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(owner, doc, startOffset, mdTable) {
		}
	}

	sealed class TypeSpecMetaDataTableVM : MetaDataTable1VM {
		public TypeSpecMetaDataTableVM(object owner, HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(owner, doc, startOffset, mdTable) {
		}
	}

	sealed class ImplMapMetaDataTableVM : MetaDataTable4InfoVM {
		public ImplMapMetaDataTableVM(object owner, HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(owner, doc, startOffset, mdTable) {
		}
	}

	sealed class FieldRVAMetaDataTableVM : MetaDataTable2VM {
		public FieldRVAMetaDataTableVM(object owner, HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(owner, doc, startOffset, mdTable) {
		}
	}

	sealed class ENCLogMetaDataTableVM : MetaDataTable2VM {
		public ENCLogMetaDataTableVM(object owner, HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(owner, doc, startOffset, mdTable) {
		}
	}

	sealed class ENCMapMetaDataTableVM : MetaDataTable1VM {
		public ENCMapMetaDataTableVM(object owner, HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(owner, doc, startOffset, mdTable) {
		}
	}

	sealed class AssemblyMetaDataTableVM : MetaDataTable9InfoVM {
		public AssemblyMetaDataTableVM(object owner, HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(owner, doc, startOffset, mdTable) {
		}
	}

	sealed class AssemblyProcessorMetaDataTableVM : MetaDataTable1VM {
		public AssemblyProcessorMetaDataTableVM(object owner, HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(owner, doc, startOffset, mdTable) {
		}
	}

	sealed class AssemblyOSMetaDataTableVM : MetaDataTable3VM {
		public AssemblyOSMetaDataTableVM(object owner, HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(owner, doc, startOffset, mdTable) {
		}
	}

	sealed class AssemblyRefMetaDataTableVM : MetaDataTable9InfoVM {
		public AssemblyRefMetaDataTableVM(object owner, HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(owner, doc, startOffset, mdTable) {
		}
	}

	sealed class AssemblyRefProcessorMetaDataTableVM : MetaDataTable2VM {
		public AssemblyRefProcessorMetaDataTableVM(object owner, HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(owner, doc, startOffset, mdTable) {
		}
	}

	sealed class AssemblyRefOSMetaDataTableVM : MetaDataTable4VM {
		public AssemblyRefOSMetaDataTableVM(object owner, HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(owner, doc, startOffset, mdTable) {
		}
	}

	sealed class FileMetaDataTableVM : MetaDataTable3InfoVM {
		public FileMetaDataTableVM(object owner, HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(owner, doc, startOffset, mdTable) {
		}
	}

	sealed class ExportedTypeMetaDataTableVM : MetaDataTable5InfoVM {
		public ExportedTypeMetaDataTableVM(object owner, HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(owner, doc, startOffset, mdTable) {
		}
	}

	sealed class ManifestResourceMetaDataTableVM : MetaDataTable4InfoVM {
		public ManifestResourceMetaDataTableVM(object owner, HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(owner, doc, startOffset, mdTable) {
		}
	}

	sealed class NestedClassMetaDataTableVM : MetaDataTable2VM {
		public NestedClassMetaDataTableVM(object owner, HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(owner, doc, startOffset, mdTable) {
		}
	}

	sealed class GenericParamMetaDataTableV11VM : MetaDataTable5InfoVM {
		public GenericParamMetaDataTableV11VM(object owner, HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(owner, doc, startOffset, mdTable) {
		}
	}

	sealed class GenericParamMetaDataTableVM : MetaDataTable4InfoVM {
		public GenericParamMetaDataTableVM(object owner, HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(owner, doc, startOffset, mdTable) {
		}
	}

	sealed class MethodSpecMetaDataTableVM : MetaDataTable2VM {
		public MethodSpecMetaDataTableVM(object owner, HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(owner, doc, startOffset, mdTable) {
		}
	}

	sealed class GenericParamConstraintMetaDataTableVM : MetaDataTable2VM {
		public GenericParamConstraintMetaDataTableVM(object owner, HexDocument doc, ulong startOffset, MDTable mdTable)
			: base(owner, doc, startOffset, mdTable) {
		}
	}
}
