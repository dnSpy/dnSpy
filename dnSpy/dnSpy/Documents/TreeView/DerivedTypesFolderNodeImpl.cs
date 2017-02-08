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
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.TreeView;
using dnSpy.Properties;

namespace dnSpy.Documents.TreeView {
	sealed class DerivedTypesFolderNodeImpl : DerivedTypesFolderNode {
		public override Guid Guid => new Guid(DocumentTreeViewConstants.DERIVEDTYPESFOLDER_NODE_GUID);
		public override NodePathName NodePathName => new NodePathName(Guid);
		protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) => DsImages.FolderClosed;
		protected override ImageReference? GetExpandedIcon(IDotNetImageService dnImgMgr) => DsImages.FolderOpened;
		public override ITreeNodeGroup TreeNodeGroup { get; }

		readonly TypeDef type;

		public DerivedTypesFolderNodeImpl(ITreeNodeGroup treeNodeGroup, TypeDef type) {
			TreeNodeGroup = treeNodeGroup;
			this.type = type;
		}

		public override void Initialize() =>
			TreeNode.LazyLoading = createChildren = DerivedTypesFinder.QuickCheck(type);
		bool createChildren;

		public override IEnumerable<TreeNodeData> CreateChildren() {
			if (!createChildren)
				yield break;
			if (derivedTypesFinder != null)
				derivedTypesFinder.Cancel();
			derivedTypesFinder = new DerivedTypesFinder(this, type);
		}
		DerivedTypesFinder derivedTypesFinder;

		protected override void WriteCore(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options) =>
			output.Write(BoxedTextColor.Text, dnSpy_Resources.DerivedTypes);
		public override FilterType GetFilterType(IDocumentTreeNodeFilter filter) =>
			filter.GetResult(this).FilterType;
	}
}
