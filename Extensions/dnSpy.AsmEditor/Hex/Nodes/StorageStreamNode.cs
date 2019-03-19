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
using dnSpy.AsmEditor.Hex.PE;
using dnSpy.AsmEditor.Properties;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Files.DotNet;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.TreeView;

namespace dnSpy.AsmEditor.Hex.Nodes {
	class StorageStreamNode : HexNode {
		public override Guid Guid => new Guid(DocumentTreeViewConstants.STRGSTREAM_NODE_GUID);
		public override NodePathName NodePathName => new NodePathName(Guid, StreamNumber.ToString());
		public DotNetHeapKind HeapKind => storageStreamVM.HeapKind;
		public override object VMObject => storageStreamVM;
		protected override ImageReference IconReference => DsImages.BinaryFile;
		public int StreamNumber => storageStreamVM.StreamNumber;

		protected override IEnumerable<HexVM> HexVMs {
			get { yield return storageStreamVM; }
		}

		readonly StorageStreamVM storageStreamVM;

		public StorageStreamNode(StorageStreamVM storageStream)
			: base(storageStream.Span) => storageStreamVM = storageStream;

		public override void OnBufferChanged(NormalizedHexChangeCollection changes) {
			base.OnBufferChanged(changes);
			if (changes.OverlapsWith(storageStreamVM.RCNameVM.Span))
				TreeNode.RefreshUI();

			foreach (HexNode node in TreeNode.DataChildren)
				node.OnBufferChanged(changes);
		}

		protected override void WriteCore(ITextColorWriter output, DocumentNodeWriteOptions options) {
			output.Write(BoxedTextColor.HexStorageStream, dnSpy_AsmEditor_Resources.HexNode_StorageStream);
			output.WriteSpace();
			output.Write(BoxedTextColor.Operator, "#");
			output.Write(BoxedTextColor.Number, StreamNumber.ToString());
			output.Write(BoxedTextColor.Punctuation, ":");
			output.WriteSpace();
			output.Write(HeapKind == DotNetHeapKind.Unknown ? BoxedTextColor.HexStorageStreamNameInvalid : BoxedTextColor.HexStorageStreamName, storageStreamVM.RCNameVM.StringZ);
		}

		public MetadataTableRecordNode FindTokenNode(uint token) {
			if (HeapKind != DotNetHeapKind.Tables)
				return null;
			return ((TablesStreamNode)TreeNode.Children[0].Data).FindTokenNode(token);
		}
	}

	sealed class TablesStorageStreamNode : StorageStreamNode {
		readonly TablesStreamVM tablesStream;

		public TablesStorageStreamNode(StorageStreamVM storageStream, TablesStreamVM tablesStream)
			: base(storageStream) => this.tablesStream = tablesStream;

		public override IEnumerable<TreeNodeData> CreateChildren() {
			yield return new TablesStreamNode(tablesStream);
		}

		protected override IEnumerable<HexSpan> Spans {
			get {
				yield return Span;
				yield return tablesStream.Span;
			}
		}
	}
}
