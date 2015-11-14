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
using dnSpy.NRefactory;
using dnSpy.Shared.UI.HexEditor;
using ICSharpCode.Decompiler;

namespace dnSpy.TreeNodes.Hex {
	public sealed class ImageSectionHeaderTreeNode : HexTreeNode {
		public override NodePathName NodePathName {
			get { return new NodePathName("secthdr", sectionNumber.ToString()); }
		}

		protected override object ViewObject {
			get { return imageSectionHeaderVM; }
		}

		protected override IEnumerable<HexVM> HexVMs {
			get { yield return imageSectionHeaderVM; }
		}

		protected override string IconName {
			get { return "BinaryFile"; }
		}

		public int SectionNumber {
			get { return sectionNumber; }
		}
		readonly int sectionNumber;

		readonly ImageSectionHeaderVM imageSectionHeaderVM;

		public ImageSectionHeaderTreeNode(HexDocument doc, ImageSectionHeader sectHdr, int sectionNumber)
			: base((ulong)sectHdr.StartOffset, (ulong)sectHdr.EndOffset - 1) {
			this.sectionNumber = sectionNumber;
			this.imageSectionHeaderVM = new ImageSectionHeaderVM(this, doc, StartOffset);
		}

		public override void OnDocumentModified(ulong modifiedStart, ulong modifiedEnd) {
			base.OnDocumentModified(modifiedStart, modifiedEnd);
			if (HexUtils.IsModified(imageSectionHeaderVM.NameVM.StartOffset, imageSectionHeaderVM.NameVM.EndOffset, modifiedStart, modifiedEnd))
				RaisePropertyChanged("Text");
		}

		protected override void Write(ITextOutput output) {
			output.Write("Section", TextTokenType.Keyword);
			output.WriteSpace();
			output.Write("#", TextTokenType.Operator);
			output.Write(sectionNumber.ToString(), TextTokenType.Number);
			output.Write(":", TextTokenType.Operator);
			output.WriteSpace();
			output.Write(string.Format("{0}", imageSectionHeaderVM.NameVM.String), TextTokenType.Type);
		}
	}
}
