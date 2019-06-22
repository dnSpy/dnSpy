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
using dnSpy.AsmEditor.Properties;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Files.DotNet;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.TreeView;

namespace dnSpy.AsmEditor.Hex.Nodes {
	sealed class MetadataTableNode : HexNode {
		public override Guid Guid => new Guid(DocumentTreeViewConstants.MDTBL_NODE_GUID);
		public override NodePathName NodePathName => new NodePathName(Guid, ((byte)MetadataTableVM.Table).ToString());
		public override object VMObject => MetadataTableVM;

		protected override IEnumerable<HexVM> HexVMs {
			get { yield return MetadataTableVM; }
		}

		protected override ImageReference IconReference => DsImages.Metadata;
		public override bool IsVirtualizingCollectionVM => true;
		public MetadataTableVM MetadataTableVM { get; }

		// It could have tens of thousands of children so prevent loading all of them when
		// single-clicking the treenode
		public override bool SingleClickExpandsChildren => false;

		public TableInfo TableInfo { get; }
		internal HexBuffer Buffer => MetadataTableVM.Buffer;

		public MetadataTableNode(MetadataTableVM mdTable)
			: base(mdTable.Span) {
			TableInfo = mdTable.TableInfo;
			MetadataTableVM = mdTable;
			MetadataTableVM.Owner = this;
		}

		public override void Initialize() => TreeNode.LazyLoading = true;

		protected override void WriteCore(ITextColorWriter output, DocumentNodeWriteOptions options) {
			output.Write(BoxedTextColor.Number, ((byte)MetadataTableVM.Table).ToString("X2"));
			output.WriteSpace();
			output.Write(BoxedTextColor.HexTableName, MetadataTableVM.Table.ToString());
			output.WriteSpace();
			output.Write(BoxedTextColor.Punctuation, "(");
			output.Write(BoxedTextColor.Number, MetadataTableVM.Rows.ToString());
			output.Write(BoxedTextColor.Punctuation, ")");
		}

		protected override void DecompileFields(IDecompiler decompiler, IDecompilerOutput output) {
			decompiler.WriteCommentLine(output, string.Empty);
			decompiler.WriteCommentBegin(output, true);
			WriteHeader(output);
			decompiler.WriteCommentEnd(output, true);
			output.WriteLine();

			for (int i = 0; i < (int)MetadataTableVM.Rows; i++) {
				var obj = MetadataTableVM.Get(i);
				decompiler.WriteCommentBegin(output, true);
				Write(output, obj);
				decompiler.WriteCommentEnd(output, true);
				output.WriteLine();
			}
		}

		public void WriteHeader(IDecompilerOutput output) {
			var cols = MetadataTableVM.TableInfo.Columns;

			output.Write($"{dnSpy_AsmEditor_Resources.RowIdentifier}\t{dnSpy_AsmEditor_Resources.Token}\t{dnSpy_AsmEditor_Resources.Offset}", BoxedTextColor.Comment);
			for (int i = 0; i < cols.Count; i++) {
				output.Write("\t", BoxedTextColor.Comment);
				output.Write(MetadataTableVM.GetColumnName(i), BoxedTextColor.Comment);
			}
			if (MetadataTableVM.HasInfo) {
				output.Write("\t", BoxedTextColor.Comment);
				output.Write(MetadataTableVM.InfoName, BoxedTextColor.Comment);
			}
			output.WriteLine();
		}

		public void Write(IDecompilerOutput output, MetadataTableRecordVM mdVM) {
			var cols = MetadataTableVM.TableInfo.Columns;

			output.Write(mdVM.RidString, BoxedTextColor.Comment);
			output.Write("\t", BoxedTextColor.Comment);
			output.Write(mdVM.TokenString, BoxedTextColor.Comment);
			output.Write("\t", BoxedTextColor.Comment);
			output.Write(mdVM.OffsetString, BoxedTextColor.Comment);
			for (int j = 0; j < cols.Count; j++) {
				output.Write("\t", BoxedTextColor.Comment);
				output.Write(mdVM.GetField(j)!.DataFieldVM.StringValue, BoxedTextColor.Comment);
			}
			if (MetadataTableVM.HasInfo) {
				output.Write("\t", BoxedTextColor.Comment);
				output.Write(mdVM.Info, BoxedTextColor.Comment);
			}
			output.WriteLine();
		}

		public override IEnumerable<TreeNodeData> CreateChildren() {
			Debug.Assert(TreeNode.Children.Count == 0);

			var pos = Span.Start;
			ulong rowSize = (ulong)MetadataTableVM.TableInfo.RowSize;
			for (uint i = 0; i < MetadataTableVM.Rows; i++) {
				yield return new MetadataTableRecordNode(TableInfo, (int)i, pos, pos + rowSize);
				pos += rowSize;
			}
		}

		public override void OnBufferChanged(NormalizedHexChangeCollection changes) {
			base.OnBufferChanged(changes);

			if (TreeNode.Children.Count == 0)
				return;

			foreach (var change in changes) {
				if (!changes.OverlapsWith(Span))
					continue;

				var start = HexPosition.Max(Span.Start, change.OldSpan.Start);
				var end = HexPosition.Min(Span.End, change.OldSpan.End);
				int i = (int)((start - Span.Start).ToUInt64() / (ulong)TableInfo.RowSize);
				int endi = (int)((end - 1 - Span.Start).ToUInt64() / (ulong)TableInfo.RowSize);
				Debug.Assert(0 <= i && i <= endi && endi < TreeNode.Children.Count);
				while (i <= endi) {
					var obj = (MetadataTableRecordNode)TreeNode.Children[i].Data;
					obj.OnBufferChanged(changes);
					i++;
				}
			}
		}

		public MetadataTableRecordNode? FindTokenNode(uint token) {
			uint rid = token & 0x00FFFFFF;
			if (rid - 1 >= MetadataTableVM.Rows)
				return null;
			TreeNode.EnsureChildrenLoaded();
			return (MetadataTableRecordNode)TreeNode.Children[(int)rid - 1].Data;
		}
	}
}
