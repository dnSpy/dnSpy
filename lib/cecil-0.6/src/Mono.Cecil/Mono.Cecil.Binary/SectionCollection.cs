//
// SectionCollection.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// (C) 2005 Jb Evain
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

namespace Mono.Cecil.Binary {

	using System;
	using System.Collections;

	public sealed class SectionCollection : ICollection, IBinaryVisitable {

		IList m_items;

		public Section this [int index]
		{
			get { return m_items [index] as Section; }
			set { m_items [index] = value; }
		}

		public int Count {
			get { return m_items.Count; }
		}

		public bool IsSynchronized {
			get { return false; }
		}

		public object SyncRoot {
			get { return this; }
		}

		internal SectionCollection ()
		{
			m_items = new ArrayList (4);
		}

		internal void Add (Section value)
		{
			m_items.Add (value);
		}

		internal void Clear ()
		{
			m_items.Clear ();
		}

		public bool Contains (Section value)
		{
			return m_items.Contains (value);
		}

		public int IndexOf (Section value)
		{
			return m_items.IndexOf (value);
		}

		internal void Insert (int index, Section value)
		{
			m_items.Insert (index, value);
		}

		internal void Remove (Section value)
		{
			m_items.Remove (value);
		}

		internal void RemoveAt (int index)
		{
			m_items.Remove (index);
		}

		public void CopyTo (Array ary, int index)
		{
			m_items.CopyTo (ary, index);
		}

		public IEnumerator GetEnumerator ()
		{
			return m_items.GetEnumerator ();
		}

		public void Accept (IBinaryVisitor visitor)
		{
			visitor.VisitSectionCollection (this);

			for (int i = 0; i < m_items.Count; i++)
				this [i].Accept (visitor);
		}
	}
}
