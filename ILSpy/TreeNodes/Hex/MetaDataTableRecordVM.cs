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
using dnSpy.AsmEditor;
using dnSpy.HexEditor;

namespace dnSpy.TreeNodes.Hex {
	abstract class MetaDataTableRecordVM : HexVM, IVirtualizedListItem {
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
		protected readonly HexDocument doc;
		protected readonly ulong startOffset;
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
			if ((uint)index < (uint)hexFields.Length)
				return hexFields[index];
			return null;
		}

		public bool Column0Present {
			get { return IsFieldPresent(0); }
		}

		public bool Column1Present {
			get { return IsFieldPresent(1); }
		}

		public bool Column2Present {
			get { return IsFieldPresent(2); }
		}

		public bool Column3Present {
			get { return IsFieldPresent(3); }
		}

		public bool Column4Present {
			get { return IsFieldPresent(4); }
		}

		public bool Column5Present {
			get { return IsFieldPresent(5); }
		}

		public bool Column6Present {
			get { return IsFieldPresent(6); }
		}

		public bool Column7Present {
			get { return IsFieldPresent(7); }
		}

		public bool Column8Present {
			get { return IsFieldPresent(8); }
		}

		bool IsFieldPresent(int index) {
			return (uint)index < (uint)hexFields.Length;
		}

		public string Column0Description {
			get { return GetFieldDescription(0); }
		}

		public string Column1Description {
			get { return GetFieldDescription(1); }
		}

		public string Column2Description {
			get { return GetFieldDescription(2); }
		}

		public string Column3Description {
			get { return GetFieldDescription(3); }
		}

		public string Column4Description {
			get { return GetFieldDescription(4); }
		}

		public string Column5Description {
			get { return GetFieldDescription(5); }
		}

		public string Column6Description {
			get { return GetFieldDescription(6); }
		}

		public string Column7Description {
			get { return GetFieldDescription(7); }
		}

		public string Column8Description {
			get { return GetFieldDescription(8); }
		}

		string GetFieldDescription(int index) {
			if ((uint)index >= (uint)hexFields.Length)
				return string.Empty;

			var col = tableInfo.Columns[index];
			var field = hexFields[index];

			switch (col.ColumnSize) {
			case ColumnSize.Module: return "Module RID";
			case ColumnSize.TypeRef: return "TypeRef RID";
			case ColumnSize.TypeDef: return "TypeDef RID";
			case ColumnSize.FieldPtr: return "FieldPtr RID";
			case ColumnSize.Field: return "Field RID";
			case ColumnSize.MethodPtr: return "MethodPtr RID";
			case ColumnSize.Method: return "Method RID";
			case ColumnSize.ParamPtr: return "ParamPtr RID";
			case ColumnSize.Param: return "Param RID";
			case ColumnSize.InterfaceImpl: return "InterfaceImpl RID";
			case ColumnSize.MemberRef: return "MemberRef RID";
			case ColumnSize.Constant: return "Constant RID";
			case ColumnSize.CustomAttribute: return "CustomAttribute RID";
			case ColumnSize.FieldMarshal: return "FieldMarshal RID";
			case ColumnSize.DeclSecurity: return "DeclSecurity RID";
			case ColumnSize.ClassLayout: return "ClassLayout RID";
			case ColumnSize.FieldLayout: return "FieldLayout RID";
			case ColumnSize.StandAloneSig: return "StandAloneSig RID";
			case ColumnSize.EventMap: return "EventMap RID";
			case ColumnSize.EventPtr: return "EventPtr RID";
			case ColumnSize.Event: return "Event RID";
			case ColumnSize.PropertyMap: return "PropertyMap RID";
			case ColumnSize.PropertyPtr: return "PropertyPtr RID";
			case ColumnSize.Property: return "Property RID";
			case ColumnSize.MethodSemantics: return "MethodSemantics RID";
			case ColumnSize.MethodImpl: return "MethodImpl RID";
			case ColumnSize.ModuleRef: return "ModuleRef RID";
			case ColumnSize.TypeSpec: return "TypeSpec RID";
			case ColumnSize.ImplMap: return "ImplMap RID";
			case ColumnSize.FieldRVA: return "FieldRVA RID";
			case ColumnSize.ENCLog: return "ENCLog RID";
			case ColumnSize.ENCMap: return "ENCMap RID";
			case ColumnSize.Assembly: return "Assembly RID";
			case ColumnSize.AssemblyProcessor: return "AssemblyProcessor RID";
			case ColumnSize.AssemblyOS: return "AssemblyOS RID";
			case ColumnSize.AssemblyRef: return "AssemblyRef RID";
			case ColumnSize.AssemblyRefProcessor: return "AssemblyRefProcessor RID";
			case ColumnSize.AssemblyRefOS: return "AssemblyRefOS RID";
			case ColumnSize.File: return "File RID";
			case ColumnSize.ExportedType: return "ExportedType RID";
			case ColumnSize.ManifestResource: return "ManifestResource RID";
			case ColumnSize.NestedClass: return "NestedClass RID";
			case ColumnSize.GenericParam: return "GenericParam RID";
			case ColumnSize.MethodSpec: return "MethodSpec RID";
			case ColumnSize.GenericParamConstraint: return "GenericParamConstraint RID";

			case ColumnSize.Byte: return "Byte";
			case ColumnSize.Int16: return "Int16";
			case ColumnSize.UInt16: return "UInt16";
			case ColumnSize.Int32: return "Int32";
			case ColumnSize.UInt32: return "UInt32";

			case ColumnSize.Strings: return GetStringsDescription(field);
			case ColumnSize.GUID: return "#GUID Heap Offset";
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
			default:
				Debug.Fail(string.Format("Unknown ColumnSize: {0}", col.ColumnSize));
				return string.Empty;
			}
		}

