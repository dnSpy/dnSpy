// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
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
		public SimpleInterningProvider()
		{
			// Intern the well-known types first; so that they are used when possible.
			foreach (ITypeReference r in KnownTypeReference.AllKnownTypeReferences)
				Intern(r);
		}
		
		sealed class ReferenceComparer : IEqualityComparer<object>
		{
			public readonly static ReferenceComparer Instance = new ReferenceComparer();
			
			public new bool Equals(object a, object b)
			{
				return ReferenceEquals(a, b);
			}
			
			public int GetHashCode(object obj)
			{
				return RuntimeHelpers.GetHashCode(obj);
			}
		}
		
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
		
		sealed class ListComparer : IEqualityComparer<IEnumerable<object>>
		{
			public bool Equals(IEnumerable<object> a, IEnumerable<object> b)
			{
				if (a.GetType() != b.GetType())
					return false;
				return Enumerable.SequenceEqual(a, b, ReferenceComparer.Instance);
			}
			
			public int GetHashCode(IEnumerable<object> obj)
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
		Dictionary<IEnumerable<object>, IEnumerable<object>> listDict = new Dictionary<IEnumerable<object>, IEnumerable<object>>(new ListComparer());
		
		public T Intern<T>(T obj) where T : class
		{
			if (obj == null)
				return null;
			ISupportsInterning s = obj as ISupportsInterning;
			if (s != null) {
				ISupportsInterning output;
				if (supportsInternDict.TryGetValue(s, out output)) {
					obj = (T)output;
				} else {
					s.PrepareForInterning(this);
					if (supportsInternDict.TryGetValue(s, out output))
						obj = (T)output;
					else
						supportsInternDict.Add(s, s);
				}
			} else if (obj is IType || Type.GetTypeCode(obj.GetType()) >= TypeCode.Boolean) {
				object output;
				if (byValueDict.TryGetValue(obj, out output))
					obj = (T)output;
				else
					byValueDict.Add(obj, obj);
			}
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
					if (list.IsReadOnly)
						list = new T[list.Count];
					list[i] = newItem;
				}
			}
			if (!list.IsReadOnly)
				list = new ReadOnlyCollection<T>(list);
			IEnumerable<object> output;
			if (listDict.TryGetValue(list, out output))
				list = (IList<T>)output;
			else
				listDict.Add(list, list);
			return list;
		}
	}
}
