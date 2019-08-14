/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
	sealed class AssemblyReferenceNodeImpl : AssemblyReferenceNode {
		public override AssemblyRef AssemblyRef { get; }
		public override Guid Guid => new Guid(DocumentTreeViewConstants.ASSEMBLYREF_NODE_GUID);
		protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) => dnImgMgr.GetImageReferenceAssemblyRef();
		public override NodePathName NodePathName => new NodePathName(Guid, AssemblyRef.FullName);
		public override ITreeNodeGroup? TreeNodeGroup { get; }

		readonly WeakReference asmRefOwnerModule;

		public AssemblyReferenceNodeImpl(ITreeNodeGroup treeNodeGroup, ModuleDef asmRefOwnerModule, AssemblyRef assemblyRef) {
			TreeNodeGroup = treeNodeGroup;
			this.asmRefOwnerModule = new WeakReference(asmRefOwnerModule);
			// Make sure we don't hold on to the original reference since it could prevent GC of the
			// owner module.
			AssemblyRef = assemblyRef.ToAssemblyRef();
			AssemblyRef.Rid = assemblyRef.Rid;
		}

		public override void Initialize() => TreeNode.LazyLoading = true;
		protected override void WriteCore(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options) {
			if ((options & DocumentNodeWriteOptions.ToolTip) != 0) {
				output.Write(AssemblyRef);
				output.WriteLine();
				WriteFilename(output);
			}
			else
				new NodeFormatter().Write(output, decompiler, AssemblyRef, GetShowToken(options));
		}

		public override IEnumerable<TreeNodeData> CreateChildren() {
			var document = Context.DocumentTreeView.DocumentService.Resolve(AssemblyRef, (ModuleDef?)asmRefOwnerModule.Target) as IDsDotNetDocument;
			if (document is null)
				yield break;
			var mod = document.ModuleDef;
			Debug2.Assert(!(mod is null));
			if (mod is null)
				yield break;
			foreach (var asmRef in mod.GetAssemblyRefs())
				yield return new AssemblyReferenceNodeImpl(Context.DocumentTreeView.DocumentTreeNodeGroups.GetGroup(DocumentTreeNodeGroupType.AssemblyRefTreeNodeGroupAssemblyRef), mod, asmRef);
		}

		public override FilterType GetFilterType(IDocumentTreeNodeFilter filter) => filter.GetResult(AssemblyRef).FilterType;
	}
}
