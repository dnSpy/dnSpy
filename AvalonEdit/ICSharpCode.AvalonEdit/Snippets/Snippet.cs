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
using ICSharpCode.AvalonEdit.Editing;

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
