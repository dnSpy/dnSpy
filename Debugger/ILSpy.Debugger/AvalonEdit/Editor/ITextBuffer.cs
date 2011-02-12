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
using System.Collections.Generic;
using System.IO;

using ICSharpCode.AvalonEdit.Document;

namespace ILSpy.Debugger.AvalonEdit.Editor
{
	/// <summary>
	/// A read-only view on a (potentially mutable) text buffer.
	/// The IDocument interfaces derives from this interface.
	/// </summary>
	public interface ITextBuffer
	{
		/// <summary>
		/// Gets a version identifier for this text buffer.
		/// Returns null for unversioned text buffers.
		/// </summary>
		ITextBufferVersion Version { get; }
		
		/// <summary>
		/// Creates an immutable snapshot of this text buffer.
		/// Unlike all other methods in this interface, this method is thread-safe.
		/// </summary>
		ITextBuffer CreateSnapshot();
		
		/// <summary>
		/// Creates an immutable snapshot of a part of this text buffer.
		/// Unlike all other methods in this interface, this method is thread-safe.
		/// </summary>
		ITextBuffer CreateSnapshot(int offset, int length);
		
		/// <summary>
		/// Creates a new TextReader to read from this text buffer.
		/// </summary>
		TextReader CreateReader();
		
		/// <summary>
		/// Creates a new TextReader to read from this text buffer.
		/// </summary>
		TextReader CreateReader(int offset, int length);
		
		/// <summary>
		/// Gets the total text length.
		/// </summary>
		/// <returns>The length of the text, in characters.</returns>
		/// <remarks>This is the same as Text.Length, but is more efficient because
		///  it doesn't require creating a String object.</remarks>
		int TextLength { get; }
		
		/// <summary>
		/// Gets the whole text as string.
		/// </summary>
		string Text { get; }
		
		/// <summary>
		/// Is raised when the Text property changes.
		/// </summary>
		event EventHandler TextChanged;
		
		/// <summary>
		/// Gets a character at the specified position in the document.
		/// </summary>
		/// <paramref name="offset">The index of the character to get.</paramref>
		/// <exception cref="ArgumentOutOfRangeException">Offset is outside the valid range (0 to TextLength-1).</exception>
		/// <returns>The character at the specified position.</returns>
		/// <remarks>This is the same as Text[offset], but is more efficient because
		///  it doesn't require creating a String object.</remarks>
		char GetCharAt(int offset);
		
		/// <summary>
		/// Retrieves the text for a portion of the document.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">offset or length is outside the valid range.</exception>
		/// <remarks>This is the same as Text.Substring, but is more efficient because
		///  it doesn't require creating a String object for the whole document.</remarks>
		string GetText(int offset, int length);
	}
	
	/// <summary>
	/// Represents a version identifier for a text buffer.
	/// </summary>
	/// <remarks>
	/// This is SharpDevelop's equivalent to AvalonEdit ChangeTrackingCheckpoint.
	/// It is used by the ParserService to efficiently detect whether a document has changed and needs reparsing.
	/// It is a separate class from ITextBuffer to allow the GC to collect the text buffer while the version checkpoint
	/// is still in use.
	/// </remarks>
	public interface ITextBufferVersion
	{
		/// <summary>
		/// Gets whether this checkpoint belongs to the same document as the other checkpoint.
		/// </summary>
		bool BelongsToSameDocumentAs(ITextBufferVersion other);
		
		/// <summary>
		/// Compares the age of this checkpoint to the other checkpoint.
		/// </summary>
		/// <remarks>This method is thread-safe.</remarks>
		/// <exception cref="ArgumentException">Raised if 'other' belongs to a different document than this version.</exception>
		/// <returns>-1 if this version is older than <paramref name="other"/>.
		/// 0 if <c>this</c> version instance represents the same version as <paramref name="other"/>.
		/// 1 if this version is newer than <paramref name="other"/>.</returns>
		int CompareAge(ITextBufferVersion other);
		
		/// <summary>
		/// Gets the changes from this checkpoint to the other checkpoint.
		/// If 'other' is older than this checkpoint, reverse changes are calculated.
		/// </summary>
		/// <remarks>This method is thread-safe.</remarks>
		/// <exception cref="ArgumentException">Raised if 'other' belongs to a different document than this checkpoint.</exception>
		IEnumerable<TextChangeEventArgs> GetChangesTo(ITextBufferVersion other);
		
		/// <summary>
		/// Calculates where the offset has moved in the other buffer version.
		/// </summary>
		/// <exception cref="ArgumentException">Raised if 'other' belongs to a different document than this checkpoint.</exception>
		int MoveOffsetTo(ITextBufferVersion other, int oldOffset, AnchorMovementType movement);
	}
	
	public sealed class StringTextBuffer : AvalonEditTextSourceAdapter
	{
		public StringTextBuffer(string text)
			: base(new StringTextSource(text))
		{
		}
	}
}
