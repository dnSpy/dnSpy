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
using dnlib.DotNet;
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Shared.UI.Search;

namespace dnSpy.Files.Tabs {
	[ExportReferenceFileTabContentCreator(Order = TabConstants.ORDER_CONTENTCREATOR_NODE)]
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
			var file = @ref as IDnSpyFile;
			if (file != null)
				return Create(file);
			var asm = @ref as AssemblyDef;
			if (asm != null)
				return Create(asm);
			var mod = @ref as ModuleDef;
			if (mod != null)
				return Create(mod);
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
			return node == null ? null : Create(node);
		}

		FileTabReferenceResult Create(IDnSpyFile file) {
			var node = fileTreeView.FindNode(file);
			return node == null ? null : Create(node);
		}

		FileTabReferenceResult Create(AssemblyDef asm) {
			var node = fileTreeView.FindNode(asm);
			return node == null ? null : Create(node);
		}

		FileTabReferenceResult Create(ModuleDef mod) {
			var node = fileTreeView.FindNode(mod);
			return node == null ? null : Create(node);
		}
	}
}
