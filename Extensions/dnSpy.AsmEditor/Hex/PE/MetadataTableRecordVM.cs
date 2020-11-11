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
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Files.DotNet;
using dnSpy.Contracts.Utilities;

namespace dnSpy.AsmEditor.Hex.PE {
	abstract class MetadataTableRecordVM : HexVM, IVirtualizedListItem {
		public int Index => (int)mdToken.Rid - 1;
		public override string Name => $"{mdToken.Table}[{mdToken.Rid:X6}]";
		public string OffsetString => $"0x{Span.Start.ToUInt64():X8}";
		public MDToken Token => mdToken;
		public override IEnumerable<HexField> HexFields => hexFields;
		public HexField? Column0 => GetField(0);
		public HexField? Column1 => GetField(1);
		public HexField? Column2 => GetField(2);
		public HexField? Column3 => GetField(3);
		public HexField? Column4 => GetField(4);
		public HexField? Column5 => GetField(5);
		public HexField? Column6 => GetField(6);
		public HexField? Column7 => GetField(7);
		public HexField? Column8 => GetField(8);

		public HexField? GetField(int index) {
			if ((uint)index < (uint)hexFields.Length)
				return hexFields[index];
			return null;
		}

		public bool Column0Present => IsFieldPresent(0);
		public bool Column1Present => IsFieldPresent(1);
		public bool Column2Present => IsFieldPresent(2);
		public bool Column3Present => IsFieldPresent(3);
		public bool Column4Present => IsFieldPresent(4);
		public bool Column5Present => IsFieldPresent(5);
		public bool Column6Present => IsFieldPresent(6);
		public bool Column7Present => IsFieldPresent(7);
		public bool Column8Present => IsFieldPresent(8);
		public bool IsFieldPresent(int index) => (uint)index < (uint)hexFields.Length;
		public string Column0Description => GetFieldDescription(0);
		public string Column1Description => GetFieldDescription(1);
		public string Column2Description => GetFieldDescription(2);
		public string Column3Description => GetFieldDescription(3);
		public string Column4Description => GetFieldDescription(4);
		public string Column5Description => GetFieldDescription(5);
		public string Column6Description => GetFieldDescription(6);
		public string Column7Description => GetFieldDescription(7);
		public string Column8Description => GetFieldDescription(8);

		public string GetFieldDescription(int index) {
			if ((uint)index >= (uint)hexFields.Length)
				return string.Empty;

			var col = mdVM.TableInfo.Columns[index];
			var field = hexFields[index];

			switch (col.ColumnSize) {
			case ColumnSize.Module:
			case ColumnSize.TypeRef:
			case ColumnSize.TypeDef:
			case ColumnSize.FieldPtr:
			case ColumnSize.Field:
			case ColumnSize.MethodPtr:
			case ColumnSize.Method:
			case ColumnSize.ParamPtr:
			case ColumnSize.Param:
			case ColumnSize.InterfaceImpl:
			case ColumnSize.MemberRef:
			case ColumnSize.Constant:
			case ColumnSize.CustomAttribute:
			case ColumnSize.FieldMarshal:
			case ColumnSize.DeclSecurity:
			case ColumnSize.ClassLayout:
			case ColumnSize.FieldLayout:
			case ColumnSize.StandAloneSig:
			case ColumnSize.EventMap:
			case ColumnSize.EventPtr:
			case ColumnSize.Event:
			case ColumnSize.PropertyMap:
			case ColumnSize.PropertyPtr:
			case ColumnSize.Property:
			case ColumnSize.MethodSemantics:
			case ColumnSize.MethodImpl:
			case ColumnSize.ModuleRef:
			case ColumnSize.TypeSpec:
			case ColumnSize.ImplMap:
			case ColumnSize.FieldRVA:
			case ColumnSize.ENCLog:
			case ColumnSize.ENCMap:
			case ColumnSize.Assembly:
			case ColumnSize.AssemblyProcessor:
			case ColumnSize.AssemblyOS:
			case ColumnSize.AssemblyRef:
			case ColumnSize.AssemblyRefProcessor:
			case ColumnSize.AssemblyRefOS:
			case ColumnSize.File:
			case ColumnSize.ExportedType:
			case ColumnSize.ManifestResource:
			case ColumnSize.NestedClass:
			case ColumnSize.GenericParam:
			case ColumnSize.MethodSpec:
			case ColumnSize.GenericParamConstraint:
			case ColumnSize.Document:
			case ColumnSize.MethodDebugInformation:
			case ColumnSize.LocalScope:
			case ColumnSize.LocalVariable:
			case ColumnSize.LocalConstant:
			case ColumnSize.ImportScope:
			case ColumnSize.StateMachineMethod:
			case ColumnSize.CustomDebugInformation:
				return GetDescription(Table.Module + (col.ColumnSize - ColumnSize.Module), field);

			case ColumnSize.Byte: return "Byte";
			case ColumnSize.Int16: return "Int16";
			case ColumnSize.UInt16: return "UInt16";
			case ColumnSize.Int32: return "Int32";
			case ColumnSize.UInt32: return "UInt32";

			case ColumnSize.Strings: return GetStringsDescription(field);
			case ColumnSize.GUID: return GetGuidDescription(field);
			case ColumnSize.Blob: return "#Blob Heap Offset";

			case ColumnSize.TypeDefOrRef: return GetCodedTokenDescription(CodedToken.TypeDefOrRef, "TypeDefOrRef", col, field);
			case ColumnSize.HasConstant: return GetCodedTokenDescription(CodedToken.HasConstant, "HasConstant", col, field);
			case ColumnSize.HasCustomAttribute: return GetCodedTokenDescription(CodedToken.HasCustomAttribute, "HasCustomAttribute", col, field);
			case ColumnSize.HasFieldMarshal: return GetCodedTokenDescription(CodedToken.HasFieldMarshal, "HasFieldMarshal", col, field);
			case ColumnSize.HasDeclSecurity: return GetCodedTokenDescription(CodedToken.HasDeclSecurity, "HasDeclSecurity", col, field);
			case ColumnSize.MemberRefParent: return GetCodedTokenDescription(CodedToken.MemberRefParent, "MemberRefParent", col, field);
			case ColumnSize.HasSemantic: return GetCodedTokenDescription(CodedToken.HasSemantic, "HasSemantic", col, field);
			case ColumnSize.MethodDefOrRef: return GetCodedTokenDescription(CodedToken.MethodDefOrRef, "MethodDefOrRef", col, field);
			case ColumnSize.MemberForwarded: return GetCodedTokenDescription(CodedToken.MemberForwarded, "MemberForwarded", col, field);
			case ColumnSize.Implementation: return GetCodedTokenDescription(CodedToken.Implementation, "Implementation", col, field);
			case ColumnSize.CustomAttributeType: return GetCodedTokenDescription(CodedToken.CustomAttributeType, "CustomAttributeType", col, field);
			case ColumnSize.ResolutionScope: return GetCodedTokenDescription(CodedToken.ResolutionScope, "ResolutionScope", col, field);
			case ColumnSize.TypeOrMethodDef: return GetCodedTokenDescription(CodedToken.TypeOrMethodDef, "TypeOrMethodDef", col, field);
			case ColumnSize.HasCustomDebugInformation: return GetCodedTokenDescription(CodedToken.HasCustomDebugInformation, "HasCustomDebugInformation", col, field);
			default:
				Debug.Fail($"Unknown ColumnSize: {col.ColumnSize}");
				return string.Empty;
			}
		}

