//
// RowCollection.cs
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

namespace Mono.Cecil.Metadata {

	using System;
	using System.Collections;

	public class RowCollection : ICollection, IMetadataRowVisitable {

		ArrayList m_items;

		public IMetadataRow this [int index] {
			get { return m_items [index] as IMetadataRow; }
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

		internal RowCollection (int size)
		{
			m_items = new ArrayList (size);
		}

		internal RowCollection ()
		{
			m_items = new ArrayList ();
		}

		internal void Add (IMetadataRow value)
		{
			m_items.Add (value);
		}

		public void Clear ()
		{
			m_items.Clear ();
		}

		public bool Contains (IMetadataRow value)
		{
			return m_items.Contains (value);
		}

		public int IndexOf (IMetadataRow value)
		{
			return m_items.IndexOf (value);
		}

		public void Insert (int index, IMetadataRow value)
		{
			m_items.Insert (index, value);
		}

		public void Remove (IMetadataRow value)
		{
			m_items.Remove (value);
		}

		public void RemoveAt (int index)
		{
			m_items.Remove (index);
		}

		public void CopyTo (Array ary, int index)
		{
			m_items.CopyTo (ary, index);
		}

		public void Sort (IComparer comp)
		{
			m_items.Sort (comp);
		}

		public IEnumerator GetEnumerator ()
		{
			return m_items.GetEnumerator ();
		}

		public void Accept (IMetadataRowVisitor visitor)
		{
			visitor.VisitRowCollection (this);

			for (int i = 0; i < m_items.Count; i++)
				this [i].Accept (visitor);

			visitor.TerminateRowCollection (this);
		}
	}
}
