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
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Highlighting;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.TreeView;
using dnSpy.Decompiler.Shared;
using dnSpy.Shared.HexEditor;
using dnSpy.Shared.Highlighting;

namespace dnSpy.AsmEditor.Hex.Nodes {
	sealed class MetaDataTableNode : HexNode {
		public override Guid Guid {
			get { return new Guid(FileTVConstants.MDTBL_NODE_GUID); }
		}

		public override NodePathName NodePathName {
			get { return new NodePathName(Guid, ((byte)tablesStreamVM.Table).ToString()); }
		}

		public override object VMObject {
			get { return tablesStreamVM; }
		}

		protected override IEnumerable<HexVM> HexVMs {
			get { yield return tablesStreamVM; }
		}

		protected override string IconName {
			get { return "MetaData"; }
		}

		public override bool IsVirtualizingCollectionVM {
			get { return true; }
		}

		public MetaDataTableVM MetaDataTableVM {
			get { return tablesStreamVM; }
		}
		readonly MetaDataTableVM tablesStreamVM;

		// It could have tens of thousands of children so prevent loading all of them when
		// single-clicking the treenode
		public override bool SingleClickExpandsChildren {
			get { return false; }
		}

		public TableInfo TableInfo {
			get { return tableInfo; }
		}
		readonly TableInfo tableInfo;

		internal HexDocument Document {
			get { return doc; }
		}
		readonly HexDocument doc;

		public MetaDataTableNode(HexDocument doc, MDTable mdTable, IMetaData md)
			: base((ulong)mdTable.StartOffset, (ulong)mdTable.EndOffset - 1) {
			this.doc = doc;
			this.tableInfo = mdTable.TableInfo;
			this.tablesStreamVM = MetaDataTableVM.Create(this, doc, StartOffset, mdTable);
			this.tablesStreamVM.FindMetaDataTable = FindMetaDataTable;
			this.tablesStreamVM.InitializeHeapOffsets((ulong)md.StringsStream.StartOffset, (ulong)md.StringsStream.EndOffset - 1);
		}

		public override void Initialize() {
			TreeNode.LazyLoading = true;
		}

		protected override void Write(ISyntaxHighlightOutput output) {
			output.Write(string.Format("{0:X2}", (byte)tablesStreamVM.Table), BoxedTextTokenKind.Number);
			output.WriteSpace();
			output.Write(string.Format("{0}", tablesStreamVM.Table), BoxedTextTokenKind.Type);
			output.WriteSpace();
			output.Write("(", BoxedTextTokenKind.Operator);
			output.Write(string.Format("{0}", tablesStreamVM.Rows), BoxedTextTokenKind.Number);
			output.Write(")", BoxedTextTokenKind.Operator);
		}

		protected override void DecompileFields(ILanguage language, ITextOutput output) {
			language.WriteCommentLine(output, string.Empty);
			language.WriteCommentBegin(output, true);
			WriteHeader(output);
			language.WriteCommentEnd(output, true);
			output.WriteLine();

			for (int i = 0; i < (int)tablesStreamVM.Rows; i++) {
				var obj = tablesStreamVM.Get(i);
				language.WriteCommentBegin(output, true);
				Write(output, obj);
				language.WriteCommentEnd(output, true);
				output.WriteLine();
			}
		}

		public void WriteHeader(ITextOutput output) {
			var cols = tablesStreamVM.TableInfo.Columns;

			output.Write(string.Format("{0}\t{1}\t{2}", dnSpy_AsmEditor_Resources.RowIdentifier, dnSpy_AsmEditor_Resources.Token, dnSpy_AsmEditor_Resources.Offset), BoxedTextTokenKind.Comment);
			for (int i = 0; i < cols.Count; i++) {
				output.Write("\t", BoxedTextTokenKind.Comment);
				output.Write(tablesStreamVM.GetColumnName(i), BoxedTextTokenKind.Comment);
			}
			if (tablesStreamVM.HasInfo) {
				output.Write("\t", BoxedTextTokenKind.Comment);
				output.Write(tablesStreamVM.InfoName, BoxedTextTokenKind.Comment);
			}
			output.WriteLine();
		}

		public void Write(ITextOutput output, MetaDataTableRecordVM mdVM) {
			var cols = tablesStreamVM.TableInfo.Columns;

			output.Write(mdVM.RidString, BoxedTextTokenKind.Comment);
			output.Write("\t", BoxedTextTokenKind.Comment);
			output.Write(mdVM.TokenString, BoxedTextTokenKind.Comment);
			output.Write("\t", BoxedTextTokenKind.Comment);
			output.Write(mdVM.OffsetString, BoxedTextTokenKind.Comment);
			for (int j = 0; j < cols.Count; j++) {
				output.Write("\t", BoxedTextTokenKind.Comment);
				output.Write(mdVM.GetField(j).DataFieldVM.StringValue, BoxedTextTokenKind.Comment);
			}
			if (tablesStreamVM.HasInfo) {
				output.Write("\t", BoxedTextTokenKind.Comment);
				output.Write(mdVM.Info, BoxedTextTokenKind.Comment);
			}
			output.WriteLine();
		}

		public override IEnumerable<ITreeNodeData> CreateChildren() {
			Debug.Assert(TreeNode.Children.Count == 0);

			ulong offs = StartOffset;
			ulong rowSize = (ulong)MetaDataTableVM.TableInfo.RowSize;
			for (uint i = 0; i < MetaDataTableVM.Rows; i++) {
				yield return new MetaDataTableRecordNode(tableInfo, (int)i, offs, offs + rowSize - 1);
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
			int i = (int)((start - StartOffset) / (ulong)tableInfo.RowSize);
			int endi = (int)((end - StartOffset) / (ulong)tableInfo.RowSize);
			Debug.Assert(0 <= i && i <= endi && endi < TreeNode.Children.Count);
			while (i <= endi) {
				var obj = (MetaDataTableRecordNode)TreeNode.Children[i].Data;
				obj.OnDocumentModified(modifiedStart, modifiedEnd);
				i++;
			}
		}

		public MetaDataTableRecordNode FindTokenNode(uint token) {
			uint rid = token & 0x00FFFFFF;
			if (rid - 1 >= tablesStreamVM.Rows)
				return null;
			TreeNode.EnsureChildrenLoaded();
			return (MetaDataTableRecordNode)TreeNode.Children[(int)rid - 1].Data;
		}

		MetaDataTableVM FindMetaDataTable(Table table) {
			return ((TablesStreamNode)TreeNode.Parent.Data).FindMetaDataTable(table);
		}
	}
}
