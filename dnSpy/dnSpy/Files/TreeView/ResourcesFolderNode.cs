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
using dnlib.DotNet;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.TreeView;
using dnSpy.Properties;

namespace dnSpy.Files.TreeView {
	sealed class ResourcesFolderNode : FileTreeNodeData, IResourcesFolderNode {
		public override Guid Guid => new Guid(FileTVConstants.RESOURCES_FOLDER_NODE_GUID);
		protected override ImageReference GetIcon(IDotNetImageManager dnImgMgr) =>
			new ImageReference(GetType().Assembly, "FolderClosed");
		protected override ImageReference? GetExpandedIcon(IDotNetImageManager dnImgMgr) =>
			new ImageReference(GetType().Assembly, "FolderOpened");
		public override NodePathName NodePathName => new NodePathName(Guid);
		public override void Initialize() => TreeNode.LazyLoading = true;
		public override ITreeNodeGroup TreeNodeGroup { get; }

		readonly ModuleDef module;

		public ResourcesFolderNode(ITreeNodeGroup treeNodeGroup, ModuleDef module) {
			this.TreeNodeGroup = treeNodeGroup;
			this.module = module;
		}

		protected override void Write(IOutputColorWriter output, ILanguage language) =>
			output.Write(BoxedOutputColor.Text, dnSpy_Resources.ResourcesFolder);

		public override IEnumerable<ITreeNodeData> CreateChildren() {
			var treeNodeGroup = Context.FileTreeView.FileTreeNodeGroups.GetGroup(FileTreeNodeGroupType.ResourceTreeNodeGroup);
			foreach (var resource in module.Resources)
				yield return Context.ResourceNodeFactory.Create(module, resource, treeNodeGroup);
		}

		public override FilterType GetFilterType(IFileTreeNodeFilter filter) {
			var res = filter.GetResult(this);
			if (res.FilterType != FilterType.Default)
				return res.FilterType;
			return FilterType.CheckChildren;
		}
	}
}
