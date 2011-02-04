// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using ICSharpCode.AvalonEdit.Utils;
using System;
using System.IO;

namespace ICSharpCode.AvalonEdit.Document
{
	/// <summary>
	/// Interface for read-only access to a text source.
	/// </summary>
	/// <seealso cref="TextDocument"/>
	/// <seealso cref="StringTextSource"/>
	public interface ITextSource
	{
		/// <summary>
		/// Gets the whole text as string.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods")]
		string Text { get; }
		
		/// <summary>
		/// Is raised when the Text property changes.
		/// </summary>
		event EventHandler TextChanged;
		
		/// <summary>
		/// Gets the total text length.
		/// </summary>
		/// <returns>The length of the text, in characters.</returns>
		/// <remarks>This is the same as Text.Length, but is more efficient because
		///  it doesn't require creating a String object.</remarks>
		int TextLength { get; }
		
		/// <summary>
		/// Gets a character at the specified position in the document.
		/// </summary>
		/// <paramref name="offset">The index of the character to get.</paramref>
		/// <exception cref="ArgumentOutOfRangeException">Offset is outside the valid range (0 to TextLength-1).</exception>
		/// <returns>The character at the specified position.</returns>
		/// <remarks>This is the same as Text[offset], but is more efficient because
		/// it doesn't require creating a String object.</remarks>
		char GetCharAt(int offset);
		
		/// <summary>
		/// Gets the index of the first occurrence of any character in the specified array.
		/// </summary>
		/// <param name="anyOf"></param>
		/// <param name="startIndex">Start index of the search.</param>
		/// <param name="count">Length of the area to search.</param>
		/// <returns>The first index where any character was found; or -1 if no occurrence was found.</returns>
		int IndexOfAny(char[] anyOf, int startIndex, int count);
		
		/// <summary>
		/// Retrieves the text for a portion of the document.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">offset or length is outside the valid range.</exception>
		/// <remarks>This is the same as Text.Substring, but is more efficient because
		///  it doesn't require creating a String object for the whole document.</remarks>
		string GetText(int offset, int length);
		
		/// <summary>
		/// Creates a snapshot of the current text.
		/// </summary>
		/// <remarks>
		/// This method is generally not thread-safe when called on a mutable text buffer, but the resulting text buffer is immutable and thread-safe.
		/// However, some implementing classes may provide additional thread-safety guarantees, see <see cref="TextDocument.CreateSnapshot()">TextDocument.CreateSnapshot</see>.
		/// </remarks>
		ITextSource CreateSnapshot();
		
		/// <summary>
		/// Creates a snapshot of a part of the current text.
		/// </summary>
		/// <remarks>
		/// This method is generally not thread-safe when called on a mutable text buffer, but the resulting text buffer is immutable and thread-safe.
		/// However, some implementing classes may provide additional thread-safety guarantees, see <see cref="TextDocument.CreateSnapshot()">TextDocument.CreateSnapshot</see>.
		/// </remarks>
		ITextSource CreateSnapshot(int offset, int length);
		
		/// <summary>
		/// Creates a text reader.
		/// If the text is changed while a reader is active, the reader will continue to read from the old text version.
		/// </summary>
		TextReader CreateReader();
	}
	
	/// <summary>
	/// Implements the ITextSource interface by wrapping another TextSource
	/// and viewing only a part of the text.
	/// </summary>
	[Obsolete("This class will be removed in a future version of AvalonEdit")]
	public sealed class TextSourceView : ITextSource
	{
		readonly ITextSource baseTextSource;
		readonly ISegment viewedSegment;
		
		/// <summary>
		/// Creates a new TextSourceView object.
		/// </summary>
		/// <param name="baseTextSource">The base text source.</param>
		/// <param name="viewedSegment">A text segment from the base text source</param>
		public TextSourceView(ITextSource baseTextSource, ISegment viewedSegment)
		{
			if (baseTextSource == null)
				throw new ArgumentNullException("baseTextSource");
			if (viewedSegment == null)
				throw new ArgumentNullException("viewedSegment");
			this.baseTextSource = baseTextSource;
			this.viewedSegment = viewedSegment;
		}
		
		/// <inheritdoc/>
		public event EventHandler TextChanged {
			add { baseTextSource.TextChanged += value; }
			remove { baseTextSource.TextChanged -= value; }
		}
		
		/// <inheritdoc/>
		public string Text {
			get {
				return baseTextSource.GetText(viewedSegment.Offset, viewedSegment.Length);
			}
		}
		
		/// <inheritdoc/>
		public int TextLength {
			get { return viewedSegment.Length; }
		}
		
		/// <inheritdoc/>
		public char GetCharAt(int offset)
		{
			return baseTextSource.GetCharAt(viewedSegment.Offset + offset);
		}
		
