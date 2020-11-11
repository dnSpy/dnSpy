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
using dnSpy.AsmEditor.Hex.PE;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Files.DotNet;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Utilities;

namespace dnSpy.AsmEditor.Hex.Nodes {
	sealed class MetadataTableRecordNode : HexNode {
		public override Guid Guid => new Guid(DocumentTreeViewConstants.MDTBLREC_NODE_GUID);
		public override NodePathName NodePathName => new NodePathName(Guid, index.ToString());
		public override object VMObject => Record;

		protected override IEnumerable<HexVM> HexVMs {
			get { yield return Record; }
		}

		protected override ImageReference IconReference => DsImages.Metadata;
		MetadataTableNode MDParent => (MetadataTableNode)TreeNode.Parent!.Data;
		MetadataTableRecordVM Record => MDParent.MetadataTableVM.Get(index);
		public override bool SingleClickExpandsChildren => false;

		readonly int index;
		readonly (int[], Action<ITextColorWriter>)? infoTuple;

		public MetadataTableRecordNode(TableInfo tableInfo, int index, HexPosition startOffset, HexPosition endOffset)
			: base(HexSpan.FromBounds(startOffset, endOffset)) {
			this.index = index;
			infoTuple = GetInfoTuple(tableInfo);
		}

		protected override void WriteCore(ITextColorWriter output, DocumentNodeWriteOptions options) {
			output.Write(BoxedTextColor.Number, (index + 1).ToString());
			if (infoTuple is not null) {
				output.WriteSpace();
				output.Write(BoxedTextColor.Operator, "-");
				output.WriteSpace();
				infoTuple.Value.Item2(output);
			}
		}

		public override void OnBufferChanged(NormalizedHexChangeCollection changes) {
			if (infoTuple is not null) {
				var tableInfo = ((MetadataTableNode)TreeNode.Parent!.Data).TableInfo;
				foreach (var index in infoTuple.Value.Item1) {
					var col = tableInfo.Columns[index];
					var span = new HexSpan(Span.Start + (ulong)col.Offset, (ulong)col.Size);
					if (changes.OverlapsWith(span)) {
						TreeNode.RefreshUI();
						break;
					}
				}
			}
		}

		(int[], Action<ITextColorWriter>)? GetInfoTuple(TableInfo tableInfo) {
			switch (tableInfo.Table) {
			case Table.Module:					return (new int[] { 1 }, WriteModuleInfo);
			case Table.TypeRef:					return (new int[] { 1, 2 }, WriteTypeRefInfo);
			case Table.TypeDef:					return (new int[] { 1, 2 }, WriteTypeDefInfo);
			case Table.FieldPtr:				return null;
			case Table.Field:					return (new int[] { 1 }, WriteFieldInfo);
			case Table.MethodPtr:				return null;
			case Table.Method:					return (new int[] { 3 }, WriteMethodInfo);
			case Table.ParamPtr:				return null;
			case Table.Param:					return (new int[] { 2 }, WriteParamInfo);
			case Table.InterfaceImpl:			return null;
			case Table.MemberRef:				return (new int[] { 1 }, WriteMemberRefInfo);
			case Table.Constant:				return null;
			case Table.CustomAttribute:			return null;
			case Table.FieldMarshal:			return null;
			case Table.DeclSecurity:			return null;
			case Table.ClassLayout:				return null;
			case Table.FieldLayout:				return null;
			case Table.StandAloneSig:			return null;
			case Table.EventMap:				return null;
			case Table.EventPtr:				return null;
			case Table.Event:					return (new int[] { 1 }, WriteEventInfo);
			case Table.PropertyMap:				return null;
			case Table.PropertyPtr:				return null;
			case Table.Property:				return (new int[] { 1 }, WritePropertyInfo);
			case Table.MethodSemantics:			return null;
			case Table.MethodImpl:				return null;
			case Table.ModuleRef:				return (new int[] { 0 }, WriteModuleRefInfo);
			case Table.TypeSpec:				return null;
			case Table.ImplMap:					return (new int[] { 2 }, WriteImplMapInfo);
			case Table.FieldRVA:				return null;
			case Table.ENCLog:					return null;
			case Table.ENCMap:					return null;
			case Table.Assembly:				return (new int[] { 7 }, WriteAssemblyInfo);
			case Table.AssemblyProcessor:		return null;
			case Table.AssemblyOS:				return null;
			case Table.AssemblyRef:				return (new int[] { 6 }, WriteAssemblyRefInfo);
			case Table.AssemblyRefProcessor:	return null;
			case Table.AssemblyRefOS:			return null;
			case Table.File:					return (new int[] { 1 }, WriteFileInfo);
			case Table.ExportedType:			return (new int[] { 2, 3 }, WriteExportedTypeInfo);
			case Table.ManifestResource:		return (new int[] { 2 }, WriteManifestResourceInfo);
			case Table.NestedClass:				return null;
			case Table.GenericParam:			return (new int[] { 3 }, WriteGenericParamInfo);
			case Table.MethodSpec:				return null;
			case Table.GenericParamConstraint:	return null;
			case Table.Document:				return null;
			case Table.MethodDebugInformation:	return null;
			case Table.LocalScope:				return null;
			case Table.LocalVariable:			return null;
			case Table.LocalConstant:			return null;
			case Table.ImportScope:				return null;
			case Table.StateMachineMethod:		return null;
			case Table.CustomDebugInformation:	return null;
			default:							throw new InvalidOperationException();
			}
		}

