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

using System.ComponentModel.Composition;
using dnlib.DotNet;
using dnlib.DotNet.Resources;
using dnSpy.Shared.UI.Highlighting;

namespace dnSpy.TreeNodes {
	[Export(typeof(IResourceFactory<ResourceElement, ResourceElementTreeNode>))]
	sealed class UnknownSerializedResourceElementTreeNodeFactory : IResourceFactory<ResourceElement, ResourceElementTreeNode> {
		public int Priority {
			get { return -1; }
		}

		public ResourceElementTreeNode Create(ModuleDef module, ResourceElement resInput) {
			var serializedData = resInput.ResourceData as BinaryResourceData;
			if (serializedData == null)
				return null;

			return new UnknownSerializedResourceElementTreeNode(resInput);
		}
	}

	public sealed class UnknownSerializedResourceElementTreeNode : SerializedResourceElementTreeNode {
		public UnknownSerializedResourceElementTreeNode(ResourceElement resElem)
			: base(resElem) {
		}

		public override NodePathName NodePathName {
			get { return new NodePathName("unserresel", NameUtils.CleanName(resElem.Name)); }
		}
	}
}
