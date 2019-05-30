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
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Documents.TreeView {
	sealed class MessageNodeImpl : MessageNode {
		public override Guid Guid { get; }
		public override NodePathName NodePathName => new NodePathName(Guid);
		protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) => imgRef;
		public override string Message { get; }
		public override ITreeNodeGroup? TreeNodeGroup { get; }

		readonly ImageReference imgRef;

		public MessageNodeImpl(ITreeNodeGroup treeNodeGroup, Guid guid, ImageReference imgRef, string msg) {
			TreeNodeGroup = treeNodeGroup;
			Guid = guid;
			this.imgRef = imgRef;
			Message = msg;
		}

		protected override void WriteCore(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options) =>
			output.Write(BoxedTextColor.Text, Message);
		public override FilterType GetFilterType(IDocumentTreeNodeFilter filter) =>
			filter.GetResultOther(this).FilterType;
	}
}
