// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
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
#if NREFACTORY
using ICSharpCode.NRefactory.Editor;
#else
using ICSharpCode.AvalonEdit.Document;
#endif

namespace ICSharpCode.AvalonEdit.Editing
{
	/// <summary>
	/// Represents a selected segment.
	/// </summary>
	public class SelectionSegment : ISegment
	{
		readonly int startOffset, endOffset;
		readonly int startVC, endVC;
		
		/// <summary>
		/// Creates a SelectionSegment from two offsets.
		/// </summary>
		public SelectionSegment(int startOffset, int endOffset)
		{
			this.startOffset = Math.Min(startOffset, endOffset);
			this.endOffset = Math.Max(startOffset, endOffset);
			this.startVC = this.endVC = -1;
		}
		
		/// <summary>
		/// Creates a SelectionSegment from two offsets and visual columns.
		/// </summary>
		public SelectionSegment(int startOffset, int startVC, int endOffset, int endVC)
		{
			if (startOffset < endOffset || (startOffset == endOffset && startVC <= endVC)) {
				this.startOffset = startOffset;
				this.startVC = startVC;
				this.endOffset = endOffset;
				this.endVC = endVC;
			} else {
				this.startOffset = endOffset;
				this.startVC = endVC;
				this.endOffset = startOffset;
				this.endVC = startVC;
			}
		}
		
		/// <summary>
		/// Gets the start offset.
		/// </summary>
		public int StartOffset {
			get { return startOffset; }
		}
		
		/// <summary>
		/// Gets the end offset.
		/// </summary>
		public int EndOffset {
			get { return endOffset; }
		}
		
		/// <summary>
		/// Gets the start visual column.
		/// </summary>
		public int StartVisualColumn {
			get { return startVC; }
		}
		
		/// <summary>
		/// Gets the end visual column.
		/// </summary>
		public int EndVisualColumn {
			get { return endVC; }
		}
		
		/// <inheritdoc/>
		int ISegment.Offset {
			get { return startOffset; }
		}
		
		/// <inheritdoc/>
		public int Length {
			get { return endOffset - startOffset; }
		}
		
		/// <inheritdoc/>
		public override string ToString()
		{
			return string.Format("[SelectionSegment StartOffset={0}, EndOffset={1}, StartVC={2}, EndVC={3}]", startOffset, endOffset, startVC, endVC);
		}
	}
}
