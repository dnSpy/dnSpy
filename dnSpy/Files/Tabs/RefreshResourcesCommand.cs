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

using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Files.TreeView.Resources;
using dnSpy.Contracts.Plugin;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Files.Tabs {
	[ExportAutoLoaded]
	sealed class RefreshResourcesCommand : IAutoLoaded {
		readonly IFileTabManager fileTabManager;
		readonly IDecompilationCache decompilationCache;

		[ImportingConstructor]
		RefreshResourcesCommand(IFileTabManager fileTabManager, IFileTreeViewSettings fileTreeViewSettings, IDecompilationCache decompilationCache) {
			this.fileTabManager = fileTabManager;
			this.decompilationCache = decompilationCache;
			fileTreeViewSettings.PropertyChanged += FileTreeViewSettings_PropertyChanged;
		}

		void FileTreeViewSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == "DeserializeResources") {
				var fileTreeViewSettings = (IFileTreeViewSettings)sender;
				if (fileTreeViewSettings.DeserializeResources)
					DeserializeResources();
			}
		}

		void DeserializeResources() {
			var modifiedResourceNodes = new HashSet<IFileTreeNodeData>();
			foreach (var node in fileTabManager.FileTreeView.TreeView.Root.Data.Descendants()) {
				var elemNode = node as ISerializedResourceElementNode;
				if (elemNode != null) {
					if (elemNode.CanDeserialize) {
						elemNode.Deserialize();
						modifiedResourceNodes.Add(elemNode);
					}
					continue;
				}
				else if (node is IResourcesFolderNode)
					modifiedResourceNodes.Add((IResourcesFolderNode)node);
			}

			RefreshResources(modifiedResourceNodes);
		}

		void RefreshResources(HashSet<IFileTreeNodeData> modifiedResourceNodes) {
			if (modifiedResourceNodes.Count == 0)
				return;

			var ownerNodes = new HashSet<IResourcesFolderNode>();
			foreach (var node in modifiedResourceNodes) {
				var owner = node.GetAncestorOrSelf<IResourcesFolderNode>();
				if (owner != null)
					ownerNodes.Add(owner);
			}
			if (ownerNodes.Count == 0)
				return;

			decompilationCache.Clear(new HashSet<IDnSpyFile>(ownerNodes.Select(a => {
				var mod = a.GetModuleNode();
				Debug.Assert(mod != null);
				return mod == null ? null : mod.DnSpyFile;
			}).Where(a => a != null)));

			var tabs = new List<IFileTab>();
			foreach (var tab in fileTabManager.VisibleFirstTabs) {
				bool refresh = tab.Content.Nodes.Any(a => ownerNodes.Contains(a.GetAncestorOrSelf<IResourcesFolderNode>()));
				if (refresh)
					tabs.Add(tab);
			}
			fileTabManager.Refresh(tabs);
		}
	}
}
