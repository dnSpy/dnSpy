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
using System.Text;
using dnSpy.AsmEditor.Properties;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Files.DotNet;

namespace dnSpy.AsmEditor.Hex.PE {
	abstract class MetadataTableVM : HexVM {
		public Table Table => TableInfo.Table;
		public uint Rows { get; }
		public TableInfo TableInfo { get; }
		public override string Name => $"{(byte)TableInfo.Table:X2} {TableInfo.Table}";
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

		public VirtualizedList<MetadataTableRecordVM> Collection { get; }

		public object? SelectedItem {
			get => selectedItem;
			set {
				if (selectedItem != value) {
					selectedItem = value;
					OnPropertyChanged(nameof(SelectedItem));
				}
			}
		}
		object? selectedItem;

		public HexBuffer Buffer => buffer;
		public TablesStreamVM TablesStream { get; }
		public object? Owner { get; set; }

		readonly HexBuffer buffer;
		readonly HexSpan stringsHeapSpan;
		readonly HexSpan guidHeapSpan;

		protected MetadataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(mdTable.Span) {
			this.buffer = buffer;
			TablesStream = tablesStream;
			this.stringsHeapSpan = stringsHeapSpan;
			this.guidHeapSpan = guidHeapSpan;
			Rows = mdTable.Rows;
			TableInfo = CreateTableInfo(mdTable.TableInfo);
			Collection = new VirtualizedList<MetadataTableRecordVM>((int)Rows, CreateItem);
		}

		MetadataTableRecordVM CreateItem(int index) {
			Debug.Assert(index >= 0 && (uint)index < Rows);
			switch (TableInfo.Table) {
			case Table.Module:					return new ModuleMetadataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.TypeRef:					return new TypeRefMetadataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.TypeDef:					return new TypeDefMetadataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.FieldPtr:				return new FieldPtrMetadataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.Field:					return new FieldMetadataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.MethodPtr:				return new MethodPtrMetadataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.Method:					return new MethodMetadataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.ParamPtr:				return new ParamPtrMetadataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.Param:					return new ParamMetadataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.InterfaceImpl:			return new InterfaceImplMetadataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.MemberRef:				return new MemberRefMetadataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.Constant:				return new ConstantMetadataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.CustomAttribute:			return new CustomAttributeMetadataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.FieldMarshal:			return new FieldMarshalMetadataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.DeclSecurity:			return new DeclSecurityMetadataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.ClassLayout:				return new ClassLayoutMetadataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.FieldLayout:				return new FieldLayoutMetadataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.StandAloneSig:			return new StandAloneSigMetadataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.EventMap:				return new EventMapMetadataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.EventPtr:				return new EventPtrMetadataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.Event:					return new EventMetadataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.PropertyMap:				return new PropertyMapMetadataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.PropertyPtr:				return new PropertyPtrMetadataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.Property:				return new PropertyMetadataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.MethodSemantics:			return new MethodSemanticsMetadataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.MethodImpl:				return new MethodImplMetadataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.ModuleRef:				return new ModuleRefMetadataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.TypeSpec:				return new TypeSpecMetadataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.ImplMap:					return new ImplMapMetadataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.FieldRVA:				return new FieldRVAMetadataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.ENCLog:					return new ENCLogMetadataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.ENCMap:					return new ENCMapMetadataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.Assembly:				return new AssemblyMetadataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.AssemblyProcessor:		return new AssemblyProcessorMetadataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.AssemblyOS:				return new AssemblyOSMetadataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.AssemblyRef:				return new AssemblyRefMetadataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.AssemblyRefProcessor:	return new AssemblyRefProcessorMetadataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.AssemblyRefOS:			return new AssemblyRefOSMetadataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.File:					return new FileMetadataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.ExportedType:			return new ExportedTypeMetadataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.ManifestResource:		return new ManifestResourceMetadataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.NestedClass:				return new NestedClassMetadataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.GenericParam:			return TableInfo.Columns.Count == 5 ? (MetadataTableRecordVM)new GenericParamMetadataTableRecordV11VM(this, new MDToken(TableInfo.Table, index + 1)) : new GenericParamMetadataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.MethodSpec:				return new MethodSpecMetadataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.GenericParamConstraint:	return new GenericParamConstraintMetadataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.Document:				return new DocumentMetadataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.MethodDebugInformation:	return new MethodDebugInformationMetadataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.LocalScope:				return new LocalScopeMetadataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.LocalVariable:			return new LocalVariableMetadataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.LocalConstant:			return new LocalConstantMetadataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.ImportScope:				return new ImportScopeMetadataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.StateMachineMethod:		return new StateMachineMethodMetadataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			case Table.CustomDebugInformation:	return new CustomDebugInformationMetadataTableRecordVM(this, new MDToken(TableInfo.Table, index + 1));
			default:							throw new InvalidOperationException();
			}
		}

