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
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnSpy.AsmEditor.Properties;
using dnSpy.Contracts.Hex;

namespace dnSpy.AsmEditor.Hex.PE {
	abstract class MetaDataTableVM : HexVM {
		public Table Table => TableInfo.Table;
		public uint Rows { get; }
		public TableInfo TableInfo { get; }
		public override string Name => string.Format("{0:X2} {1}", (byte)TableInfo.Table, TableInfo.Table);
		public string Column0Name => GetColumnName(0);
		public string Column1Name => GetColumnName(1);
		public string Column2Name => GetColumnName(2);
		public string Column3Name => GetColumnName(3);
		public string Column4Name => GetColumnName(4);
		public string Column5Name => GetColumnName(5);
		public string Column6Name => GetColumnName(6);
		public string Column7Name => GetColumnName(7);
		public string Column8Name => GetColumnName(8);
		public string Column9Name => GetColumnName(9);

		public string GetColumnName(int col) {
			Debug.Assert(col < TableInfo.Columns.Count);
			if (col >= TableInfo.Columns.Count)
				return string.Empty;
			return TableInfo.Columns[col].Name;
		}

		public string InfoName => dnSpy_AsmEditor_Resources.Info;
		public virtual bool HasInfo => false;
		public override IEnumerable<HexField> HexFields => hexFields;
		readonly HexField[] hexFields = Array.Empty<HexField>();

		public VirtualizedList<MetaDataTableRecordVM> Collection { get; }

		public object SelectedItem {
			get { return selectedItem; }
			set {
				if (selectedItem != value) {
					selectedItem = value;
					OnPropertyChanged(nameof(SelectedItem));
				}
			}
		}
		object selectedItem;

		public HexBuffer Buffer => buffer;
		public TablesStreamVM TablesStream { get; }
		public object Owner { get; set; }

		readonly HexBuffer buffer;
		readonly HexSpan stringsHeapSpan;
		readonly HexSpan guidHeapSpan;

		protected MetaDataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(new HexSpan((ulong)mdTable.StartOffset, (ulong)mdTable.EndOffset)) {
			this.buffer = buffer;
			TablesStream = tablesStream;
			this.stringsHeapSpan = stringsHeapSpan;
			this.guidHeapSpan = guidHeapSpan;
			Rows = mdTable.Rows;
			TableInfo = CreateTableInfo(mdTable.TableInfo);
			Collection = new VirtualizedList<MetaDataTableRecordVM>((int)Rows, CreateItem);
		}

		MetaDataTableRecordVM CreateItem(int index) {
			Debug.Assert(index >= 0 && (uint)index < Rows);
			switch (TableInfo.Table) {
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
			case Table.Document:				return new DocumentMetaDataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.MethodDebugInformation:	return new MethodDebugInformationMetaDataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.LocalScope:				return new LocalScopeMetaDataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.LocalVariable:			return new LocalVariableMetaDataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.LocalConstant:			return new LocalConstantMetaDataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.ImportScope:				return new ImportScopeMetaDataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.StateMachineMethod:		return new StateMachineMethodMetaDataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.CustomDebugInformation:	return new CustomDebugInformationMetaDataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			default:							throw new InvalidOperationException();
			}
		}

