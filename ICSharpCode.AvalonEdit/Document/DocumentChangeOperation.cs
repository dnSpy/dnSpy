// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

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
