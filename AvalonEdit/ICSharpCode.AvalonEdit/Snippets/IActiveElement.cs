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
using ICSharpCode.NRefactory.Editor;
using ICSharpCode.AvalonEdit.Document;

namespace ICSharpCode.AvalonEdit.Snippets
{
	/// <summary>
	/// Represents an active element that allows the snippet to stay interactive after insertion.
	/// </summary>
	public interface IActiveElement
	{
		/// <summary>
		/// Called when the all snippet elements have been inserted.
		/// </summary>
		void OnInsertionCompleted();
		
		/// <summary>
		/// Called when the interactive mode is deactivated.
		/// </summary>
		void Deactivate(SnippetEventArgs e);
		
		/// <summary>
		/// Gets whether this element is editable (the user will be able to select it with Tab).
		/// </summary>
		bool IsEditable { get; }
		
		/// <summary>
		/// Gets the segment associated with this element. May be null.
		/// </summary>
		ISegment Segment { get; }
	}
}
