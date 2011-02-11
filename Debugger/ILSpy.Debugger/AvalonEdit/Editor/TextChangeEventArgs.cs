// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
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

namespace ILSpy.Debugger.AvalonEdit.Editor
{
	/// <summary>
	/// Describes a change of the document text.
	/// This class is thread-safe.
	/// </summary>
	public class TextChangeEventArgs : EventArgs
	{
		/// <summary>
		/// The offset at which the change occurs.
		/// </summary>
		public int Offset { get; private set; }
		
		/// <summary>
		/// The text that was inserted.
		/// </summary>
		public string RemovedText { get; private set; }
		
		/// <summary>
		/// The number of characters removed.
		/// </summary>
		public int RemovalLength {
			get { return RemovedText.Length; }
		}
		
		/// <summary>
		/// The text that was inserted.
		/// </summary>
		public string InsertedText { get; private set; }
		
		/// <summary>
		/// The number of characters inserted.
		/// </summary>
		public int InsertionLength {
			get { return InsertedText.Length; }
		}
		
		/// <summary>
		/// Creates a new TextChangeEventArgs object.
		/// </summary>
		public TextChangeEventArgs(int offset, string removedText, string insertedText)
		{
			this.Offset = offset;
			this.RemovedText = removedText ?? string.Empty;
			this.InsertedText = insertedText ?? string.Empty;
		}
	}
}
