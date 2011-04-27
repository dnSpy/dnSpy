//
// MetadataToken.cs
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

namespace Mono.Cecil {

	public struct MetadataToken {

		readonly uint token;

		public uint RID	{
			get { return token & 0x00ffffff; }
		}

		public TokenType TokenType {
			get { return (TokenType) (token & 0xff000000); }
		}

		public static readonly MetadataToken Zero = new MetadataToken ((uint) 0);

		public MetadataToken (uint token)
		{
			this.token = token;
		}

		public MetadataToken (TokenType type)
			: this (type, 0)
		{
		}

		public MetadataToken (TokenType type, uint rid)
		{
			token = (uint) type | rid;
		}

		public MetadataToken (TokenType type, int rid)
		{
			token = (uint) type | (uint) rid;
		}

		public int ToInt32 ()
		{
			return (int) token;
		}

		public uint ToUInt32 ()
		{
			return token;
		}

		public override int GetHashCode ()
		{
			return (int) token;
		}

		public override bool Equals (object obj)
		{
			if (obj is MetadataToken) {
				var other = (MetadataToken) obj;
				return other.token == token;
			}

			return false;
		}

		public static bool operator == (MetadataToken one, MetadataToken other)
		{
			return one.token == other.token;
		}

		public static bool operator != (MetadataToken one, MetadataToken other)
		{
			return one.token != other.token;
		}

		public override string ToString ()
		{
			return string.Format ("[{0}:0x{1}]", TokenType, RID.ToString ("x4"));
		}
	}
}
