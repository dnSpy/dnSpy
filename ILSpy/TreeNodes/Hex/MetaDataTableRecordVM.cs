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

		public MetaDataTableRecordVM(MetaDataTableVM mdVM, HexDocument doc, ulong startOffset, MDToken mdToken, TableInfo tableInfo) {
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

		public override void OnDocumentModified(ulong modifiedStart, ulong modifiedEnd) {
			base.OnDocumentModified(modifiedStart, modifiedEnd);
			for (int i = 0; i < hexFields.Length; i++) {
				var field = hexFields[i];
				if (IsDynamicDescription(i) && HexUtils.IsModified(field.StartOffset, field.EndOffset, modifiedStart, modifiedEnd))
					InvalidateDescription(i);
			}
		}
	}
}
