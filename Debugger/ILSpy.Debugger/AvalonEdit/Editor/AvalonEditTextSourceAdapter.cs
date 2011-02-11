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

using ICSharpCode.AvalonEdit.Document;
using System;

namespace ILSpy.Debugger.AvalonEdit.Editor
{
	public class AvalonEditTextSourceAdapter : ITextBuffer
	{
		internal readonly ITextSource textSource;
		
		public AvalonEditTextSourceAdapter(ITextSource textSource)
		{
			if (textSource == null)
				throw new ArgumentNullException("textSource");
			this.textSource = textSource;
		}
		
		public virtual ITextBufferVersion Version {
			get { return null; }
		}
		
		/// <summary>
		/// Creates an immutable snapshot of this text buffer.
		/// </summary>
		public virtual ITextBuffer CreateSnapshot()
		{
			return new AvalonEditTextSourceAdapter(textSource.CreateSnapshot());
		}
		
		/// <summary>
		/// Creates an immutable snapshot of a part of this text buffer.
		/// Unlike all other methods in this interface, this method is thread-safe.
		/// </summary>
		public ITextBuffer CreateSnapshot(int offset, int length)
		{
			return new AvalonEditTextSourceAdapter(textSource.CreateSnapshot(offset, length));
		}
		
		/// <summary>
		/// Creates a new TextReader to read from this text buffer.
		/// </summary>
		public System.IO.TextReader CreateReader()
		{
			return textSource.CreateReader();
		}
		
		/// <summary>
		/// Creates a new TextReader to read from this text buffer.
		/// </summary>
		public System.IO.TextReader CreateReader(int offset, int length)
		{
			return textSource.CreateSnapshot(offset, length).CreateReader();
		}
		
		public int TextLength {
			get { return textSource.TextLength; }
		}
		
		public string Text {
			get { return textSource.Text; }
		}
		
		/// <summary>
		/// Is raised when the Text property changes.
		/// </summary>
		public event EventHandler TextChanged {
			add { textSource.TextChanged += value; }
			remove { textSource.TextChanged -= value; }
		}
		
		public char GetCharAt(int offset)
		{
			return textSource.GetCharAt(offset);
		}
		
		public string GetText(int offset, int length)
		{
			return textSource.GetText(offset, length);
		}
	}
}
