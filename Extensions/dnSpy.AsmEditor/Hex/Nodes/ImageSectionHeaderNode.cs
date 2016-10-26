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
using dnlib.PE;
using dnSpy.AsmEditor.Properties;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.HexEditor;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text;

namespace dnSpy.AsmEditor.Hex.Nodes {
	sealed class ImageSectionHeaderNode : HexNode {
		public override Guid Guid => new Guid(DocumentTreeViewConstants.IMGSECTHEADER_NODE_GUID);
		public override NodePathName NodePathName => new NodePathName(Guid, SectionNumber.ToString());
		public override object VMObject => imageSectionHeaderVM;

		protected override IEnumerable<HexVM> HexVMs {
			get { yield return imageSectionHeaderVM; }
		}

		protected override ImageReference IconReference => DsImages.BinaryFile;
		public int SectionNumber { get; }

		readonly ImageSectionHeaderVM imageSectionHeaderVM;

		public ImageSectionHeaderNode(HexDocument doc, ImageSectionHeader sectHdr, int sectionNumber)
			: base((ulong)sectHdr.StartOffset, (ulong)sectHdr.EndOffset - 1) {
			this.SectionNumber = sectionNumber;
			this.imageSectionHeaderVM = new ImageSectionHeaderVM(this, doc, StartOffset);
		}

		public override void OnDocumentModified(ulong modifiedStart, ulong modifiedEnd) {
			base.OnDocumentModified(modifiedStart, modifiedEnd);
			if (HexUtils.IsModified(imageSectionHeaderVM.NameVM.StartOffset, imageSectionHeaderVM.NameVM.EndOffset, modifiedStart, modifiedEnd))
				TreeNode.RefreshUI();
		}

		protected override void WriteCore(ITextColorWriter output, DocumentNodeWriteOptions options) {
			output.Write(BoxedTextColor.HexPeSection, dnSpy_AsmEditor_Resources.HexNode_PE_Section);
			output.WriteSpace();
			output.Write(BoxedTextColor.Operator, "#");
			output.Write(BoxedTextColor.Number, SectionNumber.ToString());
			output.Write(BoxedTextColor.Punctuation, ":");
			output.WriteSpace();
			output.Write(BoxedTextColor.HexPeSectionName, string.Format("{0}", imageSectionHeaderVM.NameVM.String));
		}
	}
}
