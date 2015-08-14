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

using System.Collections.Generic;
using dnlib.DotNet.MD;
using dnSpy.HexEditor;
using ICSharpCode.Decompiler;
using ICSharpCode.NRefactory;

namespace dnSpy.TreeNodes.Hex {
	sealed class TablesStreamTreeNode : HexTreeNode {
		public override NodePathName NodePathName {
			get { return new NodePathName("tblstrm"); }
		}

		protected override object ViewObject {
			get { return tablesStreamVM; }
		}

		protected override IEnumerable<HexVM> HexVMs {
			get { yield return tablesStreamVM; }
		}

		protected override string IconName {
			get { return "MetaData"; }
		}

		readonly TablesStreamVM tablesStreamVM;

		public TablesStreamTreeNode(HexDocument doc, TablesStream tblStream)
			: base((ulong)tblStream.StartOffset, (ulong)tblStream.MDTables[0].StartOffset - 1) {
			this.tablesStreamVM = new TablesStreamVM(doc, tblStream);

			foreach (var mdTable in tblStream.MDTables) {
				if (mdTable.Rows != 0)
					this.Children.Add(new MetaDataTableTreeNode(doc, mdTable));
			}
		}

		public override void OnDocumentModified(ulong modifiedStart, ulong modifiedEnd) {
			base.OnDocumentModified(modifiedStart, modifiedEnd);

			foreach (HexTreeNode node in Children)
				node.OnDocumentModified(modifiedStart, modifiedEnd);
		}

		protected override void Write(ITextOutput output) {
			output.Write("Tables Stream", TextTokenType.InstanceField);
		}
	}
}
