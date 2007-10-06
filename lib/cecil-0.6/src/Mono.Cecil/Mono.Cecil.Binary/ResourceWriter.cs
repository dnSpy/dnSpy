//
// ResourceWriter.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// (C) 2006 Jb Evain
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

using System.Text;

namespace Mono.Cecil.Binary {

	using System.Collections;

	class ResourceWriter {

		Image m_img;
		Section m_rsrc;
		MemoryBinaryWriter m_writer;

		ArrayList m_dataEntries;
		ArrayList m_stringEntries;

		long m_pos;

		public ResourceWriter (Image img, Section rsrc, MemoryBinaryWriter writer)
		{
			m_img = img;
			m_rsrc = rsrc;
			m_writer = writer;

			m_dataEntries = new ArrayList ();
			m_stringEntries = new ArrayList ();
		}

		public void Write ()
		{
			if (m_img.ResourceDirectoryRoot == null)
				return;

			ComputeOffset (m_img.ResourceDirectoryRoot);
			WriteResourceDirectoryTable (m_img.ResourceDirectoryRoot);
		}

		public void Patch ()
		{
			foreach (ResourceDataEntry rde in m_dataEntries) {
				GotoOffset (rde.Offset);
				m_writer.Write ((uint) rde.Data + m_rsrc.VirtualAddress);
				RestoreOffset ();
			}
		}

		void ComputeOffset (ResourceDirectoryTable root)
		{
			int offset = 0;

			Queue directoryTables = new Queue ();
			directoryTables.Enqueue (root);

			while (directoryTables.Count > 0) {
				ResourceDirectoryTable rdt = directoryTables.Dequeue () as ResourceDirectoryTable;
				rdt.Offset = offset;
				offset += 16;

				foreach (ResourceDirectoryEntry rde in rdt.Entries) {
					rde.Offset = offset;
					offset += 8;
					if (rde.IdentifiedByName)
						m_stringEntries.Add (rde.Name);

					if (rde.Child is ResourceDirectoryTable)
						directoryTables.Enqueue (rde.Child);
					else
						m_dataEntries.Add (rde.Child);
				}
			}

			foreach (ResourceDataEntry rde in m_dataEntries) {
				rde.Offset = offset;
				offset += 16;
			}

			foreach (ResourceDirectoryString rds in m_stringEntries) {
				rds.Offset = offset;
				byte [] str = Encoding.Unicode.GetBytes (rds.String);
				offset += 2 + str.Length;

				offset += 3;
				offset &= ~3;
			}

			foreach (ResourceDataEntry rde in m_dataEntries) {
				rde.Data = (uint) offset;

				offset += rde.ResourceData.Length;
				offset += 3;
				offset &= ~3;
			}

			m_writer.Write (new byte [offset]);
		}

		void WriteResourceDirectoryTable (ResourceDirectoryTable rdt)
		{
			GotoOffset (rdt.Offset);

			m_writer.Write (rdt.Characteristics);
			m_writer.Write (rdt.TimeDateStamp);
			m_writer.Write (rdt.MajorVersion);
			m_writer.Write (rdt.MinorVersion);

			ResourceDirectoryEntry [] namedEntries = GetEntries (rdt, true);
			ResourceDirectoryEntry [] idEntries = GetEntries (rdt, false);

			m_writer.Write ((ushort) namedEntries.Length);
			m_writer.Write ((ushort) idEntries.Length);

			foreach (ResourceDirectoryEntry rde in namedEntries)
				WriteResourceDirectoryEntry (rde);

			foreach (ResourceDirectoryEntry rde in idEntries)
				WriteResourceDirectoryEntry (rde);

			RestoreOffset ();
		}

		ResourceDirectoryEntry [] GetEntries (ResourceDirectoryTable rdt, bool identifiedByName)
		{
			ArrayList entries = new ArrayList ();
			foreach (ResourceDirectoryEntry rde in rdt.Entries)
				if (rde.IdentifiedByName == identifiedByName)
					entries.Add (rde);

			return entries.ToArray (typeof (ResourceDirectoryEntry)) as ResourceDirectoryEntry [];
		}

		void WriteResourceDirectoryEntry (ResourceDirectoryEntry rde)
		{
			GotoOffset (rde.Offset);

			if (rde.IdentifiedByName) {
				m_writer.Write ((uint) rde.Name.Offset | 0x80000000);
				WriteResourceDirectoryString (rde.Name);
			} else
				m_writer.Write ((uint) rde.ID);

			if (rde.Child is ResourceDirectoryTable) {
				m_writer.Write((uint) rde.Child.Offset | 0x80000000);
				WriteResourceDirectoryTable (rde.Child as ResourceDirectoryTable);
			} else {
				m_writer.Write (rde.Child.Offset);
				WriteResourceDataEntry (rde.Child as ResourceDataEntry);
			}

			RestoreOffset ();
		}

		void WriteResourceDataEntry (ResourceDataEntry rde)
		{
			GotoOffset (rde.Offset);

			m_writer.Write (0);
			m_writer.Write ((uint) rde.ResourceData.Length);
			m_writer.Write (rde.Codepage);
			m_writer.Write (rde.Reserved);

			m_writer.BaseStream.Position = rde.Data;
			m_writer.Write (rde.ResourceData);

			RestoreOffset ();
		}

		void WriteResourceDirectoryString (ResourceDirectoryString name)
		{
			GotoOffset (name.Offset);

			byte [] str = Encoding.Unicode.GetBytes (name.String);
			m_writer.Write ((ushort) str.Length);
			m_writer.Write (str);

			RestoreOffset ();
		}

		void GotoOffset (int offset)
		{
			m_pos = m_writer.BaseStream.Position;
			m_writer.BaseStream.Position = offset;
		}

		void RestoreOffset ()
		{
			m_writer.BaseStream.Position = m_pos;
		}
	}
}
