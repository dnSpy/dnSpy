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
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.TextEditor;
using dnSpy.Contracts.TreeView;
using dnSpy.Shared.Files.TreeView;

namespace dnSpy.Files.TreeView {
	sealed class MethodNode : FileTreeNodeData, IMethodNode {
		public override Guid Guid => new Guid(FileTVConstants.METHOD_NODE_GUID);
		public MethodDef MethodDef { get; }
		public override NodePathName NodePathName => new NodePathName(Guid, MethodDef.FullName);
		IMDTokenProvider IMDTokenNode.Reference => MethodDef;
		protected override ImageReference GetIcon(IDotNetImageManager dnImgMgr) => dnImgMgr.GetImageReference(MethodDef);
		public override ITreeNodeGroup TreeNodeGroup { get; }

		public MethodNode(ITreeNodeGroup treeNodeGroup, MethodDef methodDef) {
			this.TreeNodeGroup = treeNodeGroup;
			this.MethodDef = methodDef;
		}

		protected override void Write(IOutputColorWriter output, ILanguage language) =>
			new NodePrinter().Write(output, language, MethodDef, Context.ShowToken);

		public override FilterType GetFilterType(IFileTreeNodeFilter filter) {
			var res = filter.GetResult(MethodDef);
			if (res.FilterType != FilterType.Default)
				return res.FilterType;
			if (Context.Language.ShowMember(MethodDef))
				return FilterType.Visible;
			return FilterType.Hide;
		}
	}
}
