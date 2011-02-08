//
// ReadOnlyCollection.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2010 Jb Evain
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections;

namespace Mono.Collections.Generic {

	public sealed class ReadOnlyCollection<T> : Collection<T>, IList {

		static ReadOnlyCollection<T> empty;

		public static ReadOnlyCollection<T> Empty {
			get { return empty ?? (empty = new ReadOnlyCollection<T> ()); }
		}

		bool IList.IsReadOnly {
			get { return true; }
		}

		private ReadOnlyCollection ()
		{
		}

		public ReadOnlyCollection (T [] array)
		{
			if (array == null)
				throw new ArgumentNullException ();

			this.items = array;
			this.size = array.Length;
		}

		public ReadOnlyCollection (Collection<T> collection)
		{
			if (collection == null)
				throw new ArgumentNullException ();

			this.items = collection.items;
			this.size = collection.size;
		}

		internal override void Grow (int desired)
		{
			throw new InvalidOperationException ();
		}

		protected override void OnAdd (T item, int index)
		{
			throw new InvalidOperationException ();
		}

		protected override void OnClear ()
		{
			throw new InvalidOperationException ();
		}

		protected override void OnInsert (T item, int index)
		{
			throw new InvalidOperationException ();
		}

		protected override void OnRemove (T item, int index)
		{
			throw new InvalidOperationException ();
		}

		protected override void OnSet (T item, int index)
		{
			throw new InvalidOperationException ();
		}
	}
}
