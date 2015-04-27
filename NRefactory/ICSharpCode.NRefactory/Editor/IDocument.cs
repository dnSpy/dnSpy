// Copyright (c) 2010-2013 AlphaSierraPapa for the SharpDevelop Team
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

namespace ICSharpCode.NRefactory.Editor
{
	/// <summary>
	/// A document representing a source code file for refactoring.
	/// Line and column counting starts at 1.
	/// Offset counting starts at 0.
	/// </summary>
	public interface IDocument : ITextSource, IServiceProvider
	{
		/// <summary>
		/// Creates an immutable snapshot of this document.
		/// </summary>
		IDocument CreateDocumentSnapshot();
		
		/// <summary>
		/// Gets/Sets the text of the whole document..
		/// </summary>
		new string Text { get; set; } // hides ITextSource.Text to add the setter
		
		/// <summary>
		/// This event is called directly before a change is applied to the document.
		/// </summary>
		/// <remarks>
		/// It is invalid to modify the document within this event handler.
		/// Aborting the change (by throwing an exception) is likely to cause corruption of data structures
		/// that listen to the Changing and Changed events.
		/// </remarks>
		event EventHandler<TextChangeEventArgs> TextChanging;
		
		/// <summary>
		/// This event is called directly after a change is applied to the document.
		/// </summary>
		/// <remarks>
		/// It is invalid to modify the document within this event handler.
		/// Aborting the event handler (by throwing an exception) is likely to cause corruption of data structures
		/// that listen to the Changing and Changed events.
		/// </remarks>
		event EventHandler<TextChangeEventArgs> TextChanged;
		
		/// <summary>
		/// This event is called after a group of changes is completed.
		/// </summary>
		/// <seealso cref="EndUndoableAction"/>
		event EventHandler ChangeCompleted;
		
		/// <summary>
		/// Gets the number of lines in the document.
		/// </summary>
		int LineCount { get; }
		
		/// <summary>
		/// Gets the document line with the specified number.
		/// </summary>
		/// <param name="lineNumber">The number of the line to retrieve. The first line has number 1.</param>
		IDocumentLine GetLineByNumber(int lineNumber);
		
		/// <summary>
		/// Gets the document line that contains the specified offset.
		/// </summary>
		IDocumentLine GetLineByOffset(int offset);
		
		/// <summary>
		/// Gets the offset from a text location.
		/// </summary>
		/// <seealso cref="GetLocation"/>
		int GetOffset(int line, int column);
		
		/// <summary>
		/// Gets the offset from a text location.
		/// </summary>
		/// <seealso cref="GetLocation"/>
		int GetOffset(TextLocation location);
		
		/// <summary>
		/// Gets the location from an offset.
		/// </summary>
		/// <seealso cref="GetOffset(TextLocation)"/>
		TextLocation GetLocation(int offset);
		
		/// <summary>
		/// Inserts text.
		/// </summary>
		/// <param name="offset">The offset at which the text is inserted.</param>
		/// <param name="text">The new text.</param>
		/// <remarks>
		/// Anchors positioned exactly at the insertion offset will move according to their movement type.
		/// For AnchorMovementType.Default, they will move behind the inserted text.
		/// The caret will also move behind the inserted text.
		/// </remarks>
		void Insert(int offset, string text);
		
		/// <summary>
		/// Inserts text.
		/// </summary>
		/// <param name="offset">The offset at which the text is inserted.</param>
		/// <param name="text">The new text.</param>
		/// <remarks>
		/// Anchors positioned exactly at the insertion offset will move according to their movement type.
		/// For AnchorMovementType.Default, they will move behind the inserted text.
		/// The caret will also move behind the inserted text.
		/// </remarks>
		void Insert(int offset, ITextSource text);
		
		/// <summary>
		/// Inserts text.
		/// </summary>
		/// <param name="offset">The offset at which the text is inserted.</param>
		/// <param name="text">The new text.</param>
		/// <param name="defaultAnchorMovementType">
		/// Anchors positioned exactly at the insertion offset will move according to the anchor's movement type.
		/// For AnchorMovementType.Default, they will move according to the movement type specified by this parameter.
		/// The caret will also move according to the <paramref name="defaultAnchorMovementType"/> parameter.
		/// </param>
		void Insert(int offset, string text, AnchorMovementType defaultAnchorMovementType);
		
		/// <summary>
		/// Inserts text.
		/// </summary>
		/// <param name="offset">The offset at which the text is inserted.</param>
		/// <param name="text">The new text.</param>
		/// <param name="defaultAnchorMovementType">
		/// Anchors positioned exactly at the insertion offset will move according to the anchor's movement type.
		/// For AnchorMovementType.Default, they will move according to the movement type specified by this parameter.
		/// The caret will also move according to the <paramref name="defaultAnchorMovementType"/> parameter.
		/// </param>
		void Insert(int offset, ITextSource text, AnchorMovementType defaultAnchorMovementType);
		
		/// <summary>
		/// Removes text.
		/// </summary>
		/// <param name="offset">Starting offset of the text to be removed.</param>
		/// <param name="length">Length of the text to be removed.</param>
		void Remove(int offset, int length);
		
		/// <summary>
		/// Replaces text.
		/// </summary>
		/// <param name="offset">The starting offset of the text to be replaced.</param>
		/// <param name="length">The length of the text to be replaced.</param>
		/// <param name="newText">The new text.</param>
		void Replace(int offset, int length, string newText);
		
		/// <summary>
		/// Replaces text.
		/// </summary>
		/// <param name="offset">The starting offset of the text to be replaced.</param>
		/// <param name="length">The length of the text to be replaced.</param>
		/// <param name="newText">The new text.</param>
		void Replace(int offset, int length, ITextSource newText);
		
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
		/// Creates a new <see cref="ITextAnchor"/> at the specified offset.
		/// </summary>
		/// <inheritdoc cref="ITextAnchor" select="remarks|example"/>
		ITextAnchor CreateAnchor(int offset);
		
		/// <summary>
		/// Gets the name of the file the document is stored in.
		/// Could also be a non-existent dummy file name or null if no name has been set.
		/// </summary>
		string FileName { get; }
		
		/// <summary>
		/// Fired when the file name of the document changes.
		/// </summary>
		event EventHandler FileNameChanged;
	}
}
