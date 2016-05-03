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
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Highlighting;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.TreeView;
using dnSpy.Decompiler.Shared;
using dnSpy.Shared.Files.TreeView;

namespace dnSpy.Files.TreeView {
	sealed class MessageNode : FileTreeNodeData, IMessageNode {
		public override Guid Guid { get; }
		public override NodePathName NodePathName => new NodePathName(Guid);
		protected override ImageReference GetIcon(IDotNetImageManager dnImgMgr) => imgRef;
		public string Message { get; }
		public override ITreeNodeGroup TreeNodeGroup { get; }

		readonly ImageReference imgRef;

		public MessageNode(ITreeNodeGroup treeNodeGroup, Guid guid, ImageReference imgRef, string msg) {
			this.TreeNodeGroup = treeNodeGroup;
			this.Guid = guid;
			this.imgRef = imgRef;
			this.Message = msg;
		}

		protected override void Write(ISyntaxHighlightOutput output, ILanguage language) =>
			output.Write(Message, BoxedTextTokenKind.Text);
		public override FilterType GetFilterType(IFileTreeNodeFilter filter) =>
			filter.GetResult(this).FilterType;
	}
}
