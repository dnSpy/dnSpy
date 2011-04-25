//
// UserStringHeap.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2011 Jb Evain
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;

using Mono.Cecil.PE;

namespace Mono.Cecil.Metadata {

	sealed class UserStringHeap : StringHeap {

		public UserStringHeap (Section section, uint start, uint size)
			: base (section, start, size)
		{
		}

		protected override string ReadStringAt (uint index)
		{
			byte [] data = Section.Data;
			int start = (int) (index + Offset);

			uint length = (uint) (data.ReadCompressedUInt32 (ref start) & ~1);
			if (length < 1)
				return string.Empty;

			var chars = new char [length / 2];

			for (int i = start, j = 0; i < start + length; i += 2)
				chars [j++] = (char) (data [i] | (data [i + 1] << 8));

			return new string (chars);
		}
	}
}
