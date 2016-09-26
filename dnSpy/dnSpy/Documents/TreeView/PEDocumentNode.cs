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
using dnlib.PE;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.TreeView;
using dnSpy.Decompiler;

namespace dnSpy.Documents.TreeView {
	sealed class PEDocumentNode : DsDocumentNode, IPEDocumentNode {
		public PEDocumentNode(IDsDocument dsDocument)
			: base(dsDocument) {
			Debug.Assert(dsDocument.PEImage != null && dsDocument.ModuleDef == null);
		}

		public override Guid Guid => new Guid(DocumentTreeViewConstants.PEDOCUMENT_NODE_GUID);
		protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) => dnImgMgr.GetImageReference(Document.PEImage);
		public bool IsExe => (Document.PEImage.ImageNTHeaders.FileHeader.Characteristics & Characteristics.Dll) == 0;
		public override void Initialize() => TreeNode.LazyLoading = true;

		public override IEnumerable<ITreeNodeData> CreateChildren() {
			foreach (var document in Document.Children)
				yield return Context.DocumentTreeView.CreateNode(this, document);
		}

		protected override void Write(ITextColorWriter output, IDecompiler decompiler) =>
			new NodePrinter().Write(output, decompiler, Document);

		protected override void WriteToolTip(ITextColorWriter output, IDecompiler decompiler) {
			output.Write(BoxedTextColor.EnumField, TargetFrameworkUtils.GetArchString(Document.PEImage.ImageNTHeaders.FileHeader.Machine));

			output.WriteLine();
			output.WriteFilename(Document.Filename);
		}

		public override FilterType GetFilterType(IDocumentTreeNodeFilter filter) =>
			filter.GetResult(Document).FilterType;
	}
}