		string GetStringsDescription(HexField field) {
			var s = NumberVMUtils.ToString(mdVM.ReadStringsHeap(ReadFieldValue(field)), false);
			Debug.Assert(s.Length >= 2);
			if (s.Length < 2)
				return s;
			return s.Substring(1, s.Length - 2);
		}

		string GetCodedTokenDescription(CodedToken codedToken, string codedTokenName, ColumnInfo col, HexField field) {
			MDToken token;
			if (!codedToken.Decode(ReadFieldValue(field), out token))
				return string.Empty;

			return string.Format("{0} Coded Token: {1}[{2}] (0x{3:X8})", codedTokenName, token.Table, token.Rid, token.Raw);
		}

		uint ReadFieldValue(HexField field) {
			if (field.Size == 2)
				return doc.ReadUInt16(field.StartOffset);
			else if (field.Size == 4)
				return doc.ReadUInt32(field.StartOffset);
			return 0;
		}

		bool IsDynamicDescription(int index) {
			if ((uint)index >= (uint)hexFields.Length)
				return false;

			var col = tableInfo.Columns[index];
			switch (col.ColumnSize) {
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
				return true;
			default:
				return false;
			}
		}

		void InvalidateDescription(int index) {
			OnPropertyChanged(string.Format("Column{0}Description", index));
		}

		public string RidString {
			get { return mdToken.Rid.ToString(); }
		}

		public string TokenString {
			get { return string.Format("0x{0:X8}", mdToken.Raw); }
		}

		readonly MetaDataTableVM mdVM;
		readonly TableInfo tableInfo;

		protected MetaDataTableRecordVM(MetaDataTableVM mdVM, HexDocument doc, ulong startOffset, MDToken mdToken, TableInfo tableInfo) {
			this.mdVM = mdVM;
			this.name = string.Format("{0}[{1:X6}]", mdToken.Table, mdToken.Rid);
			this.doc = doc;
			this.startOffset = startOffset;
			this.endOffset = startOffset + (uint)tableInfo.RowSize - 1;
			this.mdToken = mdToken;
			this.tableInfo = tableInfo;
			this.hexFields = new HexField[tableInfo.Columns.Count];
			for (int i = 0; i < this.hexFields.Length; i++)
				this.hexFields[i] = CreateField(tableInfo.Columns[i]);
		}

		protected virtual HexField CreateField(ColumnInfo colInfo) {
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

		public override void OnDocumentModified(ulong modifiedStart, ulong modifiedEnd) {
			base.OnDocumentModified(modifiedStart, modifiedEnd);
			for (int i = 0; i < hexFields.Length; i++) {
				var field = hexFields[i];
				if (IsDynamicDescription(i) && HexUtils.IsModified(field.StartOffset, field.EndOffset, modifiedStart, modifiedEnd))
					InvalidateDescription(i);
			}
		}
	}

