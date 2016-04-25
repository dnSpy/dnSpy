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
using System.Linq;
using dnlib.DotNet;
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Highlighting;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.TreeView;
using dnSpy.Decompiler.Shared;
using dnSpy.Languages;
using dnSpy.Shared.Files.TreeView;
using dnSpy.Shared.Highlighting;

namespace dnSpy.Files.TreeView {
	sealed class ModuleFileNode : DnSpyFileNode, IModuleFileNode {
		public ModuleFileNode(IDnSpyDotNetFile dnSpyFile)
			: base(dnSpyFile) {
			Debug.Assert(dnSpyFile.ModuleDef != null);
		}

		public new IDnSpyDotNetFile DnSpyFile {
			get { return (IDnSpyDotNetFile)base.DnSpyFile; }
		}

		public override Guid Guid {
			get { return new Guid(FileTVConstants.MODULE_NODE_GUID); }
		}

		IMDTokenProvider IMDTokenNode.Reference {
			get { return DnSpyFile.ModuleDef; }
		}

		protected override ImageReference GetIcon(IDotNetImageManager dnImgMgr) {
			return dnImgMgr.GetImageReference(DnSpyFile.ModuleDef);
		}

		public override void Initialize() {
			TreeNode.LazyLoading = true;
		}

		public override IEnumerable<ITreeNodeData> CreateChildren() {
			foreach (var file in DnSpyFile.Children)
				yield return Context.FileTreeView.CreateNode(this, file);

			yield return new ResourcesFolderNode(Context.FileTreeView.FileTreeNodeGroups.GetGroup(FileTreeNodeGroupType.ResourcesFolderTreeNodeGroupModule), DnSpyFile.ModuleDef);
			yield return new ReferencesFolderNode(Context.FileTreeView.FileTreeNodeGroups.GetGroup(FileTreeNodeGroupType.ReferencesFolderTreeNodeGroupModule), this);

			var nsDict = new Dictionary<string, List<TypeDef>>(StringComparer.Ordinal);
			foreach (var td in DnSpyFile.ModuleDef.Types) {
				List<TypeDef> list;
				var ns = UTF8String.ToSystemStringOrEmpty(td.Namespace);
				if (!nsDict.TryGetValue(ns, out list))
					nsDict.Add(ns, list = new List<TypeDef>());
				list.Add(td);
			}
			foreach (var kv in nsDict)
				yield return new NamespaceNode(Context.FileTreeView.FileTreeNodeGroups.GetGroup(FileTreeNodeGroupType.NamespaceTreeNodeGroupModule), kv.Key, kv.Value);
		}

		protected override void Write(ISyntaxHighlightOutput output, ILanguage language) {
			new NodePrinter().Write(output, language, DnSpyFile.ModuleDef, false);
		}

		protected override void WriteToolTip(ISyntaxHighlightOutput output, ILanguage language) {
			output.WriteModule(DnSpyFile.ModuleDef.Name);

			output.WriteLine();
			output.Write(TargetFrameworkInfo.Create(DnSpyFile.ModuleDef).ToString(), TextTokenKind.EnumField);

			output.WriteLine();
			output.Write(TargetFrameworkUtils.GetArchString(DnSpyFile.ModuleDef), TextTokenKind.EnumField);

			output.WriteLine();
			output.WriteFilename(DnSpyFile.Filename);
		}

		public INamespaceNode Create(string name) {
			return Context.FileTreeView.Create(name);
		}

		public INamespaceNode FindNode(string ns) {
			if (ns == null)
				return null;

			TreeNode.EnsureChildrenLoaded();
			foreach (var n in TreeNode.DataChildren.OfType<INamespaceNode>()) {
				if (n.Name == ns)
					return n;
			}

			return null;
		}

		public override FilterType GetFilterType(IFileTreeNodeFilter filter) {
			return filter.GetResult(DnSpyFile.ModuleDef).FilterType;
		}
	}
}
