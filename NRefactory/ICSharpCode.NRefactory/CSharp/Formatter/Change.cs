// 
// Change.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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

namespace ICSharpCode.NRefactory
{
	public class Change
	{
		public int Offset {
			get;
			set;
		}
		
		int removedChars;
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

		public string InsertedText {
			get;
			set;
		}
		
		public Change (int offset, int removedChars, string insertedText)
		{
			if (removedChars < 0)
				throw new ArgumentOutOfRangeException ("removedChars", "removedChars needs to be >= 0");
			if (offset < 0)
				throw new ArgumentOutOfRangeException ("offset", "offset needs to be >= 0");
			this.removedChars = removedChars;
			this.Offset = offset;
			this.InsertedText = insertedText;
		}
		
		public override string ToString ()
		{
			return string.Format ("[Change: Offset={0}, RemovedChars={1}, InsertedText={2}]", Offset, RemovedChars, InsertedText);
		}
	}
	
}