		protected string ReadStringsHeap(HexField field) {
			var s = SimpleTypeConverter.ToString(mdVM.ReadStringsHeap(ReadFieldValue(field)), false);
			Debug.Assert(s.Length >= 2);
			if (s.Length < 2)
				return s;
			return s.Substring(1, s.Length - 2);
		}

		Guid? ReadGuidHeap(HexField field) => mdVM.ReadGuidHeap(ReadFieldValue(field));

		MetadataTableRecordVM? GetMetadataTableRecordVM(Table table, uint rid) {
			if (rid == 0)
				return null;
			var tblVM = mdVM.TablesStream.TryGetMetadataTable(table);
			if (tblVM is null)
				return null;
			if (rid - 1 >= (uint)tblVM.Collection.Count)
				return null;
			return tblVM.Get((int)(rid - 1));
		}

		string GetStringsDescription(HexField field) {
			var s = ReadStringsHeap(field);
			if (!string.IsNullOrEmpty(s))
				return $"{s} (#Strings Heap Offset)";
			return "#Strings Heap Offset";
		}

		string GetGuidDescription(HexField field) {
			var g = ReadGuidHeap(field);
			if (g is not null)
				return $"{g.Value.ToString()} (#GUID Heap Index)";
			return "#GUID Heap Index";
		}

		string GetInfo(Table table, uint rid) {
			var recVM = GetMetadataTableRecordVM(table, rid);
			return recVM?.Info ?? string.Empty;
		}

		string GetDescription(Table table, HexField field) {
			var info = GetInfo(table, ReadFieldValue(field));
			if (string.IsNullOrEmpty(info))
				return $"{table} RID";
			return $"{info} ({table} RID)";
		}

		string GetCodedTokenDescription(CodedToken codedToken, string codedTokenName, ColumnInfo col, HexField field) {
			if (!codedToken.Decode(ReadFieldValue(field), out MDToken token))
				return $"Invalid {codedTokenName} Coded Token";

			var info = GetInfo(token.Table, token.Rid);
			if (string.IsNullOrEmpty(info))
				return $"{codedTokenName}: {token.Table}[{token.Rid}], 0x{token.Raw:X8}";
			return $"{info} ({codedTokenName}: {token.Table}[{token.Rid}], 0x{token.Raw:X8})";
		}

		uint ReadFieldValue(HexField field) {
			if (field.Size == 2)
				return mdVM.Buffer.ReadUInt16(field.Span.Start);
			else if (field.Size == 4)
				return mdVM.Buffer.ReadUInt32(field.Span.Start);
			return 0;
		}

		bool IsDynamicDescription(int index) {
			if ((uint)index >= (uint)hexFields.Length)
				return false;

			var col = mdVM.TableInfo.Columns[index];
			switch (col.ColumnSize) {
			case ColumnSize.Module:
			case ColumnSize.TypeRef:
			case ColumnSize.TypeDef:
			case ColumnSize.FieldPtr:
			case ColumnSize.Field:
			case ColumnSize.MethodPtr:
			case ColumnSize.Method:
			case ColumnSize.ParamPtr:
			case ColumnSize.Param:
			case ColumnSize.InterfaceImpl:
			case ColumnSize.MemberRef:
			case ColumnSize.Constant:
			case ColumnSize.CustomAttribute:
			case ColumnSize.FieldMarshal:
			case ColumnSize.DeclSecurity:
			case ColumnSize.ClassLayout:
			case ColumnSize.FieldLayout:
			case ColumnSize.StandAloneSig:
			case ColumnSize.EventMap:
			case ColumnSize.EventPtr:
			case ColumnSize.Event:
			case ColumnSize.PropertyMap:
			case ColumnSize.PropertyPtr:
			case ColumnSize.Property:
			case ColumnSize.MethodSemantics:
			case ColumnSize.MethodImpl:
			case ColumnSize.ModuleRef:
			case ColumnSize.TypeSpec:
			case ColumnSize.ImplMap:
			case ColumnSize.FieldRVA:
			case ColumnSize.ENCLog:
			case ColumnSize.ENCMap:
			case ColumnSize.Assembly:
			case ColumnSize.AssemblyProcessor:
			case ColumnSize.AssemblyOS:
			case ColumnSize.AssemblyRef:
			case ColumnSize.AssemblyRefProcessor:
			case ColumnSize.AssemblyRefOS:
			case ColumnSize.File:
			case ColumnSize.ExportedType:
			case ColumnSize.ManifestResource:
			case ColumnSize.NestedClass:
			case ColumnSize.GenericParam:
			case ColumnSize.MethodSpec:
			case ColumnSize.GenericParamConstraint:

			case ColumnSize.Strings:

			case ColumnSize.TypeDefOrRef:
			case ColumnSize.HasConstant:
			case ColumnSize.HasCustomAttribute:
			case ColumnSize.HasFieldMarshal:
			case ColumnSize.HasDeclSecurity:
			case ColumnSize.MemberRefParent:
			case ColumnSize.HasSemantic:
			case ColumnSize.MethodDefOrRef:
			case ColumnSize.MemberForwarded:
			case ColumnSize.Implementation:
			case ColumnSize.CustomAttributeType:
			case ColumnSize.ResolutionScope:
			case ColumnSize.TypeOrMethodDef:
			case ColumnSize.HasCustomDebugInformation:
				return true;

			case ColumnSize.Byte:
			case ColumnSize.Int16:
			case ColumnSize.UInt16:
			case ColumnSize.Int32:
			case ColumnSize.UInt32:
			case ColumnSize.GUID:
			case ColumnSize.Blob:
			default:
				return false;
			}
		}

		void InvalidateDescription(int index) => OnPropertyChanged($"Column{index}Description");
		public string RidString => mdToken.Rid.ToString();
		public string TokenString => $"0x{mdToken.Raw:X8}";
		public virtual string Info => string.Empty;
		protected virtual int[]? InfoColumnIndexes => null;

		protected readonly MetadataTableVM mdVM;
		readonly MDToken mdToken;
		readonly HexField[] hexFields;

		protected MetadataTableRecordVM(MetadataTableVM mdVM, MDToken mdToken)
			: base(new HexSpan(mdVM.Span.Start + (mdToken.Rid - 1) * (ulong)mdVM.TableInfo.RowSize, (ulong)mdVM.TableInfo.RowSize)) {
			this.mdVM = mdVM;
			this.mdToken = mdToken;
			hexFields = new HexField[mdVM.TableInfo.Columns.Count];
			for (int i = 0; i < hexFields.Length; i++)
				hexFields[i] = CreateField(mdVM.TableInfo.Columns[i]);
		}

