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
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Documents.TreeView {
	sealed class ModuleReferenceNode : DocumentTreeNodeData, IModuleReferenceNode {
		public override Guid Guid => new Guid(DocumentTreeViewConstants.MODULEREF_NODE_GUID);
		protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) => dnImgMgr.GetImageReferenceModuleRef();
		public ModuleRef ModuleRef { get; }
		IMDTokenProvider IMDTokenNode.Reference => ModuleRef;
		public override NodePathName NodePathName => new NodePathName(Guid, ModuleRef.FullName);
		public override ITreeNodeGroup TreeNodeGroup { get; }

		public ModuleReferenceNode(ITreeNodeGroup treeNodeGroup, ModuleRef moduleRef) {
			this.TreeNodeGroup = treeNodeGroup;
			this.ModuleRef = moduleRef;
		}

		protected override void Write(ITextColorWriter output, IDecompiler decompiler) =>
			new NodePrinter().Write(output, decompiler, ModuleRef, Context.ShowToken);
		public override FilterType GetFilterType(IDocumentTreeNodeFilter filter) =>
			filter.GetResult(ModuleRef).FilterType;
	}
}