	sealed class ModuleMetaDataTableRecordVM : MetaDataTableRecordVM {
		public ModuleMetaDataTableRecordVM(MetaDataTableVM mdVM, HexDocument doc, ulong startOffset, MDToken mdToken, TableInfo tableInfo)
			: base(mdVM, doc, startOffset, mdToken, tableInfo) {
		}
	}

	sealed class TypeRefMetaDataTableRecordVM : MetaDataTableRecordVM {
		public TypeRefMetaDataTableRecordVM(MetaDataTableVM mdVM, HexDocument doc, ulong startOffset, MDToken mdToken, TableInfo tableInfo)
			: base(mdVM, doc, startOffset, mdToken, tableInfo) {
		}
	}

	sealed class TypeDefMetaDataTableRecordVM : MetaDataTableRecordVM {
		public TypeDefMetaDataTableRecordVM(MetaDataTableVM mdVM, HexDocument doc, ulong startOffset, MDToken mdToken, TableInfo tableInfo)
			: base(mdVM, doc, startOffset, mdToken, tableInfo) {
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
				return CreateTypeAttributesField(colInfo, doc, Name, startOffset);
			return base.CreateField(colInfo);
		}

		internal static UInt32FlagsHexField CreateTypeAttributesField(ColumnInfo colInfo, HexDocument doc, string name, ulong startOffset) {
			var field = new UInt32FlagsHexField(doc, name, colInfo.Name, startOffset + (uint)colInfo.Offset);
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
	}

	sealed class FieldPtrMetaDataTableRecordVM : MetaDataTableRecordVM {
		public FieldPtrMetaDataTableRecordVM(MetaDataTableVM mdVM, HexDocument doc, ulong startOffset, MDToken mdToken, TableInfo tableInfo)
			: base(mdVM, doc, startOffset, mdToken, tableInfo) {
		}
	}

	sealed class FieldMetaDataTableRecordVM : MetaDataTableRecordVM {
		public FieldMetaDataTableRecordVM(MetaDataTableVM mdVM, HexDocument doc, ulong startOffset, MDToken mdToken, TableInfo tableInfo)
			: base(mdVM, doc, startOffset, mdToken, tableInfo) {
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
				var field = new UInt16FlagsHexField(doc, Name, colInfo.Name, startOffset + (uint)colInfo.Offset);
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
	}

	sealed class MethodPtrMetaDataTableRecordVM : MetaDataTableRecordVM {
		public MethodPtrMetaDataTableRecordVM(MetaDataTableVM mdVM, HexDocument doc, ulong startOffset, MDToken mdToken, TableInfo tableInfo)
			: base(mdVM, doc, startOffset, mdToken, tableInfo) {
		}
	}

	sealed class MethodMetaDataTableRecordVM : MetaDataTableRecordVM {
		public MethodMetaDataTableRecordVM(MetaDataTableVM mdVM, HexDocument doc, ulong startOffset, MDToken mdToken, TableInfo tableInfo)
			: base(mdVM, doc, startOffset, mdToken, tableInfo) {
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
				var field = new UInt16FlagsHexField(doc, Name, colInfo.Name, startOffset + (uint)colInfo.Offset);
				field.Add(new IntegerHexBitField("CodeType", 0, 2, CodeTypeInfos));
				field.Add(new IntegerHexBitField("ManagedType", 2, 1, ManagedInfos));
				field.Add(new BooleanHexBitField("NoInlining", 3));
				field.Add(new BooleanHexBitField("ForwardRef", 4));
				field.Add(new BooleanHexBitField("Synchronized", 5));
				field.Add(new BooleanHexBitField("NoOptimization", 6));
				field.Add(new BooleanHexBitField("PreserveSig", 7));
				field.Add(new BooleanHexBitField("AggressiveInlining", 8));
				field.Add(new BooleanHexBitField("InternalCall", 12));
				return field;
			}
			else if (colInfo.Index == 2) {
				var field = new UInt16FlagsHexField(doc, Name, colInfo.Name, startOffset + (uint)colInfo.Offset);
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
	}

