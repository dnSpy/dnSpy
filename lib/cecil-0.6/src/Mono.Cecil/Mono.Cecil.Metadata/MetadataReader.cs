//
// MetadataReader.cs
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

	using Mono.Cecil.Binary;

	class MetadataReader : BaseMetadataVisitor {

		ImageReader m_ir;
		BinaryReader m_binaryReader;
		MetadataTableReader m_tableReader;
		MetadataRoot m_root;

		public MetadataTableReader TableReader {
			get { return m_tableReader; }
		}

		public MetadataReader (ImageReader brv)
		{
			m_ir = brv;
			m_binaryReader = brv.GetReader ();
		}

		public MetadataRoot GetMetadataRoot ()
		{
			return m_root;
		}

		public BinaryReader GetDataReader (RVA rva)
		{
			return m_ir.Image.GetReaderAtVirtualAddress (rva);
		}

		public override void VisitMetadataRoot (MetadataRoot root)
		{
			m_root = root;
			root.Header = new MetadataRoot.MetadataRootHeader ();
			root.Streams = new MetadataStreamCollection ();
		}

		public override void VisitMetadataRootHeader (MetadataRoot.MetadataRootHeader header)
		{
			long headpos = m_binaryReader.BaseStream.Position;

			header.Signature = m_binaryReader.ReadUInt32 ();

			if (header.Signature != MetadataRoot.MetadataRootHeader.StandardSignature)
				throw new MetadataFormatException ("Wrong magic number");

			header.MajorVersion = m_binaryReader.ReadUInt16 ();
			header.MinorVersion = m_binaryReader.ReadUInt16 ();
			header.Reserved = m_binaryReader.ReadUInt32 ();

			// read version
			uint length = m_binaryReader.ReadUInt32 ();
			if (length != 0) {
				long pos = m_binaryReader.BaseStream.Position;

				byte [] version, buffer = new byte [length];
				int read = 0;
				while (read < length) {
					byte cur = (byte)m_binaryReader.ReadSByte ();
					if (cur == 0)
						break;
					buffer [read++] = cur;
				}
				version = new byte [read];
				Buffer.BlockCopy (buffer, 0, version, 0, read);
				header.Version = Encoding.UTF8.GetString (version, 0, version.Length);

				pos += length - headpos + 3;
				pos &= ~3;
				pos += headpos;

				m_binaryReader.BaseStream.Position = pos;
			} else
				header.Version = string.Empty;

			header.Flags = m_binaryReader.ReadUInt16 ();
			header.Streams = m_binaryReader.ReadUInt16 ();
		}

		public override void VisitMetadataStreamCollection (MetadataStreamCollection coll)
		{
			for (int i = 0; i < m_root.Header.Streams; i++)
				coll.Add (new MetadataStream ());
		}

		public override void VisitMetadataStreamHeader (MetadataStream.MetadataStreamHeader header)
		{
			header.Offset = m_binaryReader.ReadUInt32 ();
			header.Size = m_binaryReader.ReadUInt32 ();

			StringBuilder buffer = new StringBuilder ();
			while (true) {
				char cur = (char) m_binaryReader.ReadSByte ();
				if (cur == '\0')
					break;
				buffer.Append (cur);
			}
			header.Name = buffer.ToString ();
			if (header.Name.Length == 0)
				throw new MetadataFormatException ("Invalid stream name");

			long rootpos = m_root.GetImage ().ResolveVirtualAddress (
				m_root.GetImage ().CLIHeader.Metadata.VirtualAddress);

			long curpos = m_binaryReader.BaseStream.Position;

			if (header.Size != 0)
				curpos -= rootpos;

			curpos += 3;
			curpos &= ~3;

			if (header.Size != 0)
				curpos += rootpos;

			m_binaryReader.BaseStream.Position = curpos;

			header.Stream.Heap = MetadataHeap.HeapFactory (header.Stream);
		}

		public override void VisitGuidHeap (GuidHeap heap)
		{
			VisitHeap (heap);
		}

		public override void VisitStringsHeap (StringsHeap heap)
		{
			VisitHeap (heap);

			if (heap.Data.Length < 1 && heap.Data [0] != 0)
				throw new MetadataFormatException ("Malformed #Strings heap");

			heap [(uint) 0] = string.Empty;
		}

		public override void VisitTablesHeap (TablesHeap heap)
		{
			VisitHeap (heap);
			heap.Tables = new TableCollection (heap);

			BinaryReader br = new BinaryReader (new MemoryStream (heap.Data));
			try {
				heap.Reserved = br.ReadUInt32 ();
				heap.MajorVersion = br.ReadByte ();
				heap.MinorVersion = br.ReadByte ();
				heap.HeapSizes = br.ReadByte ();
				heap.Reserved2 = br.ReadByte ();
				heap.Valid = br.ReadInt64 ();
				heap.Sorted = br.ReadInt64 ();
			} finally {
				// COMPACT FRAMEWORK NOTE: BinaryReader is not IDisposable
				br.Close ();
			}
		}

		public override void VisitBlobHeap (BlobHeap heap)
		{
			VisitHeap (heap);
		}

		public override void VisitUserStringsHeap (UserStringsHeap heap)
		{
			VisitHeap (heap);
		}

		void VisitHeap (MetadataHeap heap)
		{
			long cursor = m_binaryReader.BaseStream.Position;

			m_binaryReader.BaseStream.Position = m_root.GetImage ().ResolveVirtualAddress (
				m_root.GetImage ().CLIHeader.Metadata.VirtualAddress)
				+ heap.GetStream ().Header.Offset;

			heap.Data = m_binaryReader.ReadBytes ((int) heap.GetStream ().Header.Size);

			m_binaryReader.BaseStream.Position = cursor;
		}

		void SetHeapIndexSize (MetadataHeap heap, byte flag)
		{
			if (heap == null)
				return;
			TablesHeap th = m_root.Streams.TablesHeap;
			heap.IndexSize = ((th.HeapSizes & flag) > 0) ? 4 : 2;
		}

		public override void TerminateMetadataRoot (MetadataRoot root)
		{
			SetHeapIndexSize (root.Streams.StringsHeap, 0x01);
			SetHeapIndexSize (root.Streams.GuidHeap, 0x02);
			SetHeapIndexSize (root.Streams.BlobHeap, 0x04);
			m_tableReader = new MetadataTableReader (this);
			root.Streams.TablesHeap.Tables.Accept (m_tableReader);
		}
	}
}
