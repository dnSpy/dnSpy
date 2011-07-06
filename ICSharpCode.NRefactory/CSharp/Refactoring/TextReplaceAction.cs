// 
// TextReplaceChange.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Mike Krüger <mkrueger@novell.com>
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	/// <summary>
	/// This is the base action for changes in a text document.
	/// </summary>
	public abstract class TextReplaceAction : Action
	{
		/// <summary>
		/// Gets or sets the offset.
		/// </summary>
		/// <value>
		/// The offset of the replace.
		/// </value>
		public int Offset {
			get;
			set;
		}
		
		int removedChars;
		/// <summary>
		/// Gets or sets the numer of chars to removed.
		/// </summary>
		/// <value>
		/// The numer of chars to remove.
		/// </value>
		/// <exception cref='ArgumentOutOfRangeException'>
		/// Is thrown when an argument passed to a method is invalid because it is outside the allowable range of values as
		/// specified by the method.
		/// </exception>
		public int RemovedChars {
			get { 
				return removedChars; 
			}
			set {
				if (value < 0)
					throw new ArgumentOutOfRangeException ("RemovedChars", "needs to be >= 0");
				removedChars = value; 
			}
		}
		
		/// <summary>
		/// Gets or sets the inserted text.
		/// </summary>
		/// <value>
		/// The text to insert.
		/// </value>
		public virtual string InsertedText {
			get;
			set;
		}
		
		/// <summary>
		/// Initializes a new instance of the <see cref="ICSharpCode.NRefactory.CSharp.Refactoring.TextReplaceAction"/> class.
		/// </summary>
		/// <param name='offset'>
		/// The offset of the replace.
		/// </param>
		/// <param name='removedChars'>
		/// The numer of chars to remove.
		/// </param>
		/// <exception cref='ArgumentOutOfRangeException'>
		/// Is thrown when an argument passed to a method is invalid because it is outside the allowable range of values as
		/// specified by the method.
		/// </exception>
		protected TextReplaceAction (int offset, int removedChars)
		{
			if (removedChars < 0)
				throw new ArgumentOutOfRangeException ("removedChars", "removedChars needs to be >= 0");
			if (offset < 0)
				throw new ArgumentOutOfRangeException ("offset", "offset needs to be >= 0");
			this.removedChars = removedChars;
			this.Offset = offset;
		}
		
		/// <summary>
		/// Initializes a new instance of the <see cref="ICSharpCode.NRefactory.CSharp.Refactoring.TextReplaceAction"/> class.
		/// </summary>
		/// <param name='offset'>
		/// The offset of the replace.
		/// </param>
		/// <param name='removedChars'>
		/// The numer of chars to remove.
		/// </param>
		/// <param name='insertedText'>
		/// The text to insert.
		/// </param>
		/// <exception cref='ArgumentOutOfRangeException'>
		/// Is thrown when an argument passed to a method is invalid because it is outside the allowable range of values as
		/// specified by the method.
		public TextReplaceAction (int offset, int removedChars, string insertedText) : this (offset, removedChars)
		{ 
			this.InsertedText = insertedText;
		}
		
		/// <summary>
		/// Returns a <see cref="System.String"/> that represents the current <see cref="ICSharpCode.NRefactory.CSharp.Refactoring.TextReplaceAction"/>.
		/// </summary>
		/// <returns>
		/// A <see cref="System.String"/> that represents the current <see cref="ICSharpCode.NRefactory.CSharp.Refactoring.TextReplaceAction"/>.
		/// </returns>
		public override string ToString ()
		{
			return string.Format ("[TextReplaceAction: Offset={0}, RemovedChars={1}, InsertedText={2}]", Offset, RemovedChars, InsertedText == null ? "<null>" : InsertedText.Replace ("\t", "\\t").Replace ("\n", "\\n"));
		}
	}
}