		/// <inheritdoc/>
		public string GetText(int offset, int length)
		{
			return baseTextSource.GetText(viewedSegment.Offset + offset, length);
		}
		
		/// <inheritdoc/>
		public ITextSource CreateSnapshot()
		{
			return baseTextSource.CreateSnapshot(viewedSegment.Offset, viewedSegment.Length);
		}
		
		/// <inheritdoc/>
		public ITextSource CreateSnapshot(int offset, int length)
		{
			return baseTextSource.CreateSnapshot(viewedSegment.Offset + offset, length);
		}
		
		/// <inheritdoc/>
		public TextReader CreateReader()
		{
			return CreateSnapshot().CreateReader();
		}
		
		/// <inheritdoc/>
		public int IndexOfAny(char[] anyOf, int startIndex, int count)
		{
			int offset = viewedSegment.Offset;
			int result = baseTextSource.IndexOfAny(anyOf, startIndex + offset, count);
			return result >= 0 ? result - offset : result;
		}
	}
	
	/// <summary>
	/// Implements the ITextSource interface using a string.
	/// </summary>
	public sealed class StringTextSource : ITextSource
	{
		readonly string text;
		
		/// <summary>
		/// Creates a new StringTextSource.
		/// </summary>
		public StringTextSource(string text)
		{
			if (text == null)
				throw new ArgumentNullException("text");
			this.text = text;
		}
		
		// Text can never change
		event EventHandler ITextSource.TextChanged { add {} remove {} }
		
		/// <inheritdoc/>
		public string Text {
			get { return text; }
		}
		
		/// <inheritdoc/>
		public int TextLength {
			get { return text.Length; }
		}
		
		/// <inheritdoc/>
		public char GetCharAt(int offset)
		{
			// GetCharAt must throw ArgumentOutOfRangeException, not IndexOutOfRangeException
			if (offset < 0 || offset >= text.Length)
				throw new ArgumentOutOfRangeException("offset", offset, "offset must be between 0 and " + (text.Length - 1));
			return text[offset];
		}
		
		/// <inheritdoc/>
		public string GetText(int offset, int length)
		{
			return text.Substring(offset, length);
		}
		
		/// <inheritdoc/>
		public TextReader CreateReader()
		{
			return new StringReader(text);
		}
		
		/// <inheritdoc/>
		public ITextSource CreateSnapshot()
		{
			return this; // StringTextSource already is immutable
		}
		
		/// <inheritdoc/>
		public ITextSource CreateSnapshot(int offset, int length)
		{
			return new StringTextSource(text.Substring(offset, length));
		}
		
		/// <inheritdoc/>
		public int IndexOfAny(char[] anyOf, int startIndex, int count)
		{
			return text.IndexOfAny(anyOf, startIndex, count);
		}
	}
	
	/// <summary>
	/// Implements the ITextSource interface using a rope.
	/// </summary>
	public sealed class RopeTextSource : ITextSource
	{
		readonly Rope<char> rope;
		
		/// <summary>
		/// Creates a new RopeTextSource.
		/// </summary>
		public RopeTextSource(Rope<char> rope)
		{
			if (rope == null)
				throw new ArgumentNullException("rope");
			this.rope = rope;
		}
		
		/// <summary>
		/// Returns a clone of the rope used for this text source.
		/// </summary>
		/// <remarks>
		/// RopeTextSource only publishes a copy of the contained rope to ensure that the underlying rope cannot be modified.
		/// Unless the creator of the RopeTextSource still has a reference on the rope, RopeTextSource is immutable.
		/// </remarks>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification="Not a property because it creates a clone")]
		public Rope<char> GetRope()
		{
			return rope.Clone();
		}
		
		// Change event is not supported
		event EventHandler ITextSource.TextChanged { add {} remove {} }
		
		/// <inheritdoc/>
		public string Text {
			get { return rope.ToString(); }
		}
		
		/// <inheritdoc/>
		public int TextLength {
			get { return rope.Length; }
		}
		
		/// <inheritdoc/>
		public char GetCharAt(int offset)
		{
			return rope[offset];
		}
		
		/// <inheritdoc/>
		public string GetText(int offset, int length)
		{
			return rope.ToString(offset, length);
		}
		
		/// <inheritdoc/>
		public TextReader CreateReader()
		{
			return new RopeTextReader(rope);
		}
		
		/// <inheritdoc/>
		public ITextSource CreateSnapshot()
		{
			// we clone the underlying rope because the creator of the RopeTextSource might be modifying it
			return new RopeTextSource(rope.Clone());
		}
		
		/// <inheritdoc/>
		public ITextSource CreateSnapshot(int offset, int length)
		{
			return new RopeTextSource(rope.GetRange(offset, length));
		}
		
		/// <inheritdoc/>
		public int IndexOfAny(char[] anyOf, int startIndex, int count)
		{
			return rope.IndexOfAny(anyOf, startIndex, count);
		}
	}
}
