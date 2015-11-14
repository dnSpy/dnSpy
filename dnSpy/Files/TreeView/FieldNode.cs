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
using dnlib.DotNet;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Highlighting;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.TreeView;
using dnSpy.Shared.UI.Files.TreeView;

namespace dnSpy.Files.TreeView {
	sealed class FieldNode : FileTreeNodeData, IFieldNode {
		public FieldDef FieldDef {
			get { return field; }
		}
		readonly FieldDef field;

		public override Guid Guid {
			get { return new Guid(FileTVConstants.DNSPY_FIELD_NODE_GUID); }
		}

		public override NodePathName NodePathName {
			get { return new NodePathName(Guid, field.FullName); }
		}

		IMDTokenProvider IMDTokenNode.Reference {
			get { return field; }
		}

		protected override ImageReference GetIcon(IDotNetImageManager dnImgMgr) {
			return dnImgMgr.GetImageReference(field);
		}

		public override ITreeNodeGroup TreeNodeGroup {
			get { return treeNodeGroup; }
		}
		readonly ITreeNodeGroup treeNodeGroup;

		public FieldNode(ITreeNodeGroup treeNodeGroup, FieldDef field) {
			this.treeNodeGroup = treeNodeGroup;
			this.field = field;
		}

		protected override void Write(ISyntaxHighlightOutput output, ILanguage language) {
			new NodePrinter().Write(output, language, field, Context.ShowToken);
		}
	}
}