	sealed class ParamPtrMetaDataTableRecordVM : MetaDataTableRecordVM {
		public ParamPtrMetaDataTableRecordVM(MetaDataTableVM mdVM, HexDocument doc, ulong startOffset, MDToken mdToken, TableInfo tableInfo)
			: base(mdVM, doc, startOffset, mdToken, tableInfo) {
		}
	}

	sealed class ParamMetaDataTableRecordVM : MetaDataTableRecordVM {
		public ParamMetaDataTableRecordVM(MetaDataTableVM mdVM, HexDocument doc, ulong startOffset, MDToken mdToken, TableInfo tableInfo)
			: base(mdVM, doc, startOffset, mdToken, tableInfo) {
		}

		protected override HexField CreateField(ColumnInfo colInfo) {
			if (colInfo.Index == 0) {
				var field = new UInt16FlagsHexField(doc, Name, colInfo.Name, startOffset + (uint)colInfo.Offset);
				field.Add(new BooleanHexBitField("In", 0));
				field.Add(new BooleanHexBitField("Out", 1));
				field.Add(new BooleanHexBitField("Optional", 4));
				field.Add(new BooleanHexBitField("HasDefault", 12));
				field.Add(new BooleanHexBitField("HasFieldMarshal", 13));
				return field;
			}
			return base.CreateField(colInfo);
		}
	}

	sealed class InterfaceImplMetaDataTableRecordVM : MetaDataTableRecordVM {
		public InterfaceImplMetaDataTableRecordVM(MetaDataTableVM mdVM, HexDocument doc, ulong startOffset, MDToken mdToken, TableInfo tableInfo)
			: base(mdVM, doc, startOffset, mdToken, tableInfo) {
		}
	}

	sealed class MemberRefMetaDataTableRecordVM : MetaDataTableRecordVM {
		public MemberRefMetaDataTableRecordVM(MetaDataTableVM mdVM, HexDocument doc, ulong startOffset, MDToken mdToken, TableInfo tableInfo)
			: base(mdVM, doc, startOffset, mdToken, tableInfo) {
		}
	}

	sealed class ConstantMetaDataTableRecordVM : MetaDataTableRecordVM {
		public ConstantMetaDataTableRecordVM(MetaDataTableVM mdVM, HexDocument doc, ulong startOffset, MDToken mdToken, TableInfo tableInfo)
			: base(mdVM, doc, startOffset, mdToken, tableInfo) {
		}
	}

	sealed class CustomAttributeMetaDataTableRecordVM : MetaDataTableRecordVM {
		public CustomAttributeMetaDataTableRecordVM(MetaDataTableVM mdVM, HexDocument doc, ulong startOffset, MDToken mdToken, TableInfo tableInfo)
			: base(mdVM, doc, startOffset, mdToken, tableInfo) {
		}
	}

	sealed class FieldMarshalMetaDataTableRecordVM : MetaDataTableRecordVM {
		public FieldMarshalMetaDataTableRecordVM(MetaDataTableVM mdVM, HexDocument doc, ulong startOffset, MDToken mdToken, TableInfo tableInfo)
			: base(mdVM, doc, startOffset, mdToken, tableInfo) {
		}
	}

	sealed class DeclSecurityMetaDataTableRecordVM : MetaDataTableRecordVM {
		public DeclSecurityMetaDataTableRecordVM(MetaDataTableVM mdVM, HexDocument doc, ulong startOffset, MDToken mdToken, TableInfo tableInfo)
			: base(mdVM, doc, startOffset, mdToken, tableInfo) {
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
				var field = new Int16FlagsHexField(doc, Name, colInfo.Name, startOffset + (uint)colInfo.Offset);
				field.Add(new IntegerHexBitField("Action", 0, 16, SecurityActionInfos));
				return field;
			}
			return base.CreateField(colInfo);
		}
	}

	sealed class ClassLayoutMetaDataTableRecordVM : MetaDataTableRecordVM {
		public ClassLayoutMetaDataTableRecordVM(MetaDataTableVM mdVM, HexDocument doc, ulong startOffset, MDToken mdToken, TableInfo tableInfo)
			: base(mdVM, doc, startOffset, mdToken, tableInfo) {
		}
	}

