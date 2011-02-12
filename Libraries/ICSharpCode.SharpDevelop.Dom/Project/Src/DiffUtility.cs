// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections;
using System.Collections.Generic;

namespace ICSharpCode.SharpDevelop.Dom
{
	public static class DiffUtility
	{
		public static int GetAddedItems(IList original, IList changed, IList result)
		{
			return GetAddedItems(original, changed, result, Comparer.Default);
		}
		
		public static int GetAddedItems(IList original, IList changed, IList result, IComparer comparer)
		{
			int count = 0;
			if(changed != null && result != null) {
				if(original == null) {
					foreach(object item in changed) {
						result.Add(item);
					}
					count = changed.Count;
				}
				else {
					foreach(object item in changed) {
						if(!Contains(original, item, comparer)) {
							result.Add(item);
							count++;
						}
					}
				}
			}
			return count;
		}
		
		public static int GetRemovedItems(IList original, IList changed, IList result)
		{
			return GetRemovedItems(original, changed, result, Comparer.Default);
		}
		
		public static int GetRemovedItems(IList original, IList changed, IList result, IComparer comparer)
		{
			return GetAddedItems(changed, original, result, comparer);
		}
		
		static bool Contains(IList list, object value, IComparer comparer)
		{
			foreach(object item in list) {
				if(0 == comparer.Compare(item, value)) {
					return true;
				}
			}
			return false;
		}
		
		static public int Compare(IList a, IList b)
		{
			return Compare(a, b, Comparer.Default);
		}
		
		static public int Compare<T>(IList<T> a, IList<T> b)
		{
			return Compare(a, b, Comparer.Default);
		}
		
		static public int Compare<T>(IList<T> a, IList<T> b, IComparer comparer)
		{
			if (a == null || b == null) {
				return 1;
			}
			if (a.Count != b.Count) {
				return Math.Sign(a.Count - b.Count);
			}
			int limit = (a.Count < b.Count) ? a.Count : b.Count;
			for(int i=0; i < limit; i++) {
				if (a[i] is IComparable && b[i] is IComparable) {
					int cmp = comparer.Compare(a[i], b[i]);
					if (cmp != 0) {
						return cmp;
					}
				}
			}
			return a.Count - b.Count;
		}
		
		static public int Compare(IList a, IList b, IComparer comparer)
		{
			if (a == null || b == null) {
				return 1;
			}
			if (a.Count != b.Count) {
				return Math.Sign(a.Count - b.Count);
			}
			int limit = (a.Count < b.Count) ? a.Count : b.Count;
			for(int i=0; i < limit; i++) {
				if (a[i] is IComparable && b[i] is IComparable) {
					int cmp = comparer.Compare(a[i], b[i]);
					if (cmp != 0) {
						return cmp;
					}
				}
			}
			return a.Count - b.Count;
		}
		
		static public int Compare(SortedList a, SortedList b)
		{
			return Compare(a, b, Comparer.Default);
		}
		
		static public int Compare(SortedList a, SortedList b, IComparer comparer)
		{
			if (a == null || b == null) {
				return 1;
			}
			int cmp;
			int limit = (a.Count < b.Count) ? a.Count : b.Count;
			for(int i=0; i < limit; i++) {
				if(0 != (cmp = comparer.Compare(a.GetByIndex(i), b.GetByIndex(i)))) {
					return cmp;
				}
			}
			return a.Count - b.Count;
		}
	}
}
