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

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Debugger.IMModules {
	struct TreeViewUpdater {
		readonly IDocumentTabService documentTabService;
		readonly CorModuleDefFile CorModuleDefFile;
		readonly ModuleDocumentNode ModuleNode;
		readonly HashSet<uint> modifiedTypes;
		readonly HashSet<uint> loadedClassTokens;
		readonly HashSet<TypeDef> checkedTypes;
		ModuleDocumentNode modNode;

		public TreeViewUpdater(IDocumentTabService documentTabService, CorModuleDefFile cmdf, ModuleDocumentNode node, HashSet<uint> modifiedTypes, HashSet<uint> loadedClassTokens) {
			Debug.Assert(node.Document == cmdf);
			this.documentTabService = documentTabService;
			CorModuleDefFile = cmdf;
			ModuleNode = node;
			this.modifiedTypes = new HashSet<uint>(modifiedTypes);
			this.loadedClassTokens = loadedClassTokens;
			checkedTypes = new HashSet<TypeDef>();
			modNode = node;
		}

		public void Update() {
			// If none of its children have been loaded, we can safely return without doing anything
			if (ModuleNode.TreeNode.LazyLoading)
				return;

			if (loadedClassTokens != null) {
				foreach (var a in loadedClassTokens)
					modifiedTypes.Add(a);
			}
			var tokensList = new List<uint>(modifiedTypes);
			tokensList.Sort();

			bool needRedecompile = false;
			foreach (uint token in tokensList) {
				var td = CorModuleDefFile.DnModule.CorModuleDef.ResolveToken(token) as TypeDef;
				Debug.Assert(td != null);
				if (td == null)
					continue;
				Update(td);
				needRedecompile = true;
			}

			if (needRedecompile) {
				// Force a re-decompile of every view that references this module. This could be
				// optimized if necessary
				documentTabService.RefreshModifiedDocument(CorModuleDefFile);
			}
		}

		List<TypeDef> GetNonCheckedTypeAndDeclaringTypes(TypeDef td) {
			var list = new List<TypeDef>();
			while (td != null && !checkedTypes.Contains(td)) {
				list.Add(td);
				checkedTypes.Add(td);
				td = td.DeclaringType;
			}
			// Enclosing types before enclosed types
			list.Reverse();
			return list;
		}

		void Update(TypeDef td) {
			var list = GetNonCheckedTypeAndDeclaringTypes(td);
			CreateTypeNodes(list);
		}

		void CreateTypeNodes(List<TypeDef> types) {
			TypeNode parentNode = null;
			foreach (var type in types) {
				bool wasLoaded = loadedClassTokens?.Contains(type.MDToken.Raw) ?? false;

				TypeNode typeNode;
				if (type.DeclaringType == null)
					typeNode = GetOrCreateNonNestedTypeTreeNode(modNode, type);
				else {
					if (parentNode == null)
						parentNode = documentTabService.DocumentTreeView.FindNode(type.DeclaringType);
					if (parentNode == null || parentNode.TreeNode.LazyLoading)
						break;
					typeNode = GetOrCreateNestedTypeTreeNode(parentNode, type);
				}
				Debug.Assert(typeNode != null);

				if (wasLoaded || modifiedTypes.Contains(type.MDToken.Raw))
					UpdateMemberNodes(typeNode);

				parentNode = typeNode;
			}
		}

		static TypeNode GetOrCreateNonNestedTypeTreeNode(ModuleDocumentNode modNode, TypeDef type) {
			Debug.Assert(type != null && type.DeclaringType == null);
			modNode.TreeNode.EnsureChildrenLoaded();
			TypeNode typeNode;
			var nsNode = GetOrCreateNamespaceNode(modNode, type.Namespace);
			typeNode = nsNode.TreeNode.DataChildren.OfType<TypeNode>().FirstOrDefault(a => a.TypeDef == type);
			if (typeNode != null)
				return typeNode;
			typeNode = nsNode.Create(type);
			nsNode.TreeNode.AddChild(typeNode.TreeNode);
			return typeNode;
		}

		static NamespaceNode GetOrCreateNamespaceNode(ModuleDocumentNode modNode, string ns) {
			modNode.TreeNode.EnsureChildrenLoaded();
			var nsNode = modNode.TreeNode.DataChildren.OfType<NamespaceNode>().FirstOrDefault(a => a.Name == ns);
			if (nsNode != null)
				return nsNode;
			nsNode = modNode.Create(ns);
			modNode.TreeNode.AddChild(nsNode.TreeNode);
			return nsNode;
		}

		static TypeNode GetOrCreateNestedTypeTreeNode(TypeNode typeNode, TypeDef nestedType) {
			Debug.Assert(nestedType != null && nestedType.DeclaringType == typeNode.TypeDef);
			typeNode.TreeNode.EnsureChildrenLoaded();
			var childTypeNode = typeNode.TreeNode.DataChildren.OfType<TypeNode>().FirstOrDefault(a => a.TypeDef == nestedType);
			if (childTypeNode != null)
				return childTypeNode;
			childTypeNode = typeNode.Create(nestedType);
			typeNode.TreeNode.AddChild(childTypeNode.TreeNode);
			return childTypeNode;
		}

		void UpdateMemberNodes(TypeNode typeNode) {
			// If it's not been loaded, we've got nothing to do
			if (typeNode.TreeNode.LazyLoading)
				return;

			var existing = new HashSet<object>();
			foreach (var child in typeNode.TreeNode.DataChildren) {
				var tokenNode = child as IMDTokenNode;
				if (tokenNode != null)
					existing.Add(tokenNode.Reference);
			}

			foreach (var fd in typeNode.TypeDef.Fields) {
				if (existing.Contains(fd))
					continue;
				typeNode.TreeNode.AddChild(documentTabService.DocumentTreeView.TreeView.Create(documentTabService.DocumentTreeView.Create(fd)));
			}

			foreach (var pd in typeNode.TypeDef.Properties) {
				if (existing.Contains(pd))
					continue;
				typeNode.TreeNode.AddChild(documentTabService.DocumentTreeView.TreeView.Create(documentTabService.DocumentTreeView.Create(pd)));
			}

			foreach (var ed in typeNode.TypeDef.Events) {
				if (existing.Contains(ed))
					continue;
				typeNode.TreeNode.AddChild(documentTabService.DocumentTreeView.TreeView.Create(documentTabService.DocumentTreeView.Create(ed)));
			}

			var accessorMethods = typeNode.TypeDef.GetPropertyAndEventMethods();
			foreach (var md in typeNode.TypeDef.Methods) {
				if (existing.Contains(md))
					continue;
				if (!accessorMethods.Contains(md))
					typeNode.TreeNode.AddChild(documentTabService.DocumentTreeView.TreeView.Create(documentTabService.DocumentTreeView.Create(md)));
			}
		}
	}
}
