// Copyright (c) 2009-2013 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using ICSharpCode.NRefactory.Editor;

namespace ICSharpCode.NRefactory.Xml
{
	/// <summary>
	/// A syntax error.
	/// </summary>
	public class SyntaxError : ISegment
	{
		readonly int startOffset;
		readonly int endOffset;
		readonly string description;
		
		/// <summary>
		/// Creates a new syntax error.
		/// </summary>
		public SyntaxError(int startOffset, int endOffset, string description)
		{
			if (description == null)
				throw new ArgumentNullException("description");
			this.startOffset = startOffset;
			this.endOffset = endOffset;
			this.description = description;
		}
		
		/// <summary>
		/// Gets a description of the syntax error.
		/// </summary>
		public string Description {
			get { return description; }
		}
		
		/// <summary>
		/// Gets the start offset of the segment.
		/// </summary>
		public int StartOffset {
			get { return startOffset; }
		}
		
		int ISegment.Offset {
			get { return startOffset; }
		}
		
		/// <inheritdoc/>
		public int Length {
			get { return endOffset - startOffset; }
		}
		
		/// <inheritdoc/>
		public int EndOffset {
			get { return endOffset; }
		}
	}
}
