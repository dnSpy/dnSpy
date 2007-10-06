//
// ResourceReader.cs
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

namespace Mono.Cecil.Binary {

	using System.IO;
	using System.Text;

	class ResourceReader {

		Image m_img;
		Section m_rsrc;
		BinaryReader m_reader;

		public ResourceReader (Image img)
		{
			m_img = img;
		}

		public ResourceDirectoryTable Read ()
		{
			m_rsrc = GetResourceSection ();
			if (m_rsrc == null)
				return null;

			m_reader = new BinaryReader (new MemoryStream (m_rsrc.Data));
			return ReadDirectoryTable ();
		}

		Section GetResourceSection ()
		{
			foreach (Section s in m_img.Sections)
				if (s.Name == Section.Resources)
					return s;

			return null;
		}

		int GetOffset ()
		{
			return (int) m_reader.BaseStream.Position;
		}

		ResourceDirectoryTable ReadDirectoryTable ()
		{
			ResourceDirectoryTable rdt = new ResourceDirectoryTable (GetOffset ());
			rdt.Characteristics = m_reader.ReadUInt32 ();
			rdt.TimeDateStamp = m_reader.ReadUInt32 ();
			rdt.MajorVersion = m_reader.ReadUInt16 ();
			rdt.MinorVersion = m_reader.ReadUInt16 ();
			ushort nameEntries = m_reader.ReadUInt16 ();
			ushort idEntries = m_reader.ReadUInt16 ();

			for (int i = 0; i < nameEntries; i++)
				rdt.Entries.Add (ReadDirectoryEntry ());

			for (int i = 0; i < idEntries; i++)
				rdt.Entries.Add (ReadDirectoryEntry ());

			return rdt;
		}

		ResourceDirectoryEntry ReadDirectoryEntry ()
		{
			uint name = m_reader.ReadUInt32 ();
			uint child = m_reader.ReadUInt32 ();

			ResourceDirectoryEntry rde;
			if ((name & 0x80000000) != 0)
				rde = new ResourceDirectoryEntry (ReadDirectoryString ((int) name & 0x7fffffff), GetOffset ());
			else
				rde = new ResourceDirectoryEntry ((int) name & 0x7fffffff, GetOffset ());

			long pos = m_reader.BaseStream.Position;
			m_reader.BaseStream.Position = child & 0x7fffffff;

			if ((child & 0x80000000) != 0)
				rde.Child = ReadDirectoryTable ();
			else
				rde.Child = ReadDataEntry ();

			m_reader.BaseStream.Position = pos;

			return rde;
		}

		ResourceDirectoryString ReadDirectoryString (int offset)
		{
			long pos = m_reader.BaseStream.Position;
			m_reader.BaseStream.Position = offset;

			byte [] str = m_reader.ReadBytes (m_reader.ReadUInt16 ());

			ResourceDirectoryString rds = new ResourceDirectoryString (
				Encoding.Unicode.GetString (str, 0, str.Length),
				GetOffset ());

			m_reader.BaseStream.Position = pos;

			return rds;
		}

		ResourceNode ReadDataEntry ()
		{
			ResourceDataEntry rde = new ResourceDataEntry (GetOffset ());
			rde.Data = m_reader.ReadUInt32 ();
			rde.Size = m_reader.ReadUInt32 ();
			rde.Codepage = m_reader.ReadUInt32 ();
			rde.Reserved = m_reader.ReadUInt32 ();

			BinaryReader dataReader = m_img.GetReaderAtVirtualAddress (rde.Data);
			rde.ResourceData = dataReader.ReadBytes ((int) rde.Size);
			dataReader.Close ();

			return rde;
		}
	}
}
