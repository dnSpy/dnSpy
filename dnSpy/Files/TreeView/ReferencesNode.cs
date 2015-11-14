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
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Highlighting;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.TreeView;
using dnSpy.NRefactory;

namespace dnSpy.Files.TreeView {
	sealed class ReferencesNode : FileTreeNodeData, IReferencesNode {
		public override Guid Guid {
			get { return new Guid(FileTVConstants.DNSPY_REFERENCES_NODE_GUID); }
		}

		protected override ImageReference GetIcon(IDotNetImageManager dnImgMgr) {
			return new ImageReference(GetType().Assembly, "ReferenceFolderClosed");
		}

		protected override ImageReference? GetExpandedIcon(IDotNetImageManager dnImgMgr) {
			return new ImageReference(GetType().Assembly, "ReferenceFolderOpened");
		}

		public override NodePathName NodePathName {
			get { return new NodePathName(Guid); }
		}

		public override void Initialize() {
			TreeNode.LazyLoading = true;
		}

		public override ITreeNodeGroup TreeNodeGroup {
			get { return treeNodeGroup; }
		}
		readonly ITreeNodeGroup treeNodeGroup;

		readonly IModuleFileNode moduleNode;

		public ReferencesNode(ITreeNodeGroup treeNodeGroup, IModuleFileNode moduleNode) {
			Debug.Assert(moduleNode.DnSpyFile.ModuleDef != null);
			this.treeNodeGroup = treeNodeGroup;
			this.moduleNode = moduleNode;
		}

		protected override void Write(ISyntaxHighlightOutput output, ILanguage language) {
			output.Write("References", TextTokenType.Text);
		}

		public override IEnumerable<ITreeNodeData> CreateChildren() {
			foreach (var asmRef in moduleNode.DnSpyFile.ModuleDef.GetAssemblyRefs())
				yield return new AssemblyReferenceNode(TreeNodeGroups.AssemblyRefTreeNodeGroupReferences, moduleNode.DnSpyFile.ModuleDef, asmRef);
			foreach (var modRef in moduleNode.DnSpyFile.ModuleDef.GetModuleRefs())
				yield return new ModuleReferenceNode(TreeNodeGroups.ModuleRefTreeNodeGroupReferences, modRef);
		}

		public IAssemblyReferenceNode Create(AssemblyRef asmRef) {
			return Context.FileTreeView.Create(asmRef, moduleNode.DnSpyFile.ModuleDef);
		}

		public IModuleReferenceNode Create(ModuleRef modRef) {
			return Context.FileTreeView.Create(modRef);
		}
	}
}
