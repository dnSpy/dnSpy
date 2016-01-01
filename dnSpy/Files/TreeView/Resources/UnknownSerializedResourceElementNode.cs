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
using dnlib.DotNet;
using dnlib.DotNet.Resources;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Files.TreeView.Resources;
using dnSpy.Contracts.TreeView;
using dnSpy.Shared.UI.Files.TreeView.Resources;

namespace dnSpy.Files.TreeView.Resources {
	[ExportResourceNodeCreator(Order = FileTVConstants.ORDER_RSRCCREATOR_UNKNOWNSERIALIZEDRSRCELEM)]
	sealed class UnknownSerializedResourceElementNodeCreator : IResourceNodeCreator {
		public IResourceNode Create(ModuleDef module, Resource resource, ITreeNodeGroup treeNodeGroup) {
			return null;
		}

		public IResourceElementNode Create(ModuleDef module, ResourceElement resourceElement, ITreeNodeGroup treeNodeGroup) {
			if (resourceElement.ResourceData is BinaryResourceData)
				return new UnknownSerializedResourceElementNode(treeNodeGroup, resourceElement);
			return null;
		}
	}

	sealed class UnknownSerializedResourceElementNode : SerializedResourceElementNode, IUnknownSerializedResourceElementNode {
		public override Guid Guid {
			get { return new Guid(FileTVConstants.UNKNOWN_SERIALIZED_RESOURCE_ELEMENT_NODE_GUID); }
		}

		public UnknownSerializedResourceElementNode(ITreeNodeGroup treeNodeGroup, ResourceElement resourceElement)
			: base(treeNodeGroup, resourceElement) {
		}
	}
}