		public static MetaDataTableVM Create(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan) {
			switch (mdTable.Table) {
			case Table.Module:					return new ModuleMetaDataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.TypeRef:					return new TypeRefMetaDataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.TypeDef:					return new TypeDefMetaDataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.FieldPtr:				return new FieldPtrMetaDataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.Field:					return new FieldMetaDataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.MethodPtr:				return new MethodPtrMetaDataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.Method:					return new MethodMetaDataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.ParamPtr:				return new ParamPtrMetaDataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.Param:					return new ParamMetaDataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.InterfaceImpl:			return new InterfaceImplMetaDataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.MemberRef:				return new MemberRefMetaDataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.Constant:				return new ConstantMetaDataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.CustomAttribute:			return new CustomAttributeMetaDataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.FieldMarshal:			return new FieldMarshalMetaDataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.DeclSecurity:			return new DeclSecurityMetaDataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.ClassLayout:				return new ClassLayoutMetaDataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.FieldLayout:				return new FieldLayoutMetaDataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.StandAloneSig:			return new StandAloneSigMetaDataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.EventMap:				return new EventMapMetaDataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.EventPtr:				return new EventPtrMetaDataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.Event:					return new EventMetaDataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.PropertyMap:				return new PropertyMapMetaDataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.PropertyPtr:				return new PropertyPtrMetaDataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.Property:				return new PropertyMetaDataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.MethodSemantics:			return new MethodSemanticsMetaDataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.MethodImpl:				return new MethodImplMetaDataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.ModuleRef:				return new ModuleRefMetaDataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.TypeSpec:				return new TypeSpecMetaDataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.ImplMap:					return new ImplMapMetaDataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.FieldRVA:				return new FieldRVAMetaDataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.ENCLog:					return new ENCLogMetaDataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.ENCMap:					return new ENCMapMetaDataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.Assembly:				return new AssemblyMetaDataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.AssemblyProcessor:		return new AssemblyProcessorMetaDataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.AssemblyOS:				return new AssemblyOSMetaDataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.AssemblyRef:				return new AssemblyRefMetaDataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.AssemblyRefProcessor:	return new AssemblyRefProcessorMetaDataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.AssemblyRefOS:			return new AssemblyRefOSMetaDataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.File:					return new FileMetaDataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.ExportedType:			return new ExportedTypeMetaDataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.ManifestResource:		return new ManifestResourceMetaDataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.NestedClass:				return new NestedClassMetaDataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.GenericParam:			return mdTable.Columns.Count == 5 ? (MetaDataTableVM)new GenericParamMetaDataTableV11VM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) : new GenericParamMetaDataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.MethodSpec:				return new MethodSpecMetaDataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.GenericParamConstraint:	return new GenericParamConstraintMetaDataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.Document:				return new DocumentMetaDataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.MethodDebugInformation:	return new MethodDebugInformationMetaDataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.LocalScope:				return new LocalScopeMetaDataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.LocalVariable:			return new LocalVariableMetaDataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.LocalConstant:			return new LocalConstantMetaDataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.ImportScope:				return new ImportScopeMetaDataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.StateMachineMethod:		return new StateMachineMethodMetaDataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.CustomDebugInformation:	return new CustomDebugInformationMetaDataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
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
					newCols.Add(new ColumnInfo((byte)(coli++ + 1), dnSpy_AsmEditor_Resources.Padding, ColumnSize.Byte, (byte)offs, 1));
					offs++;
				}
			}

