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
using System.Diagnostics;
using dnlib.DotNet.MD;
using dnSpy.HexEditor;
using dnSpy.NRefactory;
using ICSharpCode.Decompiler;

namespace dnSpy.TreeNodes.Hex {
	sealed class MetaDataTableTreeNode : HexTreeNode {
		public override NodePathName NodePathName {
			get { return new NodePathName("mdtblstrm", ((byte)tablesStreamVM.Table).ToString()); }
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

		protected override bool IsVirtualizingCollectionVM {
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

		readonly HexDocument doc;

		public MetaDataTableTreeNode(HexDocument doc, MDTable mdTable, IMetaData md)
			: base((ulong)mdTable.StartOffset, (ulong)mdTable.EndOffset - 1) {
			LazyLoading = true;
			this.doc = doc;
			this.tablesStreamVM = MetaDataTableVM.Create(doc, StartOffset, mdTable);
			this.tablesStreamVM.InitializeHeapOffsets((ulong)md.StringsStream.StartOffset, (ulong)md.StringsStream.EndOffset - 1);
		}

		protected override void Write(ITextOutput output) {
			output.Write(string.Format("{0:X2}", (byte)tablesStreamVM.Table), TextTokenType.Number);
			output.WriteSpace();
			output.Write(string.Format("{0}", tablesStreamVM.Table), TextTokenType.Type);
			output.WriteSpace();
			output.Write('(', TextTokenType.Operator);
			output.Write(string.Format("{0}", tablesStreamVM.Rows), TextTokenType.Number);
			output.Write(')', TextTokenType.Operator);
		}

		protected override void LoadChildren() {
			Debug.Assert(Children.Count == 0);

			ulong offs = StartOffset;
			ulong rowSize = (ulong)MetaDataTableVM.TableInfo.RowSize;
			for (uint i = 0; i < MetaDataTableVM.Rows; i++) {
				Children.Add(new MetaDataTableRecordTreeNode(doc, (int)i, offs, offs + rowSize - 1));
				offs += rowSize;
            }
		}
	}
}
