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
using System.Linq;
using dnlib.DotNet.MD;
using dnSpy.AsmEditor.Hex.PE;
using dnSpy.AsmEditor.Properties;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.TreeView;

namespace dnSpy.AsmEditor.Hex.Nodes {
	sealed class TablesStreamNode : HexNode {
		public override Guid Guid => new Guid(DocumentTreeViewConstants.TBLSSTREAM_NODE_GUID);
		public override NodePathName NodePathName => new NodePathName(Guid);
		public override object VMObject => tablesStreamVM;

		protected override IEnumerable<HexVM> HexVMs {
			get { yield return tablesStreamVM; }
		}

		protected override ImageReference IconReference => DsImages.Metadata;

		readonly TablesStreamVM tablesStreamVM;

		public TablesStreamNode(HexBuffer buffer, TablesStream tblStream, IMetaData md)
			: base(HexSpan.FromBounds((ulong)tblStream.StartOffset, (ulong)tblStream.MDTables[0].StartOffset)) {
			tablesStreamVM = new TablesStreamVM(buffer, tblStream);

			newChildren = new List<TreeNodeData>();
			foreach (var mdTable in tblStream.MDTables) {
				if (mdTable.Rows != 0)
					newChildren.Add(new MetaDataTableNode(buffer, mdTable, md));
			}
		}
		List<TreeNodeData> newChildren;

		public override IEnumerable<TreeNodeData> CreateChildren() {
			foreach (var c in newChildren)
				yield return c;
			newChildren = null;
		}

		public override void OnBufferChanged(NormalizedHexChangeCollection changes) {
			base.OnBufferChanged(changes);

			foreach (HexNode node in TreeNode.DataChildren)
				node.OnBufferChanged(changes);
		}

		protected override void WriteCore(ITextColorWriter output, DocumentNodeWriteOptions options) =>
			output.Write(BoxedTextColor.HexTablesStream, dnSpy_AsmEditor_Resources.HexNode_TablesStream);

		public MetaDataTableRecordNode FindTokenNode(uint token) {
			var mdTblNode = (MetaDataTableNode)TreeNode.DataChildren.FirstOrDefault(a => ((MetaDataTableNode)a).TableInfo.Table == (Table)(token >> 24));
			return mdTblNode?.FindTokenNode(token);
		}

		public MetaDataTableVM FindMetaDataTable(Table table) {
			foreach (MetaDataTableNode node in TreeNode.DataChildren) {
				if (node.TableInfo.Table == table)
					return node.MetaDataTableVM;
			}
			return null;
		}
	}
}
