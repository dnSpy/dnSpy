// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using ICSharpCode.ILSpy.TextView;
using ICSharpCode.TreeView;

namespace ICSharpCode.ILSpy
{
	public class NavigationState : IEquatable<NavigationState>
	{
		private readonly HashSet<SharpTreeNode> treeNodes;

		public IEnumerable<SharpTreeNode> TreeNodes { get { return treeNodes; } }
		public DecompilerTextViewState ViewState { get; private set; }

		public NavigationState(DecompilerTextViewState viewState)
		{
			this.treeNodes = new HashSet<SharpTreeNode>(viewState.DecompiledNodes);
			ViewState = viewState;
		}

		public NavigationState(IEnumerable<SharpTreeNode> treeNodes)
		{
			this.treeNodes = new HashSet<SharpTreeNode>(treeNodes);
		}


		public bool Equals(NavigationState other)
		{
			// TODO: should this care about the view state as well?
			return this.treeNodes.SetEquals(other.treeNodes);
		}
	}
}
