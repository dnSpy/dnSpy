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
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.TreeView;
using dnSpy.Decompiler;

namespace dnSpy.Documents.TreeView {
	sealed class AssemblyDocumentNodeImpl : AssemblyDocumentNode {
		public AssemblyDocumentNodeImpl(IDsDotNetDocument document)
			: base(document) => Debug.Assert(document.AssemblyDef != null);

		public override Guid Guid => new Guid(DocumentTreeViewConstants.ASSEMBLY_NODE_GUID);
		protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) => dnImgMgr.GetImageReference(Document.AssemblyDef);
		public override void Initialize() => TreeNode.LazyLoading = true;

		public override IEnumerable<TreeNodeData> CreateChildren() {
			foreach (var document in Document.Children)
				yield return Context.DocumentTreeView.CreateNode(this, document);
		}

		protected override void WriteCore(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options) {
			if ((options & DocumentNodeWriteOptions.ToolTip) == 0)
				new NodePrinter().Write(output, decompiler, Document.AssemblyDef, false, Context.ShowAssemblyVersion, Context.ShowAssemblyPublicKeyToken);
			else {
				output.Write(Document.AssemblyDef);

				output.WriteLine();
				output.Write(BoxedTextColor.Text, TargetFrameworkInfo.Create(Document.AssemblyDef.ManifestModule).ToString());

				output.WriteLine();
				output.Write(BoxedTextColor.Text, TargetFrameworkUtils.GetArchString(Document.AssemblyDef.ManifestModule));

				output.WriteLine();
				output.WriteFilename(Document.Filename);
			}
		}

		public override FilterType GetFilterType(IDocumentTreeNodeFilter filter) => filter.GetResult(Document.AssemblyDef).FilterType;
	}
}
