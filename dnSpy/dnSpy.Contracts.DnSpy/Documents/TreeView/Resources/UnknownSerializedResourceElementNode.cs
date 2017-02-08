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
using dnlib.DotNet.Resources;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Contracts.Documents.TreeView.Resources {
	/// <summary>
	/// Unknown serialized <see cref="ResourceElementNode"/>
	/// </summary>
	public sealed class UnknownSerializedResourceElementNode : SerializedResourceElementNode {
		/// <summary>
		/// Guid of this node
		/// </summary>
		public override Guid Guid => new Guid(DocumentTreeViewConstants.UNKNOWN_SERIALIZED_RESOURCE_ELEMENT_NODE_GUID);

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="treeNodeGroup">Treenode group</param>
		/// <param name="resourceElement">Resource element</param>
		public UnknownSerializedResourceElementNode(ITreeNodeGroup treeNodeGroup, ResourceElement resourceElement)
			: base(treeNodeGroup, resourceElement) {
		}
	}
}
