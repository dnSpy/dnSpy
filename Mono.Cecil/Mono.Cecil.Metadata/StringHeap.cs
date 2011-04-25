//
// StringHeap.cs
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
using System.Collections.Generic;
using System.Text;

using Mono.Cecil.PE;

namespace Mono.Cecil.Metadata {

	class StringHeap : Heap {

		readonly Dictionary<uint, string> strings = new Dictionary<uint, string> ();

		public StringHeap (Section section, uint start, uint size)
			: base (section, start, size)
		{
		}

		public string Read (uint index)
		{
			if (index == 0)
				return string.Empty;

			string @string;
			if (strings.TryGetValue (index, out @string))
				return @string;

			if (index > Size - 1)
				return string.Empty;

			@string = ReadStringAt (index);
			if (@string.Length != 0)
				strings.Add (index, @string);

			return @string;
		}

		protected virtual string ReadStringAt (uint index)
		{
			int length = 0;
			byte [] data = Section.Data;
			int start = (int) (index + Offset);

			for (int i = start; ; i++) {
				if (data [i] == 0)
					break;

				length++;
			}

			return Encoding.UTF8.GetString (data, start, length);
		}
	}
}
