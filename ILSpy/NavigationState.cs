using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.ILSpy.TextView;
using ICSharpCode.TreeView;
namespace ICSharpCode.ILSpy
{
	public class NavigationState : IEquatable<NavigationState>
	{
		private HashSet<SharpTreeNode> treeNodes;

		public IEnumerable<SharpTreeNode> TreeNodes { get { return treeNodes; } }
		public DecompilerTextViewState ViewState { get; private set; }

		public NavigationState(IEnumerable<SharpTreeNode> treeNodes, DecompilerTextViewState viewState)
		{
			this.treeNodes = new HashSet<SharpTreeNode>(treeNodes);
			ViewState = viewState;
		}

		public bool Equals(NavigationState other)
		{
			// TODO: should this care about the view state as well?
			return this.treeNodes.SetEquals(other.treeNodes);
		}
	}
}
