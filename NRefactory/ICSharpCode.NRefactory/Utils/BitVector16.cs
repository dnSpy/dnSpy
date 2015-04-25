// Copyright (c) 2010-2013 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Globalization;

namespace ICSharpCode.NRefactory.Utils
{
	/// <summary>
	/// Holds 16 boolean values.
	/// </summary>
	[Serializable]
	[CLSCompliant(false)]
	public struct BitVector16 : IEquatable<BitVector16>
	{
		ushort data;
		
		public bool this[ushort mask] {
			get { return (data & mask) != 0; }
			set {
				if (value)
					data |= mask;
				else
					data &= unchecked((ushort)~mask);
			}
		}
		
		public ushort Data {
			get { return data; }
			set { data = value; }
		}
		
		#region Equals and GetHashCode implementation
		public override bool Equals(object obj)
		{
			if (obj is BitVector16)
				return Equals((BitVector16)obj); // use Equals method below
			else
				return false;
		}
		
		public bool Equals(BitVector16 other)
		{
			return this.data == other.data;
		}
		
		public override int GetHashCode()
		{
			return data;
		}
		
		public static bool operator ==(BitVector16 left, BitVector16 right)
		{
			return left.data == right.data;
		}
		
		public static bool operator !=(BitVector16 left, BitVector16 right)
		{
			return left.data != right.data;
		}
		#endregion
		
		public override string ToString()
		{
			return data.ToString("x4", CultureInfo.InvariantCulture);
		}
	}
}
