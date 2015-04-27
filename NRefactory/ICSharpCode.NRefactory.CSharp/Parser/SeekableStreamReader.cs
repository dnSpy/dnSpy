//
// SeekableStreamReader.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
using ICSharpCode.NRefactory.Editor;
using System.IO;
using System.Text;

namespace Mono.CSharp
{
	public class SeekableStreamReader : IDisposable
	{
		public const int DefaultReadAheadSize = 2048;

		readonly ITextSource textSource;

		int pos;

		static string GetAllText(Stream stream, Encoding encoding) {
			using (var rdr = new StreamReader(stream, encoding)) {
				return rdr.ReadToEnd();
			}
		}

		public SeekableStreamReader (Stream stream, Encoding encoding, char[] sharedBuffer = null) : this(new StringTextSource(GetAllText(stream, encoding)))
		{
		}

		public SeekableStreamReader (ITextSource source)
		{
			this.textSource = source;
		}


		public void Dispose ()
		{
		}
		
		/// <remarks>
		///   This value corresponds to the current position in a stream of characters.
		///   The StreamReader hides its manipulation of the underlying byte stream and all
		///   character set/decoding issues.  Thus, we cannot use this position to guess at
		///   the corresponding position in the underlying byte stream even though there is
		///   a correlation between them.
		/// </remarks>
		public int Position {
			get {
				return pos;
			}
			
			set {
				pos = value;
			}
		}

		public char GetChar (int position)
		{
			return textSource.GetCharAt (position);
		}
		
		public char[] ReadChars (int fromPosition, int toPosition)
		{
			return textSource.GetText (fromPosition, toPosition - fromPosition).ToCharArray ();
		}
		
		public int Peek ()
		{
			if (pos >= textSource.TextLength)
				return -1;
			return textSource.GetCharAt (pos);
		}
		
		public int Read ()
		{
			if (pos >= textSource.TextLength)
				return -1;
			return textSource.GetCharAt (pos++);
		}
	}
}

