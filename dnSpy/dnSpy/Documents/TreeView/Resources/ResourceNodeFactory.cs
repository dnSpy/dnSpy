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

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Resources;
using dnSpy.Contracts.Documents.TreeView.Resources;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Documents.TreeView.Resources {
	[Export(typeof(IResourceNodeFactory))]
	sealed class ResourceNodeFactory : IResourceNodeFactory {
		readonly Lazy<IResourceNodeProvider, IResourceNodeProviderMetadata>[] resourceNodeProviders;

		[ImportingConstructor]
		public ResourceNodeFactory([ImportMany] IEnumerable<Lazy<IResourceNodeProvider, IResourceNodeProviderMetadata>> resourceNodeProviders) => this.resourceNodeProviders = resourceNodeProviders.OrderBy(a => a.Metadata.Order).ToArray();

		public ResourceNode Create(ModuleDef module, Resource resource, ITreeNodeGroup treeNodeGroup) {
			if (module == null)
				throw new ArgumentNullException(nameof(module));
			if (resource == null)
				throw new ArgumentNullException(nameof(resource));
			if (treeNodeGroup == null)
				throw new ArgumentNullException(nameof(treeNodeGroup));
			foreach (var provider in resourceNodeProviders) {
				try {
					var node = provider.Value.Create(module, resource, treeNodeGroup);
					if (node != null)
						return node;
				}
				catch {
				}
			}
			return new UnknownResourceNodeImpl(treeNodeGroup, resource);
		}

		public ResourceElementNode Create(ModuleDef module, ResourceElement resourceElement, ITreeNodeGroup treeNodeGroup) {
			if (module == null)
				throw new ArgumentNullException(nameof(module));
			if (resourceElement == null)
				throw new ArgumentNullException(nameof(resourceElement));
			if (treeNodeGroup == null)
				throw new ArgumentNullException(nameof(treeNodeGroup));
			foreach (var provider in resourceNodeProviders) {
				try {
					var node = provider.Value.Create(module, resourceElement, treeNodeGroup);
					if (node != null)
						return node;
				}
				catch {
				}
			}
			return new BuiltInResourceElementNodeImpl(treeNodeGroup, resourceElement);
		}
	}
}
