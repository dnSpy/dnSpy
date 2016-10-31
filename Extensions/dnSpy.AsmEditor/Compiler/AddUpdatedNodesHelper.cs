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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using dnlib.DotNet;
using dnSpy.AsmEditor.Commands;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Documents.TreeView.Resources;

namespace dnSpy.AsmEditor.Compiler {
	interface IAddUpdatedNodesHelperProvider {
		AddUpdatedNodesHelper Create(ModuleDocumentNode modNode, ModuleImporter importer);
	}

	[Export(typeof(IAddUpdatedNodesHelperProvider))]
	sealed class AddUpdatedNodesHelperProvider : IAddUpdatedNodesHelperProvider {
		readonly Lazy<IMethodAnnotations> methodAnnotations;
		readonly Lazy<IResourceNodeFactory> resourceNodeFactory;
		readonly IDocumentTreeView documentTreeView;

		[ImportingConstructor]
		AddUpdatedNodesHelperProvider(Lazy<IMethodAnnotations> methodAnnotations, Lazy<IResourceNodeFactory> resourceNodeFactory, IDocumentTreeView documentTreeView) {
			this.methodAnnotations = methodAnnotations;
			this.resourceNodeFactory = resourceNodeFactory;
			this.documentTreeView = documentTreeView;
		}

		public AddUpdatedNodesHelper Create(ModuleDocumentNode modNode, ModuleImporter importer) =>
			new AddUpdatedNodesHelper(methodAnnotations, resourceNodeFactory, documentTreeView, modNode, importer);
	}

	sealed class AddUpdatedNodesHelper {
		readonly ModuleDocumentNode modNode;
		readonly TypeNodeCreator[] newTypeNodeCreators;
		readonly ResourceNodeCreator resourceNodeCreator;
		readonly ExistingTypeNodeUpdater[] existingTypeNodeUpdaters;
		readonly CustomAttribute[] newAssemblyCustomAttributes;
		readonly CustomAttribute[] newModuleCustomAttributes;
		readonly CustomAttribute[] origAssemblyCustomAttributes;
		readonly CustomAttribute[] origModuleCustomAttributes;

		public AddUpdatedNodesHelper(Lazy<IMethodAnnotations> methodAnnotations, Lazy<IResourceNodeFactory> resourceNodeFactory, IDocumentTreeView documentTreeView, ModuleDocumentNode modNode, ModuleImporter importer) {
			this.modNode = modNode;
			var dict = new Dictionary<string, List<TypeDef>>(StringComparer.Ordinal);
			foreach (var t in importer.NewNonNestedTypes) {
				List<TypeDef> list;
				var ns = (t.TargetType.Namespace ?? UTF8String.Empty).String;
				if (!dict.TryGetValue(ns, out list))
					dict[ns] = list = new List<TypeDef>();
				list.Add(t.TargetType);
			}
			this.newTypeNodeCreators = dict.Values.Select(a => new TypeNodeCreator(modNode, a)).ToArray();
			this.existingTypeNodeUpdaters = importer.MergedNonNestedTypes.Select(a => new ExistingTypeNodeUpdater(methodAnnotations, modNode, a)).ToArray();
			if (!importer.MergedNonNestedTypes.All(a => a.TargetType.Module == modNode.Document.ModuleDef))
				throw new InvalidOperationException();
			this.newAssemblyCustomAttributes = importer.NewAssemblyCustomAttributes;
			this.newModuleCustomAttributes = importer.NewModuleCustomAttributes;
			if (newAssemblyCustomAttributes != null)
				origAssemblyCustomAttributes = modNode.Document.AssemblyDef?.CustomAttributes.ToArray();
			if (newModuleCustomAttributes != null)
				origModuleCustomAttributes = modNode.Document.ModuleDef.CustomAttributes.ToArray();

			if (importer.NewResources.Length != 0) {
				var module = modNode.Document.ModuleDef;
				var rsrcListNode = GetResourceListTreeNode(modNode);
				Debug.Assert(rsrcListNode != null);
				if (rsrcListNode != null) {
					var newNodes = new ResourceNode[importer.NewResources.Length];
					var treeNodeGroup = documentTreeView.DocumentTreeNodeGroups.GetGroup(DocumentTreeNodeGroupType.ResourceTreeNodeGroup);
					for (int i = 0; i < newNodes.Length; i++)
						newNodes[i] = (ResourceNode)documentTreeView.TreeView.Create(resourceNodeFactory.Value.Create(module, importer.NewResources[i], treeNodeGroup)).Data;
					resourceNodeCreator = new ResourceNodeCreator(rsrcListNode, newNodes);
				}
			}
		}

		static ResourcesFolderNode GetResourceListTreeNode(ModuleDocumentNode modNode) {
			modNode.TreeNode.EnsureChildrenLoaded();
			return modNode.TreeNode.DataChildren.OfType<ResourcesFolderNode>().FirstOrDefault();
		}

		public void Execute() {
			for (int i = 0; i < newTypeNodeCreators.Length; i++)
				newTypeNodeCreators[i].Add();
			for (int i = 0; i < existingTypeNodeUpdaters.Length; i++)
				existingTypeNodeUpdaters[i].Add();
			if (origAssemblyCustomAttributes != null && newAssemblyCustomAttributes != null) {
				modNode.Document.AssemblyDef.CustomAttributes.Clear();
				foreach (var ca in newAssemblyCustomAttributes)
					modNode.Document.AssemblyDef.CustomAttributes.Add(ca);
			}
			if (origModuleCustomAttributes != null && newModuleCustomAttributes != null) {
				modNode.Document.ModuleDef.CustomAttributes.Clear();
				foreach (var ca in newModuleCustomAttributes)
					modNode.Document.ModuleDef.CustomAttributes.Add(ca);
			}
			resourceNodeCreator?.Add();
		}

		public void Undo() {
			resourceNodeCreator?.Remove();
			if (origModuleCustomAttributes != null && newModuleCustomAttributes != null) {
				modNode.Document.ModuleDef.CustomAttributes.Clear();
				foreach (var ca in origModuleCustomAttributes)
					modNode.Document.ModuleDef.CustomAttributes.Add(ca);
			}
			if (origAssemblyCustomAttributes != null && newAssemblyCustomAttributes != null) {
				modNode.Document.AssemblyDef.CustomAttributes.Clear();
				foreach (var ca in origAssemblyCustomAttributes)
					modNode.Document.AssemblyDef.CustomAttributes.Add(ca);
			}
			for (int i = existingTypeNodeUpdaters.Length - 1; i >= 0; i--)
				existingTypeNodeUpdaters[i].Remove();
			for (int i = newTypeNodeCreators.Length - 1; i >= 0; i--)
				newTypeNodeCreators[i].Remove();
		}

		public IEnumerable<object> ModifiedObjects {
			get { yield return modNode; }
		}
	}
}
