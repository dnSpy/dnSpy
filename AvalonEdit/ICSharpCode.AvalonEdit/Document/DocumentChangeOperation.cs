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
using System.Diagnostics;

namespace ICSharpCode.AvalonEdit.Document
{
	/// <summary>
	/// Describes a change to a TextDocument.
	/// </summary>
	sealed class DocumentChangeOperation : IUndoableOperationWithContext
	{
		TextDocument document;
		DocumentChangeEventArgs change;
		
		public DocumentChangeOperation(TextDocument document, DocumentChangeEventArgs change)
		{
			this.document = document;
			this.change = change;
		}
		
		public void Undo(UndoStack stack)
		{
			Debug.Assert(stack.state == UndoStack.StatePlayback);
			stack.RegisterAffectedDocument(document);
			stack.state = UndoStack.StatePlaybackModifyDocument;
			this.Undo();
			stack.state = UndoStack.StatePlayback;
		}
		
		public void Redo(UndoStack stack)
		{
			Debug.Assert(stack.state == UndoStack.StatePlayback);
			stack.RegisterAffectedDocument(document);
			stack.state = UndoStack.StatePlaybackModifyDocument;
			this.Redo();
			stack.state = UndoStack.StatePlayback;
		}
		
		public void Undo()
		{
			OffsetChangeMap map = change.OffsetChangeMapOrNull;
			document.Replace(change.Offset, change.InsertionLength, change.RemovedText, map != null ? map.Invert() : null);
		}
		
		public void Redo()
		{
			document.Replace(change.Offset, change.RemovalLength, change.InsertedText, change.OffsetChangeMapOrNull);
		}
	}
}
