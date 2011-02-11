// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections;
using System.Collections.Generic;

namespace Debugger
{
	public class CollectionItemEventArgs<T> : EventArgs
	{
		T item;
		
		public T Item {
			get {
				return item;
			}
		}
		
		public CollectionItemEventArgs(T item)
		{
			this.item = item;
		}
	}
	
	/// <summary>
	/// A collection that fires events when items are added or removed.
	/// </summary>
	public class CollectionWithEvents<T> : IEnumerable<T>
	{
		NDebugger debugger;
		
		List<T> list = new List<T>();
		
		public event EventHandler<CollectionItemEventArgs<T>> Added;
		public event EventHandler<CollectionItemEventArgs<T>> Removed;
		
		protected virtual void OnAdded(T item)
		{
			if (Added != null) {
				Added(this, new CollectionItemEventArgs<T>(item));
			}
		}
		
		protected virtual void OnRemoved(T item)
		{
			if (Removed != null) {
				Removed(this, new CollectionItemEventArgs<T>(item));
			}
		}
		
		public CollectionWithEvents(NDebugger debugger)
		{
			this.debugger = debugger;
		}
		
		protected NDebugger Debugger {
			get {
				return debugger;
			}
		}
		
		public int Count {
			get {
				return list.Count;
			}
		}
		
		public T this[int index] {
			get {
				return list[index];
			}
		}
		
		internal void Add(T item)
		{
			list.Add(item);
			OnAdded(item);
		}
		
		internal void Remove(T item)
		{
			if (list.Remove(item)) {
				OnRemoved(item);
			} else {
				throw new DebuggerException("Item is not in the collection");
			}
		}
		
		internal void Clear()
		{
			List<T> oldList = list;
			list = new List<T>();
			foreach (T item in oldList) {
				OnRemoved(item);
			}
		}
		
		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return list.GetEnumerator();
		}
		
		IEnumerator IEnumerable.GetEnumerator()
		{
			return list.GetEnumerator();
		}
	}
}