		public static MetadataTableVM Create(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan) {
			switch (mdTable.Table) {
			case Table.Module:					return new ModuleMetadataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.TypeRef:					return new TypeRefMetadataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.TypeDef:					return new TypeDefMetadataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.FieldPtr:				return new FieldPtrMetadataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.Field:					return new FieldMetadataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.MethodPtr:				return new MethodPtrMetadataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.Method:					return new MethodMetadataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.ParamPtr:				return new ParamPtrMetadataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.Param:					return new ParamMetadataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.InterfaceImpl:			return new InterfaceImplMetadataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.MemberRef:				return new MemberRefMetadataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.Constant:				return new ConstantMetadataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.CustomAttribute:			return new CustomAttributeMetadataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.FieldMarshal:			return new FieldMarshalMetadataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.DeclSecurity:			return new DeclSecurityMetadataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.ClassLayout:				return new ClassLayoutMetadataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.FieldLayout:				return new FieldLayoutMetadataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.StandAloneSig:			return new StandAloneSigMetadataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.EventMap:				return new EventMapMetadataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.EventPtr:				return new EventPtrMetadataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.Event:					return new EventMetadataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.PropertyMap:				return new PropertyMapMetadataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.PropertyPtr:				return new PropertyPtrMetadataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.Property:				return new PropertyMetadataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.MethodSemantics:			return new MethodSemanticsMetadataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.MethodImpl:				return new MethodImplMetadataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.ModuleRef:				return new ModuleRefMetadataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.TypeSpec:				return new TypeSpecMetadataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.ImplMap:					return new ImplMapMetadataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.FieldRVA:				return new FieldRVAMetadataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.ENCLog:					return new ENCLogMetadataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.ENCMap:					return new ENCMapMetadataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.Assembly:				return new AssemblyMetadataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.AssemblyProcessor:		return new AssemblyProcessorMetadataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.AssemblyOS:				return new AssemblyOSMetadataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.AssemblyRef:				return new AssemblyRefMetadataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.AssemblyRefProcessor:	return new AssemblyRefProcessorMetadataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.AssemblyRefOS:			return new AssemblyRefOSMetadataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.File:					return new FileMetadataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.ExportedType:			return new ExportedTypeMetadataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.ManifestResource:		return new ManifestResourceMetadataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.NestedClass:				return new NestedClassMetadataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.GenericParam:			return mdTable.Columns.Count == 5 ? (MetadataTableVM)new GenericParamMetadataTableV11VM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) : new GenericParamMetadataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.MethodSpec:				return new MethodSpecMetadataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.GenericParamConstraint:	return new GenericParamConstraintMetadataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.Document:				return new DocumentMetadataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.MethodDebugInformation:	return new MethodDebugInformationMetadataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.LocalScope:				return new LocalScopeMetadataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.LocalVariable:			return new LocalVariableMetadataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.LocalConstant:			return new LocalConstantMetadataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.ImportScope:				return new ImportScopeMetadataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.StateMachineMethod:		return new StateMachineMethodMetadataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
			case Table.CustomDebugInformation:	return new CustomDebugInformationMetadataTableVM(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan);
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
					if (obj is not null)
						obj.OnBufferChanged(changes);
					i++;
				}
			}
		}

		public MetadataTableRecordVM Get(int index) => Collection[index];
		public MetadataTableRecordVM? TryGet(int index) => Collection.TryGet(index);

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

	abstract class MetadataTable1VM : MetadataTableVM {
		public MetadataTable1VM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	abstract class MetadataTable2VM : MetadataTableVM {
		public MetadataTable2VM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	abstract class MetadataTable3VM : MetadataTableVM {
		public MetadataTable3VM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	abstract class MetadataTable4VM : MetadataTableVM {
		public MetadataTable4VM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	abstract class MetadataTable5VM : MetadataTableVM {
		public MetadataTable5VM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	abstract class MetadataTable6VM : MetadataTableVM {
		public MetadataTable6VM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	abstract class MetadataTable9VM : MetadataTableVM {
		public MetadataTable9VM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	abstract class MetadataTable1InfoVM : MetadataTable1VM {
		public override bool HasInfo => true;

		public MetadataTable1InfoVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	abstract class MetadataTable2InfoVM : MetadataTable2VM {
		public override bool HasInfo => true;

		public MetadataTable2InfoVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	abstract class MetadataTable3InfoVM : MetadataTable3VM {
		public override bool HasInfo => true;

		public MetadataTable3InfoVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	abstract class MetadataTable4InfoVM : MetadataTable4VM {
		public override bool HasInfo => true;

		public MetadataTable4InfoVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	abstract class MetadataTable5InfoVM : MetadataTable5VM {
		public override bool HasInfo => true;

		public MetadataTable5InfoVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	abstract class MetadataTable6InfoVM : MetadataTable6VM {
		public override bool HasInfo => true;

		public MetadataTable6InfoVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	abstract class MetadataTable9InfoVM : MetadataTable9VM {
		public override bool HasInfo => true;

		public MetadataTable9InfoVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class ModuleMetadataTableVM : MetadataTable5InfoVM {
		public ModuleMetadataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class TypeRefMetadataTableVM : MetadataTable3InfoVM {
		public TypeRefMetadataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class TypeDefMetadataTableVM : MetadataTable6InfoVM {
		public TypeDefMetadataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class FieldPtrMetadataTableVM : MetadataTable1VM {
		public FieldPtrMetadataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class FieldMetadataTableVM : MetadataTable3InfoVM {
		public FieldMetadataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class MethodPtrMetadataTableVM : MetadataTable1VM {
		public MethodPtrMetadataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class MethodMetadataTableVM : MetadataTable6InfoVM {
		public MethodMetadataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class ParamPtrMetadataTableVM : MetadataTable1VM {
		public ParamPtrMetadataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class ParamMetadataTableVM : MetadataTable3InfoVM {
		public ParamMetadataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class InterfaceImplMetadataTableVM : MetadataTable2VM {
		public InterfaceImplMetadataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class MemberRefMetadataTableVM : MetadataTable3InfoVM {
		public MemberRefMetadataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class ConstantMetadataTableVM : MetadataTable4VM {
		public ConstantMetadataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class CustomAttributeMetadataTableVM : MetadataTable3VM {
		public CustomAttributeMetadataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class FieldMarshalMetadataTableVM : MetadataTable2VM {
		public FieldMarshalMetadataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class DeclSecurityMetadataTableVM : MetadataTable3VM {
		public DeclSecurityMetadataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class ClassLayoutMetadataTableVM : MetadataTable3VM {
		public ClassLayoutMetadataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class FieldLayoutMetadataTableVM : MetadataTable2VM {
		public FieldLayoutMetadataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class StandAloneSigMetadataTableVM : MetadataTable1VM {
		public StandAloneSigMetadataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class EventMapMetadataTableVM : MetadataTable2VM {
		public EventMapMetadataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class EventPtrMetadataTableVM : MetadataTable1VM {
		public EventPtrMetadataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class EventMetadataTableVM : MetadataTable3InfoVM {
		public EventMetadataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class PropertyMapMetadataTableVM : MetadataTable2VM {
		public PropertyMapMetadataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class PropertyPtrMetadataTableVM : MetadataTable1VM {
		public PropertyPtrMetadataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class PropertyMetadataTableVM : MetadataTable3InfoVM {
		public PropertyMetadataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class MethodSemanticsMetadataTableVM : MetadataTable3VM {
		public MethodSemanticsMetadataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class MethodImplMetadataTableVM : MetadataTable3VM {
		public MethodImplMetadataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class ModuleRefMetadataTableVM : MetadataTable1InfoVM {
		public ModuleRefMetadataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class TypeSpecMetadataTableVM : MetadataTable1VM {
		public TypeSpecMetadataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class ImplMapMetadataTableVM : MetadataTable4InfoVM {
		public ImplMapMetadataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class FieldRVAMetadataTableVM : MetadataTable2VM {
		public FieldRVAMetadataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class ENCLogMetadataTableVM : MetadataTable2VM {
		public ENCLogMetadataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class ENCMapMetadataTableVM : MetadataTable1VM {
		public ENCMapMetadataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class AssemblyMetadataTableVM : MetadataTable9InfoVM {
		public AssemblyMetadataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class AssemblyProcessorMetadataTableVM : MetadataTable1VM {
		public AssemblyProcessorMetadataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class AssemblyOSMetadataTableVM : MetadataTable3VM {
		public AssemblyOSMetadataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class AssemblyRefMetadataTableVM : MetadataTable9InfoVM {
		public AssemblyRefMetadataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class AssemblyRefProcessorMetadataTableVM : MetadataTable2VM {
		public AssemblyRefProcessorMetadataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class AssemblyRefOSMetadataTableVM : MetadataTable4VM {
		public AssemblyRefOSMetadataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class FileMetadataTableVM : MetadataTable3InfoVM {
		public FileMetadataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class ExportedTypeMetadataTableVM : MetadataTable5InfoVM {
		public ExportedTypeMetadataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class ManifestResourceMetadataTableVM : MetadataTable4InfoVM {
		public ManifestResourceMetadataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class NestedClassMetadataTableVM : MetadataTable2VM {
		public NestedClassMetadataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class GenericParamMetadataTableV11VM : MetadataTable5InfoVM {
		public GenericParamMetadataTableV11VM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class GenericParamMetadataTableVM : MetadataTable4InfoVM {
		public GenericParamMetadataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class MethodSpecMetadataTableVM : MetadataTable2VM {
		public MethodSpecMetadataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class GenericParamConstraintMetadataTableVM : MetadataTable2VM {
		public GenericParamConstraintMetadataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class DocumentMetadataTableVM : MetadataTable4VM {
		public DocumentMetadataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class MethodDebugInformationMetadataTableVM : MetadataTable2VM {
		public MethodDebugInformationMetadataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class LocalScopeMetadataTableVM : MetadataTable6VM {
		public LocalScopeMetadataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class LocalVariableMetadataTableVM : MetadataTable3VM {
		public LocalVariableMetadataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class LocalConstantMetadataTableVM : MetadataTable2VM {
		public LocalConstantMetadataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class ImportScopeMetadataTableVM : MetadataTable2VM {
		public ImportScopeMetadataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class StateMachineMethodMetadataTableVM : MetadataTable2VM {
		public StateMachineMethodMetadataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}

	sealed class CustomDebugInformationMetadataTableVM : MetadataTable3VM {
		public CustomDebugInformationMetadataTableVM(HexBuffer buffer, TablesStreamVM tablesStream, MDTable mdTable, HexSpan stringsHeapSpan, HexSpan guidHeapSpan)
			: base(buffer, tablesStream, mdTable, stringsHeapSpan, guidHeapSpan) {
		}
	}
}
