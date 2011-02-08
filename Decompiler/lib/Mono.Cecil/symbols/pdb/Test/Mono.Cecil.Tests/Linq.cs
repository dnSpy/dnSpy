using System;
using System.Collections.Generic;

#if !NET_3_5 && !NET_4_0

namespace System {

	delegate TResult Func<T, TResult> (T t);
}

namespace System.Runtime.CompilerServices {

	[AttributeUsage (AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly)]
	sealed class ExtensionAttribute : Attribute {
	}
}

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
