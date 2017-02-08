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
using dnSpy.AsmEditor.Hex.PE;
using dnSpy.AsmEditor.Properties;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text;

namespace dnSpy.AsmEditor.Hex.Nodes {
	sealed class ImageFileHeaderNode : HexNode {
		protected override ImageReference IconReference => DsImages.BinaryFile;
		public override Guid Guid => new Guid(DocumentTreeViewConstants.IMGFILEHEADER_NODE_GUID);
		public override NodePathName NodePathName => new NodePathName(Guid);
		public override object VMObject => imageFileHeaderVM;

		protected override IEnumerable<HexVM> HexVMs {
			get { yield return imageFileHeaderVM; }
		}
		readonly ImageFileHeaderVM imageFileHeaderVM;

		public ImageFileHeaderNode(ImageFileHeaderVM fileHeader)
			: base(fileHeader.Span) {
			imageFileHeaderVM = fileHeader;
		}

		protected override void WriteCore(ITextColorWriter output, DocumentNodeWriteOptions options) =>
			output.Write(BoxedTextColor.HexPeFileHeader, dnSpy_AsmEditor_Resources.HexNode_FileHeader);
	}
}
