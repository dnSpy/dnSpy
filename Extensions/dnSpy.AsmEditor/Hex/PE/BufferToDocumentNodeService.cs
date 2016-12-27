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
using System.ComponentModel.Composition;
using System.Linq;
using dnSpy.AsmEditor.Hex.Nodes;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Hex.Files;

namespace dnSpy.AsmEditor.Hex.PE {
	abstract class BufferToDocumentNodeService {
		public abstract PENode FindPENode(HexBufferFile file);
	}

	[Export(typeof(BufferToDocumentNodeService))]
	sealed class BufferToDocumentNodeServiceImpl : BufferToDocumentNodeService {
		readonly IDocumentTabService documentTabService;

		[ImportingConstructor]
		BufferToDocumentNodeServiceImpl(IDocumentTabService documentTabService) {
			this.documentTabService = documentTabService;
		}

		DsDocumentNode Find(HexBufferFile file) {
			if (file == null)
				throw new ArgumentNullException(nameof(file));
			if (file.Name == string.Empty)
				return null;
			var doc = documentTabService.DocumentTreeView.DocumentService.Find(new FilenameKey(file.Name));
			if (doc == null)
				return null;
			return documentTabService.DocumentTreeView.FindNode(doc);
		}

		public override PENode FindPENode(HexBufferFile file) {
			if (file == null)
				throw new ArgumentNullException(nameof(file));
			var docNode = Find(file);
			if (docNode == null)
				return null;
			var modNode = documentTabService.DocumentTreeView.FindNode(docNode.Document.AssemblyDef?.ManifestModule);
			if (modNode == null)
				return null;
			modNode.TreeNode.EnsureChildrenLoaded();
			return modNode.TreeNode.DataChildren.OfType<PENode>().FirstOrDefault();
		}
	}
}
