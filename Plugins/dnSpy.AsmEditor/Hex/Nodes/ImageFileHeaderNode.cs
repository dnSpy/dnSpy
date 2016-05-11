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
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.TextEditor;
using dnSpy.Shared.HexEditor;

namespace dnSpy.AsmEditor.Hex.Nodes {
	sealed class ImageFileHeaderNode : HexNode {
		protected override string IconName => "BinaryFile";
		public override Guid Guid => new Guid(FileTVConstants.IMGFILEHEADER_NODE_GUID);
		public override NodePathName NodePathName => new NodePathName(Guid);
		public override object VMObject => imageFileHeaderVM;

		protected override IEnumerable<HexVM> HexVMs {
			get { yield return imageFileHeaderVM; }
		}
		readonly ImageFileHeaderVM imageFileHeaderVM;

		public ImageFileHeaderNode(HexDocument doc, ImageFileHeader fileHeader)
			: base((ulong)fileHeader.StartOffset, (ulong)fileHeader.EndOffset - 1) {
			this.imageFileHeaderVM = new ImageFileHeaderVM(this, doc, StartOffset);
		}

		protected override void Write(IOutputColorWriter output) =>
			output.Write(BoxedOutputColor.Keyword, dnSpy_AsmEditor_Resources.HexNode_FileHeader);
	}
}
