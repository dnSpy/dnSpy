// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using ICSharpCode.TreeView;

namespace ICSharpCode.ILSpy
{
	/// <summary>
	/// Stores the navigation history.
	/// </summary>
	sealed class NavigationHistory
	{
		List<SharpTreeNode> back = new List<SharpTreeNode>();
		List<SharpTreeNode> forward = new List<SharpTreeNode>();
		
		public bool CanNavigateBack {
			get { return back.Count > 0; }
		}
		
		public bool CanNavigateForward {
			get { return forward.Count > 0; }
		}
		
		public SharpTreeNode GoBack(SharpTreeNode oldNode)
		{
			if (oldNode != null)
				forward.Add(oldNode);
			
			SharpTreeNode node = back[back.Count - 1];
			back.RemoveAt(back.Count - 1);
			return node;
		}
		
		public SharpTreeNode GoForward(SharpTreeNode oldNode)
		{
			if (oldNode != null)
				back.Add(oldNode);
			
			SharpTreeNode node = forward[forward.Count - 1];
			forward.RemoveAt(forward.Count - 1);
			return node;
		}
		
		public void RemoveAll(Predicate<SharpTreeNode> predicate)
		{
			back.RemoveAll(predicate);
			forward.RemoveAll(predicate);
		}
		
		public void Clear()
		{
			back.Clear();
			forward.Clear();
		}
		
		public void Record(SharpTreeNode node)
		{
			forward.Clear();
			back.Add(node);
		}
	}
}
