/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using System.Linq;
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.TreeView;
using dnSpy.Decompiler;

namespace dnSpy.Documents.TreeView {
	sealed class ModuleDocumentNodeImpl : ModuleDocumentNode {
		public ModuleDocumentNodeImpl(IDsDotNetDocument document)
			: base(document) {
		}

		public override Guid Guid => new Guid(DocumentTreeViewConstants.MODULE_NODE_GUID);

		protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) =>
			dnImgMgr.GetImageReference(Document.ModuleDef);
		public override void Initialize() => TreeNode.LazyLoading = true;

		public override IEnumerable<TreeNodeData> CreateChildren() {
			foreach (var document in Document.Children)
				yield return Context.DocumentTreeView.CreateNode(this, document);

			yield return new ResourcesFolderNodeImpl(Context.DocumentTreeView.DocumentTreeNodeGroups.GetGroup(DocumentTreeNodeGroupType.ResourcesFolderTreeNodeGroupModule), Document.ModuleDef);
			yield return new ReferencesFolderNodeImpl(Context.DocumentTreeView.DocumentTreeNodeGroups.GetGroup(DocumentTreeNodeGroupType.ReferencesFolderTreeNodeGroupModule), this);

			var nsDict = new Dictionary<string, List<TypeDef>>(StringComparer.Ordinal);
			foreach (var td in Document.ModuleDef.Types) {
				List<TypeDef> list;
				var ns = UTF8String.ToSystemStringOrEmpty(td.Namespace);
				if (!nsDict.TryGetValue(ns, out list))
					nsDict.Add(ns, list = new List<TypeDef>());
				list.Add(td);
			}
			foreach (var kv in nsDict)
				yield return new NamespaceNodeImpl(Context.DocumentTreeView.DocumentTreeNodeGroups.GetGroup(DocumentTreeNodeGroupType.NamespaceTreeNodeGroupModule), kv.Key, kv.Value);
		}

		protected override void WriteCore(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options) {
			if ((options & DocumentNodeWriteOptions.ToolTip) == 0)
				new NodePrinter().Write(output, decompiler, Document.ModuleDef, false);
			else {
				output.WriteModule(Document.ModuleDef.Name);

				output.WriteLine();
				output.Write(BoxedTextColor.Text, TargetFrameworkInfo.Create(Document.ModuleDef).ToString());

				output.WriteLine();
				output.Write(BoxedTextColor.Text, TargetFrameworkUtils.GetArchString(Document.ModuleDef));

				output.WriteLine();
				output.WriteFilename(Document.Filename);
			}
		}

		public override NamespaceNode FindNode(string ns) {
			if (ns == null)
				return null;

			TreeNode.EnsureChildrenLoaded();
			foreach (var n in TreeNode.DataChildren.OfType<NamespaceNode>()) {
				if (n.Name == ns)
					return n;
			}

			return null;
		}

		public override FilterType GetFilterType(IDocumentTreeNodeFilter filter) =>
			filter.GetResult(Document.ModuleDef).FilterType;
	}
}
