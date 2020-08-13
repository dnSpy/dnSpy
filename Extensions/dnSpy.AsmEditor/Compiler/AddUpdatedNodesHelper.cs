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

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using dnlib.DotNet;
using dnSpy.AsmEditor.Commands;
using dnSpy.AsmEditor.Resources;
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
		readonly AssemblyDocumentNode? asmNode;
		readonly ModuleDocumentNode modNode;
		readonly TypeNodeCreator[] newTypeNodeCreators;
		readonly ResourceNodeCreator? resourceNodeCreator;
		readonly ExistingTypeNodeUpdater[] existingTypeNodeUpdaters;
		readonly DeclSecurity[]? newAssemblyDeclSecurities;
		readonly DeclSecurity[]? origAssemblyDeclSecurities;
		readonly CustomAttribute[]? newAssemblyCustomAttributes;
		readonly CustomAttribute[]? newModuleCustomAttributes;
		readonly CustomAttribute[]? origAssemblyCustomAttributes;
		readonly CustomAttribute[]? origModuleCustomAttributes;
		readonly ExportedType[]? newExportedTypes;
		readonly ExportedType[]? origExportedTypes;
		readonly Version? newAssemblyVersion;
		readonly Version? origAssemblyVersion;

		public AddUpdatedNodesHelper(Lazy<IMethodAnnotations> methodAnnotations, Lazy<IResourceNodeFactory> resourceNodeFactory, IDocumentTreeView documentTreeView, ModuleDocumentNode modNode, ModuleImporter importer) {
			asmNode = modNode.TreeNode.Parent?.Data as AssemblyDocumentNode;
			this.modNode = modNode;
			var dict = new Dictionary<string, List<TypeDef>>(StringComparer.Ordinal);
			foreach (var t in importer.NewNonNestedTypes) {
				var ns = (t.TargetType!.Namespace ?? UTF8String.Empty).String;
				if (!dict.TryGetValue(ns, out var list))
					dict[ns] = list = new List<TypeDef>();
				list.Add(t.TargetType);
			}
			newTypeNodeCreators = dict.Values.Select(a => new TypeNodeCreator(modNode, a)).ToArray();
			existingTypeNodeUpdaters = importer.MergedNonNestedTypes.Select(a => new ExistingTypeNodeUpdater(methodAnnotations, modNode, a)).ToArray();
			if (!importer.MergedNonNestedTypes.All(a => a.TargetType!.Module == modNode.Document.ModuleDef))
				throw new InvalidOperationException();
			newAssemblyDeclSecurities = importer.NewAssemblyDeclSecurities;
			newAssemblyCustomAttributes = importer.NewAssemblyCustomAttributes;
			newModuleCustomAttributes = importer.NewModuleCustomAttributes;
			newExportedTypes = importer.NewExportedTypes;
			newAssemblyVersion = importer.NewAssemblyVersion;
			if (!(newAssemblyDeclSecurities is null))
				origAssemblyDeclSecurities = modNode.Document.AssemblyDef?.DeclSecurities.ToArray();
			if (!(newAssemblyCustomAttributes is null))
				origAssemblyCustomAttributes = modNode.Document.AssemblyDef?.CustomAttributes.ToArray();
			if (!(newModuleCustomAttributes is null))
				origModuleCustomAttributes = modNode.Document.ModuleDef!.CustomAttributes.ToArray();
			if (!(newExportedTypes is null))
				origExportedTypes = modNode.Document.ModuleDef!.ExportedTypes.ToArray();
			if (!(newAssemblyVersion is null))
				origAssemblyVersion = modNode.Document.AssemblyDef?.Version;

			if (importer.NewResources!.Length != 0) {
				var module = modNode.Document.ModuleDef!;
				var rsrcListNode = GetResourceListTreeNode(modNode);
				Debug2.Assert(!(rsrcListNode is null));
				if (!(rsrcListNode is null)) {
					var newNodes = new NodeAndResource[importer.NewResources.Length];
					var treeNodeGroup = documentTreeView.DocumentTreeNodeGroups.GetGroup(DocumentTreeNodeGroupType.ResourceTreeNodeGroup);
					for (int i = 0; i < newNodes.Length; i++) {
						var resource = importer.NewResources[i];
						var node = (DocumentTreeNodeData)documentTreeView.TreeView.Create(resourceNodeFactory.Value.Create(module, resource, treeNodeGroup)).Data;
						newNodes[i] = new NodeAndResource(node);
					}
					resourceNodeCreator = new ResourceNodeCreator(rsrcListNode, newNodes);
				}
			}
		}

		static ResourcesFolderNode? GetResourceListTreeNode(ModuleDocumentNode modNode) {
			modNode.TreeNode.EnsureChildrenLoaded();
			return modNode.TreeNode.DataChildren.OfType<ResourcesFolderNode>().FirstOrDefault();
		}

		public void Execute() {
			bool refresh = false;
			for (int i = 0; i < newTypeNodeCreators.Length; i++)
				newTypeNodeCreators[i].Add();
			for (int i = 0; i < existingTypeNodeUpdaters.Length; i++)
				existingTypeNodeUpdaters[i].Add();
			if (!(origAssemblyDeclSecurities is null) && !(newAssemblyDeclSecurities is null)) {
				modNode.Document.AssemblyDef!.DeclSecurities.Clear();
				foreach (var ds in newAssemblyDeclSecurities)
					modNode.Document.AssemblyDef.DeclSecurities.Add(ds);
			}
			if (!(origAssemblyCustomAttributes is null) && !(newAssemblyCustomAttributes is null)) {
				modNode.Document.AssemblyDef!.CustomAttributes.Clear();
				foreach (var ca in newAssemblyCustomAttributes)
					modNode.Document.AssemblyDef.CustomAttributes.Add(ca);
			}
			if (!(origModuleCustomAttributes is null) && !(newModuleCustomAttributes is null)) {
				modNode.Document.ModuleDef!.CustomAttributes.Clear();
				foreach (var ca in newModuleCustomAttributes)
					modNode.Document.ModuleDef.CustomAttributes.Add(ca);
			}
			if (!(origExportedTypes is null) && !(newExportedTypes is null)) {
				modNode.Document.ModuleDef!.ExportedTypes.Clear();
				foreach (var et in newExportedTypes)
					modNode.Document.ModuleDef.ExportedTypes.Add(et);
			}
			if (!(newAssemblyVersion is null) && !(origAssemblyVersion is null)) {
				modNode.Document.AssemblyDef!.Version = newAssemblyVersion;
				refresh = true;
			}
			resourceNodeCreator?.Add();
			if (refresh)
				asmNode?.TreeNode.RefreshUI();
		}

		public void Undo() {
			bool refresh = false;
			resourceNodeCreator?.Remove();
			if (!(newAssemblyVersion is null) && !(origAssemblyVersion is null)) {
				modNode.Document.AssemblyDef!.Version = origAssemblyVersion;
				refresh = true;
			}
			if (!(origExportedTypes is null) && !(newExportedTypes is null)) {
				modNode.Document.ModuleDef!.ExportedTypes.Clear();
				foreach (var et in origExportedTypes)
					modNode.Document.ModuleDef.ExportedTypes.Add(et);
			}
			if (!(origModuleCustomAttributes is null) && !(newModuleCustomAttributes is null)) {
				modNode.Document.ModuleDef!.CustomAttributes.Clear();
				foreach (var ca in origModuleCustomAttributes)
					modNode.Document.ModuleDef.CustomAttributes.Add(ca);
			}
			if (!(origAssemblyCustomAttributes is null) && !(newAssemblyCustomAttributes is null)) {
				modNode.Document.AssemblyDef!.CustomAttributes.Clear();
				foreach (var ca in origAssemblyCustomAttributes)
					modNode.Document.AssemblyDef.CustomAttributes.Add(ca);
			}
			if (!(origAssemblyDeclSecurities is null) && !(newAssemblyDeclSecurities is null)) {
				modNode.Document.AssemblyDef!.DeclSecurities.Clear();
				foreach (var ds in origAssemblyDeclSecurities)
					modNode.Document.AssemblyDef.DeclSecurities.Add(ds);
			}
			for (int i = existingTypeNodeUpdaters.Length - 1; i >= 0; i--)
				existingTypeNodeUpdaters[i].Remove();
			for (int i = newTypeNodeCreators.Length - 1; i >= 0; i--)
				newTypeNodeCreators[i].Remove();
			if (refresh)
				asmNode?.TreeNode.RefreshUI();
		}

		public IEnumerable<object> ModifiedObjects {
			get { yield return modNode; }
		}
	}
}
