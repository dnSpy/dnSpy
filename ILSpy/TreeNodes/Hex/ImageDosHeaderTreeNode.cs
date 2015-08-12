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
using ICSharpCode.ILSpy.TreeNodes;

namespace dnSpy.TreeNodes.Hex {
	sealed class ImageDosHeaderTreeNode : HexTreeNode {
		public override NodePathName NodePathName {
			get { return new NodePathName("doshdr"); }
		}

		protected override string Name {
			get { return "DOS Header"; }
		}

		protected override object ViewObject {
			get { return imageDosHeaderVM; }
		}

		protected override IEnumerable<HexVM> HexVMs {
			get { yield return imageDosHeaderVM; }
		}

		protected override string IconName {
			get { return "BinaryFile"; }
		}

		readonly ImageDosHeaderVM imageDosHeaderVM;

		public ImageDosHeaderTreeNode(HexDocument doc, ImageDosHeader dosHeader)
			: base((ulong)dosHeader.StartOffset, (ulong)dosHeader.EndOffset - 1) {
			this.imageDosHeaderVM = new ImageDosHeaderVM(doc, StartOffset);
		}
	}
}