		string ReadStringsHeap(int index) {
			var mdt = (MetadataTableNode)TreeNode.Parent!.Data;
			var tableInfo = mdt.TableInfo;
			var s = SimpleTypeConverter.ToString(mdt.MetadataTableVM.ReadStringsHeap(ReadFieldValue(mdt.Buffer, tableInfo.Columns[index])), false);
			Debug.Assert(s.Length >= 2);
			if (s.Length < 2)
				return s;
			return s.Substring(1, s.Length - 2);
		}

		uint ReadFieldValue(HexBuffer buffer, ColumnInfo col) {
			var start = Span.Start + (ulong)col.Offset;
			if (col.Size == 2)
				return buffer.ReadUInt16(start);
			else if (col.Size == 4)
				return buffer.ReadUInt32(start);
			throw new InvalidOperationException();
		}

		void WriteModuleInfo(ITextColorWriter output) => output.Write(BoxedTextColor.AssemblyModule, ReadStringsHeap(1));

		void WriteNamespaceName(ITextColorWriter output, int nsIndex, int nameIndex) {
			var ns = ReadStringsHeap(nsIndex);
			var name = ReadStringsHeap(nameIndex);

			output.Write(BoxedTextColor.Type, name);

			if (!string.IsNullOrEmpty(ns)) {
				output.WriteSpace();
				output.Write(BoxedTextColor.Operator, "-");
				output.WriteSpace();

				var parts = ns.Split('.');
				for (int i = 0; i < parts.Length; i++) {
					output.Write(BoxedTextColor.Namespace, parts[i]);
					if (i + 1 < parts.Length)
						output.Write(BoxedTextColor.Operator, ".");
				}
			}
		}

		void WriteTypeRefInfo(ITextColorWriter output) => WriteNamespaceName(output, 2, 1);
		void WriteTypeDefInfo(ITextColorWriter output) => WriteNamespaceName(output, 2, 1);
		void WriteFieldInfo(ITextColorWriter output) => output.Write(BoxedTextColor.InstanceField, ReadStringsHeap(1));
		void WriteMethodInfo(ITextColorWriter output) => output.Write(BoxedTextColor.InstanceMethod, ReadStringsHeap(3));
		void WriteParamInfo(ITextColorWriter output) => output.Write(BoxedTextColor.Parameter, ReadStringsHeap(2));
		void WriteMemberRefInfo(ITextColorWriter output) => output.Write(BoxedTextColor.InstanceMethod, ReadStringsHeap(1));
		void WriteEventInfo(ITextColorWriter output) => output.Write(BoxedTextColor.InstanceEvent, ReadStringsHeap(1));
		void WritePropertyInfo(ITextColorWriter output) => output.Write(BoxedTextColor.InstanceProperty, ReadStringsHeap(1));
		void WriteModuleRefInfo(ITextColorWriter output) => output.Write(BoxedTextColor.AssemblyModule, ReadStringsHeap(0));
		void WriteImplMapInfo(ITextColorWriter output) => output.Write(BoxedTextColor.InstanceMethod, ReadStringsHeap(2));
		void WriteAssemblyInfo(ITextColorWriter output) => output.Write(BoxedTextColor.Assembly, ReadStringsHeap(7));
		void WriteAssemblyRefInfo(ITextColorWriter output) => output.Write(BoxedTextColor.Assembly, ReadStringsHeap(6));
		void WriteFileInfo(ITextColorWriter output) => output.WriteFilename(ReadStringsHeap(1));
		void WriteExportedTypeInfo(ITextColorWriter output) => WriteNamespaceName(output, 3, 2);
		void WriteManifestResourceInfo(ITextColorWriter output) => output.WriteFilename(ReadStringsHeap(2));
		void WriteGenericParamInfo(ITextColorWriter output) => output.Write(BoxedTextColor.TypeGenericParameter, ReadStringsHeap(3));
	}
}
