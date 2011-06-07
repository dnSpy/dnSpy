//
// MetadataToken.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// (C) 2005 Jb Evain
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

namespace Mono.Cecil.Metadata {

	public struct MetadataToken {

		uint m_rid;
		TokenType m_type;

		public uint RID {
			get { return m_rid; }
		}

		public TokenType TokenType {
			get { return m_type; }
		}

		public static readonly MetadataToken Zero = new MetadataToken ((TokenType) 0, 0);

		public MetadataToken (int token)
		{
			m_type = (TokenType) (token & 0xff000000);
			m_rid = (uint) token & 0x00ffffff;
		}

		public MetadataToken (TokenType table, uint rid)
		{
			m_type = table;
			m_rid = rid;
		}

		internal static MetadataToken FromMetadataRow (TokenType table, int rowIndex)
		{
			return new MetadataToken (table, (uint) rowIndex + 1);
		}

		public uint ToUInt ()
		{
			return (uint) m_type | m_rid;
		}

		public override int GetHashCode ()
		{
			return (int) ToUInt ();
		}

		public override bool Equals (object other)
		{
			if (other is MetadataToken) {
				MetadataToken o = (MetadataToken) other;
				return o.m_rid == m_rid && o.m_type == m_type;
			}

			return false;
		}

		public static bool operator == (MetadataToken one, MetadataToken other)
		{
			return one.Equals (other);
		}

		public static bool operator != (MetadataToken one, MetadataToken other)
		{
			return !one.Equals (other);
		}

		public override string ToString ()
		{
			return string.Format ("{0} [0x{1}]",
				m_type, m_rid.ToString ("x4"));
		}
	}
}
