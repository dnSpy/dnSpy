//
// MetadataInitializer.cs
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
	using System.IO;
	using System.Text;

	using Mono.Cecil;
	using Mono.Cecil.Binary;

	class MetadataInitializer : BaseMetadataVisitor {

		MetadataRoot m_root;

		public MetadataInitializer (ImageInitializer init)
		{
			m_root = init.Image.MetadataRoot;
		}

		public override void VisitMetadataRoot (MetadataRoot root)
		{
			root.Header = new MetadataRoot.MetadataRootHeader ();
			root.Streams = new MetadataStreamCollection ();
		}

		public override void VisitMetadataRootHeader (MetadataRoot.MetadataRootHeader header)
		{
			header.SetDefaultValues ();
		}

		public override void VisitMetadataStreamCollection (MetadataStreamCollection coll)
		{
			MetadataStream tables = new MetadataStream ();
			tables.Header.Name = MetadataStream.Tables;
			tables.Heap = MetadataHeap.HeapFactory (tables);
			TablesHeap th = tables.Heap as TablesHeap;
			th.Tables = new TableCollection (th);
			m_root.Streams.Add (tables);
		}

		public override void VisitTablesHeap (TablesHeap th)
		{
			th.Reserved = 0;
			th.MajorVersion = 1;
			th.MinorVersion = 0;
			th.Reserved2 = 1;
			th.Sorted = 0x2003301fa00;
		}
	}
}
