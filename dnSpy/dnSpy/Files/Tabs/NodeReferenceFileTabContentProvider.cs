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
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.Tabs.DocViewer;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Search;

namespace dnSpy.Files.Tabs {
	[ExportReferenceFileTabContentProvider(Order = TabConstants.ORDER_CONTENTPROVIDER_NODE)]
	sealed class NodeReferenceFileTabContentProvider : IReferenceFileTabContentProvider {
		readonly IFileTabContentFactoryManager fileTabContentFactoryManager;
		readonly IFileTreeView fileTreeView;

		[ImportingConstructor]
		NodeReferenceFileTabContentProvider(IFileTabContentFactoryManager fileTabContentFactoryManager, IFileTreeView fileTreeView) {
			this.fileTabContentFactoryManager = fileTabContentFactoryManager;
			this.fileTreeView = fileTreeView;
		}

		public FileTabReferenceResult Create(IFileTabManager fileTabManager, IFileTabContent sourceContent, object @ref) {
			var textRef = @ref as TextReference;
			if (textRef != null)
				@ref = textRef.Reference;
			var node = @ref as IFileTreeNodeData;
			if (node != null)
				return Create(node);
			var nsRef = @ref as NamespaceRef;
			if (nsRef != null)
				return Create(nsRef);
			var nsRef2 = @ref as NamespaceReference;
			if (nsRef2 != null)
				return Create(nsRef2);
			var file = @ref as IDnSpyFile;
			if (file != null)
				return Create(file);
			var asm = @ref as AssemblyDef;
			if (asm != null)
				return Create(asm);
			var mod = @ref as ModuleDef;
			if (mod != null)
				return Create(mod);
			var asmRef = @ref as IAssembly;
			if (asmRef != null) {
				file = fileTreeView.FileManager.Resolve(asmRef, null);
				if (file != null)
					return Create(file);
			}
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

		FileTabReferenceResult Create(NamespaceReference nsRef) {
			var asm = fileTreeView.FileManager.Resolve(nsRef.Assembly, null) as IDnSpyDotNetFile;
			if (asm == null)
				return null;
			var mod = asm.Children.FirstOrDefault() as IDnSpyDotNetFile;
			if (mod == null)
				return null;
			var node = fileTreeView.FindNamespaceNode(mod, nsRef.Namespace);
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
