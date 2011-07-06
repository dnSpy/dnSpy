// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT license (for details please see \doc\license.txt)

using System;
using System.IO;

namespace ICSharpCode.Editor
{
	/// <summary>
	/// Implements the ITextSource interface using a string.
	/// </summary>
	[Serializable]
	public class StringTextSource : ITextSource
	{
		readonly string text;
		
		/// <summary>
		/// Creates a new StringTextSource with the given text.
		/// </summary>
		public StringTextSource(string text)
		{
			if (text == null)
				throw new ArgumentNullException("text");
			this.text = text;
		}
		
		ITextSourceVersion ITextSource.Version {
			get { return null; }
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
			return this; // StringTextBuffer is immutable
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
		public int IndexOfAny(char[] anyOf, int startIndex, int count)
		{
			return text.IndexOfAny(anyOf, startIndex, count);
		}
	}
}
