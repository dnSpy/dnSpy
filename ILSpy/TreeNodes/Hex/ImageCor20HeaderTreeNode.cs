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

using System.Collections.Generic;
using dnlib.PE;
using dnSpy.HexEditor;
using dnSpy.NRefactory;
using ICSharpCode.Decompiler;

namespace dnSpy.TreeNodes.Hex {
	sealed class ImageCor20HeaderTreeNode : HexTreeNode {
		public override NodePathName NodePathName {
			get { return new NodePathName("cor20hdr"); }
		}

		protected override object ViewObject {
			get { return imageCor20HeaderVM; }
		}

		protected override IEnumerable<HexVM> HexVMs {
			get { yield return imageCor20HeaderVM; }
		}

		protected override string IconName {
			get { return "BinaryFile"; }
		}

		readonly ImageCor20HeaderVM imageCor20HeaderVM;

		public static ImageCor20HeaderTreeNode Create(HexDocument doc, IPEImage peImage) {
			var dnDir = peImage.ImageNTHeaders.OptionalHeader.DataDirectories[14];
			if (dnDir.VirtualAddress != 0 && dnDir.Size >= 0x48)
				return new ImageCor20HeaderTreeNode(doc, (ulong)peImage.ToFileOffset(dnDir.VirtualAddress));
			return null;
		}

		public ImageCor20HeaderTreeNode(HexDocument doc, ulong startOffset)
			: base(startOffset, startOffset + 0x48 - 1) {
			this.imageCor20HeaderVM = new ImageCor20HeaderVM(doc, StartOffset);
		}

		protected override void Write(ITextOutput output) {
			output.Write("Cor20 Header", TextTokenType.InstanceField);
		}
	}
}
