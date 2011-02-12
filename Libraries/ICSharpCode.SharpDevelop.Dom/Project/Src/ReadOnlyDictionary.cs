// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;

namespace ICSharpCode.SharpDevelop.Dom
{
	/// <summary>
	/// Wraps a IDictonary, allowing only the read-only operations.
	/// </summary>
	sealed class ReadOnlyDictionary<TKey, TValue> : IDictionary<TKey, TValue>
	{
		IDictionary<TKey, TValue> baseDictionary;
		
		public ReadOnlyDictionary(IDictionary<TKey, TValue> baseDictionary)
		{
			if (baseDictionary == null)
				throw new ArgumentNullException("baseDictionary");
			this.baseDictionary = baseDictionary;
		}
		
		public TValue this[TKey key] {
			get { return baseDictionary[key]; }
			set { throw new NotSupportedException(); }
		}
		
		public ICollection<TKey> Keys {
			get { return baseDictionary.Keys; }
		}
		
		public ICollection<TValue> Values {
			get { return baseDictionary.Values; }
		}
		
		public int Count {
			get { return baseDictionary.Count; }
		}
		
		public bool IsReadOnly {
			get { return true; }
		}
		
		public bool ContainsKey(TKey key)
		{
			return baseDictionary.ContainsKey(key);
		}
		
		public void Add(TKey key, TValue value)
		{
			throw new NotSupportedException();
		}
		
		public bool Remove(TKey key)
		{
			throw new NotSupportedException();
		}
		
		public bool TryGetValue(TKey key, out TValue value)
		{
			return baseDictionary.TryGetValue(key, out value);
		}
		
		public void Add(KeyValuePair<TKey, TValue> item)
		{
			throw new NotSupportedException();
		}
		
		public void Clear()
		{
			throw new NotSupportedException();
		}
		
		public bool Contains(KeyValuePair<TKey, TValue> item)
		{
			return baseDictionary.Contains(item);
		}
		
		public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
		{
			throw new NotSupportedException();
		}
		
		public bool Remove(KeyValuePair<TKey, TValue> item)
		{
			throw new NotSupportedException();
		}
		
		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			return baseDictionary.GetEnumerator();
		}
		
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return baseDictionary.GetEnumerator();
		}
	}
}
