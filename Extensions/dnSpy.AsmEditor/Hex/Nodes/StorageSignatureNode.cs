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
using dnlib.DotNet.MD;
using dnSpy.AsmEditor.Hex.PE;
using dnSpy.AsmEditor.Properties;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text;

namespace dnSpy.AsmEditor.Hex.Nodes {
	sealed class StorageSignatureNode : HexNode {
		public override Guid Guid => new Guid(DocumentTreeViewConstants.STRGSIG_NODE_GUID);
		public override NodePathName NodePathName => new NodePathName(Guid);
		public override object VMObject => storageSignatureVM;
		protected override ImageReference IconReference => DsImages.BinaryFile;

		protected override IEnumerable<HexVM> HexVMs {
			get { yield return storageSignatureVM; }
		}

		readonly StorageSignatureVM storageSignatureVM;

		public StorageSignatureNode(HexBuffer buffer, MetaDataHeader mdHeader)
			: base(HexSpan.FromBounds((ulong)mdHeader.StartOffset, (ulong)mdHeader.StorageHeaderOffset)) {
			storageSignatureVM = new StorageSignatureVM(buffer, Span.Start, (int)(Span.Length - 0x10).ToUInt64());
		}

		protected override void WriteCore(ITextColorWriter output, DocumentNodeWriteOptions options) =>
			output.Write(BoxedTextColor.HexStorageSignature, dnSpy_AsmEditor_Resources.HexNode_StorageSignature);
	}
}
