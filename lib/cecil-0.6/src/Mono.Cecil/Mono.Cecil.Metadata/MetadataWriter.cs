//
// MetadataWriter.cs
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
	using System.IO;
	using System.Text;

	using Mono.Cecil;
	using Mono.Cecil.Binary;

	internal sealed class MetadataWriter : BaseMetadataVisitor {

		AssemblyDefinition m_assembly;
		MetadataRoot m_root;
		TargetRuntime m_runtime;
		ImageWriter m_imgWriter;
		MetadataTableWriter m_tableWriter;
		MemoryBinaryWriter m_binaryWriter;

		IDictionary m_stringCache;
		MemoryBinaryWriter m_stringWriter;

		IDictionary m_guidCache;
		MemoryBinaryWriter m_guidWriter;

		IDictionary m_usCache;
		MemoryBinaryWriter m_usWriter;

		IDictionary m_blobCache;
		MemoryBinaryWriter m_blobWriter;

		MemoryBinaryWriter m_tWriter;

		MemoryBinaryWriter m_cilWriter;

		MemoryBinaryWriter m_fieldDataWriter;
		MemoryBinaryWriter m_resWriter;

		uint m_mdStart, m_mdSize;
		uint m_resStart, m_resSize;
		uint m_snsStart, m_snsSize;
		uint m_debugHeaderStart;
		uint m_imporTableStart;

		uint m_entryPointToken;

		RVA m_cursor = new RVA (0x2050);

		public MemoryBinaryWriter CilWriter {
			get { return m_cilWriter; }
		}

		public uint DebugHeaderPosition {
			get { return m_debugHeaderStart; }
		}

		public uint ImportTablePosition {
			get { return m_imporTableStart; }
		}

		public uint EntryPointToken {
			get { return m_entryPointToken; }
			set { m_entryPointToken = value; }
		}

		public MetadataWriter (AssemblyDefinition asm, MetadataRoot root,
			AssemblyKind kind, TargetRuntime rt, BinaryWriter writer)
		{
			m_assembly = asm;
			m_root = root;
			m_runtime = rt;
			m_imgWriter = new ImageWriter (this, kind, writer);
			m_binaryWriter = m_imgWriter.GetTextWriter ();

			m_stringCache = new Hashtable ();
			m_stringWriter = new MemoryBinaryWriter (Encoding.UTF8);
			m_stringWriter.Write ((byte) 0);

			m_guidCache = new Hashtable ();
			m_guidWriter = new MemoryBinaryWriter ();

			m_usCache = new Hashtable ();
			m_usWriter = new MemoryBinaryWriter (Encoding.Unicode);
			m_usWriter.Write ((byte) 0);

			m_blobCache = new Hashtable ();
			m_blobWriter = new MemoryBinaryWriter ();
			m_blobWriter.Write ((byte) 0);

			m_tWriter = new MemoryBinaryWriter ();
			m_tableWriter = new MetadataTableWriter (this, m_tWriter);

			m_cilWriter = new MemoryBinaryWriter ();

			m_fieldDataWriter = new MemoryBinaryWriter ();
			m_resWriter = new MemoryBinaryWriter ();
		}

		public MetadataRoot GetMetadataRoot ()
		{
			return m_root;
		}

		public ImageWriter GetImageWriter ()
		{
			return m_imgWriter;
		}

		public MemoryBinaryWriter GetWriter ()
		{
			return m_binaryWriter;
		}

		public MetadataTableWriter GetTableVisitor ()
		{
			return m_tableWriter;
		}

		public void AddData (int length)
		{
			m_cursor += new RVA ((uint) length);
		}

		public RVA GetDataCursor ()
		{
			return m_cursor;
		}

		public uint AddString (string str)
		{
			if (str == null || str.Length == 0)
				return 0;

			if (m_stringCache.Contains (str))
				return (uint) m_stringCache [str];

			uint pointer = (uint) m_stringWriter.BaseStream.Position;
			m_stringCache [str] = pointer;
			m_stringWriter.Write (Encoding.UTF8.GetBytes (str));
			m_stringWriter.Write ('\0');
			return pointer;
		}

		public uint AddBlob (byte [] data)
		{
			if (data == null || data.Length == 0)
				return 0;

			// using CompactFramework compatible version of
			// Convert.ToBase64String
			string key = Convert.ToBase64String (data, 0, data.Length);
			if (m_blobCache.Contains (key))
				return (uint) m_blobCache [key];

			uint pointer = (uint) m_blobWriter.BaseStream.Position;
			m_blobCache [key] = pointer;
			Utilities.WriteCompressedInteger (m_blobWriter, data.Length);
			m_blobWriter.Write (data);
			return pointer;
		}

		public uint AddGuid (Guid g)
		{
			if (m_guidCache.Contains (g))
				return (uint) m_guidCache [g];

			uint pointer = (uint) m_guidWriter.BaseStream.Position;
			m_guidCache [g] = pointer;
			m_guidWriter.Write (g.ToByteArray ());
			return pointer + 1;
		}

		public uint AddUserString (string str)
		{
			if (str == null)
				return 0;

			if (m_usCache.Contains (str))
				return (uint) m_usCache [str];

			uint pointer = (uint) m_usWriter.BaseStream.Position;
			m_usCache [str] = pointer;
			byte [] us = Encoding.Unicode.GetBytes (str);
			Utilities.WriteCompressedInteger (m_usWriter, us.Length + 1);
			m_usWriter.Write (us);
			m_usWriter.Write ((byte) (RequiresSpecialHandling (us) ? 1 : 0));
			return pointer;
		}

		static bool RequiresSpecialHandling (byte [] chars)
		{
			for (int i = 0; i < chars.Length; i++) {
				byte c = chars [i];
				if ((i % 2) == 1)
					if (c != 0)
						return true;

				if (InRange (0x01, 0x08, c) ||
					InRange (0x0e, 0x1f, c) ||
					c == 0x27 ||
					c == 0x2d ||
					c == 0x7f) {

					return true;
				}
			}

			return false;
		}

		static bool InRange (int left, int right, int value)
		{
			return left <= value && value <= right;
		}

		void CreateStream (string name)
		{
			MetadataStream stream = new MetadataStream ();
			stream.Header.Name = name;
			stream.Heap = MetadataHeap.HeapFactory (stream);
			m_root.Streams.Add (stream);
		}

		void SetHeapSize (MetadataHeap heap, MemoryBinaryWriter data, byte flag)
		{
			if (data.BaseStream.Length > 65536) {
				m_root.Streams.TablesHeap.HeapSizes |= flag;
				heap.IndexSize = 4;
			} else
				heap.IndexSize = 2;
		}

		public uint AddResource (byte [] data)
		{
			uint offset = (uint) m_resWriter.BaseStream.Position;
			m_resWriter.Write (data.Length);
			m_resWriter.Write (data);
			m_resWriter.QuadAlign ();
			return offset;
		}

		public void AddFieldInitData (byte [] data)
		{
			m_fieldDataWriter.Write (data);
			m_fieldDataWriter.QuadAlign ();
		}

		uint GetStrongNameSignatureSize ()
		{
			if (m_assembly.Name.PublicKey != null) {
				// in fx 2.0 the key may be from 384 to 16384 bits
				// so we must calculate the signature size based on
				// the size of the public key (minus the 32 byte header)
				int size = m_assembly.Name.PublicKey.Length;
				if (size > 32)
					return (uint) (size - 32);
				// note: size == 16 for the ECMA "key" which is replaced
				// by the runtime with a 1024 bits key (128 bytes)
			}
			return 128; // default strongname signature size
		}

		public override void VisitMetadataRoot (MetadataRoot root)
		{
			WriteMemStream (m_cilWriter);
			WriteMemStream (m_fieldDataWriter);
			m_resStart = (uint) m_binaryWriter.BaseStream.Position;
			WriteMemStream (m_resWriter);
			m_resSize = (uint) (m_binaryWriter.BaseStream.Position - m_resStart);

			// for now, we only reserve the place for the strong name signature
			if ((m_assembly.Name.Flags & AssemblyFlags.PublicKey) > 0) {
				m_snsStart = (uint) m_binaryWriter.BaseStream.Position;
				m_snsSize = GetStrongNameSignatureSize ();
				m_binaryWriter.Write (new byte [m_snsSize]);
				m_binaryWriter.QuadAlign ();
			}

			// save place for debug header
			if (m_imgWriter.GetImage ().DebugHeader != null) {
				m_debugHeaderStart = (uint) m_binaryWriter.BaseStream.Position;
				m_binaryWriter.Write (new byte [m_imgWriter.GetImage ().DebugHeader.GetSize ()]);
				m_binaryWriter.QuadAlign ();
			}

			m_mdStart = (uint) m_binaryWriter.BaseStream.Position;

			if (m_stringWriter.BaseStream.Length > 1) {
				CreateStream (MetadataStream.Strings);
				SetHeapSize (root.Streams.StringsHeap, m_stringWriter, 0x01);
				m_stringWriter.QuadAlign ();
			}

			if (m_guidWriter.BaseStream.Length > 0) {
				CreateStream (MetadataStream.GUID);
				SetHeapSize (root.Streams.GuidHeap, m_guidWriter, 0x02);
			}

			if (m_blobWriter.BaseStream.Length > 1) {
				CreateStream (MetadataStream.Blob);
				SetHeapSize (root.Streams.BlobHeap, m_blobWriter, 0x04);
				m_blobWriter.QuadAlign ();
			}

			if (m_usWriter.BaseStream.Length > 2) {
				CreateStream (MetadataStream.UserStrings);
				m_usWriter.QuadAlign ();
			}

			m_root.Header.MajorVersion = 1;
			m_root.Header.MinorVersion = 1;

			switch (m_runtime) {
			case TargetRuntime.NET_1_0 :
				m_root.Header.Version = "v1.0.3705";
				break;
			case TargetRuntime.NET_1_1 :
				m_root.Header.Version = "v1.1.4322";
				break;
			case TargetRuntime.NET_2_0 :
				m_root.Header.Version = "v2.0.50727";
				break;
			}

			m_root.Streams.TablesHeap.Tables.Accept (m_tableWriter);

			if (m_tWriter.BaseStream.Length == 0)
				m_root.Streams.Remove (m_root.Streams.TablesHeap.GetStream ());
		}

		public override void VisitMetadataRootHeader (MetadataRoot.MetadataRootHeader header)
		{
			m_binaryWriter.Write (header.Signature);
			m_binaryWriter.Write (header.MajorVersion);
			m_binaryWriter.Write (header.MinorVersion);
			m_binaryWriter.Write (header.Reserved);
			m_binaryWriter.Write (header.Version.Length + 3 & (~3));
			m_binaryWriter.Write (Encoding.ASCII.GetBytes (header.Version));
			m_binaryWriter.QuadAlign ();
			m_binaryWriter.Write (header.Flags);
			m_binaryWriter.Write ((ushort) m_root.Streams.Count);
		}

		public override void VisitMetadataStreamCollection (MetadataStreamCollection streams)
		{
			foreach (MetadataStream stream in streams) {
				MetadataStream.MetadataStreamHeader header = stream.Header;

				header.Offset = (uint) (m_binaryWriter.BaseStream.Position);
				m_binaryWriter.Write (header.Offset);
				MemoryBinaryWriter container;
				string name = header.Name;
				uint size = 0;
				switch (header.Name) {
				case MetadataStream.Tables :
					container = m_tWriter;
					size += 24; // header
					break;
				case MetadataStream.Strings :
					name += "\0\0\0\0";
					container = m_stringWriter;
					break;
				case MetadataStream.GUID :
					container = m_guidWriter;
					break;
				case MetadataStream.Blob :
					container = m_blobWriter;
					break;
				case MetadataStream.UserStrings :
					container = m_usWriter;
					break;
				default :
					throw new MetadataFormatException ("Unknown stream kind");
				}

				size += (uint) (container.BaseStream.Length + 3 & (~3));
				m_binaryWriter.Write (size);
				m_binaryWriter.Write (Encoding.ASCII.GetBytes (name));
				m_binaryWriter.QuadAlign ();
			}
		}

		void WriteMemStream (MemoryBinaryWriter writer)
		{
			m_binaryWriter.Write (writer);
			m_binaryWriter.QuadAlign ();
		}

		void PatchStreamHeaderOffset (MetadataHeap heap)
		{
			long pos = m_binaryWriter.BaseStream.Position;
			m_binaryWriter.BaseStream.Position = heap.GetStream ().Header.Offset;
			m_binaryWriter.Write ((uint) (pos - m_mdStart));
			m_binaryWriter.BaseStream.Position = pos;
		}

		public override void VisitGuidHeap (GuidHeap heap)
		{
			PatchStreamHeaderOffset (heap);
			WriteMemStream (m_guidWriter);
		}

		public override void VisitStringsHeap (StringsHeap heap)
		{
			PatchStreamHeaderOffset (heap);
			WriteMemStream (m_stringWriter);
		}

		public override void VisitTablesHeap (TablesHeap heap)
		{
			PatchStreamHeaderOffset (heap);
			m_binaryWriter.Write (heap.Reserved);
			switch (m_runtime) {
			case TargetRuntime.NET_1_0 :
			case TargetRuntime.NET_1_1 :
				heap.MajorVersion = 1;
				heap.MinorVersion = 0;
				break;
			case TargetRuntime.NET_2_0 :
				heap.MajorVersion = 2;
				heap.MinorVersion = 0;
				break;
			}
			m_binaryWriter.Write (heap.MajorVersion);
			m_binaryWriter.Write (heap.MinorVersion);
			m_binaryWriter.Write (heap.HeapSizes);
			m_binaryWriter.Write (heap.Reserved2);
			m_binaryWriter.Write (heap.Valid);
			m_binaryWriter.Write (heap.Sorted);
			WriteMemStream (m_tWriter);
		}

		public override void VisitBlobHeap (BlobHeap heap)
		{
			PatchStreamHeaderOffset (heap);
			WriteMemStream (m_blobWriter);
		}

		public override void VisitUserStringsHeap (UserStringsHeap heap)
		{
			PatchStreamHeaderOffset (heap);
			WriteMemStream (m_usWriter);
		}

		void PatchHeader ()
		{
			Image img = m_imgWriter.GetImage ();

			img.CLIHeader.EntryPointToken = m_entryPointToken;

			if (m_mdSize > 0)
				img.CLIHeader.Metadata = new DataDirectory (
					img.TextSection.VirtualAddress + m_mdStart, m_imporTableStart - m_mdStart);

			if (m_resSize > 0)
				img.CLIHeader.Resources = new DataDirectory (
					img.TextSection.VirtualAddress + m_resStart, m_resSize);

			if (m_snsStart > 0)
				img.CLIHeader.StrongNameSignature = new DataDirectory (
					img.TextSection.VirtualAddress + m_snsStart, m_snsSize);

			if (m_debugHeaderStart > 0)
				img.PEOptionalHeader.DataDirectories.Debug = new DataDirectory (
					img.TextSection.VirtualAddress + m_debugHeaderStart, 0x1c);
		}

		public override void TerminateMetadataRoot (MetadataRoot root)
		{
			m_mdSize = (uint) (m_binaryWriter.BaseStream.Position - m_mdStart);
			m_imporTableStart = (uint) m_binaryWriter.BaseStream.Position;
			m_binaryWriter.Write (new byte [0x60]); // imports
			m_imgWriter.Initialize ();
			PatchHeader ();
			root.GetImage ().Accept (m_imgWriter);
		}
	}
}
