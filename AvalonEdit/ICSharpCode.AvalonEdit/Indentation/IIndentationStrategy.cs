// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using ICSharpCode.AvalonEdit.Document;
using System;

namespace ICSharpCode.AvalonEdit.Indentation
{
	/// <summary>
	/// Strategy how the text editor handles indentation when new lines are inserted.
	/// </summary>
	public interface IIndentationStrategy
	{
		/// <summary>
		/// Sets the indentation for the specified line.
		/// Usually this is constructed from the indentation of the previous line.
		/// </summary>
		void IndentLine(TextDocument document, DocumentLine line);
		
		/// <summary>
		/// Reindents a set of lines.
		/// </summary>
		void IndentLines(TextDocument document, int beginLine, int endLine);
	}
}
