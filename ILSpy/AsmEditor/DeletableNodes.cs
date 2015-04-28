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
using System.Diagnostics;
using System.Linq;
using ICSharpCode.ILSpy.TreeNodes;

namespace ICSharpCode.ILSpy.AsmEditor
{
	public struct DeletableNodes
	{
		readonly ILSpyTreeNode[] nodes;
		ILSpyTreeNode[] parents;
		int[] indexes;

		public int Count {
			get { return nodes.Length; }
		}

		public ILSpyTreeNode[] Nodes {
			get { return nodes; }
		}

		public DeletableNodes(ILSpyTreeNode node)
			: this(new[] { node })
		{
		}

		public DeletableNodes(IEnumerable<ILSpyTreeNode> nodes)
		{
			this.nodes = nodes.ToArray();
			this.parents = null;
			this.indexes = null;
		}

		/// <summary>
		/// Deletes the nodes. An exception is thrown if they've already been deleted but not restored.
		/// </summary>
		public void Delete()
		{
			Debug.Assert(indexes == null && parents == null);
			if (indexes != null || parents != null)
				throw new ArgumentException("Nodes have already been deleted");

			indexes = new int[nodes.Length];
			parents = new ILSpyTreeNode[nodes.Length];
			for (int i = 0; i < nodes.Length; i++) {
				var node = nodes[i];
				var parent = (ILSpyTreeNode)node.Parent;
				parents[i] = parent;
				indexes[i] = parent.Children.IndexOf(node);

				NamespaceTreeNode nsNode;
				TypeTreeNode typeNode;
				if ((nsNode = node as NamespaceTreeNode) != null)
					nsNode.OnBeforeRemoved();
				else if ((typeNode = node as TypeTreeNode) != null)
					typeNode.OnBeforeRemoved();

				parent.Children.RemoveAt(indexes[i]);
			}
		}

		/// <summary>
		/// Restores the deleted nodes. An exception is thrown if the nodes haven't been deleted.
		/// </summary>
		public void Restore()
		{
			Debug.Assert(indexes != null && parents != null);
			if (indexes == null || parents == null)
				throw new ArgumentException("Nodes have already been restored");

			for (int i = nodes.Length - 1; i >= 0; i--) {
				var node = nodes[i];
				parents[i].Children.Insert(indexes[i], node);

				NamespaceTreeNode nsNode;
				TypeTreeNode typeNode;
				if ((nsNode = node as NamespaceTreeNode) != null)
					nsNode.OnReadded();
				else if ((typeNode = node as TypeTreeNode) != null)
					typeNode.OnReadded();
			}

			this.indexes = null;
			this.parents = null;
		}
	}
}
