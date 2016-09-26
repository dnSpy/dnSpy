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
using dnSpy.AsmEditor.Properties;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.HexEditor;
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

		protected override string IconName => "MetaData";

		readonly TablesStreamVM tablesStreamVM;

		public TablesStreamNode(HexDocument doc, TablesStream tblStream, IMetaData md)
			: base((ulong)tblStream.StartOffset, (ulong)tblStream.MDTables[0].StartOffset - 1) {
			this.tablesStreamVM = new TablesStreamVM(this, doc, tblStream);

			this.newChildren = new List<ITreeNodeData>();
			foreach (var mdTable in tblStream.MDTables) {
				if (mdTable.Rows != 0)
					this.newChildren.Add(new MetaDataTableNode(doc, mdTable, md));
			}
		}
		List<ITreeNodeData> newChildren;

		public override IEnumerable<ITreeNodeData> CreateChildren() {
			foreach (var c in newChildren)
				yield return c;
			newChildren = null;
		}

		public override void OnDocumentModified(ulong modifiedStart, ulong modifiedEnd) {
			base.OnDocumentModified(modifiedStart, modifiedEnd);

			foreach (HexNode node in TreeNode.DataChildren)
				node.OnDocumentModified(modifiedStart, modifiedEnd);
		}

		protected override void Write(ITextColorWriter output) =>
			output.Write(BoxedTextColor.InstanceField, dnSpy_AsmEditor_Resources.HexNode_TablesStream);

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
