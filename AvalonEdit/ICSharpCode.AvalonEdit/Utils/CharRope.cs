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
using System.Globalization;
using System.IO;
using System.Text;

namespace ICSharpCode.AvalonEdit.Utils
{
	/// <summary>
	/// Poor man's template specialization: extension methods for Rope&lt;char&gt;.
	/// </summary>
	public static class CharRope
	{
		/// <summary>
		/// Creates a new rope from the specified text.
		/// </summary>
		public static Rope<char> Create(string text)
		{
			if (text == null)
				throw new ArgumentNullException("text");
			return new Rope<char>(InitFromString(text));
		}
		
		/// <summary>
		/// Retrieves the text for a portion of the rope.
		/// Runs in O(lg N + M), where M=<paramref name="length"/>.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">offset or length is outside the valid range.</exception>
		/// <remarks>
		/// This method counts as a read access and may be called concurrently to other read accesses.
		/// </remarks>
		public static string ToString(this Rope<char> rope, int startIndex, int length)
		{
			if (rope == null)
				throw new ArgumentNullException("rope");
			#if DEBUG
			if (length < 0)
				throw new ArgumentOutOfRangeException("length", length, "Value must be >= 0");
			#endif
			if (length == 0)
				return string.Empty;
			char[] buffer = new char[length];
			rope.CopyTo(startIndex, buffer, 0, length);
			return new string(buffer);
		}
		
		/// <summary>
		/// Retrieves the text for a portion of the rope and writes it to the specified text writer.
		/// Runs in O(lg N + M), where M=<paramref name="length"/>.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">offset or length is outside the valid range.</exception>
		/// <remarks>
		/// This method counts as a read access and may be called concurrently to other read accesses.
		/// </remarks>
		public static void WriteTo(this Rope<char> rope, TextWriter output, int startIndex, int length)
		{
			if (rope == null)
				throw new ArgumentNullException("rope");
			if (output == null)
				throw new ArgumentNullException("output");
			rope.VerifyRange(startIndex, length);
			rope.root.WriteTo(startIndex, output, length);
		}
		
		/// <summary>
		/// Appends text to this rope.
		/// Runs in O(lg N + M).
		/// </summary>
		/// <exception cref="ArgumentNullException">newElements is null.</exception>
		public static void AddText(this Rope<char> rope, string text)
		{
			InsertText(rope, rope.Length, text);
		}
		
		/// <summary>
		/// Inserts text into this rope.
		/// Runs in O(lg N + M).
		/// </summary>
		/// <exception cref="ArgumentNullException">newElements is null.</exception>
		/// <exception cref="ArgumentOutOfRangeException">index or length is outside the valid range.</exception>
		public static void InsertText(this Rope<char> rope, int index, string text)
		{
			if (rope == null)
				throw new ArgumentNullException("rope");
			rope.InsertRange(index, text.ToCharArray(), 0, text.Length);
			/*if (index < 0 || index > rope.Length) {
				throw new ArgumentOutOfRangeException("index", index, "0 <= index <= " + rope.Length.ToString(CultureInfo.InvariantCulture));
			}
			if (text == null)
				throw new ArgumentNullException("text");
			if (text.Length == 0)
				return;
			rope.root = rope.root.Insert(index, text);
			rope.OnChanged();*/
		}
		
		internal static RopeNode<char> InitFromString(string text)
		{
			if (text.Length == 0) {
				return RopeNode<char>.emptyRopeNode;
			}
			RopeNode<char> node = RopeNode<char>.CreateNodes(text.Length);
			FillNode(node, text, 0);
			return node;
		}
		
		static void FillNode(RopeNode<char> node, string text, int start)
		{
			if (node.contents != null) {
				text.CopyTo(start, node.contents, 0, node.length);
			} else {
				FillNode(node.left, text, start);
				FillNode(node.right, text, start + node.left.length);
			}
		}
		
		internal static void WriteTo(this RopeNode<char> node, int index, TextWriter output, int count)
		{
			if (node.height == 0) {
				if (node.contents == null) {
					// function node
					node.GetContentNode().WriteTo(index, output, count);
				} else {
					// leaf node: append data
					output.Write(node.contents, index, count);
				}
			} else {
				// concat node: do recursive calls
				if (index + count <= node.left.length) {
					node.left.WriteTo(index, output, count);
				} else if (index >= node.left.length) {
					node.right.WriteTo(index - node.left.length, output, count);
				} else {
					int amountInLeft = node.left.length - index;
					node.left.WriteTo(index, output, amountInLeft);
					node.right.WriteTo(0, output, count - amountInLeft);
				}
			}
		}
		
		/// <summary>
		/// Gets the index of the first occurrence of any element in the specified array.
		/// </summary>
		/// <param name="rope">The target rope.</param>
		/// <param name="anyOf">Array of characters being searched.</param>
		/// <param name="startIndex">Start index of the search.</param>
		/// <param name="length">Length of the area to search.</param>
		/// <returns>The first index where any character was found; or -1 if no occurrence was found.</returns>
		public static int IndexOfAny(this Rope<char> rope, char[] anyOf, int startIndex, int length)
		{
			if (rope == null)
				throw new ArgumentNullException("rope");
			if (anyOf == null)
				throw new ArgumentNullException("anyOf");
			rope.VerifyRange(startIndex, length);
			
			while (length > 0) {
				var entry = rope.FindNodeUsingCache(startIndex).PeekOrDefault();
				char[] contents = entry.node.contents;
				int startWithinNode = startIndex - entry.nodeStartIndex;
				int nodeLength = Math.Min(entry.node.length, startWithinNode + length);
				for (int i = startIndex - entry.nodeStartIndex; i < nodeLength; i++) {
					char element = contents[i];
					foreach (char needle in anyOf) {
						if (element == needle)
							return entry.nodeStartIndex + i;
					}
				}
				length -= nodeLength - startWithinNode;
				startIndex = entry.nodeStartIndex + nodeLength;
			}
			return -1;
		}
		
		/// <summary>
		/// Gets the index of the first occurrence of the search text.
		/// </summary>
		public static int IndexOf(this Rope<char> rope, string searchText, int startIndex, int length, StringComparison comparisonType)
		{
			if (rope == null)
				throw new ArgumentNullException("rope");
			if (searchText == null)
				throw new ArgumentNullException("searchText");
			rope.VerifyRange(startIndex, length);
			int pos = rope.ToString(startIndex, length).IndexOf(searchText, comparisonType);
			if (pos < 0)
				return -1;
			else
				return pos + startIndex;
		}
		
		/// <summary>
		/// Gets the index of the last occurrence of the search text.
		/// </summary>
		public static int LastIndexOf(this Rope<char> rope, string searchText, int startIndex, int length, StringComparison comparisonType)
		{
			if (rope == null)
				throw new ArgumentNullException("rope");
			if (searchText == null)
				throw new ArgumentNullException("searchText");
			rope.VerifyRange(startIndex, length);
			int pos = rope.ToString(startIndex, length).LastIndexOf(searchText, comparisonType);
			if (pos < 0)
				return -1;
			else
				return pos + startIndex;
		}
	}
}
