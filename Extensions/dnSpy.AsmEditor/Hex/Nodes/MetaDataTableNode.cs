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
using dnSpy.AsmEditor.Hex.PE;
using dnSpy.AsmEditor.Properties;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Hex;
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
		internal HexBuffer Buffer => MetaDataTableVM.Buffer;

		public MetaDataTableNode(MetaDataTableVM mdTable)
			: base(mdTable.Span) {
			TableInfo = mdTable.TableInfo;
			MetaDataTableVM = mdTable;
			MetaDataTableVM.Owner = this;
		}

		public override void Initialize() => TreeNode.LazyLoading = true;

		protected override void WriteCore(ITextColorWriter output, DocumentNodeWriteOptions options) {
			output.Write(BoxedTextColor.Number, string.Format("{0:X2}", (byte)MetaDataTableVM.Table));
			output.WriteSpace();
			output.Write(BoxedTextColor.HexTableName, string.Format("{0}", MetaDataTableVM.Table));
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

		public override IEnumerable<TreeNodeData> CreateChildren() {
			Debug.Assert(TreeNode.Children.Count == 0);

			var pos = Span.Start;
			ulong rowSize = (ulong)MetaDataTableVM.TableInfo.RowSize;
			for (uint i = 0; i < MetaDataTableVM.Rows; i++) {
				yield return new MetaDataTableRecordNode(TableInfo, (int)i, pos, pos + rowSize);
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
					var obj = (MetaDataTableRecordNode)TreeNode.Children[i].Data;
					obj.OnBufferChanged(changes);
					i++;
				}
			}
		}

		public MetaDataTableRecordNode FindTokenNode(uint token) {
			uint rid = token & 0x00FFFFFF;
			if (rid - 1 >= MetaDataTableVM.Rows)
				return null;
			TreeNode.EnsureChildrenLoaded();
			return (MetaDataTableRecordNode)TreeNode.Children[(int)rid - 1].Data;
		}
	}
}
