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

using System;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Highlighting;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.TreeView;
using dnSpy.NRefactory;

namespace dnSpy.Files.TreeView {
	sealed class MessageNode : FileTreeNodeData, IMessageNode {
		public override Guid Guid {
			get { return guid; }
		}
		readonly Guid guid;

		public override NodePathName NodePathName {
			get { return new NodePathName(Guid); }
		}

		protected override ImageReference GetIcon(IDotNetImageManager dnImgMgr) {
			return imgRef;
		}

		public string Message {
			get { return msg; }
		}
		readonly string msg;

		public override ITreeNodeGroup TreeNodeGroup {
			get { return treeNodeGroup; }
		}
		readonly ITreeNodeGroup treeNodeGroup;

		readonly ImageReference imgRef;

		public MessageNode(ITreeNodeGroup treeNodeGroup, Guid guid, ImageReference imgRef, string msg) {
			this.treeNodeGroup = treeNodeGroup;
			this.guid = guid;
			this.imgRef = imgRef;
			this.msg = msg;
		}

		protected override void Write(ISyntaxHighlightOutput output, ILanguage language) {
			output.Write(msg, TextTokenType.Text);
		}
	}
}
