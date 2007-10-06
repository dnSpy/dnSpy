//
// MetadataStreamCollection.cs
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

	public class MetadataStreamCollection : ICollection, IMetadataVisitable {

		IList m_items;

		BlobHeap m_blobHeap;
		GuidHeap m_guidHeap;
		StringsHeap m_stringsHeap;
		UserStringsHeap m_usHeap;
		TablesHeap m_tablesHeap;

		public MetadataStream this [int index] {
			get { return m_items [index] as MetadataStream; }
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

		public BlobHeap BlobHeap {
			get {
				if (m_blobHeap == null)
					m_blobHeap = GetHeap (MetadataStream.Blob) as BlobHeap;
				return m_blobHeap;
			}
		}

		public GuidHeap GuidHeap {
			get {
				if (m_guidHeap == null)
					m_guidHeap = GetHeap (MetadataStream.GUID) as GuidHeap;
				return m_guidHeap;
			}
		}

		public StringsHeap StringsHeap {
			get {
				if (m_stringsHeap == null)
					m_stringsHeap = GetHeap (MetadataStream.Strings) as StringsHeap;
				return m_stringsHeap;
			}
		}

		public TablesHeap TablesHeap {
			get {
				if (m_tablesHeap == null)
					m_tablesHeap = GetHeap (MetadataStream.Tables) as TablesHeap;
				return m_tablesHeap;
			}
		}

		public UserStringsHeap UserStringsHeap {
			get {
				if (m_usHeap == null)
					m_usHeap = GetHeap (MetadataStream.UserStrings) as UserStringsHeap;
				return m_usHeap;
			}
		}

		public MetadataStreamCollection ()
		{
			m_items = new ArrayList (5);
		}

		private MetadataHeap GetHeap (string name)
		{
			for (int i = 0; i < m_items.Count; i++) {
				MetadataStream stream = m_items [i] as MetadataStream;
				if (stream.Heap.Name == name)
					return stream.Heap;
			}

			return null;
		}

		internal void Add (MetadataStream value)
		{
			m_items.Add (value);
		}

		internal void Remove (MetadataStream value)
		{
			m_items.Remove (value);
		}

		public void CopyTo (Array ary, int index)
		{
			m_items.CopyTo (ary, index);
		}

		public IEnumerator GetEnumerator ()
		{
			return m_items.GetEnumerator ();
		}

		public void Accept (IMetadataVisitor visitor)
		{
			visitor.VisitMetadataStreamCollection (this);

			for (int i = 0; i < m_items.Count; i++)
				this [i].Accept (visitor);
		}
	}
}
