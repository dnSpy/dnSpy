//
// ArrayType.cs
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

namespace Mono.Cecil {

	using System;
	using System.Text;

	using Mono.Cecil.Signatures;

	public sealed class ArrayType : TypeSpecification {

		private ArrayDimensionCollection m_dimensions;

		public ArrayDimensionCollection Dimensions {
			get { return m_dimensions; }
		}

		public int Rank {
			get { return m_dimensions.Count; }
		}

		public bool IsSizedArray {
			get {
				if (this.Rank != 1)
					return false;
				ArrayDimension dim = m_dimensions [0];
				return dim.UpperBound == 0;
			}
		}

		public override string Name {
			get { return string.Concat (base.Name, Suffix ()); }
		}

		public override string FullName {
			get { return string.Concat (base.FullName, Suffix ()); }
		}

		string Suffix ()
		{
			StringBuilder sb = new StringBuilder ();
			sb.Append ("[");
			for (int i = 0; i < m_dimensions.Count; i++) {
				ArrayDimension dim = m_dimensions [i];
				string rank = dim.ToString ();
				if (i < m_dimensions.Count - 1)
					sb.Append (",");
				if (rank.Length > 0) {
					sb.Append (" ");
					sb.Append (rank);
				}
			}
			sb.Append ("]");
			return sb.ToString ();
		}

		internal ArrayType (TypeReference elementsType, ArrayShape shape) : base (elementsType)
		{
			m_dimensions = new ArrayDimensionCollection (this);
			for (int i = 0; i < shape.Rank; i++) {
				int lower = 0, upper = 0;
				if (i < shape.NumSizes)
					if (i < shape.NumLoBounds) {
						lower = shape.LoBounds [i];
						upper = shape.LoBounds [i] + shape.Sizes [i] - 1;
					} else
						upper = shape.Sizes [i] - 1;

				m_dimensions.Add (new ArrayDimension (lower, upper));
			}
		}

		public ArrayType (TypeReference elementsType) : base (elementsType)
		{
			m_dimensions = new ArrayDimensionCollection (this);
			m_dimensions.Add (new ArrayDimension (0, 0));
		}
	}
}
