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
using System.Collections.Generic;
#if NREFACTORY
using ICSharpCode.NRefactory.Editor;
#else
using ICSharpCode.AvalonEdit.Document;
#endif

namespace ICSharpCode.AvalonEdit.Editing
{
	/// <summary>
	/// Determines whether the document can be modified.
	/// </summary>
	public interface IReadOnlySectionProvider
	{
		/// <summary>
		/// Gets whether insertion is possible at the specified offset.
		/// </summary>
		bool CanInsert(int offset);
		
		/// <summary>
		/// Gets the deletable segments inside the given segment.
		/// </summary>
		/// <remarks>
		/// All segments in the result must be within the given segment, and they must be returned in order
		/// (e.g. if two segments are returned, EndOffset of first segment must be less than StartOffset of second segment).
		/// 
		/// For replacements, the last segment being returned will be replaced with the new text. If an empty list is returned,
		/// no replacement will be done.
		/// </remarks>
		IEnumerable<ISegment> GetDeletableSegments(ISegment segment);
	}
}
