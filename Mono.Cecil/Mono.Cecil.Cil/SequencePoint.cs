//
// SequencePoint.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2011 Jb Evain
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

	public sealed class SequencePoint {

		Document document;

		int start_line;
		int start_column;
		int end_line;
		int end_column;

		public int StartLine {
			get { return start_line; }
			set { start_line = value; }
		}

		public int StartColumn {
			get { return start_column; }
			set { start_column = value; }
		}

		public int EndLine {
			get { return end_line; }
			set { end_line = value; }
		}

		public int EndColumn {
			get { return end_column; }
			set { end_column = value; }
		}

		public Document Document {
			get { return document; }
			set { document = value; }
		}

		public SequencePoint (Document document)
		{
			this.document = document;
		}
	}
}
