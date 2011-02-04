// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Utils;

namespace ICSharpCode.AvalonEdit.Highlighting
{
	/// <summary>
	/// Represents a highlighted document.
	/// </summary>
	/// <remarks>This interface is used by the <see cref="HighlightingColorizer"/> to register the highlighter as a TextView service.</remarks>
	public interface IHighlighter
	{
		/// <summary>
		/// Gets the underlying text document.
		/// </summary>
		TextDocument Document { get; }
		
			/// <summary>
		/// Gets the span stack at the end of the specified line.
		/// -> GetSpanStack(1) returns the spans at the start of the second line.
		/// </summary>
		/// <remarks>GetSpanStack(0) is valid and will always return the empty stack.</remarks>
		ImmutableStack<HighlightingSpan> GetSpanStack(int lineNumber);
		
		/// <summary>
		/// Highlights the specified document line.
		/// </summary>
		/// <param name="lineNumber">The line to highlight.</param>
		/// <returns>A <see cref="HighlightedLine"/> line object that represents the highlighted sections.</returns>
		HighlightedLine HighlightLine(int lineNumber);
	}
}
