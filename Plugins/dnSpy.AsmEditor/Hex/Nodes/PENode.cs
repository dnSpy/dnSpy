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
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.PE;
using dnSpy.AsmEditor.Properties;
using dnSpy.Contracts.Files.Tabs.TextEditor;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.TreeView;
using dnSpy.Shared.Files.TreeView;
using dnSpy.Shared.HexEditor;

namespace dnSpy.AsmEditor.Hex.Nodes {
	sealed class PENode : FileTreeNodeData, IDecompileSelf {
		readonly IHexDocumentManager hexDocMgr;
		readonly IPEImage peImage;
		readonly ModuleDefMD module;

		public PENode(IHexDocumentManager hexDocMgr, IPEImage peImage, ModuleDefMD module) {
			this.hexDocMgr = hexDocMgr;
			this.peImage = peImage;
			this.module = module;
		}

		public override void Initialize() => TreeNode.LazyLoading = true;
		protected override ImageReference GetIcon(IDotNetImageManager dnImgMgr) => new ImageReference(GetType().Assembly, "ModuleFile");
		public override FilterType GetFilterType(IFileTreeNodeFilter filter) => filter.GetResult(this).FilterType;

		public override IEnumerable<ITreeNodeData> CreateChildren() {
			Debug.Assert(TreeNode.Children.Count == 0 && weakDocListener == null);
			if (weakDocListener != null)
				yield break;

			var doc = hexDocMgr.GetOrCreate(peImage);
			if (doc == null)
				yield break;

			weakDocListener = new WeakDocumentListener(this, doc);

			yield return new ImageDosHeaderNode(doc, peImage.ImageDosHeader);
			yield return new ImageFileHeaderNode(doc, peImage.ImageNTHeaders.FileHeader);
			if (peImage.ImageNTHeaders.OptionalHeader is ImageOptionalHeader32)
				yield return new ImageOptionalHeader32Node(doc, (ImageOptionalHeader32)peImage.ImageNTHeaders.OptionalHeader);
			else
				yield return new ImageOptionalHeader64Node(doc, (ImageOptionalHeader64)peImage.ImageNTHeaders.OptionalHeader);
			for (int i = 0; i < peImage.ImageSectionHeaders.Count; i++)
				yield return new ImageSectionHeaderNode(doc, peImage.ImageSectionHeaders[i], i);
			var cor20Hdr = ImageCor20HeaderNode.Create(doc, peImage);
			if (cor20Hdr != null)
				yield return cor20Hdr;
			if (module != null) {
				var md = module.MetaData;
				yield return new StorageSignatureNode(doc, md.MetaDataHeader);
				yield return new StorageHeaderNode(doc, md.MetaDataHeader);
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
					yield return new StorageStreamNode(doc, sh, i, knownStream, md);
				}
			}
		}
		WeakDocumentListener weakDocListener;

		sealed class WeakDocumentListener {
			readonly WeakReference nodeWeakRef;

			public WeakDocumentListener(PENode node, HexDocument doc) {
				this.nodeWeakRef = new WeakReference(node);
				doc.OnDocumentModified += HexDocument_OnDocumentModified;
			}

			void HexDocument_OnDocumentModified(object sender, HexDocumentModifiedEventArgs e) {
				var node = (PENode)nodeWeakRef.Target;
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
			foreach (HexNode node in TreeNode.DataChildren)
				node.OnDocumentModified(e.StartOffset, e.EndOffset);
		}

		public bool Decompile(IDecompileNodeContext context) {
			context.ContentTypeString = context.Language.ContentTypeString;
			var children = context.ExecuteInUIThread(() => {
				TreeNode.EnsureChildrenLoaded();
				return TreeNode.DataChildren.OfType<HexNode>().ToArray();
			});
			context.Language.WriteCommentLine(context.Output, dnSpy_AsmEditor_Resources.HexNode_PE);
			context.Language.WriteCommentLine(context.Output, dnSpy_AsmEditor_Resources.NodesUseHexEditorMsg);
			foreach (HexNode node in children) {
				context.Language.WriteCommentLine(context.Output, string.Empty);
				node.Decompile(context);
			}
			return true;
		}

		public MetaDataTableRecordNode FindTokenNode(uint token) {
			if ((token & 0x00FFFFFF) == 0)
				return null;
			TreeNode.EnsureChildrenLoaded();
			var stgStreamNode = (StorageStreamNode)TreeNode.DataChildren.FirstOrDefault(a => a is StorageStreamNode && ((StorageStreamNode)a).StorageStreamType == StorageStreamType.Tables);
			return stgStreamNode?.FindTokenNode(token);
		}

		protected override void Write(IOutputColorWriter output, ILanguage language) =>
			output.Write(BoxedOutputColor.Text, dnSpy_AsmEditor_Resources.HexNode_PE);
		public override Guid Guid => new Guid(FileTVConstants.PE_NODE_GUID);
		public override NodePathName NodePathName => new NodePathName(Guid);
		public override ITreeNodeGroup TreeNodeGroup => PETreeNodeGroup.Instance;
	}

	sealed class PETreeNodeGroup : ITreeNodeGroup {
		public static readonly PETreeNodeGroup Instance = new PETreeNodeGroup(FileTVConstants.ORDER_MODULE_PE);

		public PETreeNodeGroup(double order) {
			this.Order = order;
		}

		public double Order { get; }

		public int Compare(ITreeNodeData x, ITreeNodeData y) {
			if (x == y) return 0;
			var a = x as PENode;
			var b = y as PENode;
			if (a == null) return -1;
			if (b == null) return 1;
			return 0;
		}
	}
}
