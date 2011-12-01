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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;

using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Simple interning provider.
	/// </summary>
	public sealed class SimpleInterningProvider : IInterningProvider
	{
		sealed class InterningComparer : IEqualityComparer<ISupportsInterning>
		{
			public bool Equals(ISupportsInterning x, ISupportsInterning y)
			{
				return x.EqualsForInterning(y);
			}
			
			public int GetHashCode(ISupportsInterning obj)
			{
				return obj.GetHashCodeForInterning();
			}
		}
		
		sealed class ListComparer : IEqualityComparer<IEnumerable>
		{
			public bool Equals(IEnumerable a, IEnumerable b)
			{
				if (a.GetType() != b.GetType())
					return false;
				IEnumerator e1 = a.GetEnumerator();
				IEnumerator e2 = b.GetEnumerator();
				while (e1.MoveNext()) {
					// e1 has more elements than e2; or elements are different
					if (!e2.MoveNext() || e1.Current != e2.Current)
						return false;
				}
				if (e2.MoveNext()) // e2 has more elements than e1
					return false;
				// No need to dispose e1/e2: non-generic IEnumerator doesn't implement IDisposable,
				// and the underlying enumerator will likely be a List<T>.Enumerator which has an empty Dispose() method.
				return true;
			}
			
			public int GetHashCode(IEnumerable obj)
			{
				int hashCode = obj.GetType().GetHashCode();
				unchecked {
					foreach (object o in obj) {
						hashCode *= 27;
						hashCode += RuntimeHelpers.GetHashCode(o);
					}
				}
				return hashCode;
			}
		}
		
		Dictionary<object, object> byValueDict = new Dictionary<object, object>();
		Dictionary<ISupportsInterning, ISupportsInterning> supportsInternDict = new Dictionary<ISupportsInterning, ISupportsInterning>(new InterningComparer());
		Dictionary<IEnumerable, IEnumerable> listDict = new Dictionary<IEnumerable, IEnumerable>(new ListComparer());
		
		int stackDepth = 0;
		
		public T Intern<T>(T obj) where T : class
		{
			if (obj == null)
				return null;
			ISupportsInterning s = obj as ISupportsInterning;
			if (s != null) {
				ISupportsInterning output;
				//if (supportsInternDict.TryGetValue(s, out output)) {
				//	obj = (T)output;
				//} else {
					s.PrepareForInterning(this);
					if (supportsInternDict.TryGetValue(s, out output))
						obj = (T)output;
					else
						supportsInternDict.Add(s, s);
				//}
			} else if (Type.GetTypeCode(obj.GetType()) >= TypeCode.Boolean) {
				// Intern primitive types (and strings) by value
				object output;
				if (byValueDict.TryGetValue(obj, out output))
					obj = (T)output;
				else
					byValueDict.Add(obj, obj);
			}
			stackDepth--;
			return obj;
		}
		
		public IList<T> InternList<T>(IList<T> list) where T : class
		{
			if (list == null)
				return null;
			for (int i = 0; i < list.Count; i++) {
				T oldItem = list[i];
				T newItem = Intern(oldItem);
				if (oldItem != newItem) {
					if (list.IsReadOnly) {
						T[] array = new T[list.Count];
						list.CopyTo(array, 0);
						list = array;
					}
					list[i] = newItem;
				}
			}
			if (!list.IsReadOnly)
				list = new ReadOnlyCollection<T>(list);
			IEnumerable output;
			if (listDict.TryGetValue(list, out output))
				list = (IList<T>)output;
			else
				listDict.Add(list, list);
			return list;
		}
	}
}
