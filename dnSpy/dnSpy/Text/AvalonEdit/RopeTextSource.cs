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
	/// Implements the ITextSource interface using a rope.
	/// </summary>
	[Serializable]
	sealed class RopeTextSource : ITextSource {
		readonly Rope<char> rope;

		/// <summary>
		/// Creates a new RopeTextSource.
		/// </summary>
		public RopeTextSource(Rope<char> rope) {
			if (rope == null)
				throw new ArgumentNullException("rope");
			this.rope = rope.Clone();
		}

		/// <summary>
		/// Returns a clone of the rope used for this text source.
		/// </summary>
		/// <remarks>
		/// RopeTextSource only publishes a copy of the contained rope to ensure that the underlying rope cannot be modified.
		/// </remarks>
		public Rope<char> GetRope() => rope.Clone();

		/// <inheritdoc/>
		public string Text => rope.ToString();

		/// <inheritdoc/>
		public int TextLength => rope.Length;

		/// <inheritdoc/>
		public char GetCharAt(int offset) => rope[offset];

		/// <inheritdoc/>
		public string GetText(int offset, int length) => rope.ToString(offset, length);

		/// <inheritdoc/>
		public ITextSource CreateSnapshot() => this;

		/// <inheritdoc/>
		public int IndexOfAny(char[] anyOf, int startIndex, int count) => rope.IndexOfAny(anyOf, startIndex, count);

		/// <inheritdoc/>
		public void WriteTextTo(TextWriter writer) => rope.WriteTo(writer, 0, rope.Length);

		/// <inheritdoc/>
		public void WriteTextTo(TextWriter writer, int offset, int length) => rope.WriteTo(writer, offset, length);

		/// <inheritdoc/>
		public void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count) => rope.CopyTo(sourceIndex, destination, destinationIndex, count);

		/// <inheritdoc/>
		public char[] ToCharArray(int startIndex, int length) {
			var array = new char[length];
			CopyTo(startIndex, array, 0, length);
			return array;
		}
	}
}
