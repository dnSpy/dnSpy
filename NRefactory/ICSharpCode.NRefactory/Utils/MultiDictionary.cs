// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
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
using System.Linq;

namespace ICSharpCode.NRefactory.Utils
{
	/// <summary>
	/// A dictionary that allows multiple pairs with the same key.
	/// </summary>
	public class MultiDictionary<TKey, TValue> : ILookup<TKey, TValue>
	{
		readonly Dictionary<TKey, List<TValue>> dict;
		
		public MultiDictionary()
		{
			dict = new Dictionary<TKey, List<TValue>>();
		}
		
		public MultiDictionary(IEqualityComparer<TKey> comparer)
		{
			dict = new Dictionary<TKey, List<TValue>>(comparer);
		}
		
		public void Add(TKey key, TValue value)
		{
			List<TValue> valueList;
			if (!dict.TryGetValue(key, out valueList)) {
				valueList = new List<TValue>();
				dict.Add(key, valueList);
			}
			valueList.Add(value);
		}

		public bool Remove(TKey key, TValue value)
		{
			List<TValue> valueList;
			if (dict.TryGetValue(key, out valueList)) {
				if (valueList.Remove(value)) {
					if (valueList.Count == 0)
						dict.Remove(key);
					return true;
				}
			}
			return false;
		}
		
		public void Clear()
		{
			dict.Clear();
		}
		
		#if NET_4_5
		public IReadOnlyList<TValue> this[TKey key] {
		#else
		public IList<TValue> this[TKey key] {
		#endif
			get {
				List<TValue> list;
				if (dict.TryGetValue(key, out list))
					return list;
				else
					return EmptyList<TValue>.Instance;
			}
		}
		
		public int Count {
			get { return dict.Count; }
		}
		
		IEnumerable<TValue> ILookup<TKey, TValue>.this[TKey key] {
			get { return this[key]; }
		}
		
		bool ILookup<TKey, TValue>.Contains(TKey key)
		{
			return dict.ContainsKey(key);
		}
		
		public IEnumerator<IGrouping<TKey, TValue>> GetEnumerator()
		{
			foreach (var pair in dict)
				yield return new Grouping(pair.Key, pair.Value);
		}
		
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		
		sealed class Grouping : IGrouping<TKey, TValue>
		{
			readonly TKey key;
			readonly List<TValue> values;
			
			public Grouping(TKey key, List<TValue> values)
			{
				this.key = key;
				this.values = values;
			}
			
			public TKey Key {
				get { return key; }
			}
			
			public IEnumerator<TValue> GetEnumerator()
			{
				return values.GetEnumerator();
			}
			
			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
			{
				return values.GetEnumerator();
			}
		}
	}
}
