// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Windows.Documents;

using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Utils;

namespace ICSharpCode.AvalonEdit.Snippets
{
	/// <summary>
	/// A code snippet that can be inserted into the text editor.
	/// </summary>
	[Serializable]
	public class Snippet : SnippetContainerElement
	{
		/// <summary>
		/// Inserts the snippet into the text area.
		/// </summary>
		public void Insert(TextArea textArea)
		{
			if (textArea == null)
				throw new ArgumentNullException("textArea");
			
			ISegment selection = textArea.Selection.SurroundingSegment;
			int insertionPosition = textArea.Caret.Offset;
			
			if (selection != null) // if something is selected
				// use selection start instead of caret position,
				// because caret could be at end of selection or anywhere inside.
				// Removal of the selected text causes the caret position to be invalid.
				insertionPosition = selection.Offset + TextUtilities.GetWhitespaceAfter(textArea.Document, selection.Offset).Length;
			
			InsertionContext context = new InsertionContext(textArea, insertionPosition);
			
			using (context.Document.RunUpdate()) {
				if (selection != null)
					textArea.Document.Remove(insertionPosition, selection.EndOffset - insertionPosition);
				Insert(context);
				context.RaiseInsertionCompleted(EventArgs.Empty);
			}
		}
	}
}
