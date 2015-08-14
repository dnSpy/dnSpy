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
	sealed class MetaDataTableTreeNode : HexTreeNode {
		public override NodePathName NodePathName {
			get { return new NodePathName("tblstrm", ((byte)tablesStreamVM.Table).ToString()); }
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

		readonly MetaDataTableVM tablesStreamVM;

		public MetaDataTableTreeNode(HexDocument doc, MDTable mdTable)
			: base((ulong)mdTable.StartOffset, (ulong)mdTable.EndOffset - 1) {
			this.tablesStreamVM = MetaDataTableVM.Create(doc, StartOffset, mdTable);
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
	}
}
