//
// TableCollection.cs
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

	public class TableCollection : ICollection, IMetadataTableVisitable	{

		IMetadataTable [] m_tables = new IMetadataTable [TablesHeap.MaxTableCount];

		TablesHeap m_heap;

		public IMetadataTable this [int index] {
			get { return m_tables [index]; }
			set { m_tables [index] = value; }
		}

		public int Count {
			get {
				return GetList ().Count;
			}
		}

		public bool IsSynchronized {
			get { return false; }
		}

		public object SyncRoot {
			get { return this; }
		}

		public TablesHeap Heap {
			get { return m_heap; }
		}

		internal TableCollection (TablesHeap heap)
		{
			m_heap = heap;
		}

		internal void Add (IMetadataTable value)
		{
			m_tables [value.Id] = value;
		}

		public bool Contains (IMetadataTable value)
		{
			return m_tables [value.Id] != null;
		}

		internal void Remove (IMetadataTable value)
		{
			m_tables [value.Id] = null;
		}

		public void CopyTo (Array array, int index)
		{
			GetList ().CopyTo (array, index);
		}

		internal IList GetList ()
		{
			IList tables = new ArrayList ();
			for (int i = 0; i < m_tables.Length; i++) {
				IMetadataTable table = m_tables [i];
				if (table != null)
					tables.Add (table);
			}

			return tables;
		}

		public IEnumerator GetEnumerator ()
		{
			return GetList ().GetEnumerator ();
		}

		public void Accept (IMetadataTableVisitor visitor)
		{
			visitor.VisitTableCollection (this);

			foreach (IMetadataTable table in GetList ())
				table.Accept (visitor);

			visitor.TerminateTableCollection (this);
		}
	}
}
