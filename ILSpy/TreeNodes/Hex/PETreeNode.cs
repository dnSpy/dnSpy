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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Threading;
using dnlib.DotNet;
using dnlib.DotNet.MD;
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
		readonly ModuleDefMD module;

		public PETreeNode(IPEImage peImage, ModuleDefMD module) {
			this.peImage = peImage;
			this.module = module;
			LazyLoading = true;
		}

		public override object Icon {
			get { return ImageCache.Instance.GetImage("ModuleFile", BackgroundType.TreeNode); }
		}

		public sealed override FilterResult Filter(FilterSettings settings) {
			var res = settings.Filter.GetFilterResult(this);
			if (res.FilterResult != null)
				return res.FilterResult.Value;
			return base.Filter(settings);
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
			var cor20Hdr = ImageCor20HeaderTreeNode.Create(doc, peImage);
			if (cor20Hdr != null)
				Children.Add(cor20Hdr);
			if (module != null) {
				var md = module.MetaData;
				Children.Add(new StorageSignatureTreeNode(doc, md.MetaDataHeader));
				Children.Add(new StorageHeaderTreeNode(doc, md.MetaDataHeader));
				var knownStreams = new List<DotNetStream> {
					md.StringsStream,
					md.USStream,
					md.BlobStream,
					md.GuidStream,
					md.TablesStream,
				};
				if (md.IsCompressed) {
					foreach (var stream in md.AllStreams) {
						if (stream.Name == "#!")
							knownStreams.Add(stream);
					}
				}
				for (int i = 0; i < md.MetaDataHeader.StreamHeaders.Count; i++) {
					var sh = md.MetaDataHeader.StreamHeaders[i];
					var knownStream = knownStreams.FirstOrDefault(a => a.StreamHeader == sh);
					Children.Add(new StorageStreamTreeNode(doc, sh, i, knownStream));
				}
			}
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
			// Descendants() shouldn't be used since some of the nodes could have thousands of
			// children and it's better if the parent can quickly check whether any of its children
			// need to get notified.
			foreach (HexTreeNode node in Children)
				node.OnDocumentModified(e.StartOffset, e.EndOffset);
		}

		public override void Decompile(Language language, ITextOutput output, DecompilationOptions options) {
			App.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(EnsureChildrenFiltered));
			language.WriteCommentLine(output, "PE");
			language.WriteCommentLine(output, "All tree nodes below use the hex editor to modify the PE file");
			foreach (HexTreeNode node in Children) {
				language.WriteCommentLine(output, string.Empty);
				node.Decompile(language, output, options);
			}
		}

		protected override void Write(ITextOutput output, Language language) {
			output.Write("PE", TextTokenType.Text);
		}

		public override NodePathName NodePathName {
			get { return new NodePathName("pe"); }
		}
	}
}