			return new TableInfo(info.Table, info.Name, newCols.ToArray(), offs);
		}

		public override void OnBufferChanged(NormalizedHexChangeCollection changes) {
			foreach (var change in changes) {
				if (!change.OldSpan.OverlapsWith(Span))
					continue;

				var start = HexPosition.Max(Span.Start, change.OldSpan.Start);
				var end = HexPosition.Min(Span.End, change.OldSpan.End);
				int i = (int)((start - Span.Start).ToUInt64() / (ulong)TableInfo.RowSize);
				int endi = (int)((end - 1 - Span.Start).ToUInt64() / (ulong)TableInfo.RowSize);
				Debug.Assert(0 <= i && i <= endi && endi < Collection.Count);
				while (i <= endi) {
					var obj = Collection.TryGet(i);
					if (obj != null)
						obj.OnBufferChanged(changes);
					i++;
				}
			}
		}

		public MetaDataTableRecordVM Get(int index) => Collection[index];
		public MetaDataTableRecordVM TryGet(int index) => Collection.TryGet(index);

		public string ReadStringsHeap(uint offset, uint maxLen = 0x200) {
			if (offset == 0)
				return string.Empty;
			var offs = stringsHeapSpan.Start + offset;
			var bytes = new List<byte>();
			bool tooLongString = false;
			while (offs < stringsHeapSpan.End) {
				int b = buffer.TryReadByte(offs);
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

		public Guid? ReadGuidHeap(uint index) {
			if (index == 0)
				return null;
			var offs = guidHeapSpan.Start + (index - 1) * 16;
			var bytes = new byte[16];
			int i = 0;
			while (offs < guidHeapSpan.End) {
				int b = buffer.TryReadByte(offs);
				if (b < 0)
					break;
				bytes[i++] = (byte)b;
				offs++;
				if (i == 16)
					return new Guid(bytes);
			}
			return null;
		}
	}

	abstract class MetaDataTable1VM : MetaDataTableVM {
		public MetaDataTable1VM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	abstract class MetaDataTable2VM : MetaDataTableVM {
		public MetaDataTable2VM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	abstract class MetaDataTable3VM : MetaDataTableVM {
		public MetaDataTable3VM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	abstract class MetaDataTable4VM : MetaDataTableVM {
		public MetaDataTable4VM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	abstract class MetaDataTable5VM : MetaDataTableVM {
		public MetaDataTable5VM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	abstract class MetaDataTable6VM : MetaDataTableVM {
		public MetaDataTable6VM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	abstract class MetaDataTable9VM : MetaDataTableVM {
		public MetaDataTable9VM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	abstract class MetaDataTable1InfoVM : MetaDataTable1VM {
		public override bool HasInfo => true;

		public MetaDataTable1InfoVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	abstract class MetaDataTable2InfoVM : MetaDataTable2VM {
		public override bool HasInfo => true;

		public MetaDataTable2InfoVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	abstract class MetaDataTable3InfoVM : MetaDataTable3VM {
		public override bool HasInfo => true;

		public MetaDataTable3InfoVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	abstract class MetaDataTable4InfoVM : MetaDataTable4VM {
		public override bool HasInfo => true;

		public MetaDataTable4InfoVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	abstract class MetaDataTable5InfoVM : MetaDataTable5VM {
		public override bool HasInfo => true;

		public MetaDataTable5InfoVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	abstract class MetaDataTable6InfoVM : MetaDataTable6VM {
		public override bool HasInfo => true;

		public MetaDataTable6InfoVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	abstract class MetaDataTable9InfoVM : MetaDataTable9VM {
		public override bool HasInfo => true;

		public MetaDataTable9InfoVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class ModuleMetaDataTableVM : MetaDataTable5InfoVM {
		public ModuleMetaDataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class TypeRefMetaDataTableVM : MetaDataTable3InfoVM {
		public TypeRefMetaDataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class TypeDefMetaDataTableVM : MetaDataTable6InfoVM {
		public TypeDefMetaDataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class FieldPtrMetaDataTableVM : MetaDataTable1VM {
		public FieldPtrMetaDataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class FieldMetaDataTableVM : MetaDataTable3InfoVM {
		public FieldMetaDataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class MethodPtrMetaDataTableVM : MetaDataTable1VM {
		public MethodPtrMetaDataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class MethodMetaDataTableVM : MetaDataTable6InfoVM {
		public MethodMetaDataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class ParamPtrMetaDataTableVM : MetaDataTable1VM {
		public ParamPtrMetaDataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class ParamMetaDataTableVM : MetaDataTable3InfoVM {
		public ParamMetaDataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class InterfaceImplMetaDataTableVM : MetaDataTable2VM {
		public InterfaceImplMetaDataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class MemberRefMetaDataTableVM : MetaDataTable3InfoVM {
		public MemberRefMetaDataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class ConstantMetaDataTableVM : MetaDataTable4VM {
		public ConstantMetaDataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class CustomAttributeMetaDataTableVM : MetaDataTable3VM {
		public CustomAttributeMetaDataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class FieldMarshalMetaDataTableVM : MetaDataTable2VM {
		public FieldMarshalMetaDataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class DeclSecurityMetaDataTableVM : MetaDataTable3VM {
		public DeclSecurityMetaDataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class ClassLayoutMetaDataTableVM : MetaDataTable3VM {
		public ClassLayoutMetaDataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class FieldLayoutMetaDataTableVM : MetaDataTable2VM {
		public FieldLayoutMetaDataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class StandAloneSigMetaDataTableVM : MetaDataTable1VM {
		public StandAloneSigMetaDataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class EventMapMetaDataTableVM : MetaDataTable2VM {
		public EventMapMetaDataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class EventPtrMetaDataTableVM : MetaDataTable1VM {
		public EventPtrMetaDataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class EventMetaDataTableVM : MetaDataTable3InfoVM {
		public EventMetaDataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class PropertyMapMetaDataTableVM : MetaDataTable2VM {
		public PropertyMapMetaDataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class PropertyPtrMetaDataTableVM : MetaDataTable1VM {
		public PropertyPtrMetaDataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class PropertyMetaDataTableVM : MetaDataTable3InfoVM {
		public PropertyMetaDataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class MethodSemanticsMetaDataTableVM : MetaDataTable3VM {
		public MethodSemanticsMetaDataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class MethodImplMetaDataTableVM : MetaDataTable3VM {
		public MethodImplMetaDataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class ModuleRefMetaDataTableVM : MetaDataTable1InfoVM {
		public ModuleRefMetaDataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class TypeSpecMetaDataTableVM : MetaDataTable1VM {
		public TypeSpecMetaDataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class ImplMapMetaDataTableVM : MetaDataTable4InfoVM {
		public ImplMapMetaDataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class FieldRVAMetaDataTableVM : MetaDataTable2VM {
		public FieldRVAMetaDataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class ENCLogMetaDataTableVM : MetaDataTable2VM {
		public ENCLogMetaDataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class ENCMapMetaDataTableVM : MetaDataTable1VM {
		public ENCMapMetaDataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class AssemblyMetaDataTableVM : MetaDataTable9InfoVM {
		public AssemblyMetaDataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class AssemblyProcessorMetaDataTableVM : MetaDataTable1VM {
		public AssemblyProcessorMetaDataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class AssemblyOSMetaDataTableVM : MetaDataTable3VM {
		public AssemblyOSMetaDataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class AssemblyRefMetaDataTableVM : MetaDataTable9InfoVM {
		public AssemblyRefMetaDataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class AssemblyRefProcessorMetaDataTableVM : MetaDataTable2VM {
		public AssemblyRefProcessorMetaDataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class AssemblyRefOSMetaDataTableVM : MetaDataTable4VM {
		public AssemblyRefOSMetaDataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class FileMetaDataTableVM : MetaDataTable3InfoVM {
		public FileMetaDataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class ExportedTypeMetaDataTableVM : MetaDataTable5InfoVM {
		public ExportedTypeMetaDataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class ManifestResourceMetaDataTableVM : MetaDataTable4InfoVM {
		public ManifestResourceMetaDataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class NestedClassMetaDataTableVM : MetaDataTable2VM {
		public NestedClassMetaDataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class GenericParamMetaDataTableV11VM : MetaDataTable5InfoVM {
		public GenericParamMetaDataTableV11VM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class GenericParamMetaDataTableVM : MetaDataTable4InfoVM {
		public GenericParamMetaDataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class MethodSpecMetaDataTableVM : MetaDataTable2VM {
		public MethodSpecMetaDataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class GenericParamConstraintMetaDataTableVM : MetaDataTable2VM {
		public GenericParamConstraintMetaDataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class DocumentMetaDataTableVM : MetaDataTable4VM {
		public DocumentMetaDataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class MethodDebugInformationMetaDataTableVM : MetaDataTable2VM {
		public MethodDebugInformationMetaDataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class LocalScopeMetaDataTableVM : MetaDataTable6VM {
		public LocalScopeMetaDataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class LocalVariableMetaDataTableVM : MetaDataTable3VM {
		public LocalVariableMetaDataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class LocalConstantMetaDataTableVM : MetaDataTable2VM {
		public LocalConstantMetaDataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class ImportScopeMetaDataTableVM : MetaDataTable2VM {
		public ImportScopeMetaDataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class StateMachineMethodMetaDataTableVM : MetaDataTable2VM {
		public StateMachineMethodMetaDataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class CustomDebugInformationMetaDataTableVM : MetaDataTable3VM {
		public CustomDebugInformationMetaDataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}
}
