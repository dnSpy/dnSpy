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
using ICSharpCode.AvalonEdit.Utils;
#if NREFACTORY
using ICSharpCode.NRefactory.Editor;
#endif

namespace ICSharpCode.AvalonEdit.Document
{
	/// <summary>
	/// Implements the ITextSource interface using a rope.
	/// </summary>
	[Serializable]
	public sealed class RopeTextSource : ITextSource
	{
		readonly Rope<char> rope;
		readonly ITextSourceVersion version;
		
		/// <summary>
		/// Creates a new RopeTextSource.
		/// </summary>
		public RopeTextSource(Rope<char> rope)
		{
			if (rope == null)
				throw new ArgumentNullException("rope");
			this.rope = rope.Clone();
		}
		
		/// <summary>
		/// Creates a new RopeTextSource.
		/// </summary>
		public RopeTextSource(Rope<char> rope, ITextSourceVersion version)
		{
			if (rope == null)
				throw new ArgumentNullException("rope");
			this.rope = rope.Clone();
			this.version = version;
		}
		
		/// <summary>
		/// Returns a clone of the rope used for this text source.
		/// </summary>
		/// <remarks>
		/// RopeTextSource only publishes a copy of the contained rope to ensure that the underlying rope cannot be modified.
		/// </remarks>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification="Not a property because it creates a clone")]
		public Rope<char> GetRope()
		{
			return rope.Clone();
		}
		
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
		public string GetText(ISegment segment)
		{
			return rope.ToString(segment.Offset, segment.Length);
		}
		
		/// <inheritdoc/>
		public TextReader CreateReader()
		{
			return new RopeTextReader(rope);
		}
		
		/// <inheritdoc/>
		public TextReader CreateReader(int offset, int length)
		{
			return new RopeTextReader(rope.GetRange(offset, length));
		}
		
		/// <inheritdoc/>
		public ITextSource CreateSnapshot()
		{
			return this;
		}
		
		/// <inheritdoc/>
		public ITextSource CreateSnapshot(int offset, int length)
		{
			return new RopeTextSource(rope.GetRange(offset, length));
		}
		
		/// <inheritdoc/>
		public int IndexOf(char c, int startIndex, int count)
		{
			return rope.IndexOf(c, startIndex, count);
		}
		
		/// <inheritdoc/>
		public int IndexOfAny(char[] anyOf, int startIndex, int count)
		{
			return rope.IndexOfAny(anyOf, startIndex, count);
		}
		
		/// <inheritdoc/>
		public int LastIndexOf(char c, int startIndex, int count)
		{
			return rope.LastIndexOf(c, startIndex, count);
		}
		
		/// <inheritdoc/>
		public ITextSourceVersion Version {
			get { return version; }
		}
		
		/// <inheritdoc/>
		public int IndexOf(string searchText, int startIndex, int count, StringComparison comparisonType)
		{
			return rope.IndexOf(searchText, startIndex, count, comparisonType);
		}
		
		/// <inheritdoc/>
		public int LastIndexOf(string searchText, int startIndex, int count, StringComparison comparisonType)
		{
			return rope.LastIndexOf(searchText, startIndex, count, comparisonType);
		}
		
		/// <inheritdoc/>
		public void WriteTextTo(TextWriter writer)
		{
			rope.WriteTo(writer, 0, rope.Length);
		}
		
		/// <inheritdoc/>
		public void WriteTextTo(TextWriter writer, int offset, int length)
		{
			rope.WriteTo(writer, offset, length);
		}
	}
}