		protected virtual HexField CreateField(ColumnInfo colInfo) {
			switch (colInfo.ColumnSize) {
			case ColumnSize.Int16: return new Int16HexField(mdVM.Buffer, Name, colInfo.Name, Span.Start + (uint)colInfo.Offset);
			case ColumnSize.Int32: return new Int32HexField(mdVM.Buffer, Name, colInfo.Name, Span.Start + (uint)colInfo.Offset);
			}

			switch (colInfo.Size) {
			case 1: return new ByteHexField(mdVM.Buffer, Name, colInfo.Name, Span.Start + (uint)colInfo.Offset);
			case 2: return new UInt16HexField(mdVM.Buffer, Name, colInfo.Name, Span.Start + (uint)colInfo.Offset);
			case 4: return new UInt32HexField(mdVM.Buffer, Name, colInfo.Name, Span.Start + (uint)colInfo.Offset);
			default: throw new InvalidOperationException();
			}
		}

		public override void OnBufferChanged(NormalizedHexChangeCollection changes) {
			base.OnBufferChanged(changes);
			for (int i = 0; i < hexFields.Length; i++) {
				var field = hexFields[i];
				if (IsDynamicDescription(i) && changes.OverlapsWith(field.Span))
					InvalidateDescription(i);
			}
			var infoCols = InfoColumnIndexes;
			if (infoCols is not null) {
				foreach (var index in infoCols) {
					var field = hexFields[index];
					if (changes.OverlapsWith(field.Span)) {
						OnPropertyChanged(nameof(Info));
						break;
					}
				}
			}
		}
	}

	sealed class ModuleMetadataTableRecordVM : MetadataTableRecordVM {
		public ModuleMetadataTableRecordVM(MetadataTableVM mdVM, MDToken mdToken)
			: base(mdVM, mdToken) {
		}

		public override string Info => ReadStringsHeap(Column1!);
		protected override int[]? InfoColumnIndexes => infoColIndexes;
		static readonly int[] infoColIndexes = new int[] { 1 };
	}

	sealed class TypeRefMetadataTableRecordVM : MetadataTableRecordVM {
		public TypeRefMetadataTableRecordVM(MetadataTableVM mdVM, MDToken mdToken)
			: base(mdVM, mdToken) {
		}

