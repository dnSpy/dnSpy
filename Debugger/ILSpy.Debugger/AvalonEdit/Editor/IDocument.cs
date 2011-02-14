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
using ICSharpCode.NRefactory.CSharp;

namespace ILSpy.Debugger.AvalonEdit.Editor
{
	/// <summary>
	/// A document representing a source code file for refactoring.
	/// Line and column counting starts at 1.
	/// Offset counting starts at 0.
	/// </summary>
	public interface IDocument : ITextBuffer
	{
		/// <summary>
		/// Gets/Sets the whole text as string.
		/// </summary>
		new string Text { get; set; } // hides TextBuffer.Text to add the setter
		
		/// <summary>
		/// Gets the total number of lines in the document.
		/// </summary>
		int TotalNumberOfLines { get; }
		
		/// <summary>
		/// Gets the document line with the specified number.
		/// </summary>
		/// <param name="lineNumber">The number of the line to retrieve. The first line has number 1.</param>
		IDocumentLine GetLine(int lineNumber);
		
		/// <summary>
		/// Gets the document line that contains the specified offset.
		/// </summary>
		IDocumentLine GetLineForOffset(int offset);
		
		int PositionToOffset(int line, int column);
		AstLocation OffsetToPosition(int offset);
		
		void Insert(int offset, string text);
		void Insert(int offset, string text, AnchorMovementType defaultAnchorMovementType);
		void Remove(int offset, int length);
		void Replace(int offset, int length, string newText);
		
		/// <summary>
		/// Make the document combine the following actions into a single
		/// action for undo purposes.
		/// </summary>
		void StartUndoableAction();
		
		/// <summary>
		/// Ends the undoable action started with <see cref="StartUndoableAction"/>.
		/// </summary>
		void EndUndoableAction();
		
		/// <summary>
		/// Creates an undo group. Dispose the returned value to close the undo group.
		/// </summary>
		/// <returns>An object that closes the undo group when Dispose() is called.</returns>
		IDisposable OpenUndoGroup();
		
		/// <summary>
		/// Creates a new text anchor at the specified position.
		/// </summary>
		ITextAnchor CreateAnchor(int offset);
		
		event EventHandler<TextChangeEventArgs> Changing;
		event EventHandler<TextChangeEventArgs> Changed;
	}
}
