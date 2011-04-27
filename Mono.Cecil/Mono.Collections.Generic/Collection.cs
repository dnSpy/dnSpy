//
// Collection.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2011 Jb Evain
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
using System.Collections.Generic;

namespace Mono.Collections.Generic {

	public class Collection<T> : IList<T>, IList {

		internal T [] items;
		internal int size;
		int version;

		public int Count {
			get { return size; }
		}

		public T this [int index] {
			get {
				if (index >= size)
					throw new ArgumentOutOfRangeException ();

				return items [index];
			}
			set {
				CheckIndex (index);
				if (index == size)
					throw new ArgumentOutOfRangeException ();

				OnSet (value, index);

				items [index] = value;
			}
		}

		bool ICollection<T>.IsReadOnly {
			get { return false; }
		}

		bool IList.IsFixedSize {
			get { return false; }
		}

		bool IList.IsReadOnly {
			get { return false; }
		}

		object IList.this [int index] {
			get { return this [index]; }
			set {
				CheckIndex (index);

				try {
					this [index] = (T) value;
					return;
				} catch (InvalidCastException) {
				} catch (NullReferenceException) {
				}

				throw new ArgumentException ();
			}
		}

		int ICollection.Count {
			get { return Count; }
		}

		bool ICollection.IsSynchronized {
			get { return false; }
		}

		object ICollection.SyncRoot {
			get { return this; }
		}

		public Collection ()
		{
			items = Empty<T>.Array;
		}

		public Collection (int capacity)
		{
			if (capacity < 0)
				throw new ArgumentOutOfRangeException ();

			items = new T [capacity];
		}

		public Collection (ICollection<T> items)
		{
			if (items == null)
				throw new ArgumentNullException ("items");

			this.items = new T [items.Count];
			items.CopyTo (this.items, 0);
			this.size = this.items.Length;
		}

		public void Add (T item)
		{
			if (size == items.Length)
				Grow (1);

			OnAdd (item, size);

			items [size++] = item;
			version++;
		}

		public bool Contains (T item)
		{
			return IndexOf (item) != -1;
		}

		public int IndexOf (T item)
		{
			return Array.IndexOf (items, item, 0, size);
		}

		public void Insert (int index, T item)
		{
			CheckIndex (index);
			if (size == items.Length)
				Grow (1);

			OnInsert (item, index);

			Shift (index, 1);
			items [index] = item;
			version++;
		}

		public void RemoveAt (int index)
		{
			if (index < 0 || index >= size)
				throw new ArgumentOutOfRangeException ();

			var item = items [index];

			OnRemove (item, index);

			Shift (index, -1);
			Array.Clear (items, size, 1);
			version++;
		}

		public bool Remove (T item)
		{
			var index = IndexOf (item);
			if (index == -1)
				return false;

			OnRemove (item, index);

			Shift (index, -1);
			Array.Clear (items, size, 1);
			version++;

			return true;
		}

		public void Clear ()
		{
			OnClear ();

			Array.Clear (items, 0, size);
			size = 0;
			version++;
		}

		public void CopyTo (T [] array, int arrayIndex)
		{
			Array.Copy (items, 0, array, arrayIndex, size);
		}

		public T [] ToArray ()
		{
			var array = new T [size];
			Array.Copy (items, 0, array, 0, size);
			return array;
		}

		void CheckIndex (int index)
		{
			if (index < 0 || index > size)
				throw new ArgumentOutOfRangeException ();
		}

		void Shift (int start, int delta)
		{
			if (delta < 0)
				start -= delta;

			if (start < size)
				Array.Copy (items, start, items, start + delta, size - start);

			size += delta;

			if (delta < 0)
				Array.Clear (items, size, -delta);
		}

		protected virtual void OnAdd (T item, int index)
		{
		}

		protected virtual void OnInsert (T item, int index)
		{
		}

		protected virtual void OnSet (T item, int index)
		{
		}

		protected virtual void OnRemove (T item, int index)
		{
		}

		protected virtual void OnClear ()
		{
		}

		internal virtual void Grow (int desired)
		{
			int new_size = size + desired;
			if (new_size <= items.Length)
				return;

			const int default_capacity = 4;

			new_size = System.Math.Max (
				System.Math.Max (items.Length * 2, default_capacity),
				new_size);

#if !CF
			Array.Resize (ref items, new_size);
#else
			var array = new T [new_size];
			Array.Copy (items, array, size);
			items = array;
#endif
		}

		int IList.Add (object value)
		{
			try {
				Add ((T) value);
				return size - 1;
			} catch (InvalidCastException) {
			} catch (NullReferenceException) {
			}

			throw new ArgumentException ();
		}

		void IList.Clear ()
		{
			Clear ();
		}

		bool IList.Contains (object value)
		{
			return ((IList) this).IndexOf (value) > -1;
		}

		int IList.IndexOf (object value)
		{
			try {
				return IndexOf ((T) value);
			} catch (InvalidCastException) {
			} catch (NullReferenceException) {
			}

			return -1;
		}

		void IList.Insert (int index, object value)
		{
			CheckIndex (index);

			try {
				Insert (index, (T) value);
				return;
			} catch (InvalidCastException) {
			} catch (NullReferenceException) {
			}

			throw new ArgumentException ();
		}

		void IList.Remove (object value)
		{
			try {
				Remove ((T) value);
			} catch (InvalidCastException) {
			} catch (NullReferenceException) {
			}
		}

		void IList.RemoveAt (int index)
		{
			RemoveAt (index);
		}

		void ICollection.CopyTo (Array array, int index)
		{
			Array.Copy (items, 0, array, index, size);
		}

		public Enumerator GetEnumerator ()
		{
			return new Enumerator (this);
		}

		IEnumerator IEnumerable.GetEnumerator ()
		{
			return new Enumerator (this);
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator ()
		{
			return new Enumerator (this);
		}

		public struct Enumerator : IEnumerator<T>, IDisposable {

			Collection<T> collection;
			T current;

			int next;
			readonly int version;

			public T Current {
				get { return current; }
			}

			object IEnumerator.Current {
				get {
					CheckState ();

					if (next <= 0)
						throw new InvalidOperationException ();

					return current;
				}
			}

			internal Enumerator (Collection<T> collection)
				: this ()
			{
				this.collection = collection;
				this.version = collection.version;
			}

			public bool MoveNext ()
			{
				CheckState ();

				if (next < 0)
					return false;

				if (next < collection.size) {
					current = collection.items [next++];
					return true;
				}

				next = -1;
				return false;
			}

			public void Reset ()
			{
				CheckState ();

				next = 0;
			}

			void CheckState ()
			{
				if (collection == null)
					throw new ObjectDisposedException (GetType ().FullName);

				if (version != collection.version)
					throw new InvalidOperationException ();
			}

			public void Dispose ()
			{
				collection = null;
			}
		}
	}
}
