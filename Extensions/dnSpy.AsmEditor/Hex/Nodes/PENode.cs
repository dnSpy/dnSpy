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
using dnSpy.AsmEditor.Hex.PE;
using dnSpy.AsmEditor.Properties;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Files;
using dnSpy.Contracts.Hex.Files.DotNet;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.TreeView;

namespace dnSpy.AsmEditor.Hex.Nodes {
	sealed class PENode : DocumentTreeNodeData, IDecompileSelf {
		readonly Func<HexBufferFile> createBufferFile;
		readonly PEStructureProviderFactory peStructureProviderFactory;

		public PENode(Func<HexBufferFile> createBufferFile, PEStructureProviderFactory peStructureProviderFactory) {
			if (createBufferFile == null)
				throw new ArgumentNullException(nameof(createBufferFile));
			if (peStructureProviderFactory == null)
				throw new ArgumentNullException(nameof(peStructureProviderFactory));
			this.createBufferFile = createBufferFile;
			this.peStructureProviderFactory = peStructureProviderFactory;
		}

		public override void Initialize() => TreeNode.LazyLoading = true;
		protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) => DsImages.ModulePublic;
		public override FilterType GetFilterType(IDocumentTreeNodeFilter filter) => filter.GetResultOther(this).FilterType;

		public override IEnumerable<TreeNodeData> CreateChildren() {
			Debug.Assert(TreeNode.Children.Count == 0 && weakDocListener == null);
			if (weakDocListener != null)
				yield break;

			var file = createBufferFile();
			if (file == null)
				yield break;
			var peStructureProvider = peStructureProviderFactory.TryGetProvider(file);
			if (peStructureProvider == null)
				yield break;

			weakDocListener = new WeakDocumentListener(this, file.Buffer);

			yield return new ImageDosHeaderNode(peStructureProvider.ImageDosHeader);
			yield return new ImageFileHeaderNode(peStructureProvider.ImageFileHeader);
			if (peStructureProvider.ImageOptionalHeader is ImageOptionalHeader32VM)
				yield return new ImageOptionalHeader32Node((ImageOptionalHeader32VM)peStructureProvider.ImageOptionalHeader);
			else
				yield return new ImageOptionalHeader64Node((ImageOptionalHeader64VM)peStructureProvider.ImageOptionalHeader);
			for (int i = 0; i < peStructureProvider.Sections.Length; i++)
				yield return new ImageSectionHeaderNode(peStructureProvider.Sections[i], i);
			var cor20Hdr = ImageCor20HeaderNode.Create(peStructureProvider.ImageCor20Header);
			if (cor20Hdr != null)
				yield return cor20Hdr;
			if (cor20Hdr != null) {
				yield return new StorageSignatureNode(peStructureProvider.StorageSignature);
				yield return new StorageHeaderNode(peStructureProvider.StorageHeader);
				foreach (var storageStream in peStructureProvider.StorageStreams) {
					if (storageStream.HeapKind == DotNetHeapKind.Tables)
						yield return new TablesStorageStreamNode(storageStream, peStructureProvider.TablesStream);
					else
						yield return new StorageStreamNode(storageStream);
				}
			}
		}
		WeakDocumentListener weakDocListener;

		sealed class WeakDocumentListener {
			readonly WeakReference nodeWeakRef;

			public WeakDocumentListener(PENode node, HexBuffer buffer) {
				nodeWeakRef = new WeakReference(node);
				buffer.Changed += Buffer_Changed;
			}

			void Buffer_Changed(object sender, HexContentChangedEventArgs e) {
				var node = (PENode)nodeWeakRef.Target;
				if (node != null)
					node.Buffer_Changed(sender, e);
				else {
					var buffer = (HexBuffer)sender;
					buffer.Changed -= Buffer_Changed;
				}
			}
		}

		void Buffer_Changed(object sender, HexContentChangedEventArgs e) {
			// Descendants() shouldn't be used since some of the nodes could have thousands of
			// children and it's better if the parent can quickly check whether any of its children
			// need to get notified.
			foreach (HexNode node in TreeNode.DataChildren)
				node.OnBufferChanged(e.Changes);
		}

		public bool Decompile(IDecompileNodeContext context) {
			context.ContentTypeString = context.Decompiler.ContentTypeString;
			var children = context.UIThread(() => {
				TreeNode.EnsureChildrenLoaded();
				return TreeNode.DataChildren.OfType<HexNode>().ToArray();
			});
			context.Decompiler.WriteCommentLine(context.Output, dnSpy_AsmEditor_Resources.HexNode_PE);
			context.Decompiler.WriteCommentLine(context.Output, dnSpy_AsmEditor_Resources.NodesUseHexEditorMsg);
			foreach (HexNode node in children) {
				context.Decompiler.WriteCommentLine(context.Output, string.Empty);
				node.Decompile(context);
			}
			return true;
		}

		public MetaDataTableRecordNode FindTokenNode(uint token) {
			if ((token & 0x00FFFFFF) == 0)
				return null;
			TreeNode.EnsureChildrenLoaded();
			var stgStreamNode = (StorageStreamNode)TreeNode.DataChildren.FirstOrDefault(a => a is StorageStreamNode && ((StorageStreamNode)a).HeapKind == DotNetHeapKind.Tables);
			return stgStreamNode?.FindTokenNode(token);
		}

		public HexNode FindNode(HexVM structure, HexField field) {
			var mdTblRecord = structure as MetaDataTableRecordVM;
			if (mdTblRecord != null)
				return FindTokenNode(mdTblRecord.Token.Raw);

			TreeNode.EnsureChildrenLoaded();
			foreach (var child in TreeNode.DataChildren.OfType<HexNode>()) {
				var node = child.FindNode(structure, field);
				if (node != null)
					return node;
			}
			return null;
		}

		protected override void WriteCore(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options) =>
			output.Write(BoxedTextColor.Text, dnSpy_AsmEditor_Resources.HexNode_PE);
		public override Guid Guid => new Guid(DocumentTreeViewConstants.PE_NODE_GUID);
		public override NodePathName NodePathName => new NodePathName(Guid);
		public override ITreeNodeGroup TreeNodeGroup => PETreeNodeGroup.Instance;
	}

	sealed class PETreeNodeGroup : ITreeNodeGroup {
		public static readonly PETreeNodeGroup Instance = new PETreeNodeGroup(DocumentTreeViewConstants.ORDER_MODULE_PE);

		public PETreeNodeGroup(double order) {
			Order = order;
		}

		public double Order { get; }

		public int Compare(TreeNodeData x, TreeNodeData y) {
			if (x == y) return 0;
			var a = x as PENode;
			var b = y as PENode;
			if (a == null) return -1;
			if (b == null) return 1;
			return 0;
		}
	}
}
