//
// Image.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// (C) 2005 - 2007 Jb Evain
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

	using System;
	using System.IO;

	using Mono.Cecil.Metadata;

	public sealed class Image : IBinaryVisitable {

		DOSHeader m_dosHeader;
		PEFileHeader m_peFileHeader;
		PEOptionalHeader m_peOptionalHeader;

		SectionCollection m_sections;
		Section m_textSection;

		ImportAddressTable m_importAddressTable;
		CLIHeader m_cliHeader;
		ImportTable m_importTable;
		ImportLookupTable m_importLookupTable;
		HintNameTable m_hintNameTable;
		ExportTable m_exportTable;

		DebugHeader m_debugHeader;
		MetadataRoot m_mdRoot;

		ResourceDirectoryTable m_rsrcRoot;

		FileInfo m_img;

		public DOSHeader DOSHeader {
			get { return m_dosHeader; }
		}

		public PEFileHeader PEFileHeader {
			get { return m_peFileHeader; }
		}

		public PEOptionalHeader PEOptionalHeader {
			get { return m_peOptionalHeader; }
		}

		public SectionCollection Sections {
			get { return m_sections; }
		}

		public Section TextSection {
			get { return m_textSection; }
			set { m_textSection = value; }
		}

		public ImportAddressTable ImportAddressTable {
			get { return m_importAddressTable; }
		}

		public CLIHeader CLIHeader {
			get { return m_cliHeader; }
			set { m_cliHeader = value; }
		}

		public DebugHeader DebugHeader {
			get { return m_debugHeader; }
			set { m_debugHeader = value; }
		}

		public MetadataRoot MetadataRoot {
			get { return m_mdRoot; }
		}

		public ImportTable ImportTable {
			get { return m_importTable; }
		}

		public ImportLookupTable ImportLookupTable {
			get { return m_importLookupTable; }
		}

		public HintNameTable HintNameTable {
			get { return m_hintNameTable; }
		}

		public ExportTable ExportTable {
			get { return m_exportTable; }
			set { m_exportTable = value; }
		}

		internal ResourceDirectoryTable ResourceDirectoryRoot {
			get { return m_rsrcRoot; }
			set { m_rsrcRoot = value; }
		}

		public FileInfo FileInformation {
			get { return m_img; }
		}

		internal Image ()
		{
			m_dosHeader = new DOSHeader ();
			m_peFileHeader = new PEFileHeader ();
			m_peOptionalHeader = new PEOptionalHeader ();
			m_sections = new SectionCollection ();
			m_importAddressTable = new ImportAddressTable ();
			m_importTable = new ImportTable ();
			m_importLookupTable = new ImportLookupTable ();
			m_hintNameTable = new HintNameTable ();
			m_mdRoot = new MetadataRoot (this);
		}

		internal Image (FileInfo img) : this ()
		{
			m_img = img;
		}

		public long ResolveVirtualAddress (RVA rva)
		{
			foreach (Section sect in this.Sections) {
				if (rva >= sect.VirtualAddress &&
					rva < sect.VirtualAddress + sect.SizeOfRawData)

					return rva + sect.PointerToRawData - sect.VirtualAddress;
			}

			throw new ArgumentOutOfRangeException ("Cannot map the rva to any section");
		}

		public BinaryReader GetReaderAtVirtualAddress (RVA rva)
		{
			foreach (Section sect in this.Sections) {
				if (rva >= sect.VirtualAddress &&
					rva < sect.VirtualAddress + sect.SizeOfRawData) {

					BinaryReader br = new BinaryReader (new MemoryStream (sect.Data));
					br.BaseStream.Position = rva - sect.VirtualAddress;
					return br;
				}
			}

			return null;
		}

		public void AddDebugHeader ()
		{
			m_debugHeader = new DebugHeader ();
			m_debugHeader.SetDefaultValues ();
		}

		internal void SetFileInfo (FileInfo file)
		{
			m_img = file;
		}

		public void Accept (IBinaryVisitor visitor)
		{
			visitor.VisitImage (this);

			m_dosHeader.Accept (visitor);
			m_peFileHeader.Accept (visitor);
			m_peOptionalHeader.Accept (visitor);

			m_sections.Accept (visitor);

			m_importAddressTable.Accept (visitor);

			AcceptIfNotNull (m_cliHeader, visitor);
			AcceptIfNotNull (m_debugHeader, visitor);

			m_importTable.Accept (visitor);
			m_importLookupTable.Accept (visitor);
			m_hintNameTable.Accept (visitor);
			AcceptIfNotNull (m_exportTable, visitor);

			visitor.TerminateImage (this);
		}

		static void AcceptIfNotNull (IBinaryVisitable visitable, IBinaryVisitor visitor)
		{
			if (visitable == null)
				return;

			visitable.Accept (visitor);
		}

		public static Image CreateImage ()
		{
			Image img = new Image ();

			ImageInitializer init = new ImageInitializer (img);
			img.Accept (init);

			return img;
		}

		public static Image GetImage (string file)
		{
			return ImageReader.Read (file).Image;
		}

		public static Image GetImage (byte [] image)
		{
			return ImageReader.Read (image).Image;
		}

		public static Image GetImage (Stream stream)
		{
			return ImageReader.Read (stream).Image;
		}
	}
}
