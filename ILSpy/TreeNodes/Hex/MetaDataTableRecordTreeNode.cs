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
using dnSpy.HexEditor;
using ICSharpCode.Decompiler;
using ICSharpCode.NRefactory;

namespace dnSpy.TreeNodes.Hex {
	sealed class MetaDataTableRecordTreeNode : HexTreeNode {
		public override NodePathName NodePathName {
			get { return new NodePathName("mdtblrec", index.ToString()); }
		}

		// Don't cache it since the VM object will never get freed (it's stored in a weak ref in
		// the virtualized list)
		protected override bool CanCacheUIObject {
			get { return false; }
		}

		protected override object ViewObject {
			get { return Record; }
		}

		protected override IEnumerable<HexVM> HexVMs {
			get { yield return Record; }
		}

		protected override string IconName {
			get { return "MetaData"; }
		}

		MetaDataTableTreeNode MDParent {
			get { return (MetaDataTableTreeNode)Parent; }
		}

		MetaDataTableRecordVM Record {
			get { return MDParent.MetaDataTableVM.Get(index); }
		}

		public override bool SingleClickExpandsChildren {
			get { return false; }
		}

		readonly int index;

		public MetaDataTableRecordTreeNode(HexDocument doc, int index, ulong startOffset, ulong endOffset)
			: base(startOffset, endOffset) {
			this.index = index;
		}

		protected override void Write(ITextOutput output) {
			output.Write(string.Format("{0}", index + 1), TextTokenType.Number);
		}
	}
}
