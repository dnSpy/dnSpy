//
// RVA.cs
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

	public struct RVA {

		public static readonly RVA Zero = new RVA (0);

		uint m_rva;

		public uint Value {
			get { return m_rva; }
			set { m_rva = value; }
		}

		public RVA (uint rva)
		{
			m_rva = rva;
		}

		public override int GetHashCode ()
		{
			return (int) m_rva;
		}

		public override bool Equals (object other)
		{
			if (other is RVA)
				return this.m_rva == ((RVA) other).m_rva;

			return false;
		}

		public override string ToString ()
		{
			return string.Format ("0x{0}", m_rva.ToString ("X"));
		}

		public static bool operator == (RVA one, RVA other)
		{
			return one.Equals (other);
		}

		public static bool operator != (RVA one, RVA other)
		{
			return !one.Equals (other);
		}

		public static bool operator < (RVA one, RVA other)
		{
			return one.m_rva < other.m_rva;
		}

		public static bool operator > (RVA one, RVA other)
		{
			return one.m_rva > other.m_rva;
		}

		public static bool operator <= (RVA one, RVA other)
		{
			return one.m_rva <= other.m_rva;
		}

		public static bool operator >= (RVA one, RVA other)
		{
			return one.m_rva >= other.m_rva;
		}

		public static RVA operator + (RVA rva, uint x)
		{
			return new RVA (rva.m_rva + x);
		}

		public static RVA operator - (RVA rva, uint x)
		{
			return new RVA (rva.m_rva - x);
		}

		public static implicit operator RVA (uint val)
		{
			return val == 0 ? RVA.Zero : new RVA (val);
		}

		public static implicit operator uint (RVA rva)
		{
			return rva.m_rva;
		}
	}
}
