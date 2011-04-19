// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

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
