/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Documents.TreeView;

namespace dnSpy.AsmEditor.Hex.Nodes {
	[ExportReferenceDocumentTabContentProvider(Order = TabConstants.ORDER_CONTENTPROVIDER_HEXTOKENREF)]
	sealed class TokenReferenceDocumentTabContentProvider : IReferenceDocumentTabContentProvider {
		public DocumentTabReferenceResult? Create(IDocumentTabService documentTabService, DocumentTabContent? sourceContent, object? @ref) {
			var tokRef = @ref as TokenReference;
			if (tokRef is null)
				tokRef = (@ref as TextReference)?.Reference as TokenReference;
			if (tokRef is not null)
				return Create(tokRef, documentTabService);
			return null;
		}

		DocumentTabReferenceResult? Create(TokenReference tokRef, IDocumentTabService documentTabService) {
			var node = HexDocumentTreeNodeDataFinder.FindNode(documentTabService.DocumentTreeView, tokRef);
			if (node is null)
				return null;
			var content = documentTabService.TryCreateContent(new DocumentTreeNodeData[] { node });
			if (content is null)
				return null;
			return new DocumentTabReferenceResult(content);
		}
	}
}
