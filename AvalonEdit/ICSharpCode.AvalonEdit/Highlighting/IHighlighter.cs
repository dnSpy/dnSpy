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

namespace ICSharpCode.AvalonEdit.Highlighting
{
	/// <summary>
	/// Represents a highlighted document.
	/// </summary>
	/// <remarks>This interface is used by the <see cref="HighlightingColorizer"/> to register the highlighter as a TextView service.</remarks>
	public interface IHighlighter : IDisposable
	{
		/// <summary>
		/// Gets the underlying text document.
		/// </summary>
		IDocument Document { get; }
		
		/// <summary>
		/// Gets the stack of active colors (the colors associated with the active spans) at the end of the specified line.
		/// -> GetColorStack(1) returns the colors at the start of the second line.
		/// </summary>
		/// <remarks>
		/// GetColorStack(0) is valid and will return the empty stack.
		/// The elements are returned in inside-out order (first element of result enumerable is the color of the innermost span).
		/// </remarks>
		IEnumerable<HighlightingColor> GetColorStack(int lineNumber);
		
		// Starting with SD 5.0, this interface exports GetColorStack() instead of GetSpanStack().
		// This was done because custom highlighter implementations might not use the HighlightingSpan class (AST-based highlighting).
		
		/// <summary>
		/// Highlights the specified document line.
		/// </summary>
		/// <param name="lineNumber">The line to highlight.</param>
		/// <returns>A <see cref="HighlightedLine"/> line object that represents the highlighted sections.</returns>
		HighlightedLine HighlightLine(int lineNumber);
		
		/// <summary>
		/// Enforces a highlighting state update (triggering the HighlightingStateChanged event if necessary)
		/// for all lines up to (and inclusive) the specified line number.
		/// </summary>
		void UpdateHighlightingState(int lineNumber);
		
		/// <summary>
		/// Notification when the highlighter detects that the highlighting state at the
		/// <b>beginning</b> of the specified lines has changed.
		/// <c>fromLineNumber</c> and <c>toLineNumber</c> are both inclusive;
		/// the common case of a single-line change is represented by <c>fromLineNumber == toLineNumber</c>.
		/// 
		/// During highlighting, the highlighting of line X will cause this event to be raised
		/// for line X+1 if the highlighting state at the end of line X has changed from its previous state.
		/// This event may also be raised outside of the highlighting process to signalize that
		/// changes to external data (not the document text; but e.g. semantic information)
		/// require a re-highlighting of the specified lines.
		/// </summary>
		/// <remarks>
		/// For implementers: there is the requirement that, during highlighting,
		/// if there was no state changed reported for the beginning of line X,
		/// and there were no document changes between the start of line X and the start of line Y (with Y > X),
		/// then this event must not be raised for any line between X and Y (inclusive).
		/// 
		/// Equal input state + unchanged line = Equal output state.
		/// 
		/// See the comment in the HighlightingColorizer.OnHighlightStateChanged implementation
		/// for details about the requirements for a correct custom IHighlighter.
		/// 
		/// Outside of the highlighting process, this event can be raised without such restrictions.
		/// </remarks>
		event HighlightingStateChangedEventHandler HighlightingStateChanged;
		
		/// <summary>
		/// Opens a group of <see cref="HighlightLine"/> calls.
		/// It is not necessary to call this method before calling <see cref="HighlightLine"/>,
		/// however, doing so can make the highlighting much more performant in some cases
		/// (e.g. the C# semantic highlighter in SharpDevelop will re-use the resolver within a highlighting group).
		/// </summary>
		/// <remarks>
		/// The group is closed by either a <see cref="EndHighlighting"/> or a <see cref="IDisposable.Dispose"/> call.
		/// Nested groups are not allowed.
		/// </remarks>
		void BeginHighlighting();
		
		/// <summary>
		/// Closes the currently opened group of <see cref="HighlightLine"/> calls.
		/// </summary>
		/// <seealso cref="BeginHighlighting"/>.
		void EndHighlighting();
		
		/// <summary>
		/// Retrieves the HighlightingColor with the specified name. Returns null if no color matching the name is found.
		/// </summary>
		HighlightingColor GetNamedColor(string name);
		
		/// <summary>
		/// Gets the default text color.
		/// </summary>
		HighlightingColor DefaultTextColor { get; }
	}
	
	/// <summary>
	/// Event handler for <see cref="IHighlighter.HighlightingStateChanged"/>
	/// </summary>
	public delegate void HighlightingStateChangedEventHandler(int fromLineNumber, int toLineNumber);
}
