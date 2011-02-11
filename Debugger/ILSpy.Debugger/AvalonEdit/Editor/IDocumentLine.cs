// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
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

namespace ILSpy.Debugger.AvalonEdit.Editor
{
	/// <summary>
	/// A line inside a <see cref="IDocument"/>.
	/// </summary>
	public interface IDocumentLine
	{
		/// <summary>
		/// Gets the starting offset of the line in the document's text.
		/// </summary>
		int Offset { get; }
		
		/// <summary>
		/// Gets the length of this line (=the number of characters on the line).
		/// </summary>
		int Length { get; }
		
		/// <summary>
		/// Gets the ending offset of the line in the document's text (= Offset + Length).
		/// </summary>
		int EndOffset { get; }
		
		/// <summary>
		/// Gets the length of this line, including the line delimiter.
		/// </summary>
		int TotalLength { get; }
		
		/// <summary>
		/// Gets the length of the line terminator.
		/// Returns 1 or 2; or 0 at the end of the document.
		/// </summary>
		int DelimiterLength { get; }
		
		/// <summary>
		/// Gets the number of this line.
		/// The first line has the number 1.
		/// </summary>
		int LineNumber { get; }
		
		/// <summary>
		/// Gets the text on this line.
		/// </summary>
		string Text { get; }
	}
}
