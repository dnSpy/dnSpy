//
// DataDirectory.cs
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

namespace Mono.Cecil.Binary {

	using Mono.Cecil.Metadata;

	public struct DataDirectory {

		public static readonly DataDirectory Zero = new DataDirectory(RVA.Zero, 0);

		RVA m_virtualAddress;
		uint m_size;

		public RVA VirtualAddress {
			get { return m_virtualAddress; }
			set { m_virtualAddress = value; }
		}

		public uint Size {
			get { return m_size; }
			set { m_size = value; }
		}

		public DataDirectory (RVA virtualAddress, uint size)
		{
			m_virtualAddress = virtualAddress;
			m_size = size;
		}

		public override int GetHashCode ()
		{
			return (m_virtualAddress.GetHashCode () ^ (int) m_size << 1);
		}

		public override bool Equals (object other)
		{
			if (other is DataDirectory) {
				DataDirectory odd = (DataDirectory)other;
				return (this.m_virtualAddress == odd.m_virtualAddress &&
				this.m_size == odd.m_size);
			}
			return false;
		}

		public override string ToString ()
		{
			return string.Format ("{0} [{1}]",
								  m_virtualAddress.ToString (), m_size.ToString ("X"));
		}

		public static bool operator == (DataDirectory one, DataDirectory other)
		{
			return one.Equals (other);
		}

		public static bool operator != (DataDirectory one, DataDirectory other)
		{
			return !one.Equals (other);
		}
	}
}
