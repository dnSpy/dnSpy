/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Documents.TreeView.Resources;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Documents.Tabs {
	[ExportAutoLoaded]
	sealed class RefreshResourcesCommand : IAutoLoaded {
		readonly IDocumentTabService documentTabService;
		readonly IDecompilationCache decompilationCache;

		[ImportingConstructor]
		RefreshResourcesCommand(IDocumentTabService documentTabService, IDocumentTreeViewSettings documentTreeViewSettings, IDecompilationCache decompilationCache) {
			this.documentTabService = documentTabService;
			this.decompilationCache = decompilationCache;
			documentTreeViewSettings.PropertyChanged += DocumentTreeViewSettings_PropertyChanged;
		}

		void DocumentTreeViewSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == nameof(IDocumentTreeViewSettings.DeserializeResources)) {
				var documentTreeViewSettings = (IDocumentTreeViewSettings)sender;
				if (documentTreeViewSettings.DeserializeResources)
					DeserializeResources();
			}
		}

		void DeserializeResources() {
			var modifiedResourceNodes = new HashSet<TreeNodeData>();
			foreach (var node in documentTabService.DocumentTreeView.TreeView.Root.Data.Descendants()) {
				var elemNode = node as SerializedResourceElementNode;
				if (elemNode != null) {
					if (elemNode.CanDeserialize) {
						elemNode.Deserialize();
						modifiedResourceNodes.Add(elemNode);
					}
					continue;
				}
				else if (node is ResourcesFolderNode)
					modifiedResourceNodes.Add(node);
			}

			RefreshResources(modifiedResourceNodes);
		}

		void RefreshResources(HashSet<TreeNodeData> modifiedResourceNodes) {
			if (modifiedResourceNodes.Count == 0)
				return;

			var ownerNodes = new HashSet<ResourcesFolderNode>();
			foreach (var node in modifiedResourceNodes) {
				var owner = node.GetAncestorOrSelf<ResourcesFolderNode>();
				if (owner != null)
					ownerNodes.Add(owner);
			}
			if (ownerNodes.Count == 0)
				return;

			decompilationCache.Clear(new HashSet<IDsDocument>(ownerNodes.Select(a => {
				var mod = a.GetModuleNode();
				Debug.Assert(mod != null);
				return mod?.Document;
			}).Where(a => a != null)));

			var tabs = new List<IDocumentTab>();
			foreach (var tab in documentTabService.VisibleFirstTabs) {
				bool refresh = tab.Content.Nodes.Any(a => ownerNodes.Contains(a.GetAncestorOrSelf<ResourcesFolderNode>()));
				if (refresh)
					tabs.Add(tab);
			}
			documentTabService.Refresh(tabs);
		}
	}
}
