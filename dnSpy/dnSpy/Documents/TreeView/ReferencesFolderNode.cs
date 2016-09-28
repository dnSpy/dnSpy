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
using System.Diagnostics;
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.TreeView;
using dnSpy.Properties;

namespace dnSpy.Documents.TreeView {
	sealed class ReferencesFolderNode : DocumentTreeNodeData, IReferencesFolderNode {
		public override Guid Guid => new Guid(DocumentTreeViewConstants.REFERENCES_FOLDER_NODE_GUID);
		protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) => DsImages.Reference;
		public override NodePathName NodePathName => new NodePathName(Guid);
		public override void Initialize() => TreeNode.LazyLoading = true;
		public override ITreeNodeGroup TreeNodeGroup { get; }

		readonly IModuleDocumentNode moduleNode;

		public ReferencesFolderNode(ITreeNodeGroup treeNodeGroup, IModuleDocumentNode moduleNode) {
			Debug.Assert(moduleNode.Document.ModuleDef != null);
			this.TreeNodeGroup = treeNodeGroup;
			this.moduleNode = moduleNode;
		}

		protected override void Write(ITextColorWriter output, IDecompiler decompiler) =>
			output.Write(BoxedTextColor.Text, dnSpy_Resources.ReferencesFolder);

		public override IEnumerable<ITreeNodeData> CreateChildren() {
			foreach (var asmRef in moduleNode.Document.ModuleDef.GetAssemblyRefs())
				yield return new AssemblyReferenceNode(Context.DocumentTreeView.DocumentTreeNodeGroups.GetGroup(DocumentTreeNodeGroupType.AssemblyRefTreeNodeGroupReferences), moduleNode.Document.ModuleDef, asmRef);
			foreach (var modRef in moduleNode.Document.ModuleDef.GetModuleRefs())
				yield return new ModuleReferenceNode(Context.DocumentTreeView.DocumentTreeNodeGroups.GetGroup(DocumentTreeNodeGroupType.ModuleRefTreeNodeGroupReferences), modRef);
		}

		public IAssemblyReferenceNode Create(AssemblyRef asmRef) =>
			Context.DocumentTreeView.Create(asmRef, moduleNode.Document.ModuleDef);
		public IModuleReferenceNode Create(ModuleRef modRef) => Context.DocumentTreeView.Create(modRef);
		public override FilterType GetFilterType(IDocumentTreeNodeFilter filter) =>
			filter.GetResult(this).FilterType;
	}
}
