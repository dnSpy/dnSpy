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

namespace ICSharpCode.AvalonEdit.Utils
{
	/// <summary>
	/// Represents a string with a segment.
	/// Similar to System.ArraySegment&lt;T&gt;, but for strings instead of arrays.
	/// </summary>
	public struct StringSegment : IEquatable<StringSegment>
	{
		readonly string text;
		readonly int offset;
		readonly int count;
		
		/// <summary>
		/// Creates a new StringSegment.
		/// </summary>
		public StringSegment(string text, int offset, int count)
		{
			if (text == null)
				throw new ArgumentNullException("text");
			if (offset < 0 || offset > text.Length)
				throw new ArgumentOutOfRangeException("offset");
			if (offset + count > text.Length)
				throw new ArgumentOutOfRangeException("count");
			this.text = text;
			this.offset = offset;
			this.count = count;
		}
		
		/// <summary>
		/// Creates a new StringSegment.
		/// </summary>
		public StringSegment(string text)
		{
			if (text == null)
				throw new ArgumentNullException("text");
			this.text = text;
			this.offset = 0;
			this.count = text.Length;
		}
		
		/// <summary>
		/// Gets the string used for this segment.
		/// </summary>
		public string Text {
			get { return text; }
		}
		
		/// <summary>
		/// Gets the start offset of the segment with the text.
		/// </summary>
		public int Offset {
			get { return offset; }
		}
		
		/// <summary>
		/// Gets the length of the segment.
		/// </summary>
		public int Count {
			get { return count; }
		}
		
		#region Equals and GetHashCode implementation
		/// <inheritdoc/>
		public override bool Equals(object obj)
		{
			if (obj is StringSegment)
				return Equals((StringSegment)obj); // use Equals method below
			else
				return false;
		}
		
		/// <inheritdoc/>
		public bool Equals(StringSegment other)
		{
			// add comparisions for all members here
			return object.ReferenceEquals(this.text, other.text) && offset == other.offset && count == other.count;
		}
		
		/// <inheritdoc/>
		public override int GetHashCode()
		{
			return text.GetHashCode() ^ offset ^ count;
		}
		
		/// <summary>
		/// Equality operator.
		/// </summary>
		public static bool operator ==(StringSegment left, StringSegment right)
		{
			return left.Equals(right);
		}
		
		/// <summary>
		/// Inequality operator.
		/// </summary>
		public static bool operator !=(StringSegment left, StringSegment right)
		{
			return !left.Equals(right);
		}
		#endregion
	}
}
