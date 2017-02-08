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

using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Documents.TreeView.Resources;
using dnSpy.Contracts.TreeView.Text;

namespace dnSpy.Documents.TreeView {
	sealed class DocumentTreeNodeDataContext : IDocumentTreeNodeDataContext {
		public IDocumentTreeView DocumentTreeView { get; private set; }
		public IDecompiler Decompiler { get; internal set; }
		public IResourceNodeFactory ResourceNodeFactory { get; private set; }
		public IDocumentTreeNodeFilter Filter { get; private set; }
		public ITreeViewNodeTextElementProvider TreeViewNodeTextElementProvider { get; private set; }
		public int FilterVersion { get; set; }
		public bool SyntaxHighlight { get; internal set; }
		public bool SingleClickExpandsChildren { get; internal set; }
		public bool ShowAssemblyVersion { get; internal set; }
		public bool ShowAssemblyPublicKeyToken { get; internal set; }
		public bool ShowToken { get; internal set; }
		public bool UseNewRenderer { get; internal set; }
		public bool DeserializeResources { get; internal set; }
		public bool CanDragAndDrop { get; set; }

		public DocumentTreeNodeDataContext(IDocumentTreeView documentTreeView, IResourceNodeFactory resourceNodeFactory, IDocumentTreeNodeFilter filter, ITreeViewNodeTextElementProvider treeViewNodeTextElementProvider) {
			DocumentTreeView = documentTreeView;
			ResourceNodeFactory = resourceNodeFactory;
			Filter = filter;
			TreeViewNodeTextElementProvider = treeViewNodeTextElementProvider;
			FilterVersion = 1;
			CanDragAndDrop = true;
		}

		public void Clear() {
			DocumentTreeView = null;
			Decompiler = null;
			ResourceNodeFactory = null;
			Filter = null;
			TreeViewNodeTextElementProvider = null;
		}
	}
}
