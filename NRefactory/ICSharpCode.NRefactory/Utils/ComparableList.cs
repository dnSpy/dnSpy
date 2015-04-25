//
// NullValueAnalysisTests.cs
//
// Author:
//       Luís Reis <luiscubal@gmail.com
//
// Copyright (c) 2013 Luís Reis
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.Utils
{
	/// <summary>
	/// A list that can be compared to other ComparableLists for equality.
	/// Can not be used to store null values.
	/// </summary>
	public sealed class ComparableList<T> : IList<T>, IEquatable<ComparableList<T>>
	{
		List<T> elements;

		public ComparableList()
		{
			elements = new List<T> ();
		}

		public ComparableList(IEnumerable<T> values)
		{
			elements = new List<T> (values);
		}

		public int IndexOf (T item)
		{
			if (item == null)
				throw new ArgumentNullException ("item");
			return elements.IndexOf (item);
		}

		public void Insert (int index, T item)
		{
			elements.Insert (index, item);
		}

		public void RemoveAt (int index)
		{
			elements.RemoveAt (index);
		}

		public T this [int index] {
			get {
				return elements [index];
			}
			set {
				elements [index] = value;
			}
		}

		public void Add (T item)
		{
			elements.Add (item);
		}

		public void Clear ()
		{
			elements.Clear ();
		}

		public bool Contains (T item)
		{
			return elements.Contains (item);
		}

		public void CopyTo (T[] array, int arrayIndex)
		{
			elements.CopyTo (array, arrayIndex);
		}

		public bool Remove (T item)
		{
			return elements.Remove (item);
		}

		public int Count {
			get {
				return elements.Count;
			}
		}

		public bool IsReadOnly {
			get {
				return false;
			}
		}

		public IEnumerator<T> GetEnumerator()
		{
			return elements.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator ();
		}

		public override bool Equals (object obj)
		{
			return Equals (obj as ComparableList<T>);
		}

		public bool Equals (ComparableList<T> obj)
		{
			if (obj == null || Count != obj.Count) {
				return false;
			}

			for (int index = 0; index < Count; ++index) {
				if (!this [index].Equals(obj [index])) {
					return false;
				}
			}

			return true;
		}

		public override int GetHashCode ()
		{
			int hash = 19;
			foreach (var item in this) {
				unchecked {
					hash *= 31;
					hash += item.GetHashCode();
				}
			}
			return hash;
		}

		public static bool operator==(ComparableList<T> item1, ComparableList<T> item2) {
			if (object.ReferenceEquals (item1, null))
				return object.ReferenceEquals (item2, null);
			return item1.Equals (item2);
		}

		public static bool operator!=(ComparableList<T> item1, ComparableList<T> item2) {
			return !(item1 == item2);
		}
	}
}

