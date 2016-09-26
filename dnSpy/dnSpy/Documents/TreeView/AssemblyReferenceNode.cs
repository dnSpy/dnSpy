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
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Documents.TreeView {
	sealed class AssemblyReferenceNode : DocumentTreeNodeData, IAssemblyReferenceNode {
		public AssemblyRef AssemblyRef { get; }
		IMDTokenProvider IMDTokenNode.Reference => AssemblyRef;
		public override Guid Guid => new Guid(DocumentTreeViewConstants.ASSEMBLYREF_NODE_GUID);
		protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) => dnImgMgr.GetImageReferenceAssemblyRef();
		public override NodePathName NodePathName => new NodePathName(Guid, AssemblyRef.FullName);
		public override ITreeNodeGroup TreeNodeGroup { get; }

		readonly WeakReference asmRefOwnerModule;

		public AssemblyReferenceNode(ITreeNodeGroup treeNodeGroup, ModuleDef asmRefOwnerModule, AssemblyRef assemblyRef) {
			this.TreeNodeGroup = treeNodeGroup;
			this.asmRefOwnerModule = new WeakReference(asmRefOwnerModule);
			// Make sure we don't hold on to the original reference since it could prevent GC of the
			// owner module.
			this.AssemblyRef = assemblyRef.ToAssemblyRef();
			this.AssemblyRef.Rid = assemblyRef.Rid;
		}

		public override void Initialize() => TreeNode.LazyLoading = true;
		protected override void Write(ITextColorWriter output, IDecompiler decompiler) =>
			new NodePrinter().Write(output, decompiler, AssemblyRef, Context.ShowToken);

		public override IEnumerable<ITreeNodeData> CreateChildren() {
			var document = Context.DocumentTreeView.DocumentService.Resolve(AssemblyRef, (ModuleDef)asmRefOwnerModule.Target) as IDsDotNetDocument;
			if (document == null)
				yield break;
			var mod = document.ModuleDef;
			Debug.Assert(mod != null);
			if (mod == null)
				yield break;
			foreach (var asmRef in mod.GetAssemblyRefs())
				yield return new AssemblyReferenceNode(Context.DocumentTreeView.DocumentTreeNodeGroups.GetGroup(DocumentTreeNodeGroupType.AssemblyRefTreeNodeGroupAssemblyRef), mod, asmRef);
		}

		public override FilterType GetFilterType(IDocumentTreeNodeFilter filter) => filter.GetResult(AssemblyRef).FilterType;
	}
}
