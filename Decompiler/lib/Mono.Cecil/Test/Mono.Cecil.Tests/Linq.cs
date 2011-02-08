using System;
using System.Collections.Generic;

using Mono;

#if !NET_3_5 && !NET_4_0

namespace System.Linq {

	static class Enumerable {

		public static IEnumerable<TRet> Select<TItem, TRet> (this IEnumerable<TItem> self, Func<TItem, TRet> selector)
		{
			foreach (var item in self)
				yield return selector (item);
		}

		public static IEnumerable<T> Where<T> (this IEnumerable<T> self, Func<T, bool> predicate)
		{
			foreach (var item in self)
				if (predicate (item))
					yield return item;
		}

		public static List<T> ToList<T> (this IEnumerable<T> self)
		{
			return new List<T> (self);
		}

		public static T [] ToArray<T> (this IEnumerable<T> self)
		{
			return self.ToList ().ToArray ();
		}

		public static T First<T> (this IEnumerable<T> self)
		{
			using (var enumerator = self.GetEnumerator ()) {
				if (!enumerator.MoveNext ())
					throw new InvalidOperationException ();

				return enumerator.Current;
			}
		}
	}
}

#endif
