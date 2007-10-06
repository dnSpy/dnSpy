//
// TablesHeap.cs
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

	public class TablesHeap : MetadataHeap {

		public uint Reserved;
		public byte MajorVersion;
		public byte MinorVersion;
		public byte HeapSizes;
		public byte Reserved2;
		public long Valid;
		public long Sorted;

		TableCollection m_tables;

		public const int MaxTableCount = 45;

		public TableCollection Tables {
			get { return m_tables; }
			set { m_tables = value; }
		}

		public IMetadataTable this [int id]
		{
			get { return m_tables [id]; }
			set { m_tables [id] = value; }
		}

		internal TablesHeap (MetadataStream stream) : base(stream, MetadataStream.Tables)
		{
		}

		public bool HasTable (int id)
		{
			return (Valid & (1L << id)) != 0;
		}

		public override void Accept (IMetadataVisitor visitor)
		{
			visitor.VisitTablesHeap (this);
		}
	}
}
