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

namespace dnSpy.AsmEditor.Hex.Nodes {
	abstract class MetaDataTableVM : HexVM {
		public Func<Table, MetaDataTableVM> FindMetaDataTable { get; set; }
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
		public HexSpan Span { get; }

		readonly HexBuffer buffer;
		readonly HexSpan stringsHeapSpan;
		readonly HexSpan guidHeapSpan;

		protected MetaDataTableVM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner) {
			this.buffer = buffer;
			Span = new HexSpan(startOffset, (ulong)mdTable.Rows * mdTable.RowSize);
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

		public static MetaDataTableVM Create(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan) {
			switch (mdTable.Table) {
			case Table.Module:					return new ModuleMetaDataTableVM(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.TypeRef:					return new TypeRefMetaDataTableVM(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.TypeDef:					return new TypeDefMetaDataTableVM(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.FieldPtr:				return new FieldPtrMetaDataTableVM(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.Field:					return new FieldMetaDataTableVM(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.MethodPtr:				return new MethodPtrMetaDataTableVM(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.Method:					return new MethodMetaDataTableVM(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.ParamPtr:				return new ParamPtrMetaDataTableVM(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.Param:					return new ParamMetaDataTableVM(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.InterfaceImpl:			return new InterfaceImplMetaDataTableVM(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.MemberRef:				return new MemberRefMetaDataTableVM(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.Constant:				return new ConstantMetaDataTableVM(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.CustomAttribute:			return new CustomAttributeMetaDataTableVM(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.FieldMarshal:			return new FieldMarshalMetaDataTableVM(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.DeclSecurity:			return new DeclSecurityMetaDataTableVM(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.ClassLayout:				return new ClassLayoutMetaDataTableVM(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.FieldLayout:				return new FieldLayoutMetaDataTableVM(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.StandAloneSig:			return new StandAloneSigMetaDataTableVM(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.EventMap:				return new EventMapMetaDataTableVM(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.EventPtr:				return new EventPtrMetaDataTableVM(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.Event:					return new EventMetaDataTableVM(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.PropertyMap:				return new PropertyMapMetaDataTableVM(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.PropertyPtr:				return new PropertyPtrMetaDataTableVM(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.Property:				return new PropertyMetaDataTableVM(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.MethodSemantics:			return new MethodSemanticsMetaDataTableVM(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.MethodImpl:				return new MethodImplMetaDataTableVM(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.ModuleRef:				return new ModuleRefMetaDataTableVM(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.TypeSpec:				return new TypeSpecMetaDataTableVM(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.ImplMap:					return new ImplMapMetaDataTableVM(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.FieldRVA:				return new FieldRVAMetaDataTableVM(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.ENCLog:					return new ENCLogMetaDataTableVM(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.ENCMap:					return new ENCMapMetaDataTableVM(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.Assembly:				return new AssemblyMetaDataTableVM(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.AssemblyProcessor:		return new AssemblyProcessorMetaDataTableVM(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.AssemblyOS:				return new AssemblyOSMetaDataTableVM(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.AssemblyRef:				return new AssemblyRefMetaDataTableVM(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.AssemblyRefProcessor:	return new AssemblyRefProcessorMetaDataTableVM(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.AssemblyRefOS:			return new AssemblyRefOSMetaDataTableVM(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.File:					return new FileMetaDataTableVM(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.ExportedType:			return new ExportedTypeMetaDataTableVM(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.ManifestResource:		return new ManifestResourceMetaDataTableVM(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.NestedClass:				return new NestedClassMetaDataTableVM(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.GenericParam:			return mdTable.Columns.Count == 5 ? (MetaDataTableVM)new GenericParamMetaDataTableV11VM(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) : new GenericParamMetaDataTableVM(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.MethodSpec:				return new MethodSpecMetaDataTableVM(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.GenericParamConstraint:	return new GenericParamConstraintMetaDataTableVM(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.Document:				return new DocumentMetaDataTableVM(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.MethodDebugInformation:	return new MethodDebugInformationMetaDataTableVM(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.LocalScope:				return new LocalScopeMetaDataTableVM(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.LocalVariable:			return new LocalVariableMetaDataTableVM(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.LocalConstant:			return new LocalConstantMetaDataTableVM(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.ImportScope:				return new ImportScopeMetaDataTableVM(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.StateMachineMethod:		return new StateMachineMethodMetaDataTableVM(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.CustomDebugInformation:	return new CustomDebugInformationMetaDataTableVM(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan);
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
		public MetaDataTable1VM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	abstract class MetaDataTable2VM : MetaDataTableVM {
		public MetaDataTable2VM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	abstract class MetaDataTable3VM : MetaDataTableVM {
		public MetaDataTable3VM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	abstract class MetaDataTable4VM : MetaDataTableVM {
		public MetaDataTable4VM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	abstract class MetaDataTable5VM : MetaDataTableVM {
		public MetaDataTable5VM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	abstract class MetaDataTable6VM : MetaDataTableVM {
		public MetaDataTable6VM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	abstract class MetaDataTable9VM : MetaDataTableVM {
		public MetaDataTable9VM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	abstract class MetaDataTable1InfoVM : MetaDataTable1VM {
		public override bool HasInfo => true;

		public MetaDataTable1InfoVM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	abstract class MetaDataTable2InfoVM : MetaDataTable2VM {
		public override bool HasInfo => true;

		public MetaDataTable2InfoVM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	abstract class MetaDataTable3InfoVM : MetaDataTable3VM {
		public override bool HasInfo => true;

		public MetaDataTable3InfoVM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	abstract class MetaDataTable4InfoVM : MetaDataTable4VM {
		public override bool HasInfo => true;

		public MetaDataTable4InfoVM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	abstract class MetaDataTable5InfoVM : MetaDataTable5VM {
		public override bool HasInfo => true;

		public MetaDataTable5InfoVM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	abstract class MetaDataTable6InfoVM : MetaDataTable6VM {
		public override bool HasInfo => true;

		public MetaDataTable6InfoVM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	abstract class MetaDataTable9InfoVM : MetaDataTable9VM {
		public override bool HasInfo => true;

		public MetaDataTable9InfoVM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class ModuleMetaDataTableVM : MetaDataTable5InfoVM {
		public ModuleMetaDataTableVM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class TypeRefMetaDataTableVM : MetaDataTable3InfoVM {
		public TypeRefMetaDataTableVM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class TypeDefMetaDataTableVM : MetaDataTable6InfoVM {
		public TypeDefMetaDataTableVM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class FieldPtrMetaDataTableVM : MetaDataTable1VM {
		public FieldPtrMetaDataTableVM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class FieldMetaDataTableVM : MetaDataTable3InfoVM {
		public FieldMetaDataTableVM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class MethodPtrMetaDataTableVM : MetaDataTable1VM {
		public MethodPtrMetaDataTableVM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class MethodMetaDataTableVM : MetaDataTable6InfoVM {
		public MethodMetaDataTableVM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class ParamPtrMetaDataTableVM : MetaDataTable1VM {
		public ParamPtrMetaDataTableVM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class ParamMetaDataTableVM : MetaDataTable3InfoVM {
		public ParamMetaDataTableVM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class InterfaceImplMetaDataTableVM : MetaDataTable2VM {
		public InterfaceImplMetaDataTableVM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class MemberRefMetaDataTableVM : MetaDataTable3InfoVM {
		public MemberRefMetaDataTableVM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class ConstantMetaDataTableVM : MetaDataTable4VM {
		public ConstantMetaDataTableVM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class CustomAttributeMetaDataTableVM : MetaDataTable3VM {
		public CustomAttributeMetaDataTableVM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class FieldMarshalMetaDataTableVM : MetaDataTable2VM {
		public FieldMarshalMetaDataTableVM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class DeclSecurityMetaDataTableVM : MetaDataTable3VM {
		public DeclSecurityMetaDataTableVM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class ClassLayoutMetaDataTableVM : MetaDataTable3VM {
		public ClassLayoutMetaDataTableVM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class FieldLayoutMetaDataTableVM : MetaDataTable2VM {
		public FieldLayoutMetaDataTableVM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class StandAloneSigMetaDataTableVM : MetaDataTable1VM {
		public StandAloneSigMetaDataTableVM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class EventMapMetaDataTableVM : MetaDataTable2VM {
		public EventMapMetaDataTableVM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class EventPtrMetaDataTableVM : MetaDataTable1VM {
		public EventPtrMetaDataTableVM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class EventMetaDataTableVM : MetaDataTable3InfoVM {
		public EventMetaDataTableVM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class PropertyMapMetaDataTableVM : MetaDataTable2VM {
		public PropertyMapMetaDataTableVM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class PropertyPtrMetaDataTableVM : MetaDataTable1VM {
		public PropertyPtrMetaDataTableVM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class PropertyMetaDataTableVM : MetaDataTable3InfoVM {
		public PropertyMetaDataTableVM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class MethodSemanticsMetaDataTableVM : MetaDataTable3VM {
		public MethodSemanticsMetaDataTableVM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class MethodImplMetaDataTableVM : MetaDataTable3VM {
		public MethodImplMetaDataTableVM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class ModuleRefMetaDataTableVM : MetaDataTable1InfoVM {
		public ModuleRefMetaDataTableVM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class TypeSpecMetaDataTableVM : MetaDataTable1VM {
		public TypeSpecMetaDataTableVM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class ImplMapMetaDataTableVM : MetaDataTable4InfoVM {
		public ImplMapMetaDataTableVM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class FieldRVAMetaDataTableVM : MetaDataTable2VM {
		public FieldRVAMetaDataTableVM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class ENCLogMetaDataTableVM : MetaDataTable2VM {
		public ENCLogMetaDataTableVM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class ENCMapMetaDataTableVM : MetaDataTable1VM {
		public ENCMapMetaDataTableVM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class AssemblyMetaDataTableVM : MetaDataTable9InfoVM {
		public AssemblyMetaDataTableVM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class AssemblyProcessorMetaDataTableVM : MetaDataTable1VM {
		public AssemblyProcessorMetaDataTableVM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class AssemblyOSMetaDataTableVM : MetaDataTable3VM {
		public AssemblyOSMetaDataTableVM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class AssemblyRefMetaDataTableVM : MetaDataTable9InfoVM {
		public AssemblyRefMetaDataTableVM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class AssemblyRefProcessorMetaDataTableVM : MetaDataTable2VM {
		public AssemblyRefProcessorMetaDataTableVM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class AssemblyRefOSMetaDataTableVM : MetaDataTable4VM {
		public AssemblyRefOSMetaDataTableVM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class FileMetaDataTableVM : MetaDataTable3InfoVM {
		public FileMetaDataTableVM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class ExportedTypeMetaDataTableVM : MetaDataTable5InfoVM {
		public ExportedTypeMetaDataTableVM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class ManifestResourceMetaDataTableVM : MetaDataTable4InfoVM {
		public ManifestResourceMetaDataTableVM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class NestedClassMetaDataTableVM : MetaDataTable2VM {
		public NestedClassMetaDataTableVM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class GenericParamMetaDataTableV11VM : MetaDataTable5InfoVM {
		public GenericParamMetaDataTableV11VM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class GenericParamMetaDataTableVM : MetaDataTable4InfoVM {
		public GenericParamMetaDataTableVM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class MethodSpecMetaDataTableVM : MetaDataTable2VM {
		public MethodSpecMetaDataTableVM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class GenericParamConstraintMetaDataTableVM : MetaDataTable2VM {
		public GenericParamConstraintMetaDataTableVM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class DocumentMetaDataTableVM : MetaDataTable4VM {
		public DocumentMetaDataTableVM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class MethodDebugInformationMetaDataTableVM : MetaDataTable2VM {
		public MethodDebugInformationMetaDataTableVM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class LocalScopeMetaDataTableVM : MetaDataTable6VM {
		public LocalScopeMetaDataTableVM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class LocalVariableMetaDataTableVM : MetaDataTable3VM {
		public LocalVariableMetaDataTableVM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class LocalConstantMetaDataTableVM : MetaDataTable2VM {
		public LocalConstantMetaDataTableVM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class ImportScopeMetaDataTableVM : MetaDataTable2VM {
		public ImportScopeMetaDataTableVM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class StateMachineMethodMetaDataTableVM : MetaDataTable2VM {
		public StateMachineMethodMetaDataTableVM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class CustomDebugInformationMetaDataTableVM : MetaDataTable3VM {
		public CustomDebugInformationMetaDataTableVM(object owner, HexBuffer buffer, HexPosition startOffset, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(owner, buffer, startOffset, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}
}
