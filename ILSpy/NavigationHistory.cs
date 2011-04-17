// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using ICSharpCode.TreeView;
using System.Diagnostics;

namespace ICSharpCode.ILSpy
{
	/// <summary>
	/// Stores the navigation history.
	/// </summary>
	internal sealed class NavigationHistory<T>
		where T : class
	{
		T current;
		List<T> back = new List<T>();
		List<T> forward = new List<T>();
		
		public bool CanNavigateBack {
			get { return back.Count > 0; }
		}
		
		public bool CanNavigateForward {
			get { return forward.Count > 0; }
		}
		
		public T GoBack()
		{
			forward.Add(current);
			current = back[back.Count - 1];
			back.RemoveAt(back.Count - 1);
			return current;
		}
		
		public T GoForward()
		{
			back.Add(current);
			current = forward[forward.Count - 1];
			forward.RemoveAt(forward.Count - 1);
			return current;
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
		
		public void Replace(T node)
		{
			current = node;
		}

		public void Record(T node)
		{
			if (current != null)
				back.Add(current);

			forward.Clear();
			current = node;
		}
	}
}
