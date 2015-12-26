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

using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.Tabs.TextEditor;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Decompiler;

namespace dnSpy.AsmEditor.Hex.Nodes {
	[ExportReferenceFileTabContentCreator(Order = TabConstants.ORDER_CONTENTCREATOR_HEXTOKENREF)]
	sealed class TokenReferenceFileTabContentCreator : IReferenceFileTabContentCreator {
		public FileTabReferenceResult Create(IFileTabManager fileTabManager, IFileTabContent sourceContent, object @ref) {
			var tokRef = @ref as TokenReference;
			if (tokRef == null) {
				var codeRef = @ref as CodeReference;
				tokRef = codeRef == null ? null : codeRef.Reference as TokenReference;
			}
			if (tokRef != null)
				return Create(tokRef, fileTabManager);
			return null;
		}

		FileTabReferenceResult Create(TokenReference tokRef, IFileTabManager fileTabManager) {
			var node = HexFileTreeNodeDataFinder.FindNode(fileTabManager.FileTreeView, tokRef);
			if (node == null)
				return null;
			var content = fileTabManager.TryCreateContent(new IFileTreeNodeData[] { node });
			if (content == null)
				return null;
			return new FileTabReferenceResult(content);
		}
	}
}
