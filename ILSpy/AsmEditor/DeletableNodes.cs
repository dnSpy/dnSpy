
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
