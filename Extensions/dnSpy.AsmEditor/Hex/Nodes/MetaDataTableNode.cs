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
using dnlib.DotNet.MD;
using dnSpy.AsmEditor.Properties;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.HexEditor;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.TreeView;

namespace dnSpy.AsmEditor.Hex.Nodes {
	sealed class MetaDataTableNode : HexNode {
		public override Guid Guid => new Guid(DocumentTreeViewConstants.MDTBL_NODE_GUID);
		public override NodePathName NodePathName => new NodePathName(Guid, ((byte)MetaDataTableVM.Table).ToString());
		public override object VMObject => MetaDataTableVM;

		protected override IEnumerable<HexVM> HexVMs {
			get { yield return MetaDataTableVM; }
		}

		protected override ImageReference IconReference => DsImages.Metadata;
		public override bool IsVirtualizingCollectionVM => true;
		public MetaDataTableVM MetaDataTableVM { get; }

		// It could have tens of thousands of children so prevent loading all of them when
		// single-clicking the treenode
		public override bool SingleClickExpandsChildren => false;

		public TableInfo TableInfo { get; }
		internal HexDocument Document { get; }

		public MetaDataTableNode(HexDocument doc, MDTable mdTable, IMetaData md)
			: base((ulong)mdTable.StartOffset, (ulong)mdTable.EndOffset - 1) {
			this.Document = doc;
			this.TableInfo = mdTable.TableInfo;
			this.MetaDataTableVM = MetaDataTableVM.Create(this, doc, StartOffset, mdTable);
			this.MetaDataTableVM.FindMetaDataTable = FindMetaDataTable;
			this.MetaDataTableVM.InitializeHeapOffsets((ulong)md.StringsStream.StartOffset, (ulong)md.StringsStream.EndOffset - 1);
		}

		public override void Initialize() => TreeNode.LazyLoading = true;

		protected override void Write(ITextColorWriter output) {
			output.Write(BoxedTextColor.Number, string.Format("{0:X2}", (byte)MetaDataTableVM.Table));
			output.WriteSpace();
			output.Write(BoxedTextColor.Type, string.Format("{0}", MetaDataTableVM.Table));
			output.WriteSpace();
			output.Write(BoxedTextColor.Punctuation, "(");
			output.Write(BoxedTextColor.Number, string.Format("{0}", MetaDataTableVM.Rows));
			output.Write(BoxedTextColor.Punctuation, ")");
		}

		protected override void DecompileFields(IDecompiler decompiler, IDecompilerOutput output) {
			decompiler.WriteCommentLine(output, string.Empty);
			decompiler.WriteCommentBegin(output, true);
			WriteHeader(output);
			decompiler.WriteCommentEnd(output, true);
			output.WriteLine();

			for (int i = 0; i < (int)MetaDataTableVM.Rows; i++) {
				var obj = MetaDataTableVM.Get(i);
				decompiler.WriteCommentBegin(output, true);
				Write(output, obj);
				decompiler.WriteCommentEnd(output, true);
				output.WriteLine();
			}
		}

		public void WriteHeader(IDecompilerOutput output) {
			var cols = MetaDataTableVM.TableInfo.Columns;

			output.Write(string.Format("{0}\t{1}\t{2}", dnSpy_AsmEditor_Resources.RowIdentifier, dnSpy_AsmEditor_Resources.Token, dnSpy_AsmEditor_Resources.Offset), BoxedTextColor.Comment);
			for (int i = 0; i < cols.Count; i++) {
				output.Write("\t", BoxedTextColor.Comment);
				output.Write(MetaDataTableVM.GetColumnName(i), BoxedTextColor.Comment);
			}
			if (MetaDataTableVM.HasInfo) {
				output.Write("\t", BoxedTextColor.Comment);
				output.Write(MetaDataTableVM.InfoName, BoxedTextColor.Comment);
			}
			output.WriteLine();
		}

		public void Write(IDecompilerOutput output, MetaDataTableRecordVM mdVM) {
			var cols = MetaDataTableVM.TableInfo.Columns;

			output.Write(mdVM.RidString, BoxedTextColor.Comment);
			output.Write("\t", BoxedTextColor.Comment);
			output.Write(mdVM.TokenString, BoxedTextColor.Comment);
			output.Write("\t", BoxedTextColor.Comment);
			output.Write(mdVM.OffsetString, BoxedTextColor.Comment);
			for (int j = 0; j < cols.Count; j++) {
				output.Write("\t", BoxedTextColor.Comment);
				output.Write(mdVM.GetField(j).DataFieldVM.StringValue, BoxedTextColor.Comment);
			}
			if (MetaDataTableVM.HasInfo) {
				output.Write("\t", BoxedTextColor.Comment);
				output.Write(mdVM.Info, BoxedTextColor.Comment);
			}
			output.WriteLine();
		}

		public override IEnumerable<ITreeNodeData> CreateChildren() {
			Debug.Assert(TreeNode.Children.Count == 0);

			ulong offs = StartOffset;
			ulong rowSize = (ulong)MetaDataTableVM.TableInfo.RowSize;
			for (uint i = 0; i < MetaDataTableVM.Rows; i++) {
				yield return new MetaDataTableRecordNode(TableInfo, (int)i, offs, offs + rowSize - 1);
				offs += rowSize;
			}
		}

		public override void OnDocumentModified(ulong modifiedStart, ulong modifiedEnd) {
			base.OnDocumentModified(modifiedStart, modifiedEnd);

			if (TreeNode.Children.Count == 0)
				return;

			if (!HexUtils.IsModified(StartOffset, EndOffset, modifiedStart, modifiedEnd))
				return;

			ulong start = Math.Max(StartOffset, modifiedStart);
			ulong end = Math.Min(EndOffset, modifiedEnd);
			int i = (int)((start - StartOffset) / (ulong)TableInfo.RowSize);
			int endi = (int)((end - StartOffset) / (ulong)TableInfo.RowSize);
			Debug.Assert(0 <= i && i <= endi && endi < TreeNode.Children.Count);
			while (i <= endi) {
				var obj = (MetaDataTableRecordNode)TreeNode.Children[i].Data;
				obj.OnDocumentModified(modifiedStart, modifiedEnd);
				i++;
			}
		}

		public MetaDataTableRecordNode FindTokenNode(uint token) {
			uint rid = token & 0x00FFFFFF;
			if (rid - 1 >= MetaDataTableVM.Rows)
				return null;
			TreeNode.EnsureChildrenLoaded();
			return (MetaDataTableRecordNode)TreeNode.Children[(int)rid - 1].Data;
		}

		MetaDataTableVM FindMetaDataTable(Table table) => ((TablesStreamNode)TreeNode.Parent.Data).FindMetaDataTable(table);
	}
}
