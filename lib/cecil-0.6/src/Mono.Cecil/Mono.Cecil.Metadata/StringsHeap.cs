//
// StringsHeap.cs
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

	public class StringsHeap : MetadataHeap {

		IDictionary m_strings;

		public string this [uint index] {
			get {
				string str = m_strings [index] as string;
				if (str == null) {
					str = ReadStringAt (index);
					m_strings [index] = str;
				}
				return str;
			}
			set { m_strings [index] = value; }
		}

		internal StringsHeap (MetadataStream stream) : base (stream, MetadataStream.Strings)
		{
			m_strings = new Hashtable ();
		}

		string ReadStringAt (uint index)
		{
			if (index > Data.Length - 1)
				return string.Empty;

			int length = 0;
			for (int i = (int) index; i < Data.Length; i++) {
				if (Data [i] == 0)
					break;

				length++;
			}

			return Encoding.UTF8.GetString (Data, (int) index, length);
		}

		public override void Accept (IMetadataVisitor visitor)
		{
			visitor.VisitStringsHeap (this);
		}
	}
}
