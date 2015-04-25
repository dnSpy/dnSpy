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
using System.IO;

namespace ICSharpCode.NRefactory.Editor
{
	/// <summary>
	/// Implements the ITextSource interface using a string.
	/// </summary>
	[Serializable]
	public class StringTextSource : ITextSource
	{
		/// <summary>
		/// Gets a text source containing the empty string.
		/// </summary>
		public static readonly StringTextSource Empty = new StringTextSource(string.Empty);
		
		readonly string text;
		readonly ITextSourceVersion version;
		
		/// <summary>
		/// Creates a new StringTextSource with the given text.
		/// </summary>
		public StringTextSource(string text)
		{
			if (text == null)
				throw new ArgumentNullException("text");
			this.text = text;
		}
		
		/// <summary>
		/// Creates a new StringTextSource with the given text.
		/// </summary>
		public StringTextSource(string text, ITextSourceVersion version)
		{
			if (text == null)
				throw new ArgumentNullException("text");
			this.text = text;
			this.version = version;
		}
		
		/// <inheritdoc/>
		public ITextSourceVersion Version {
			get { return version; }
		}
		
		/// <inheritdoc/>
		public int TextLength {
			get { return text.Length; }
		}
		
		/// <inheritdoc/>
		public string Text {
			get { return text; }
		}
		
		/// <inheritdoc/>
		public ITextSource CreateSnapshot()
		{
			return this; // StringTextSource is immutable
		}
		
		/// <inheritdoc/>
		public ITextSource CreateSnapshot(int offset, int length)
		{
			return new StringTextSource(text.Substring(offset, length));
		}
		
		/// <inheritdoc/>
		public TextReader CreateReader()
		{
			return new StringReader(text);
		}
		
		/// <inheritdoc/>
		public TextReader CreateReader(int offset, int length)
		{
			return new StringReader(text.Substring(offset, length));
		}
		
		/// <inheritdoc/>
		public void WriteTextTo(TextWriter writer)
		{
			writer.Write(text);
		}
		
		/// <inheritdoc/>
		public void WriteTextTo(TextWriter writer, int offset, int length)
		{
			writer.Write(text.Substring(offset, length));
		}
		
		/// <inheritdoc/>
		public char GetCharAt(int offset)
		{
			return text[offset];
		}
		
		/// <inheritdoc/>
		public string GetText(int offset, int length)
		{
			return text.Substring(offset, length);
		}
		
		/// <inheritdoc/>
		public string GetText(ISegment segment)
		{
			if (segment == null)
				throw new ArgumentNullException("segment");
			return text.Substring(segment.Offset, segment.Length);
		}
		
		/// <inheritdoc/>
		public int IndexOf(char c, int startIndex, int count)
		{
			return text.IndexOf(c, startIndex, count);
		}
		
		/// <inheritdoc/>
		public int IndexOfAny(char[] anyOf, int startIndex, int count)
		{
			return text.IndexOfAny(anyOf, startIndex, count);
		}
		
		/// <inheritdoc/>
		public int IndexOf(string searchText, int startIndex, int count, StringComparison comparisonType)
		{
			return text.IndexOf(searchText, startIndex, count, comparisonType);
		}
		
		/// <inheritdoc/>
		public int LastIndexOf(char c, int startIndex, int count)
		{
			return text.LastIndexOf(c, startIndex + count - 1, count);
		}
		
		/// <inheritdoc/>
		public int LastIndexOf(string searchText, int startIndex, int count, StringComparison comparisonType)
		{
			return text.LastIndexOf(searchText, startIndex + count - 1, count, comparisonType);
		}
	}
}
