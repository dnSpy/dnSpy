//
// ByteBufferEqualityComparer.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2010 Jb Evain
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

namespace Mono.Cecil.PE {

	sealed class ByteBufferEqualityComparer : IEqualityComparer<ByteBuffer> {

		public bool Equals (ByteBuffer x, ByteBuffer y)
		{
			if (x.length != y.length)
				return false;

			var x_buffer = x.buffer;
			var y_buffer = y.buffer;

			for (int i = 0; i < x.length; i++)
				if (x_buffer [i] != y_buffer [i])
					return false;

			return true;
		}

		public int GetHashCode (ByteBuffer buffer)
		{
#if !BYTE_BUFFER_WELL_DISTRIBUTED_HASH
			var hash = 0;
			var bytes = buffer.buffer;
			for (int i = 0; i < buffer.length; i++)
				hash = (hash * 37) ^ bytes [i];

			return hash;
#else
			const uint p = 16777619;
			uint hash = 2166136261;

			var bytes = buffer.buffer;
			for (int i = 0; i < buffer.length; i++)
			    hash = (hash ^ bytes [i]) * p;

			hash += hash << 13;
			hash ^= hash >> 7;
			hash += hash << 3;
			hash ^= hash >> 17;
			hash += hash << 5;

			return (int) hash;
#endif
		}
	}
}
