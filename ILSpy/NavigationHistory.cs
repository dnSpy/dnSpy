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
	sealed class NavigationHistory<T>
	{
		List<T> back = new List<T>();
		List<T> forward = new List<T>();
		
		public bool CanNavigateBack {
			get { return back.Count > 0; }
		}
		
		public bool CanNavigateForward {
			get { return forward.Count > 0; }
		}
		
		public T GoBack(T oldNode)
		{
			if (oldNode != null)
				forward.Add(oldNode);
			
			T node = back[back.Count - 1];
			back.RemoveAt(back.Count - 1);
			return node;
		}
		
		public T GoForward(T oldNode)
		{
			if (oldNode != null)
				back.Add(oldNode);
			
			T node = forward[forward.Count - 1];
			forward.RemoveAt(forward.Count - 1);
			return node;
		}
		
		public void RemoveAll(Predicate<T> predicate)
		{
			back.RemoveAll(predicate);
			forward.RemoveAll(predicate);
		}
		
		public void Clear()
		{
			back.Clear();
			forward.Clear();
		}
		
		public void Record(T node)
		{
			forward.Clear();
			back.Add(node);
		}
	}
}
