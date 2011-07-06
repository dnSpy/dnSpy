// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.IO;

namespace ICSharpCode.Editor
{
	/// <summary>
	/// A read-only view on a (potentially mutable) text source.
	/// The IDocument interfaces derives from this interface.
	/// </summary>
	public interface ITextSource
	{
		/// <summary>
		/// Gets a version identifier for this text source.
		/// Returns null for unversioned text sources.
		/// </summary>
		ITextSourceVersion Version { get; }
		
		/// <summary>
		/// Creates an immutable snapshot of this text source.
		/// Unlike all other methods in this interface, this method is thread-safe.
		/// </summary>
		ITextSource CreateSnapshot();
		
		/// <summary>
		/// Creates an immutable snapshot of a part of this text source.
		/// Unlike all other methods in this interface, this method is thread-safe.
		/// </summary>
		ITextSource CreateSnapshot(int offset, int length);
		
		/// <summary>
		/// Creates a new TextReader to read from this text source.
		/// </summary>
		TextReader CreateReader();
		
		/// <summary>
		/// Creates a new TextReader to read from this text source.
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
		
		/// <summary>
		/// Retrieves the text for a portion of the document.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">offset or length is outside the valid range.</exception>
		string GetText(ISegment segment);
		
		/// <summary>
		/// Gets the index of the first occurrence of any character in the specified array.
		/// </summary>
		/// <param name="anyOf">Characters to search for</param>
		/// <param name="startIndex">Start index of the search.</param>
		/// <param name="count">Length of the area to search.</param>
		/// <returns>The first index where any character was found; or -1 if no occurrence was found.</returns>
		int IndexOfAny(char[] anyOf, int startIndex, int count);
		
		/* What about:
		void Insert (int offset, string value);
		void Remove (int offset, int count);
		void Remove (ISegment segment);
		
		void Replace (int offset, int count, string value);
		
		Or more search operations:
		
		IEnumerable<int> SearchForward (string pattern, int startIndex);
		IEnumerable<int> SearchForwardIgnoreCase (string pattern, int startIndex);
		
		IEnumerable<int> SearchBackward (string pattern, int startIndex);
		IEnumerable<int> SearchBackwardIgnoreCase (string pattern, int startIndex);
		*/
	}
	
	/// <summary>
	/// Represents a version identifier for a text source.
	/// </summary>
	/// <remarks>
	/// Verions can be used to efficiently detect whether a document has changed and needs reparsing;
	/// or even to implement incremental parsers.
	/// It is a separate class from ITextBuffer to allow the GC to collect the text buffer while
	/// the version checkpoint is still in use.
	/// </remarks>
	public interface ITextSourceVersion
	{
		/// <summary>
		/// Gets whether this checkpoint belongs to the same document as the other checkpoint.
		/// </summary>
		bool BelongsToSameDocumentAs(ITextSourceVersion other);
		
		/// <summary>
		/// Compares the age of this checkpoint to the other checkpoint.
		/// </summary>
		/// <remarks>This method is thread-safe.</remarks>
		/// <exception cref="ArgumentException">Raised if 'other' belongs to a different document than this version.</exception>
		/// <returns>-1 if this version is older than <paramref name="other"/>.
		/// 0 if <c>this</c> version instance represents the same version as <paramref name="other"/>.
		/// 1 if this version is newer than <paramref name="other"/>.</returns>
		int CompareAge(ITextSourceVersion other);
		
		/// <summary>
		/// Gets the changes from this checkpoint to the other checkpoint.
		/// If 'other' is older than this checkpoint, reverse changes are calculated.
		/// </summary>
		/// <remarks>This method is thread-safe.</remarks>
		/// <exception cref="ArgumentException">Raised if 'other' belongs to a different document than this checkpoint.</exception>
		IEnumerable<TextChangeEventArgs> GetChangesTo(ITextSourceVersion other);
		
		/// <summary>
		/// Calculates where the offset has moved in the other buffer version.
		/// </summary>
		/// <exception cref="ArgumentException">Raised if 'other' belongs to a different document than this checkpoint.</exception>
		int MoveOffsetTo(ITextSourceVersion other, int oldOffset, AnchorMovementType movement);
	}
}
