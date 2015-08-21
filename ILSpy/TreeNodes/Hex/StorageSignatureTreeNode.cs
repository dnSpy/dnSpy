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
using dnSpy.NRefactory;
using ICSharpCode.Decompiler;

namespace dnSpy.TreeNodes.Hex {
	sealed class StorageSignatureTreeNode : HexTreeNode {
		public override NodePathName NodePathName {
			get { return new NodePathName("strgsig"); }
		}

		protected override object ViewObject {
			get { return storageSignatureVM; }
		}

		protected override IEnumerable<HexVM> HexVMs {
			get { yield return storageSignatureVM; }
		}

		protected override string IconName {
			get { return "BinaryFile"; }
		}

		readonly StorageSignatureVM storageSignatureVM;

		public StorageSignatureTreeNode(HexDocument doc, MetaDataHeader mdHeader)
			: base((ulong)mdHeader.StartOffset, (ulong)mdHeader.StorageHeaderOffset - 1) {
			this.storageSignatureVM = new StorageSignatureVM(this, doc, StartOffset, (int)(EndOffset - StartOffset + 1 - 0x10));
		}

		protected override void Write(ITextOutput output) {
			output.Write("Storage Signature", TextTokenType.InstanceField);
		}
	}
}
