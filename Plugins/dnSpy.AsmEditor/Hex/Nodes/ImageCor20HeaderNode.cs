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
using dnSpy.Contracts.Highlighting;
using dnSpy.Decompiler.Shared;
using dnSpy.Shared.HexEditor;

namespace dnSpy.AsmEditor.Hex.Nodes {
	sealed class ImageCor20HeaderNode : HexNode {
		protected override string IconName => "BinaryFile";
		public override Guid Guid => new Guid(FileTVConstants.IMGCOR20HEADER_NODE_GUID);
		public override NodePathName NodePathName => new NodePathName(Guid);
		public override object VMObject => imageCor20HeaderVM;

		protected override IEnumerable<HexVM> HexVMs {
			get { yield return imageCor20HeaderVM; }
		}
		readonly ImageCor20HeaderVM imageCor20HeaderVM;

		public static ImageCor20HeaderNode Create(HexDocument doc, IPEImage peImage) {
			var dnDir = peImage.ImageNTHeaders.OptionalHeader.DataDirectories[14];
			if (dnDir.VirtualAddress != 0 && dnDir.Size >= 0x48)
				return new ImageCor20HeaderNode(doc, (ulong)peImage.ToFileOffset(dnDir.VirtualAddress));
			return null;
		}

		public ImageCor20HeaderNode(HexDocument doc, ulong startOffset)
			: base(startOffset, startOffset + 0x48 - 1) {
			this.imageCor20HeaderVM = new ImageCor20HeaderVM(this, doc, StartOffset);
		}

		protected override void Write(ISyntaxHighlightOutput output) =>
			output.Write(dnSpy_AsmEditor_Resources.HexNode_Cor20_Header, BoxedTextTokenKind.InstanceField);
	}
}
