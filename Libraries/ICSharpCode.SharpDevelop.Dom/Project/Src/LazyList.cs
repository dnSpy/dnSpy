// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;

namespace ICSharpCode.SharpDevelop.Dom
{
	/// <summary>
	/// A list that lazily initializes its content. The base list returned by the initializer may be
	/// modified.
	/// </summary>
	sealed class LazyList<T> : IList<T>
	{
		readonly Func<IList<T>> initializer;
		IList<T> innerList;
		
		public IList<T> InnerList {
			get {
				if (innerList == null)
					innerList = initializer();
				return innerList;
			}
		}
		
		public LazyList(Func<IList<T>> initializer)
		{
			if (initializer == null) throw new ArgumentNullException("initializer");
			this.initializer = initializer;
		}
		
		public T this[int index] {
			get { return InnerList[index]; }
			set { InnerList[index] = value; }
		}
		
		public int Count {
			get { return InnerList.Count; }
		}
		
		public bool IsReadOnly {
			get { return InnerList.IsReadOnly; }
		}
		
		public int IndexOf(T item)
		{
			return InnerList.IndexOf(item);
		}
		
		public void Insert(int index, T item)
		{
			InnerList.Insert(index, item);
		}
		
		public void RemoveAt(int index)
		{
			InnerList.RemoveAt(index);
		}
		
		public void Add(T item)
		{
			InnerList.Add(item);
		}
		
		public void Clear()
		{
			InnerList.Clear();
		}
		
		public bool Contains(T item)
		{
			return InnerList.Contains(item);
		}
		
		public void CopyTo(T[] array, int arrayIndex)
		{
			InnerList.CopyTo(array, arrayIndex);
		}
		
		public bool Remove(T item)
		{
			return InnerList.Remove(item);
		}
		
		public IEnumerator<T> GetEnumerator()
		{
			return InnerList.GetEnumerator();
		}
		
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}
	}
}