	sealed class FieldLayoutMetaDataTableRecordVM : MetaDataTableRecordVM {
		public FieldLayoutMetaDataTableRecordVM(MetaDataTableVM mdVM, HexDocument doc, ulong startOffset, MDToken mdToken, TableInfo tableInfo)
			: base(mdVM, doc, startOffset, mdToken, tableInfo) {
		}
	}

	sealed class StandAloneSigMetaDataTableRecordVM : MetaDataTableRecordVM {
		public StandAloneSigMetaDataTableRecordVM(MetaDataTableVM mdVM, HexDocument doc, ulong startOffset, MDToken mdToken, TableInfo tableInfo)
			: base(mdVM, doc, startOffset, mdToken, tableInfo) {
		}
	}

	sealed class EventMapMetaDataTableRecordVM : MetaDataTableRecordVM {
		public EventMapMetaDataTableRecordVM(MetaDataTableVM mdVM, HexDocument doc, ulong startOffset, MDToken mdToken, TableInfo tableInfo)
			: base(mdVM, doc, startOffset, mdToken, tableInfo) {
		}
	}

	sealed class EventPtrMetaDataTableRecordVM : MetaDataTableRecordVM {
		public EventPtrMetaDataTableRecordVM(MetaDataTableVM mdVM, HexDocument doc, ulong startOffset, MDToken mdToken, TableInfo tableInfo)
			: base(mdVM, doc, startOffset, mdToken, tableInfo) {
		}
	}

	sealed class EventMetaDataTableRecordVM : MetaDataTableRecordVM {
		public EventMetaDataTableRecordVM(MetaDataTableVM mdVM, HexDocument doc, ulong startOffset, MDToken mdToken, TableInfo tableInfo)
			: base(mdVM, doc, startOffset, mdToken, tableInfo) {
		}

		protected override HexField CreateField(ColumnInfo colInfo) {
			if (colInfo.Index == 0) {
				var field = new UInt16FlagsHexField(doc, Name, colInfo.Name, startOffset + (uint)colInfo.Offset);
				field.Add(new BooleanHexBitField("SpecialName", 9));
				field.Add(new BooleanHexBitField("RTSpecialName", 10));
				return field;
			}
			return base.CreateField(colInfo);
		}
	}

	sealed class PropertyMapMetaDataTableRecordVM : MetaDataTableRecordVM {
		public PropertyMapMetaDataTableRecordVM(MetaDataTableVM mdVM, HexDocument doc, ulong startOffset, MDToken mdToken, TableInfo tableInfo)
			: base(mdVM, doc, startOffset, mdToken, tableInfo) {
		}
	}

	sealed class PropertyPtrMetaDataTableRecordVM : MetaDataTableRecordVM {
		public PropertyPtrMetaDataTableRecordVM(MetaDataTableVM mdVM, HexDocument doc, ulong startOffset, MDToken mdToken, TableInfo tableInfo)
			: base(mdVM, doc, startOffset, mdToken, tableInfo) {
		}
	}

	sealed class PropertyMetaDataTableRecordVM : MetaDataTableRecordVM {
		public PropertyMetaDataTableRecordVM(MetaDataTableVM mdVM, HexDocument doc, ulong startOffset, MDToken mdToken, TableInfo tableInfo)
			: base(mdVM, doc, startOffset, mdToken, tableInfo) {
		}

		protected override HexField CreateField(ColumnInfo colInfo) {
			if (colInfo.Index == 0) {
				var field = new UInt16FlagsHexField(doc, Name, colInfo.Name, startOffset + (uint)colInfo.Offset);
				field.Add(new BooleanHexBitField("SpecialName", 9));
				field.Add(new BooleanHexBitField("RTSpecialName", 10));
				field.Add(new BooleanHexBitField("HasDefault", 12));
				return field;
			}
			return base.CreateField(colInfo);
		}
	}

	sealed class MethodSemanticsMetaDataTableRecordVM : MetaDataTableRecordVM {
		public MethodSemanticsMetaDataTableRecordVM(MetaDataTableVM mdVM, HexDocument doc, ulong startOffset, MDToken mdToken, TableInfo tableInfo)
			: base(mdVM, doc, startOffset, mdToken, tableInfo) {
		}

