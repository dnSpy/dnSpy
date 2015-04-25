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

namespace ICSharpCode.NRefactory.Editor
{
	/// <summary>
	/// Describes a change of the document text.
	/// This class is thread-safe.
	/// </summary>
	[Serializable]
	public class TextChangeEventArgs : EventArgs
	{
		readonly int offset;
		readonly ITextSource removedText;
		readonly ITextSource insertedText;
		
		/// <summary>
		/// The offset at which the change occurs.
		/// </summary>
		public int Offset {
			get { return offset; }
		}
		
		/// <summary>
		/// The text that was removed.
		/// </summary>
		public ITextSource RemovedText {
			get { return removedText; }
		}
		
		/// <summary>
		/// The number of characters removed.
		/// </summary>
		public int RemovalLength {
			get { return removedText.TextLength; }
		}
		
		/// <summary>
		/// The text that was inserted.
		/// </summary>
		public ITextSource InsertedText {
			get { return insertedText; }
		}
		
		/// <summary>
		/// The number of characters inserted.
		/// </summary>
		public int InsertionLength {
			get { return insertedText.TextLength; }
		}
		
		/// <summary>
		/// Creates a new TextChangeEventArgs object.
		/// </summary>
		public TextChangeEventArgs(int offset, string removedText, string insertedText)
		{
			if (offset < 0)
				throw new ArgumentOutOfRangeException("offset", offset, "offset must not be negative");
			this.offset = offset;
			this.removedText = removedText != null ? new StringTextSource(removedText) : StringTextSource.Empty;
			this.insertedText = insertedText != null ? new StringTextSource(insertedText) : StringTextSource.Empty;
		}
		
		/// <summary>
		/// Creates a new TextChangeEventArgs object.
		/// </summary>
		public TextChangeEventArgs(int offset, ITextSource removedText, ITextSource insertedText)
		{
			if (offset < 0)
				throw new ArgumentOutOfRangeException("offset", offset, "offset must not be negative");
			this.offset = offset;
			this.removedText = removedText ?? StringTextSource.Empty;
			this.insertedText = insertedText ?? StringTextSource.Empty;
		}
		
		/// <summary>
		/// Gets the new offset where the specified offset moves after this document change.
		/// </summary>
		public virtual int GetNewOffset(int offset, AnchorMovementType movementType = AnchorMovementType.Default)
		{
			if (offset >= this.Offset && offset <= this.Offset + this.RemovalLength) {
				if (movementType == AnchorMovementType.BeforeInsertion)
					return this.Offset;
				else
					return this.Offset + this.InsertionLength;
			} else if (offset > this.Offset) {
				return offset + this.InsertionLength - this.RemovalLength;
			} else {
				return offset;
			}
		}
		
		/// <summary>
		/// Creates TextChangeEventArgs for the reverse change.
		/// </summary>
		public virtual TextChangeEventArgs Invert()
		{
			return new TextChangeEventArgs(offset, insertedText, removedText);
		}
	}
}
