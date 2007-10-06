//
// SequencePoint.cs
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

namespace Mono.Cecil.Cil {

	public class SequencePoint {

		Document m_document;

		int m_startLine;
		int m_startColumn;
		int m_endLine;
		int m_endColumn;

		public int StartLine {
			get { return m_startLine; }
			set { m_startLine = value; }
		}

		public int StartColumn {
			get { return m_startColumn; }
			set { m_startColumn = value; }
		}

		public int EndLine {
			get { return m_endLine; }
			set { m_endLine = value; }
		}

		public int EndColumn {
			get { return m_endColumn; }
			set { m_endColumn = value; }
		}

		public Document Document {
			get { return m_document; }
			set { m_document = value; }
		}

		public SequencePoint (Document document)
		{
			m_document = document;
		}

		public SequencePoint (Document doc, int startLine, int startCol, int endLine, int endCol) : this (doc)
		{
			m_startLine = startLine;
			m_startColumn = startCol;
			m_endLine = endLine;
			m_endColumn = endCol;
		}
	}
}
