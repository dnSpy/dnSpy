//
// UserStringsHeap.cs
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

	using System.Collections;
	using System.Text;

	public class UserStringsHeap : MetadataHeap {

		readonly IDictionary m_strings;

		public string this [uint offset] {
			get {
				string us = m_strings [offset] as string;
				if (us != null)
					return us;

				us = ReadStringAt ((int) offset);
				if (us != null && us.Length != 0)
					m_strings [offset] = us;

				return us;
			}
			set { m_strings [offset] = value; }
		}

		internal UserStringsHeap (MetadataStream stream) : base (stream, MetadataStream.UserStrings)
		{
			m_strings = new Hashtable ();
		}

		string ReadStringAt (int offset)
		{
			int length = Utilities.ReadCompressedInteger (this.Data, offset, out offset) - 1;
			if (length < 1)
				return string.Empty;

			return Encoding.Unicode.GetString (this.Data, offset, length);
		}

		public override void Accept (IMetadataVisitor visitor)
		{
			visitor.VisitUserStringsHeap (this);
		}
	}
}
