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

using System.ComponentModel.Composition;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Shared.UI.Search;

namespace dnSpy.Files.Tabs {
	[ExportReferenceFileTabContentCreator(Order = TabsConstants.ORDER_CONTENTCREATOR_NODE)]
	sealed class NodeReferenceFileTabContentCreator : IReferenceFileTabContentCreator {
		readonly IFileTabContentFactoryManager fileTabContentFactoryManager;
		readonly IFileTreeView fileTreeView;

		[ImportingConstructor]
		NodeReferenceFileTabContentCreator(IFileTabContentFactoryManager fileTabContentFactoryManager, IFileTreeView fileTreeView) {
			this.fileTabContentFactoryManager = fileTabContentFactoryManager;
			this.fileTreeView = fileTreeView;
		}

		public FileTabReferenceResult Create(IFileTabManager fileTabManager, IFileTabContent sourceContent, object @ref) {
			var node = @ref as IFileTreeNodeData;
			if (node != null)
				return Create(node);
			var nsRef = @ref as NamespaceRef;
			if (nsRef != null)
				return Create(nsRef);
			return null;
		}

		FileTabReferenceResult Create(IFileTreeNodeData node) {
			var content = fileTabContentFactoryManager.CreateTabContent(new IFileTreeNodeData[] { node });
			if (content == null)
				return null;
			return new FileTabReferenceResult(content);
		}

		FileTabReferenceResult Create(NamespaceRef nsRef) {
			var node = fileTreeView.FindNamespaceNode(nsRef.Module, nsRef.Namespace);
			if (node != null)
				return Create(node);
			return null;
		}
	}
}
