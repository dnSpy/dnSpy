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
	sealed class ModuleReferenceNode : FileTreeNodeData, IModuleReferenceNode {
		public override Guid Guid => new Guid(FileTVConstants.MODULEREF_NODE_GUID);
		protected override ImageReference GetIcon(IDotNetImageManager dnImgMgr) => dnImgMgr.GetImageReferenceModuleRef();
		public ModuleRef ModuleRef { get; }
		IMDTokenProvider IMDTokenNode.Reference => ModuleRef;
		public override NodePathName NodePathName => new NodePathName(Guid, ModuleRef.FullName);
		public override ITreeNodeGroup TreeNodeGroup { get; }

		public ModuleReferenceNode(ITreeNodeGroup treeNodeGroup, ModuleRef moduleRef) {
			this.TreeNodeGroup = treeNodeGroup;
			this.ModuleRef = moduleRef;
		}

		protected override void Write(IOutputColorWriter output, ILanguage language) =>
			new NodePrinter().Write(output, language, ModuleRef, Context.ShowToken);
		public override FilterType GetFilterType(IFileTreeNodeFilter filter) =>
			filter.GetResult(ModuleRef).FilterType;
	}
}
