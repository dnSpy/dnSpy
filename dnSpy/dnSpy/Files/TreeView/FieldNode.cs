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
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Files.TreeView {
	sealed class FieldNode : FileTreeNodeData, IFieldNode {
		public FieldDef FieldDef { get; }
		public override Guid Guid => new Guid(FileTVConstants.FIELD_NODE_GUID);
		public override NodePathName NodePathName => new NodePathName(Guid, FieldDef.FullName);
		IMDTokenProvider IMDTokenNode.Reference => FieldDef;
		protected override ImageReference GetIcon(IDotNetImageManager dnImgMgr) => dnImgMgr.GetImageReference(FieldDef);
		public override ITreeNodeGroup TreeNodeGroup { get; }

		public FieldNode(ITreeNodeGroup treeNodeGroup, FieldDef field) {
			this.TreeNodeGroup = treeNodeGroup;
			this.FieldDef = field;
		}

		protected override void Write(ITextColorWriter output, IDecompiler decompiler) =>
			new NodePrinter().Write(output, decompiler, FieldDef, Context.ShowToken);

		public override FilterType GetFilterType(IFileTreeNodeFilter filter) {
			var res = filter.GetResult(FieldDef);
			if (res.FilterType != FilterType.Default)
				return res.FilterType;
			if (Context.Decompiler.ShowMember(FieldDef))
				return FilterType.Visible;
			return FilterType.Hide;
		}
	}
}
