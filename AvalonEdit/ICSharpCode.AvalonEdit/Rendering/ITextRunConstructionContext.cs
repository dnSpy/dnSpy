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
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Utils;

namespace ICSharpCode.AvalonEdit.Rendering
{
	/// <summary>
	/// Contains information relevant for text run creation.
	/// </summary>
	public interface ITextRunConstructionContext
	{
		/// <summary>
		/// Gets the text document.
		/// </summary>
		TextDocument Document { get; }
		
		/// <summary>
		/// Gets the text view for which the construction runs.
		/// </summary>
		TextView TextView { get; }
		
		/// <summary>
		/// Gets the visual line that is currently being constructed.
		/// </summary>
		VisualLine VisualLine { get; }
		
		/// <summary>
		/// Gets the global text run properties.
		/// </summary>
		TextRunProperties GlobalTextRunProperties { get; }
		
		/// <summary>
		/// Gets a piece of text from the document.
		/// </summary>
		/// <remarks>
		/// This method is allowed to return a larger string than requested.
		/// It does this by returning a <see cref="StringSegment"/> that describes the requested segment within the returned string.
		/// This method should be the preferred text access method in the text transformation pipeline, as it can avoid repeatedly allocating string instances
		/// for text within the same line.
		/// </remarks>
		StringSegment GetText(int offset, int length);
	}
}
