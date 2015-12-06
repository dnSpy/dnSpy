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
using System.Collections.Generic;
using System.Diagnostics;
using dnlib.DotNet;
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Highlighting;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.TreeView;
using dnSpy.Shared.UI.Files.TreeView;

namespace dnSpy.Files.TreeView {
	sealed class AssemblyReferenceNode : FileTreeNodeData, IAssemblyReferenceNode {
		public AssemblyRef AssemblyRef {
			get { return assemblyRef; }
		}
		readonly AssemblyRef assemblyRef;

		IMDTokenProvider IMDTokenNode.Reference {
			get { return AssemblyRef; }
		}

		public override Guid Guid {
			get { return new Guid(FileTVConstants.ASSEMBLYREF_NODE_GUID); }
		}

		protected override ImageReference GetIcon(IDotNetImageManager dnImgMgr) {
			return dnImgMgr.GetImageReferenceAssemblyRef();
		}

		public override NodePathName NodePathName {
			get { return new NodePathName(Guid, assemblyRef.FullName); }
		}

		public override ITreeNodeGroup TreeNodeGroup {
			get { return treeNodeGroup; }
		}
		readonly ITreeNodeGroup treeNodeGroup;

		readonly WeakReference asmRefOwnerModule;

		public AssemblyReferenceNode(ITreeNodeGroup treeNodeGroup, ModuleDef asmRefOwnerModule, AssemblyRef assemblyRef) {
			this.treeNodeGroup = treeNodeGroup;
			this.asmRefOwnerModule = new WeakReference(asmRefOwnerModule);
			// Make sure we don't hold on to the original reference since it could prevent GC of the
			// owner module.
			this.assemblyRef = assemblyRef.ToAssemblyRef();
			this.assemblyRef.Rid = assemblyRef.Rid;
		}

		public override void Initialize() {
			TreeNode.LazyLoading = true;
		}

		protected override void Write(ISyntaxHighlightOutput output, ILanguage language) {
			new NodePrinter().Write(output, language, assemblyRef, Context.ShowToken);
		}

		public override IEnumerable<ITreeNodeData> CreateChildren() {
			var file = Context.FileTreeView.FileManager.Resolve(assemblyRef, (ModuleDef)asmRefOwnerModule.Target) as IDnSpyDotNetFile;
			if (file == null)
				yield break;
			var mod = file.ModuleDef;
			Debug.Assert(mod != null);
			if (mod == null)
				yield break;
			foreach (var asmRef in mod.GetAssemblyRefs())
				yield return new AssemblyReferenceNode(Context.FileTreeView.FileTreeNodeGroups.GetGroup(FileTreeNodeGroupType.AssemblyRefTreeNodeGroupAssemblyRef), mod, asmRef);
		}
	}
}
