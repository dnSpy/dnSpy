//
// GuidHeap.cs
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

	using System;
	using System.Collections;

	public class GuidHeap : MetadataHeap {

		readonly IDictionary m_guids;

		public IDictionary Guids {
			get { return m_guids; }
		}

		public GuidHeap (MetadataStream stream) : base (stream, MetadataStream.GUID)
		{
			m_guids = new Hashtable ();
		}

		public Guid this [uint index] {
			get {
				if (index == 0)
					return new Guid (new byte [16]);

				int idx = (int) index - 1;

				if (m_guids.Contains (idx))
					return (Guid) m_guids [idx];

				if (idx + 16 > this.Data.Length)
					throw new IndexOutOfRangeException ();

				byte[] buffer = new byte [16];
				Buffer.BlockCopy (this.Data, idx, buffer, 0, 16);
				Guid res = new Guid (buffer);
				m_guids [idx] = res;
				return res;
			}
			set { m_guids [index] = value; }
		}

		public override void Accept (IMetadataVisitor visitor)
		{
			visitor.VisitGuidHeap (this);
		}
	}
}
