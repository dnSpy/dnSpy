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

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Resources;
using dnSpy.Contracts.Files.TreeView.Resources;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Files.TreeView.Resources {
	[Export, Export(typeof(IResourceNodeFactory)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class ResourceNodeFactory : IResourceNodeFactory {
		readonly Lazy<IResourceNodeCreator, IResourceNodeCreatorMetadata>[] creators;

		[ImportingConstructor]
		public ResourceNodeFactory([ImportMany] IEnumerable<Lazy<IResourceNodeCreator, IResourceNodeCreatorMetadata>> mefCreators) {
			this.creators = mefCreators.OrderBy(a => a.Metadata.Order).ToArray();
		}

		public IResourceNode Create(ModuleDef module, Resource resource, ITreeNodeGroup treeNodeGroup) {
			if (module == null || resource == null || treeNodeGroup == null)
				throw new ArgumentNullException();
			foreach (var creator in creators) {
				var node = creator.Value.Create(module, resource, treeNodeGroup);
				if (node != null)
					return node;
			}
			return new UnknownResourceNode(treeNodeGroup, resource);
		}

		public IResourceElementNode Create(ModuleDef module, ResourceElement resourceElement, ITreeNodeGroup treeNodeGroup) {
			if (module == null || resourceElement == null || treeNodeGroup == null)
				throw new ArgumentNullException();
			foreach (var creator in creators) {
				var node = creator.Value.Create(module, resourceElement, treeNodeGroup);
				if (node != null)
					return node;
			}
			return new BuiltInResourceElementNode(treeNodeGroup, resourceElement);
		}
	}
}
