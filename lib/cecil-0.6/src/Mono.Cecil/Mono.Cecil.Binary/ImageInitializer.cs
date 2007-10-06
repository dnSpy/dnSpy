//
// ImageInitializer.cs
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

namespace Mono.Cecil.Binary {

	using System;

	using Mono.Cecil.Metadata;

	class ImageInitializer : BaseImageVisitor {

		Image m_image;
		MetadataInitializer m_mdinit;

		public Image Image {
			get { return m_image; }
		}

		public MetadataInitializer Metadata {
			get { return m_mdinit; }
		}

		public ImageInitializer (Image image)
		{
			m_image = image;
			m_image.CLIHeader = new CLIHeader ();
			m_mdinit = new MetadataInitializer (this);
		}

		public override void VisitDOSHeader (DOSHeader header)
		{
			header.SetDefaultValues ();
		}

		public override void VisitPEOptionalHeader (PEOptionalHeader header)
		{
			header.SetDefaultValues ();
		}

		public override void VisitPEFileHeader (PEFileHeader header)
		{
			header.SetDefaultValues ();
			header.TimeDateStamp = TimeDateStampFromEpoch ();
		}

		public override void VisitNTSpecificFieldsHeader (PEOptionalHeader.NTSpecificFieldsHeader header)
		{
			header.SetDefaultValues ();
		}

		public override void VisitStandardFieldsHeader (PEOptionalHeader.StandardFieldsHeader header)
		{
			header.SetDefaultValues ();
		}

		public override void VisitDataDirectoriesHeader (PEOptionalHeader.DataDirectoriesHeader header)
		{
			header.SetDefaultValues ();
		}

		public override void VisitSectionCollection (SectionCollection coll)
		{
			Section text = new Section ();
			text.Name = Section.Text;
			text.Characteristics = SectionCharacteristics.ContainsCode |
				SectionCharacteristics.MemoryRead | SectionCharacteristics.MemExecute;
			m_image.TextSection = text;

			Section reloc = new Section ();
			reloc.Name =  Section.Relocs;
			reloc.Characteristics = SectionCharacteristics.ContainsInitializedData |
				SectionCharacteristics.MemDiscardable | SectionCharacteristics.MemoryRead;

			coll.Add (text);
			coll.Add (reloc);
		}

		public override void VisitSection (Section sect)
		{
			sect.SetDefaultValues ();
		}

		public override void VisitDebugHeader (DebugHeader dh)
		{
			if (dh != null)
				dh.SetDefaultValues ();
		}

		public override void VisitCLIHeader (CLIHeader header)
		{
			header.SetDefaultValues ();
			m_image.MetadataRoot.Accept (m_mdinit);
		}

		public override void VisitImportTable (ImportTable it)
		{
			it.ImportAddressTable = new RVA (0x2000);
		}

		public override void VisitHintNameTable (HintNameTable hnt)
		{
			hnt.Hint = 0;
			hnt.RuntimeLibrary = HintNameTable.RuntimeCorEE;
			hnt.EntryPoint = 0x25ff;
			hnt.RVA = new RVA (0x402000);
		}

		public static uint TimeDateStampFromEpoch ()
		{
			return (uint) DateTime.UtcNow.Subtract (
				new DateTime (1970, 1, 1)).TotalSeconds;
		}
	}
}
