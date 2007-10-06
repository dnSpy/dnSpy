//
// MetadataStream.cs
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

	public class MetadataStream : IMetadataVisitable {

		public const string Strings = "#Strings";
		public const string Tables = "#~";
		public const string IncrementalTables = "#-";
		public const string Blob = "#Blob";
		public const string GUID = "#GUID";
		public const string UserStrings = "#US";

		MetadataStreamHeader m_header;
		MetadataHeap m_heap;

		public MetadataStreamHeader Header {
			get { return m_header; }
			set { m_header = value; }
		}

		public MetadataHeap Heap {
			get { return m_heap; }
			set { m_heap = value; }
		}

		internal MetadataStream ()
		{
			m_header = new MetadataStreamHeader (this);
		}

		public void Accept (IMetadataVisitor visitor)
		{
			visitor.VisitMetadataStream (this);

			m_header.Accept (visitor);
			if (m_heap != null)
				m_heap.Accept (visitor);
		}

		public class MetadataStreamHeader : IMetadataVisitable {

			public uint Offset;
			public uint Size;
			public string Name;

			private MetadataStream m_stream;

			public MetadataStream Stream {
				get { return m_stream; }
			}

			internal MetadataStreamHeader (MetadataStream stream)
			{
				m_stream = stream;
			}

			public void Accept (IMetadataVisitor visitor)
			{
				visitor.VisitMetadataStreamHeader (this);
			}
		}
	}
}