		public override string Info => CreateTypeString(ReadStringsHeap(Column2!), ReadStringsHeap(Column1!));
		protected override int[]? InfoColumnIndexes => infoColIndexes;
		static readonly int[] infoColIndexes = new int[] { 1, 2 };
		public static string CreateTypeString(string ns, string name) => string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}";
	}

	sealed class TypeDefMetadataTableRecordVM : MetadataTableRecordVM {
		public TypeDefMetadataTableRecordVM(MetadataTableVM mdVM, MDToken mdToken)
			: base(mdVM, mdToken) {
		}

		static readonly IntegerHexBitFieldEnumInfo[] VisibilityInfos = new IntegerHexBitFieldEnumInfo[] {
			new IntegerHexBitFieldEnumInfo(0, "NotPublic"),
			new IntegerHexBitFieldEnumInfo(1, "Public"),
			new IntegerHexBitFieldEnumInfo(2, "Nested Public"),
			new IntegerHexBitFieldEnumInfo(3, "Nested Private"),
			new IntegerHexBitFieldEnumInfo(4, "Nested Family"),
			new IntegerHexBitFieldEnumInfo(5, "Nested Assembly"),
			new IntegerHexBitFieldEnumInfo(6, "Nested Family and Assembly"),
			new IntegerHexBitFieldEnumInfo(7, "Nested Family or Assembly"),
		};

		static readonly IntegerHexBitFieldEnumInfo[] LayoutInfos = new IntegerHexBitFieldEnumInfo[] {
			new IntegerHexBitFieldEnumInfo(0, "Auto"),
			new IntegerHexBitFieldEnumInfo(1, "Sequential"),
			new IntegerHexBitFieldEnumInfo(2, "Explicit"),
		};

		static readonly IntegerHexBitFieldEnumInfo[] SemanticsInfos = new IntegerHexBitFieldEnumInfo[] {
			new IntegerHexBitFieldEnumInfo(0, "Class"),
			new IntegerHexBitFieldEnumInfo(1, "Interface"),
		};

		static readonly IntegerHexBitFieldEnumInfo[] StringFormatInfos = new IntegerHexBitFieldEnumInfo[] {
			new IntegerHexBitFieldEnumInfo(0, "Ansi"),
			new IntegerHexBitFieldEnumInfo(1, "Unicode"),
			new IntegerHexBitFieldEnumInfo(2, "Auto"),
			new IntegerHexBitFieldEnumInfo(3, "CustomFormat"),
		};

		static readonly IntegerHexBitFieldEnumInfo[] CustomFormatInfos = new IntegerHexBitFieldEnumInfo[] {
			new IntegerHexBitFieldEnumInfo(0, "Value0"),
			new IntegerHexBitFieldEnumInfo(1, "Value1"),
			new IntegerHexBitFieldEnumInfo(2, "Value2"),
			new IntegerHexBitFieldEnumInfo(3, "Value3"),
		};

		protected override HexField CreateField(ColumnInfo colInfo) {
			if (colInfo.Index == 0)
				return CreateTypeAttributesField(colInfo, mdVM.Buffer, Name, Span.Start);
			return base.CreateField(colInfo);
		}

		internal static UInt32FlagsHexField CreateTypeAttributesField(ColumnInfo colInfo, HexBuffer buffer, string name, HexPosition startOffset) {
			var field = new UInt32FlagsHexField(buffer, name, colInfo.Name, startOffset + (uint)colInfo.Offset);
			field.Add(new IntegerHexBitField("Visibility", 0, 3, VisibilityInfos));
			field.Add(new IntegerHexBitField("Layout", 3, 2, LayoutInfos));
			field.Add(new IntegerHexBitField("Semantics", 5, 1, SemanticsInfos));
			field.Add(new BooleanHexBitField("Abstract", 7));
			field.Add(new BooleanHexBitField("Sealed", 8));
			field.Add(new BooleanHexBitField("SpecialName", 10));
			field.Add(new BooleanHexBitField("RTSpecialName", 11));
			field.Add(new BooleanHexBitField("Import", 12));
			field.Add(new BooleanHexBitField("Serializable", 13));
			field.Add(new BooleanHexBitField("WindowsRuntime", 14));
			field.Add(new IntegerHexBitField("String", 16, 2, StringFormatInfos));
			field.Add(new BooleanHexBitField("HasSecurity", 18));
			field.Add(new BooleanHexBitField("BeforeFieldInit", 20));
			field.Add(new BooleanHexBitField("Forwarder", 21));
			field.Add(new IntegerHexBitField("Custom", 22, 2, CustomFormatInfos));
			return field;
		}

		public override string Info => TypeRefMetadataTableRecordVM.CreateTypeString(ReadStringsHeap(Column2!), ReadStringsHeap(Column1!));
		protected override int[]? InfoColumnIndexes => infoColIndexes;
		static readonly int[] infoColIndexes = new int[] { 1, 2 };
	}

	sealed class FieldPtrMetadataTableRecordVM : MetadataTableRecordVM {
		public FieldPtrMetadataTableRecordVM(MetadataTableVM mdVM, MDToken mdToken)
			: base(mdVM, mdToken) {
		}
	}

	sealed class FieldMetadataTableRecordVM : MetadataTableRecordVM {
		public FieldMetadataTableRecordVM(MetadataTableVM mdVM, MDToken mdToken)
			: base(mdVM, mdToken) {
		}

		static readonly IntegerHexBitFieldEnumInfo[] AccessInfos = new IntegerHexBitFieldEnumInfo[] {
			new IntegerHexBitFieldEnumInfo(0, "PrivateScope"),
			new IntegerHexBitFieldEnumInfo(1, "Private"),
			new IntegerHexBitFieldEnumInfo(2, "Family and Assembly"),
			new IntegerHexBitFieldEnumInfo(3, "Assembly"),
			new IntegerHexBitFieldEnumInfo(4, "Family"),
			new IntegerHexBitFieldEnumInfo(5, "Family or Assembly"),
			new IntegerHexBitFieldEnumInfo(6, "Public"),
		};

		protected override HexField CreateField(ColumnInfo colInfo) {
			if (colInfo.Index == 0) {
				var field = new UInt16FlagsHexField(mdVM.Buffer, Name, colInfo.Name, Span.Start + (uint)colInfo.Offset);
				field.Add(new IntegerHexBitField("Access", 0, 3, AccessInfos));
				field.Add(new BooleanHexBitField("Static", 4));
				field.Add(new BooleanHexBitField("InitOnly", 5));
				field.Add(new BooleanHexBitField("Literal", 6));
				field.Add(new BooleanHexBitField("NotSerialized", 7));
				field.Add(new BooleanHexBitField("HasFieldRVA", 8));
				field.Add(new BooleanHexBitField("SpecialName", 9));
				field.Add(new BooleanHexBitField("RTSpecialName", 10));
				field.Add(new BooleanHexBitField("HasFieldMarshal", 12));
				field.Add(new BooleanHexBitField("PinvokeImpl", 13));
				field.Add(new BooleanHexBitField("HasDefault", 15));
				return field;
			}
			return base.CreateField(colInfo);
		}

		public override string Info => ReadStringsHeap(Column1!);
		protected override int[]? InfoColumnIndexes => infoColIndexes;
		static readonly int[] infoColIndexes = new int[] { 1 };
	}

	sealed class MethodPtrMetadataTableRecordVM : MetadataTableRecordVM {
		public MethodPtrMetadataTableRecordVM(MetadataTableVM mdVM, MDToken mdToken)
			: base(mdVM, mdToken) {
		}
	}

	sealed class MethodMetadataTableRecordVM : MetadataTableRecordVM {
		public MethodMetadataTableRecordVM(MetadataTableVM mdVM, MDToken mdToken)
			: base(mdVM, mdToken) {
		}

		static readonly IntegerHexBitFieldEnumInfo[] CodeTypeInfos = new IntegerHexBitFieldEnumInfo[] {
			new IntegerHexBitFieldEnumInfo(0, "IL"),
			new IntegerHexBitFieldEnumInfo(1, "Native"),
			new IntegerHexBitFieldEnumInfo(2, "OPTIL"),
			new IntegerHexBitFieldEnumInfo(3, "Runtime"),
		};

		static readonly IntegerHexBitFieldEnumInfo[] ManagedInfos = new IntegerHexBitFieldEnumInfo[] {
			new IntegerHexBitFieldEnumInfo(0, "Managed"),
			new IntegerHexBitFieldEnumInfo(1, "Unmanaged"),
		};

		static readonly IntegerHexBitFieldEnumInfo[] AccessInfos = new IntegerHexBitFieldEnumInfo[] {
			new IntegerHexBitFieldEnumInfo(0, "PrivateScope"),
			new IntegerHexBitFieldEnumInfo(1, "Private"),
			new IntegerHexBitFieldEnumInfo(2, "Family and Assembly"),
			new IntegerHexBitFieldEnumInfo(3, "Assembly"),
			new IntegerHexBitFieldEnumInfo(4, "Family"),
			new IntegerHexBitFieldEnumInfo(5, "Family or Assembly"),
			new IntegerHexBitFieldEnumInfo(6, "Public"),
		};

		static readonly IntegerHexBitFieldEnumInfo[] VtableLayoutInfos = new IntegerHexBitFieldEnumInfo[] {
			new IntegerHexBitFieldEnumInfo(0, "ReuseSlot"),
			new IntegerHexBitFieldEnumInfo(1, "NewSlot"),
		};

		protected override HexField CreateField(ColumnInfo colInfo) {
			if (colInfo.Index == 1) {
				var field = new UInt16FlagsHexField(mdVM.Buffer, Name, colInfo.Name, Span.Start + (uint)colInfo.Offset);
				field.Add(new IntegerHexBitField("CodeType", 0, 2, CodeTypeInfos));
				field.Add(new IntegerHexBitField("ManagedType", 2, 1, ManagedInfos));
				field.Add(new BooleanHexBitField("NoInlining", 3));
				field.Add(new BooleanHexBitField("ForwardRef", 4));
				field.Add(new BooleanHexBitField("Synchronized", 5));
				field.Add(new BooleanHexBitField("NoOptimization", 6));
				field.Add(new BooleanHexBitField("PreserveSig", 7));
				field.Add(new BooleanHexBitField("AggressiveInlining", 8));
				field.Add(new BooleanHexBitField("AggressiveOptimization", 9));
				field.Add(new BooleanHexBitField("SecurityMitigations", 10));
				field.Add(new BooleanHexBitField("InternalCall", 12));
				return field;
			}
			else if (colInfo.Index == 2) {
				var field = new UInt16FlagsHexField(mdVM.Buffer, Name, colInfo.Name, Span.Start + (uint)colInfo.Offset);
				field.Add(new IntegerHexBitField("Access", 0, 3, AccessInfos));
				field.Add(new BooleanHexBitField("UnmanagedExport", 3));
				field.Add(new BooleanHexBitField("Static", 4));
				field.Add(new BooleanHexBitField("Final", 5));
				field.Add(new BooleanHexBitField("Virtual", 6));
				field.Add(new BooleanHexBitField("HideBySig", 7));
				field.Add(new IntegerHexBitField("VtableLayout", 8, 1, VtableLayoutInfos));
				field.Add(new BooleanHexBitField("CheckAccessOnOverride", 9));
				field.Add(new BooleanHexBitField("Abstract", 10));
				field.Add(new BooleanHexBitField("SpecialName", 11));
				field.Add(new BooleanHexBitField("RTSpecialName", 12));
				field.Add(new BooleanHexBitField("PinvokeImpl", 13));
				field.Add(new BooleanHexBitField("HasSecurity", 14));
				field.Add(new BooleanHexBitField("RequireSecObject", 15));
				return field;
			}
			return base.CreateField(colInfo);
		}

		public override string Info => ReadStringsHeap(Column3!);
		protected override int[]? InfoColumnIndexes => infoColIndexes;
		static readonly int[] infoColIndexes = new int[] { 3 };
	}

	sealed class ParamPtrMetadataTableRecordVM : MetadataTableRecordVM {
		public ParamPtrMetadataTableRecordVM(MetadataTableVM mdVM, MDToken mdToken)
			: base(mdVM, mdToken) {
		}
	}

	sealed class ParamMetadataTableRecordVM : MetadataTableRecordVM {
		public ParamMetadataTableRecordVM(MetadataTableVM mdVM, MDToken mdToken)
			: base(mdVM, mdToken) {
		}

		protected override HexField CreateField(ColumnInfo colInfo) {
			if (colInfo.Index == 0) {
				var field = new UInt16FlagsHexField(mdVM.Buffer, Name, colInfo.Name, Span.Start + (uint)colInfo.Offset);
				field.Add(new BooleanHexBitField("In", 0));
				field.Add(new BooleanHexBitField("Out", 1));
				field.Add(new BooleanHexBitField("Lcid", 2));
				field.Add(new BooleanHexBitField("Retval", 3));
				field.Add(new BooleanHexBitField("Optional", 4));
				field.Add(new BooleanHexBitField("HasDefault", 12));
				field.Add(new BooleanHexBitField("HasFieldMarshal", 13));
				return field;
			}
			return base.CreateField(colInfo);
		}

		public override string Info => ReadStringsHeap(Column2!);
		protected override int[]? InfoColumnIndexes => infoColIndexes;
		static readonly int[] infoColIndexes = new int[] { 2 };
	}

	sealed class InterfaceImplMetadataTableRecordVM : MetadataTableRecordVM {
		public InterfaceImplMetadataTableRecordVM(MetadataTableVM mdVM, MDToken mdToken)
			: base(mdVM, mdToken) {
		}
	}

	sealed class MemberRefMetadataTableRecordVM : MetadataTableRecordVM {
		public MemberRefMetadataTableRecordVM(MetadataTableVM mdVM, MDToken mdToken)
			: base(mdVM, mdToken) {
		}

		public override string Info => ReadStringsHeap(Column1!);
		protected override int[]? InfoColumnIndexes => infoColIndexes;
		static readonly int[] infoColIndexes = new int[] { 1 };
	}

	sealed class ConstantMetadataTableRecordVM : MetadataTableRecordVM {
		public ConstantMetadataTableRecordVM(MetadataTableVM mdVM, MDToken mdToken)
			: base(mdVM, mdToken) {
		}
	}

	sealed class CustomAttributeMetadataTableRecordVM : MetadataTableRecordVM {
		public CustomAttributeMetadataTableRecordVM(MetadataTableVM mdVM, MDToken mdToken)
			: base(mdVM, mdToken) {
		}
	}

	sealed class FieldMarshalMetadataTableRecordVM : MetadataTableRecordVM {
		public FieldMarshalMetadataTableRecordVM(MetadataTableVM mdVM, MDToken mdToken)
			: base(mdVM, mdToken) {
		}
	}

	sealed class DeclSecurityMetadataTableRecordVM : MetadataTableRecordVM {
		public DeclSecurityMetadataTableRecordVM(MetadataTableVM mdVM, MDToken mdToken)
			: base(mdVM, mdToken) {
		}

		static readonly IntegerHexBitFieldEnumInfo[] SecurityActionInfos = new IntegerHexBitFieldEnumInfo[] {
			new IntegerHexBitFieldEnumInfo(0x0000, "ActionNil"),
			new IntegerHexBitFieldEnumInfo(0x0001, "Request"),
			new IntegerHexBitFieldEnumInfo(0x0002, "Demand"),
			new IntegerHexBitFieldEnumInfo(0x0003, "Assert"),
			new IntegerHexBitFieldEnumInfo(0x0004, "Deny"),
			new IntegerHexBitFieldEnumInfo(0x0005, "PermitOnly"),
			new IntegerHexBitFieldEnumInfo(0x0006, "LinktimeCheck"),
			new IntegerHexBitFieldEnumInfo(0x0007, "InheritanceCheck"),
			new IntegerHexBitFieldEnumInfo(0x0008, "RequestMinimum"),
			new IntegerHexBitFieldEnumInfo(0x0009, "RequestOptional"),
			new IntegerHexBitFieldEnumInfo(0x000A, "RequestRefuse"),
			new IntegerHexBitFieldEnumInfo(0x000B, "PrejitGrant"),
			new IntegerHexBitFieldEnumInfo(0x000C, "PrejitDenied"),
			new IntegerHexBitFieldEnumInfo(0x000D, "NonCasDemand"),
			new IntegerHexBitFieldEnumInfo(0x000E, "NonCasLinkDemand"),
			new IntegerHexBitFieldEnumInfo(0x000F, "NonCasInheritance"),
		};

		protected override HexField CreateField(ColumnInfo colInfo) {
			if (colInfo.Index == 0) {
				var field = new UInt16FlagsHexField(mdVM.Buffer, Name, colInfo.Name, Span.Start + (uint)colInfo.Offset);
				field.Add(new IntegerHexBitField("Action", 0, 16, SecurityActionInfos));
				return field;
			}
			return base.CreateField(colInfo);
		}
	}

	sealed class ClassLayoutMetadataTableRecordVM : MetadataTableRecordVM {
		public ClassLayoutMetadataTableRecordVM(MetadataTableVM mdVM, MDToken mdToken)
			: base(mdVM, mdToken) {
		}
	}

	sealed class FieldLayoutMetadataTableRecordVM : MetadataTableRecordVM {
		public FieldLayoutMetadataTableRecordVM(MetadataTableVM mdVM, MDToken mdToken)
			: base(mdVM, mdToken) {
		}
	}

	sealed class StandAloneSigMetadataTableRecordVM : MetadataTableRecordVM {
		public StandAloneSigMetadataTableRecordVM(MetadataTableVM mdVM, MDToken mdToken)
			: base(mdVM, mdToken) {
		}
	}

	sealed class EventMapMetadataTableRecordVM : MetadataTableRecordVM {
		public EventMapMetadataTableRecordVM(MetadataTableVM mdVM, MDToken mdToken)
			: base(mdVM, mdToken) {
		}
	}

	sealed class EventPtrMetadataTableRecordVM : MetadataTableRecordVM {
		public EventPtrMetadataTableRecordVM(MetadataTableVM mdVM, MDToken mdToken)
			: base(mdVM, mdToken) {
		}
	}

	sealed class EventMetadataTableRecordVM : MetadataTableRecordVM {
		public EventMetadataTableRecordVM(MetadataTableVM mdVM, MDToken mdToken)
			: base(mdVM, mdToken) {
		}

		protected override HexField CreateField(ColumnInfo colInfo) {
			if (colInfo.Index == 0) {
				var field = new UInt16FlagsHexField(mdVM.Buffer, Name, colInfo.Name, Span.Start + (uint)colInfo.Offset);
				field.Add(new BooleanHexBitField("SpecialName", 9));
				field.Add(new BooleanHexBitField("RTSpecialName", 10));
				return field;
			}
			return base.CreateField(colInfo);
		}

		public override string Info => ReadStringsHeap(Column1!);
		protected override int[]? InfoColumnIndexes => infoColIndexes;
		static readonly int[] infoColIndexes = new int[] { 1 };
	}

	sealed class PropertyMapMetadataTableRecordVM : MetadataTableRecordVM {
		public PropertyMapMetadataTableRecordVM(MetadataTableVM mdVM, MDToken mdToken)
			: base(mdVM, mdToken) {
		}
	}

	sealed class PropertyPtrMetadataTableRecordVM : MetadataTableRecordVM {
		public PropertyPtrMetadataTableRecordVM(MetadataTableVM mdVM, MDToken mdToken)
			: base(mdVM, mdToken) {
		}
	}

	sealed class PropertyMetadataTableRecordVM : MetadataTableRecordVM {
		public PropertyMetadataTableRecordVM(MetadataTableVM mdVM, MDToken mdToken)
			: base(mdVM, mdToken) {
		}

		protected override HexField CreateField(ColumnInfo colInfo) {
			if (colInfo.Index == 0) {
				var field = new UInt16FlagsHexField(mdVM.Buffer, Name, colInfo.Name, Span.Start + (uint)colInfo.Offset);
				field.Add(new BooleanHexBitField("SpecialName", 9));
				field.Add(new BooleanHexBitField("RTSpecialName", 10));
				field.Add(new BooleanHexBitField("HasDefault", 12));
				return field;
			}
			return base.CreateField(colInfo);
		}

		public override string Info => ReadStringsHeap(Column1!);
		protected override int[]? InfoColumnIndexes => infoColIndexes;
		static readonly int[] infoColIndexes = new int[] { 1 };
	}

	sealed class MethodSemanticsMetadataTableRecordVM : MetadataTableRecordVM {
		public MethodSemanticsMetadataTableRecordVM(MetadataTableVM mdVM, MDToken mdToken)
			: base(mdVM, mdToken) {
		}

		protected override HexField CreateField(ColumnInfo colInfo) {
			if (colInfo.Index == 0) {
				var field = new UInt16FlagsHexField(mdVM.Buffer, Name, colInfo.Name, Span.Start + (uint)colInfo.Offset);
				field.Add(new BooleanHexBitField("Setter", 0));
				field.Add(new BooleanHexBitField("Getter", 1));
				field.Add(new BooleanHexBitField("Other", 2));
				field.Add(new BooleanHexBitField("AddOn", 3));
				field.Add(new BooleanHexBitField("RemoveOn", 4));
				field.Add(new BooleanHexBitField("Fire", 5));
				return field;
			}
			return base.CreateField(colInfo);
		}
	}

	sealed class MethodImplMetadataTableRecordVM : MetadataTableRecordVM {
		public MethodImplMetadataTableRecordVM(MetadataTableVM mdVM, MDToken mdToken)
			: base(mdVM, mdToken) {
		}
	}

	sealed class ModuleRefMetadataTableRecordVM : MetadataTableRecordVM {
		public ModuleRefMetadataTableRecordVM(MetadataTableVM mdVM, MDToken mdToken)
			: base(mdVM, mdToken) {
		}

		public override string Info => ReadStringsHeap(Column0!);
		protected override int[]? InfoColumnIndexes => infoColIndexes;
		static readonly int[] infoColIndexes = new int[] { 0 };
	}

	sealed class TypeSpecMetadataTableRecordVM : MetadataTableRecordVM {
		public TypeSpecMetadataTableRecordVM(MetadataTableVM mdVM, MDToken mdToken)
			: base(mdVM, mdToken) {
		}
	}

	sealed class ImplMapMetadataTableRecordVM : MetadataTableRecordVM {
		public ImplMapMetadataTableRecordVM(MetadataTableVM mdVM, MDToken mdToken)
			: base(mdVM, mdToken) {
		}

		static readonly IntegerHexBitFieldEnumInfo[] CharSetInfos = new IntegerHexBitFieldEnumInfo[] {
			new IntegerHexBitFieldEnumInfo(0, "NotSpec"),
			new IntegerHexBitFieldEnumInfo(1, "Ansi"),
			new IntegerHexBitFieldEnumInfo(2, "Unicode"),
			new IntegerHexBitFieldEnumInfo(3, "Auto"),
		};

		static readonly IntegerHexBitFieldEnumInfo[] BestFitInfos = new IntegerHexBitFieldEnumInfo[] {
			new IntegerHexBitFieldEnumInfo(0, "UseAssem"),
			new IntegerHexBitFieldEnumInfo(1, "Enabled"),
			new IntegerHexBitFieldEnumInfo(2, "Disabled"),
		};

		static readonly IntegerHexBitFieldEnumInfo[] ThrowOnUnmappableCharInfos = new IntegerHexBitFieldEnumInfo[] {
			new IntegerHexBitFieldEnumInfo(0, "UseAssem"),
			new IntegerHexBitFieldEnumInfo(1, "Enabled"),
			new IntegerHexBitFieldEnumInfo(2, "Disabled"),
		};

		static readonly IntegerHexBitFieldEnumInfo[] CallConvInfos = new IntegerHexBitFieldEnumInfo[] {
			new IntegerHexBitFieldEnumInfo(1, "Winapi"),
			new IntegerHexBitFieldEnumInfo(2, "Cdecl"),
			new IntegerHexBitFieldEnumInfo(3, "Stdcall"),
			new IntegerHexBitFieldEnumInfo(4, "Thiscall"),
			new IntegerHexBitFieldEnumInfo(5, "Fastcall"),
		};

		protected override HexField CreateField(ColumnInfo colInfo) {
			if (colInfo.Index == 0) {
				var field = new UInt16FlagsHexField(mdVM.Buffer, Name, colInfo.Name, Span.Start + (uint)colInfo.Offset);
				field.Add(new BooleanHexBitField("NoMangle", 0));
				field.Add(new IntegerHexBitField("CharSet", 1, 2, CharSetInfos));
				field.Add(new IntegerHexBitField("BestFit", 4, 2, BestFitInfos));
				field.Add(new BooleanHexBitField("SupportsLastError", 6));
				field.Add(new IntegerHexBitField("CallConv", 8, 3, CallConvInfos));
				field.Add(new IntegerHexBitField("ThrowOnUnmappableChar", 12, 2, ThrowOnUnmappableCharInfos));
				return field;
			}
			return base.CreateField(colInfo);
		}

		public override string Info => ReadStringsHeap(Column2!);
		protected override int[]? InfoColumnIndexes => infoColIndexes;
		static readonly int[] infoColIndexes = new int[] { 2 };
	}

	sealed class FieldRVAMetadataTableRecordVM : MetadataTableRecordVM {
		public FieldRVAMetadataTableRecordVM(MetadataTableVM mdVM, MDToken mdToken)
			: base(mdVM, mdToken) {
		}
	}

	sealed class ENCLogMetadataTableRecordVM : MetadataTableRecordVM {
		public ENCLogMetadataTableRecordVM(MetadataTableVM mdVM, MDToken mdToken)
			: base(mdVM, mdToken) {
		}
	}

	sealed class ENCMapMetadataTableRecordVM : MetadataTableRecordVM {
		public ENCMapMetadataTableRecordVM(MetadataTableVM mdVM, MDToken mdToken)
			: base(mdVM, mdToken) {
		}
	}

	sealed class AssemblyMetadataTableRecordVM : MetadataTableRecordVM {
		public AssemblyMetadataTableRecordVM(MetadataTableVM mdVM, MDToken mdToken)
			: base(mdVM, mdToken) {
		}

		static readonly IntegerHexBitFieldEnumInfo[] HashAlgoInfos = new IntegerHexBitFieldEnumInfo[] {
			new IntegerHexBitFieldEnumInfo(0, "None"),
			new IntegerHexBitFieldEnumInfo(0x8001, "MD2"),
			new IntegerHexBitFieldEnumInfo(0x8002, "MD4"),
			new IntegerHexBitFieldEnumInfo(0x8003, "MD5"),
			new IntegerHexBitFieldEnumInfo(0x8004, "SHA1"),
			new IntegerHexBitFieldEnumInfo(0x8005, "MAC"),
			new IntegerHexBitFieldEnumInfo(0x8008, "SSL3_SHAMD5"),
			new IntegerHexBitFieldEnumInfo(0x8009, "HMAC"),
			new IntegerHexBitFieldEnumInfo(0x800A, "TLS1PRF"),
			new IntegerHexBitFieldEnumInfo(0x800B, "HASH_REPLACE_OWF"),
			new IntegerHexBitFieldEnumInfo(0x800C, "SHA_256"),
			new IntegerHexBitFieldEnumInfo(0x800D, "SHA_384"),
			new IntegerHexBitFieldEnumInfo(0x800E, "SHA_512"),
		};

		static readonly IntegerHexBitFieldEnumInfo[] PAInfos = new IntegerHexBitFieldEnumInfo[] {
			new IntegerHexBitFieldEnumInfo(0, "None"),
			new IntegerHexBitFieldEnumInfo(1, "MSIL"),
			new IntegerHexBitFieldEnumInfo(2, "x86"),
			new IntegerHexBitFieldEnumInfo(3, "IA64"),
			new IntegerHexBitFieldEnumInfo(4, "AMD64"),
			new IntegerHexBitFieldEnumInfo(5, "ARM"),
			new IntegerHexBitFieldEnumInfo(7, "NoPlatform"),
		};

		static readonly IntegerHexBitFieldEnumInfo[] ContentTypeInfos = new IntegerHexBitFieldEnumInfo[] {
			new IntegerHexBitFieldEnumInfo(0, "Default"),
			new IntegerHexBitFieldEnumInfo(1, "WindowsRuntime"),
		};

		protected override HexField CreateField(ColumnInfo colInfo) {
			if (colInfo.Index == 0) {
				var field = new UInt32FlagsHexField(mdVM.Buffer, Name, colInfo.Name, Span.Start + (uint)colInfo.Offset);
				field.Add(new IntegerHexBitField("Hash Algorithm", 0, 32, HashAlgoInfos));
				return field;
			}
			else if (1 <= colInfo.Index && colInfo.Index <= 4)
				return new UInt16HexField(mdVM.Buffer, Name, colInfo.Name, Span.Start + (uint)colInfo.Offset, true);
			else if (colInfo.Index == 5)
				return CreateAssemblyAttributesField(colInfo, mdVM.Buffer, Name, Span.Start);
			return base.CreateField(colInfo);
		}

		internal static UInt32FlagsHexField CreateAssemblyAttributesField(ColumnInfo colInfo, HexBuffer buffer, string name, HexPosition startOffset) {
			var field = new UInt32FlagsHexField(buffer, name, colInfo.Name, startOffset + (uint)colInfo.Offset);
			field.Add(new BooleanHexBitField("PublicKey", 0));
			field.Add(new IntegerHexBitField("Processor Arch", 4, 3, PAInfos));
			field.Add(new BooleanHexBitField("Processor Arch Specified", 7));
			field.Add(new BooleanHexBitField("Retargetable", 8));
			field.Add(new IntegerHexBitField("ContentType", 9, 3, ContentTypeInfos));
			field.Add(new BooleanHexBitField("DisableJITcompileOptimizer", 14));
			field.Add(new BooleanHexBitField("EnableJITcompileTracking", 15));
			return field;
		}

		public override string Info => ReadStringsHeap(Column7!);
		protected override int[]? InfoColumnIndexes => infoColIndexes;
		static readonly int[] infoColIndexes = new int[] { 7 };
	}

	sealed class AssemblyProcessorMetadataTableRecordVM : MetadataTableRecordVM {
		public AssemblyProcessorMetadataTableRecordVM(MetadataTableVM mdVM, MDToken mdToken)
			: base(mdVM, mdToken) {
		}
	}

	sealed class AssemblyOSMetadataTableRecordVM : MetadataTableRecordVM {
		public AssemblyOSMetadataTableRecordVM(MetadataTableVM mdVM, MDToken mdToken)
			: base(mdVM, mdToken) {
		}

		protected override HexField CreateField(ColumnInfo colInfo) {
			if (1 <= colInfo.Index && colInfo.Index <= 2)
				return new UInt32HexField(mdVM.Buffer, Name, colInfo.Name, Span.Start + (uint)colInfo.Offset, true);
			return base.CreateField(colInfo);
		}
	}

	sealed class AssemblyRefMetadataTableRecordVM : MetadataTableRecordVM {
		public AssemblyRefMetadataTableRecordVM(MetadataTableVM mdVM, MDToken mdToken)
			: base(mdVM, mdToken) {
		}

		protected override HexField CreateField(ColumnInfo colInfo) {
			if (0 <= colInfo.Index && colInfo.Index <= 3)
				return new UInt16HexField(mdVM.Buffer, Name, colInfo.Name, Span.Start + (uint)colInfo.Offset, true);
			else if (colInfo.Index == 4)
				return AssemblyMetadataTableRecordVM.CreateAssemblyAttributesField(colInfo, mdVM.Buffer, Name, Span.Start);
			return base.CreateField(colInfo);
		}

		public override string Info => ReadStringsHeap(Column6!);
		protected override int[]? InfoColumnIndexes => infoColIndexes;
		static readonly int[] infoColIndexes = new int[] { 6 };
	}

	sealed class AssemblyRefProcessorMetadataTableRecordVM : MetadataTableRecordVM {
		public AssemblyRefProcessorMetadataTableRecordVM(MetadataTableVM mdVM, MDToken mdToken)
			: base(mdVM, mdToken) {
		}
	}

	sealed class AssemblyRefOSMetadataTableRecordVM : MetadataTableRecordVM {
		public AssemblyRefOSMetadataTableRecordVM(MetadataTableVM mdVM, MDToken mdToken)
			: base(mdVM, mdToken) {
		}

		protected override HexField CreateField(ColumnInfo colInfo) {
			if (1 <= colInfo.Index && colInfo.Index <= 2)
				return new UInt32HexField(mdVM.Buffer, Name, colInfo.Name, Span.Start + (uint)colInfo.Offset, true);
			return base.CreateField(colInfo);
		}
	}

	sealed class FileMetadataTableRecordVM : MetadataTableRecordVM {
		public FileMetadataTableRecordVM(MetadataTableVM mdVM, MDToken mdToken)
			: base(mdVM, mdToken) {
		}

		protected override HexField CreateField(ColumnInfo colInfo) {
			if (colInfo.Index == 0) {
				var field = new UInt32FlagsHexField(mdVM.Buffer, Name, colInfo.Name, Span.Start + (uint)colInfo.Offset);
				field.Add(new BooleanHexBitField("ContainsNoMetaData", 0));
				return field;
			}
			return base.CreateField(colInfo);
		}

		public override string Info => ReadStringsHeap(Column1!);
		protected override int[]? InfoColumnIndexes => infoColIndexes;
		static readonly int[] infoColIndexes = new int[] { 1 };
	}

	sealed class ExportedTypeMetadataTableRecordVM : MetadataTableRecordVM {
		public ExportedTypeMetadataTableRecordVM(MetadataTableVM mdVM, MDToken mdToken)
			: base(mdVM, mdToken) {
		}

		protected override HexField CreateField(ColumnInfo colInfo) {
			if (colInfo.Index == 0)
				return TypeDefMetadataTableRecordVM.CreateTypeAttributesField(colInfo, mdVM.Buffer, Name, Span.Start);
			return base.CreateField(colInfo);
		}

		public override string Info => TypeRefMetadataTableRecordVM.CreateTypeString(ReadStringsHeap(Column3!), ReadStringsHeap(Column2!));
		protected override int[]? InfoColumnIndexes => infoColIndexes;
		static readonly int[] infoColIndexes = new int[] { 2, 3 };
	}

	sealed class ManifestResourceMetadataTableRecordVM : MetadataTableRecordVM {
		public ManifestResourceMetadataTableRecordVM(MetadataTableVM mdVM, MDToken mdToken)
			: base(mdVM, mdToken) {
		}

		static readonly IntegerHexBitFieldEnumInfo[] VisibilityInfos = new IntegerHexBitFieldEnumInfo[] {
			new IntegerHexBitFieldEnumInfo(1, "Public"),
			new IntegerHexBitFieldEnumInfo(2, "Private"),
		};

		protected override HexField CreateField(ColumnInfo colInfo) {
			if (colInfo.Index == 1) {
				var field = new UInt32FlagsHexField(mdVM.Buffer, Name, colInfo.Name, Span.Start + (uint)colInfo.Offset);
				field.Add(new IntegerHexBitField("Visibility", 0, 3, VisibilityInfos));
				return field;
			}
			return base.CreateField(colInfo);
		}

		public override string Info => ReadStringsHeap(Column2!);
		protected override int[]? InfoColumnIndexes => infoColIndexes;
		static readonly int[] infoColIndexes = new int[] { 2 };
	}

	sealed class NestedClassMetadataTableRecordVM : MetadataTableRecordVM {
		public NestedClassMetadataTableRecordVM(MetadataTableVM mdVM, MDToken mdToken)
			: base(mdVM, mdToken) {
		}
	}

	sealed class GenericParamMetadataTableRecordV11VM : MetadataTableRecordVM {
		public GenericParamMetadataTableRecordV11VM(MetadataTableVM mdVM, MDToken mdToken)
			: base(mdVM, mdToken) {
		}

		protected override HexField CreateField(ColumnInfo colInfo) {
			if (colInfo.Index == 1)
				return GenericParamMetadataTableRecordVM.CreateGenericParamAttributesField(colInfo, mdVM.Buffer, Name, Span.Start);
			return base.CreateField(colInfo);
		}

		public override string Info => ReadStringsHeap(Column3!);
		protected override int[]? InfoColumnIndexes => infoColIndexes;
		static readonly int[] infoColIndexes = new int[] { 3 };
	}

	sealed class GenericParamMetadataTableRecordVM : MetadataTableRecordVM {
		public GenericParamMetadataTableRecordVM(MetadataTableVM mdVM, MDToken mdToken)
			: base(mdVM, mdToken) {
		}

		static readonly IntegerHexBitFieldEnumInfo[] VarianceInfos = new IntegerHexBitFieldEnumInfo[] {
			new IntegerHexBitFieldEnumInfo(0, "NonVariant"),
			new IntegerHexBitFieldEnumInfo(1, "Covariant"),
			new IntegerHexBitFieldEnumInfo(2, "Contravariant"),
		};

		protected override HexField CreateField(ColumnInfo colInfo) {
			if (colInfo.Index == 1)
				return CreateGenericParamAttributesField(colInfo, mdVM.Buffer, Name, Span.Start);
			return base.CreateField(colInfo);
		}

		internal static UInt16FlagsHexField CreateGenericParamAttributesField(ColumnInfo colInfo, HexBuffer buffer, string name, HexPosition startOffset) {
			var field = new UInt16FlagsHexField(buffer, name, colInfo.Name, startOffset + (uint)colInfo.Offset);
			field.Add(new IntegerHexBitField("Variance", 0, 2, VarianceInfos));
			field.Add(new BooleanHexBitField("Reference", 2));
			field.Add(new BooleanHexBitField("Struct", 3));
			field.Add(new BooleanHexBitField("Default ctor", 4));
			return field;
		}

		public override string Info => ReadStringsHeap(Column3!);
		protected override int[]? InfoColumnIndexes => infoColIndexes;
		static readonly int[] infoColIndexes = new int[] { 3 };
	}

	sealed class MethodSpecMetadataTableRecordVM : MetadataTableRecordVM {
		public MethodSpecMetadataTableRecordVM(MetadataTableVM mdVM, MDToken mdToken)
			: base(mdVM, mdToken) {
		}
	}

	sealed class GenericParamConstraintMetadataTableRecordVM : MetadataTableRecordVM {
		public GenericParamConstraintMetadataTableRecordVM(MetadataTableVM mdVM, MDToken mdToken)
			: base(mdVM, mdToken) {
		}
	}

	sealed class DocumentMetadataTableRecordVM : MetadataTableRecordVM {
		public DocumentMetadataTableRecordVM(MetadataTableVM mdVM, MDToken mdToken)
			: base(mdVM, mdToken) {
		}
	}

	sealed class MethodDebugInformationMetadataTableRecordVM : MetadataTableRecordVM {
		public MethodDebugInformationMetadataTableRecordVM(MetadataTableVM mdVM, MDToken mdToken)
			: base(mdVM, mdToken) {
		}
	}

	sealed class LocalScopeMetadataTableRecordVM : MetadataTableRecordVM {
		public LocalScopeMetadataTableRecordVM(MetadataTableVM mdVM, MDToken mdToken)
			: base(mdVM, mdToken) {
		}
	}

	sealed class LocalVariableMetadataTableRecordVM : MetadataTableRecordVM {
		public LocalVariableMetadataTableRecordVM(MetadataTableVM mdVM, MDToken mdToken)
			: base(mdVM, mdToken) {
		}
	}

	sealed class LocalConstantMetadataTableRecordVM : MetadataTableRecordVM {
		public LocalConstantMetadataTableRecordVM(MetadataTableVM mdVM, MDToken mdToken)
			: base(mdVM, mdToken) {
		}
	}

	sealed class ImportScopeMetadataTableRecordVM : MetadataTableRecordVM {
		public ImportScopeMetadataTableRecordVM(MetadataTableVM mdVM, MDToken mdToken)
			: base(mdVM, mdToken) {
		}
	}

	sealed class StateMachineMethodMetadataTableRecordVM : MetadataTableRecordVM {
		public StateMachineMethodMetadataTableRecordVM(MetadataTableVM mdVM, MDToken mdToken)
			: base(mdVM, mdToken) {
		}
	}

	sealed class CustomDebugInformationMetadataTableRecordVM : MetadataTableRecordVM {
		public CustomDebugInformationMetadataTableRecordVM(MetadataTableVM mdVM, MDToken mdToken)
			: base(mdVM, mdToken) {
		}
	}
}
