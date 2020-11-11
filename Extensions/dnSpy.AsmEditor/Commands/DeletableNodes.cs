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
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Documents.TreeView;

namespace dnSpy.AsmEditor.Commands {
	struct DeletableNodes<T> where T : DocumentTreeNodeData {
		readonly T[] nodes;
		DocumentTreeNodeData[]? parents;

		public int Count => nodes.Length;
		public T[] Nodes => nodes;
		public DocumentTreeNodeData[]? Parents => parents;

		public DeletableNodes(T node)
			: this(new[] { node }) {
		}

		public DeletableNodes(IEnumerable<T> nodes) {
			this.nodes = nodes.ToArray();
			parents = null;
		}

		/// <summary>
		/// Deletes the nodes. An exception is thrown if they've already been deleted but not restored.
		/// The model (dnlib) elements must be deleted after this method is called, not before.
		/// </summary>
		public void Delete() {
			Debug2.Assert(parents is null);
			if (parents is not null)
				throw new ArgumentException("Nodes have already been deleted");

			parents = new DocumentTreeNodeData[nodes.Length];
			for (int i = 0; i < nodes.Length; i++) {
				var node = nodes[i];
				var parent = (DocumentTreeNodeData)node.TreeNode.Parent!.Data;
				parents[i] = parent;

				parent.TreeNode.Children.Remove(node.TreeNode);
			}
		}

		/// <summary>
		/// Restores the deleted nodes. An exception is thrown if the nodes haven't been deleted.
		/// The model (dnlib) elements must be restored before this method is called, not after.
		/// </summary>
		public void Restore() {
			Debug2.Assert(parents is not null);
			if (parents is null)
				throw new ArgumentException("Nodes have already been restored");

			for (int i = nodes.Length - 1; i >= 0; i--) {
				var node = nodes[i];
				parents[i].TreeNode.AddChild(node.TreeNode);
			}

			parents = null;
		}
	}
}
