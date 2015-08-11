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

using System;
using System.Diagnostics;
using dnlib.PE;
using dnSpy.AsmEditor;
using dnSpy.HexEditor;
using dnSpy.Images;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TreeNodes;
using ICSharpCode.NRefactory;

namespace dnSpy.TreeNodes.Hex {
	sealed class PETreeNode : ILSpyTreeNode {
		readonly IPEImage peImage;

		public PETreeNode(IPEImage peImage) {
			this.peImage = peImage;
			LazyLoading = true;
		}

		public override object Icon {
			get {
				//TODO: Fix image
				return ImageCache.Instance.GetImage("AssemblyWarning", BackgroundType.TreeNode);
			}
		}

		protected override void LoadChildren() {
			Debug.Assert(Children.Count == 0 && weakDocListener == null);
			if (weakDocListener != null)
				return;

			var doc = HexDocumentManager.Instance.GetOrCreate(peImage);
			if (doc == null)
				return;

			weakDocListener = new WeakDocumentListener(this, doc);

			Children.Add(new ImageDosHeaderTreeNode(doc, peImage.ImageDosHeader));
			Children.Add(new ImageFileHeaderTreeNode(doc, peImage.ImageNTHeaders.FileHeader));
			if (peImage.ImageNTHeaders.OptionalHeader is ImageOptionalHeader32)
				Children.Add(new ImageOptionalHeader32TreeNode(doc, (ImageOptionalHeader32)peImage.ImageNTHeaders.OptionalHeader));
			else
				Children.Add(new ImageOptionalHeader64TreeNode(doc, (ImageOptionalHeader64)peImage.ImageNTHeaders.OptionalHeader));
			for (int i = 0; i < peImage.ImageSectionHeaders.Count; i++)
				Children.Add(new ImageSectionHeaderTreeNode(doc, peImage.ImageSectionHeaders[i], i));
		}
		WeakDocumentListener weakDocListener;

		sealed class WeakDocumentListener {
			readonly WeakReference nodeWeakRef;

			public WeakDocumentListener(PETreeNode node, HexDocument doc) {
				this.nodeWeakRef = new WeakReference(node);
				doc.OnDocumentModified += HexDocument_OnDocumentModified;
			}

			void HexDocument_OnDocumentModified(object sender, HexDocumentModifiedEventArgs e) {
				var node = (PETreeNode)nodeWeakRef.Target;
				if (node != null)
					node.HexDocument_OnDocumentModified(sender, e);
				else {
					var doc = (HexDocument)sender;
					doc.OnDocumentModified -= HexDocument_OnDocumentModified;
				}
			}
		}

		void HexDocument_OnDocumentModified(object sender, HexDocumentModifiedEventArgs e) {
			foreach (HexTreeNode node in Children)
				node.OnDocumentModified(e.StartOffset, e.EndOffset);
		}

		public override void Decompile(Language language, ITextOutput output, DecompilationOptions options) {
			language.WriteCommentLine(output, "PE");
			language.WriteCommentLine(output, "All tree nodes below use the hex editor to modify the PE file");
		}

		protected override void Write(ITextOutput output, Language language) {
			output.Write("PE", TextTokenType.Text);
		}

		public override NodePathName NodePathName {
			get { return new NodePathName("pe"); }
		}
	}
}