		protected override HexField CreateField(ColumnInfo colInfo) {
			if (colInfo.Index == 0) {
				var field = new UInt16FlagsHexField(doc, Name, colInfo.Name, startOffset + (uint)colInfo.Offset);
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

	sealed class MethodImplMetaDataTableRecordVM : MetaDataTableRecordVM {
		public MethodImplMetaDataTableRecordVM(MetaDataTableVM mdVM, HexDocument doc, ulong startOffset, MDToken mdToken, TableInfo tableInfo)
			: base(mdVM, doc, startOffset, mdToken, tableInfo) {
		}
	}

	sealed class ModuleRefMetaDataTableRecordVM : MetaDataTableRecordVM {
		public ModuleRefMetaDataTableRecordVM(MetaDataTableVM mdVM, HexDocument doc, ulong startOffset, MDToken mdToken, TableInfo tableInfo)
			: base(mdVM, doc, startOffset, mdToken, tableInfo) {
		}
	}

	sealed class TypeSpecMetaDataTableRecordVM : MetaDataTableRecordVM {
		public TypeSpecMetaDataTableRecordVM(MetaDataTableVM mdVM, HexDocument doc, ulong startOffset, MDToken mdToken, TableInfo tableInfo)
			: base(mdVM, doc, startOffset, mdToken, tableInfo) {
		}
	}

	sealed class ImplMapMetaDataTableRecordVM : MetaDataTableRecordVM {
		public ImplMapMetaDataTableRecordVM(MetaDataTableVM mdVM, HexDocument doc, ulong startOffset, MDToken mdToken, TableInfo tableInfo)
			: base(mdVM, doc, startOffset, mdToken, tableInfo) {
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
				var field = new UInt16FlagsHexField(doc, Name, colInfo.Name, startOffset + (uint)colInfo.Offset);
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
	}

	sealed class FieldRVAMetaDataTableRecordVM : MetaDataTableRecordVM {
		public FieldRVAMetaDataTableRecordVM(MetaDataTableVM mdVM, HexDocument doc, ulong startOffset, MDToken mdToken, TableInfo tableInfo)
			: base(mdVM, doc, startOffset, mdToken, tableInfo) {
		}
	}

	sealed class ENCLogMetaDataTableRecordVM : MetaDataTableRecordVM {
		public ENCLogMetaDataTableRecordVM(MetaDataTableVM mdVM, HexDocument doc, ulong startOffset, MDToken mdToken, TableInfo tableInfo)
			: base(mdVM, doc, startOffset, mdToken, tableInfo) {
		}
	}

	sealed class ENCMapMetaDataTableRecordVM : MetaDataTableRecordVM {
		public ENCMapMetaDataTableRecordVM(MetaDataTableVM mdVM, HexDocument doc, ulong startOffset, MDToken mdToken, TableInfo tableInfo)
			: base(mdVM, doc, startOffset, mdToken, tableInfo) {
		}
	}

	sealed class AssemblyMetaDataTableRecordVM : MetaDataTableRecordVM {
		public AssemblyMetaDataTableRecordVM(MetaDataTableVM mdVM, HexDocument doc, ulong startOffset, MDToken mdToken, TableInfo tableInfo)
			: base(mdVM, doc, startOffset, mdToken, tableInfo) {
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
				var field = new UInt32FlagsHexField(doc, Name, colInfo.Name, startOffset + (uint)colInfo.Offset);
				field.Add(new IntegerHexBitField("Hash Algorithm", 0, 32, HashAlgoInfos));
				return field;
			}
			else if (colInfo.Index == 5)
				return CreateAssemblyAttributesField(colInfo, doc, Name, startOffset);
			return base.CreateField(colInfo);
		}

		internal static UInt32FlagsHexField CreateAssemblyAttributesField(ColumnInfo colInfo, HexDocument doc, string name, ulong startOffset) {
			var field = new UInt32FlagsHexField(doc, name, colInfo.Name, startOffset + (uint)colInfo.Offset);
			field.Add(new BooleanHexBitField("PublicKey", 0));
			field.Add(new IntegerHexBitField("Processor Arch", 4, 3, PAInfos));
			field.Add(new BooleanHexBitField("PA_Specified", 7));
			field.Add(new BooleanHexBitField("Retargetable", 8));
			field.Add(new IntegerHexBitField("ContentType", 9, 3, ContentTypeInfos));
			field.Add(new BooleanHexBitField("DisableJITcompileOptimizer", 14));
			field.Add(new BooleanHexBitField("EnableJITcompileTracking", 15));
			return field;
		}
	}

	sealed class AssemblyProcessorMetaDataTableRecordVM : MetaDataTableRecordVM {
		public AssemblyProcessorMetaDataTableRecordVM(MetaDataTableVM mdVM, HexDocument doc, ulong startOffset, MDToken mdToken, TableInfo tableInfo)
			: base(mdVM, doc, startOffset, mdToken, tableInfo) {
		}
	}

	sealed class AssemblyOSMetaDataTableRecordVM : MetaDataTableRecordVM {
		public AssemblyOSMetaDataTableRecordVM(MetaDataTableVM mdVM, HexDocument doc, ulong startOffset, MDToken mdToken, TableInfo tableInfo)
			: base(mdVM, doc, startOffset, mdToken, tableInfo) {
		}
	}

	sealed class AssemblyRefMetaDataTableRecordVM : MetaDataTableRecordVM {
		public AssemblyRefMetaDataTableRecordVM(MetaDataTableVM mdVM, HexDocument doc, ulong startOffset, MDToken mdToken, TableInfo tableInfo)
			: base(mdVM, doc, startOffset, mdToken, tableInfo) {
		}

		protected override HexField CreateField(ColumnInfo colInfo) {
			if (colInfo.Index == 4)
				return AssemblyMetaDataTableRecordVM.CreateAssemblyAttributesField(colInfo, doc, Name, startOffset);
			return base.CreateField(colInfo);
		}
	}

	sealed class AssemblyRefProcessorMetaDataTableRecordVM : MetaDataTableRecordVM {
		public AssemblyRefProcessorMetaDataTableRecordVM(MetaDataTableVM mdVM, HexDocument doc, ulong startOffset, MDToken mdToken, TableInfo tableInfo)
			: base(mdVM, doc, startOffset, mdToken, tableInfo) {
		}
	}

	sealed class AssemblyRefOSMetaDataTableRecordVM : MetaDataTableRecordVM {
		public AssemblyRefOSMetaDataTableRecordVM(MetaDataTableVM mdVM, HexDocument doc, ulong startOffset, MDToken mdToken, TableInfo tableInfo)
			: base(mdVM, doc, startOffset, mdToken, tableInfo) {
		}
	}

	sealed class FileMetaDataTableRecordVM : MetaDataTableRecordVM {
		public FileMetaDataTableRecordVM(MetaDataTableVM mdVM, HexDocument doc, ulong startOffset, MDToken mdToken, TableInfo tableInfo)
			: base(mdVM, doc, startOffset, mdToken, tableInfo) {
		}

		protected override HexField CreateField(ColumnInfo colInfo) {
			if (colInfo.Index == 0) {
				var field = new UInt32FlagsHexField(doc, Name, colInfo.Name, startOffset + (uint)colInfo.Offset);
				field.Add(new BooleanHexBitField("ContainsNoMetaData", 0));
				return field;
			}
			return base.CreateField(colInfo);
		}
	}

	sealed class ExportedTypeMetaDataTableRecordVM : MetaDataTableRecordVM {
		public ExportedTypeMetaDataTableRecordVM(MetaDataTableVM mdVM, HexDocument doc, ulong startOffset, MDToken mdToken, TableInfo tableInfo)
			: base(mdVM, doc, startOffset, mdToken, tableInfo) {
		}

		protected override HexField CreateField(ColumnInfo colInfo) {
			if (colInfo.Index == 0)
				return TypeDefMetaDataTableRecordVM.CreateTypeAttributesField(colInfo, doc, Name, startOffset);
			return base.CreateField(colInfo);
		}
	}

	sealed class ManifestResourceMetaDataTableRecordVM : MetaDataTableRecordVM {
		public ManifestResourceMetaDataTableRecordVM(MetaDataTableVM mdVM, HexDocument doc, ulong startOffset, MDToken mdToken, TableInfo tableInfo)
			: base(mdVM, doc, startOffset, mdToken, tableInfo) {
		}

		static readonly IntegerHexBitFieldEnumInfo[] VisibilityInfos = new IntegerHexBitFieldEnumInfo[] {
			new IntegerHexBitFieldEnumInfo(1, "Public"),
			new IntegerHexBitFieldEnumInfo(2, "Private"),
		};

		protected override HexField CreateField(ColumnInfo colInfo) {
			if (colInfo.Index == 1) {
				var field = new UInt32FlagsHexField(doc, Name, colInfo.Name, startOffset + (uint)colInfo.Offset);
				field.Add(new IntegerHexBitField("Visibility", 0, 3, VisibilityInfos));
				return field;
			}
			return base.CreateField(colInfo);
		}
	}

	sealed class NestedClassMetaDataTableRecordVM : MetaDataTableRecordVM {
		public NestedClassMetaDataTableRecordVM(MetaDataTableVM mdVM, HexDocument doc, ulong startOffset, MDToken mdToken, TableInfo tableInfo)
			: base(mdVM, doc, startOffset, mdToken, tableInfo) {
		}
	}

	sealed class GenericParamMetaDataTableRecordV11VM : MetaDataTableRecordVM {
		public GenericParamMetaDataTableRecordV11VM(MetaDataTableVM mdVM, HexDocument doc, ulong startOffset, MDToken mdToken, TableInfo tableInfo)
			: base(mdVM, doc, startOffset, mdToken, tableInfo) {
		}

		protected override HexField CreateField(ColumnInfo colInfo) {
			if (colInfo.Index == 1)
				return GenericParamMetaDataTableRecordVM.CreateGenericParamAttributesField(colInfo, doc, Name, startOffset);
			return base.CreateField(colInfo);
		}
	}

	sealed class GenericParamMetaDataTableRecordVM : MetaDataTableRecordVM {
		public GenericParamMetaDataTableRecordVM(MetaDataTableVM mdVM, HexDocument doc, ulong startOffset, MDToken mdToken, TableInfo tableInfo)
			: base(mdVM, doc, startOffset, mdToken, tableInfo) {
		}

		static readonly IntegerHexBitFieldEnumInfo[] VarianceInfos = new IntegerHexBitFieldEnumInfo[] {
			new IntegerHexBitFieldEnumInfo(0, "NonVariant"),
			new IntegerHexBitFieldEnumInfo(1, "Covariant"),
			new IntegerHexBitFieldEnumInfo(2, "Contravariant"),
		};

		protected override HexField CreateField(ColumnInfo colInfo) {
			if (colInfo.Index == 1)
				return CreateGenericParamAttributesField(colInfo, doc, Name, startOffset);
			return base.CreateField(colInfo);
		}

		internal static UInt16FlagsHexField CreateGenericParamAttributesField(ColumnInfo colInfo, HexDocument doc, string name, ulong startOffset) {
			var field = new UInt16FlagsHexField(doc, name, colInfo.Name, startOffset + (uint)colInfo.Offset);
			field.Add(new IntegerHexBitField("Variance", 0, 2, VarianceInfos));
			field.Add(new BooleanHexBitField("Reference", 2));
			field.Add(new BooleanHexBitField("Struct", 3));
			field.Add(new BooleanHexBitField("Default ctor", 4));
			return field;
		}
	}

	sealed class MethodSpecMetaDataTableRecordVM : MetaDataTableRecordVM {
		public MethodSpecMetaDataTableRecordVM(MetaDataTableVM mdVM, HexDocument doc, ulong startOffset, MDToken mdToken, TableInfo tableInfo)
			: base(mdVM, doc, startOffset, mdToken, tableInfo) {
		}
	}

	sealed class GenericParamConstraintMetaDataTableRecordVM : MetaDataTableRecordVM {
		public GenericParamConstraintMetaDataTableRecordVM(MetaDataTableVM mdVM, HexDocument doc, ulong startOffset, MDToken mdToken, TableInfo tableInfo)
			: base(mdVM, doc, startOffset, mdToken, tableInfo) {
		}
	}
}
