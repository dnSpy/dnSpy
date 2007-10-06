//
// CopyImageVisitor.cs
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

	class CopyImageVisitor : BaseImageVisitor {

		Image m_newImage;
		Image m_originalImage;

		public CopyImageVisitor (Image originalImage)
		{
			m_originalImage = originalImage;
		}

		public override void VisitImage (Image img)
		{
			m_newImage = img;
			if (m_originalImage.DebugHeader != null)
				m_newImage.AddDebugHeader ();
		}

		public override void VisitDebugHeader (DebugHeader dbgHeader)
		{
			DebugHeader old = m_originalImage.DebugHeader;
			dbgHeader.Age = old.Age;
			dbgHeader.Characteristics = old.Characteristics;
			dbgHeader.FileName = old.FileName;
			dbgHeader.Signature = old.Signature;
			dbgHeader.TimeDateStamp = ImageInitializer.TimeDateStampFromEpoch();
			dbgHeader.Type = old.Type;
		}

		public override void VisitSectionCollection (SectionCollection sections)
		{
			Section old = null;
			foreach (Section s in m_originalImage.Sections)
				if (s.Name == Section.Resources)
					old = s;

			if (old == null)
				return;

			Section rsrc = new Section ();
			rsrc.Characteristics = old.Characteristics;
			rsrc.Name = old.Name;

			sections.Add (rsrc);
		}

		public override void TerminateImage (Image img)
		{
			if (m_originalImage.ResourceDirectoryRoot == null)
				return;

			m_newImage.ResourceDirectoryRoot = CloneResourceDirectoryTable (m_originalImage.ResourceDirectoryRoot);
		}

		ResourceDirectoryTable CloneResourceDirectoryTable (ResourceDirectoryTable old)
		{
			ResourceDirectoryTable rdt = new ResourceDirectoryTable ();
			foreach (ResourceDirectoryEntry oldEntry in old.Entries)
				rdt.Entries.Add (CloneResourceDirectoryEntry (oldEntry));

			return rdt;
		}

		ResourceDirectoryEntry CloneResourceDirectoryEntry (ResourceDirectoryEntry old)
		{
			ResourceDirectoryEntry rde;
			if (old.IdentifiedByName)
				rde = new ResourceDirectoryEntry(old.Name);
			else
				rde = new ResourceDirectoryEntry (old.ID);

			if (old.Child is ResourceDirectoryTable)
				rde.Child = CloneResourceDirectoryTable (old.Child as ResourceDirectoryTable);
			else
				rde.Child = CloneResourceDataEntry (old.Child as ResourceDataEntry);

			return rde;
		}

		ResourceDataEntry CloneResourceDataEntry (ResourceDataEntry old)
		{
			ResourceDataEntry rde = new ResourceDataEntry ();
			rde.Size = old.Size;
			rde.Codepage = old.Codepage;
			rde.ResourceData = old.ResourceData;

			return rde;
		}
	}
}
