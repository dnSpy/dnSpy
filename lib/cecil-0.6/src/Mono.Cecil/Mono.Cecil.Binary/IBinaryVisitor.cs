//
// IBinaryVisitor.cs
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

	public interface IBinaryVisitor {
		void VisitImage (Image img);
		void VisitDOSHeader (DOSHeader header);
		void VisitPEFileHeader (PEFileHeader header);
		void VisitPEOptionalHeader (PEOptionalHeader header);
		void VisitStandardFieldsHeader (PEOptionalHeader.StandardFieldsHeader header);
		void VisitNTSpecificFieldsHeader (PEOptionalHeader.NTSpecificFieldsHeader header);
		void VisitDataDirectoriesHeader (PEOptionalHeader.DataDirectoriesHeader header);
		void VisitSectionCollection (SectionCollection coll);
		void VisitSection (Section section);
		void VisitImportAddressTable (ImportAddressTable iat);
		void VisitDebugHeader (DebugHeader dh);
		void VisitCLIHeader (CLIHeader header);
		void VisitImportTable (ImportTable it);
		void VisitImportLookupTable (ImportLookupTable ilt);
		void VisitHintNameTable (HintNameTable hnt);
		void VisitExportTable (ExportTable et);

		void TerminateImage (Image img);
	}
}
