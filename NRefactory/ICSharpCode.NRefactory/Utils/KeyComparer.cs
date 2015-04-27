// Copyright (c) 2010-2013 AlphaSierraPapa for the SharpDevelop Team
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

namespace ICSharpCode.NRefactory.Utils
{
	public static class KeyComparer
	{
		public static KeyComparer<TElement, TKey> Create<TElement, TKey>(Func<TElement, TKey> keySelector)
		{
			return new KeyComparer<TElement, TKey>(keySelector, Comparer<TKey>.Default, EqualityComparer<TKey>.Default);
		}
		
		public static KeyComparer<TElement, TKey> Create<TElement, TKey>(Func<TElement, TKey> keySelector, IComparer<TKey> comparer, IEqualityComparer<TKey> equalityComparer)
		{
			return new KeyComparer<TElement, TKey>(keySelector, comparer, equalityComparer);
		}
		
		public static IComparer<TElement> Create<TElement, TKey>(Func<TElement, TKey> keySelector, IComparer<TKey> comparer)
		{
			return new KeyComparer<TElement, TKey>(keySelector, comparer, EqualityComparer<TKey>.Default);
		}
		
		public static IEqualityComparer<TElement> Create<TElement, TKey>(Func<TElement, TKey> keySelector, IEqualityComparer<TKey> equalityComparer)
		{
			return new KeyComparer<TElement, TKey>(keySelector, Comparer<TKey>.Default, equalityComparer);
		}
	}
	
	public class KeyComparer<TElement, TKey> : IComparer<TElement>, IEqualityComparer<TElement>
	{
		readonly Func<TElement, TKey> keySelector;
		readonly IComparer<TKey> keyComparer;
		readonly IEqualityComparer<TKey> keyEqualityComparer;
		
		public KeyComparer(Func<TElement, TKey> keySelector, IComparer<TKey> keyComparer, IEqualityComparer<TKey> keyEqualityComparer)
		{
			if (keySelector == null)
				throw new ArgumentNullException("keySelector");
			if (keyComparer == null)
				throw new ArgumentNullException("keyComparer");
			if (keyEqualityComparer == null)
				throw new ArgumentNullException("keyEqualityComparer");
			this.keySelector = keySelector;
			this.keyComparer = keyComparer;
			this.keyEqualityComparer = keyEqualityComparer;
		}
		
		public int Compare(TElement x, TElement y)
		{
			return keyComparer.Compare(keySelector(x), keySelector(y));
		}
		
		public bool Equals(TElement x, TElement y)
		{
			return keyEqualityComparer.Equals(keySelector(x), keySelector(y));
		}
		
		public int GetHashCode(TElement obj)
		{
			return keyEqualityComparer.GetHashCode(keySelector(obj));
		}
	}
}
