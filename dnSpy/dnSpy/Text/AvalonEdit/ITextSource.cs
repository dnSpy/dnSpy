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
using System.IO;

namespace dnSpy.Text.AvalonEdit {
	/// <summary>
	/// A read-only view on a (potentially mutable) text source.
	/// The IDocument interface derives from this interface.
	/// </summary>
	interface ITextSource {
		/// <summary>
		/// Creates an immutable snapshot of this text source.
		/// Unlike all other methods in this interface, this method is thread-safe.
		/// </summary>
		ITextSource CreateSnapshot();

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
		/// Writes the text from this document into the TextWriter.
		/// </summary>
		void WriteTextTo(TextWriter writer);

		/// <summary>
		/// Writes the text from this document into the TextWriter.
		/// </summary>
		void WriteTextTo(TextWriter writer, int offset, int length);

		/// <summary>
		/// Gets the index of the first occurrence of any character in the specified array.
		/// </summary>
		/// <param name="anyOf">Characters to search for</param>
		/// <param name="startIndex">Start index of the area to search.</param>
		/// <param name="count">Length of the area to search.</param>
		/// <returns>The first index where any character was found; or -1 if no occurrence was found.</returns>
		int IndexOfAny(char[] anyOf, int startIndex, int count);

		/// <summary>
		/// Copies this to <paramref name="destination"/>
		/// </summary>
		/// <param name="sourceIndex">Source position</param>
		/// <param name="destination">Destination</param>
		/// <param name="destinationIndex">Destination index</param>
		/// <param name="count">Number of characters to copy</param>
		void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count);

		/// <summary>
		/// Copy part of this to a character array
		/// </summary>
		/// <param name="startIndex">Start position</param>
		/// <param name="length">Number of characters</param>
		/// <returns></returns>
		char[] ToCharArray(int startIndex, int length);
	}

	/// <summary>
	/// Implements the ITextSource interface using a string.
	/// </summary>
	[Serializable]
	sealed class StringTextSource : ITextSource {
		/// <summary>
		/// Gets a text source containing the empty string.
		/// </summary>
		public static readonly StringTextSource Empty = new StringTextSource(string.Empty);

		readonly string text;

		/// <summary>
		/// Creates a new StringTextSource with the given text.
		/// </summary>
		public StringTextSource(string text) => this.text = text ?? throw new ArgumentNullException("text");

		/// <inheritdoc/>
		public int TextLength {
			get { return text.Length; }
		}

		/// <inheritdoc/>
		public string Text {
			get { return text; }
		}

		/// <inheritdoc/>
		public ITextSource CreateSnapshot() => this; // StringTextSource is immutable

		/// <inheritdoc/>
		public void WriteTextTo(TextWriter writer) => writer.Write(text);

		/// <inheritdoc/>
		public void WriteTextTo(TextWriter writer, int offset, int length) => writer.Write(text.Substring(offset, length));

		/// <inheritdoc/>
		public char GetCharAt(int offset) => text[offset];

		/// <inheritdoc/>
		public string GetText(int offset, int length) => text.Substring(offset, length);

		/// <inheritdoc/>
		public int IndexOfAny(char[] anyOf, int startIndex, int count) => text.IndexOfAny(anyOf, startIndex, count);

		/// <inheritdoc/>
		public void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count) => text.CopyTo(sourceIndex, destination, destinationIndex, count);

		/// <inheritdoc/>
		public char[] ToCharArray(int startIndex, int length) {
			var array = new char[length];
			CopyTo(startIndex, array, 0, length);
			return array;
		}
	}
}
