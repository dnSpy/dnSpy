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

using System.ComponentModel.Composition;
using System.Linq;
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Search;

namespace dnSpy.Documents.Tabs {
	[ExportReferenceDocumentTabContentProvider(Order = TabConstants.ORDER_CONTENTPROVIDER_NODE)]
	sealed class NodeReferenceDocumentTabContentProvider : IReferenceDocumentTabContentProvider {
		readonly IDocumentTabContentFactoryService documentTabContentFactoryService;
		readonly IDocumentTreeView documentTreeView;

		[ImportingConstructor]
		NodeReferenceDocumentTabContentProvider(IDocumentTabContentFactoryService documentTabContentFactoryService, IDocumentTreeView documentTreeView) {
			this.documentTabContentFactoryService = documentTabContentFactoryService;
			this.documentTreeView = documentTreeView;
		}

		public DocumentTabReferenceResult Create(IDocumentTabService documentTabService, IDocumentTabContent sourceContent, object @ref) {
			var textRef = @ref as TextReference;
			if (textRef != null)
				@ref = textRef.Reference;
			var node = @ref as IDocumentTreeNodeData;
			if (node != null)
				return Create(node);
			var nsRef = @ref as NamespaceRef;
			if (nsRef != null)
				return Create(nsRef);
			var nsRef2 = @ref as NamespaceReference;
			if (nsRef2 != null)
				return Create(nsRef2);
			var document = @ref as IDsDocument;
			if (document != null)
				return Create(document);
			var asm = @ref as AssemblyDef;
			if (asm != null)
				return Create(asm);
			var mod = @ref as ModuleDef;
			if (mod != null)
				return Create(mod);
			var asmRef = @ref as IAssembly;
			if (asmRef != null) {
				document = documentTreeView.DocumentService.Resolve(asmRef, null);
				if (document != null)
					return Create(document);
			}
			return null;
		}

		DocumentTabReferenceResult Create(IDocumentTreeNodeData node) {
			var content = documentTabContentFactoryService.CreateTabContent(new IDocumentTreeNodeData[] { node });
			if (content == null)
				return null;
			return new DocumentTabReferenceResult(content);
		}

		DocumentTabReferenceResult Create(NamespaceRef nsRef) {
			var node = documentTreeView.FindNamespaceNode(nsRef.Module, nsRef.Namespace);
			return node == null ? null : Create(node);
		}

		DocumentTabReferenceResult Create(NamespaceReference nsRef) {
			var asm = documentTreeView.DocumentService.Resolve(nsRef.Assembly, null) as IDsDotNetDocument;
			if (asm == null)
				return null;
			var mod = asm.Children.FirstOrDefault() as IDsDotNetDocument;
			if (mod == null)
				return null;
			var node = documentTreeView.FindNamespaceNode(mod, nsRef.Namespace);
			return node == null ? null : Create(node);
		}

		DocumentTabReferenceResult Create(IDsDocument document) {
			var node = documentTreeView.FindNode(document);
			return node == null ? null : Create(node);
		}

		DocumentTabReferenceResult Create(AssemblyDef asm) {
			var node = documentTreeView.FindNode(asm);
			return node == null ? null : Create(node);
		}

		DocumentTabReferenceResult Create(ModuleDef mod) {
			var node = documentTreeView.FindNode(mod);
			return node == null ? null : Create(node);
		}
	}
}
