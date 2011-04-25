//
// Row.cs
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

using System.Collections.Generic;

namespace Mono.Cecil.Metadata {

	struct Row<T1, T2> {
		internal T1 Col1;
		internal T2 Col2;

		public Row (T1 col1, T2 col2)
		{
			Col1 = col1;
			Col2 = col2;
		}
	}

	struct Row<T1, T2, T3> {
		internal T1 Col1;
		internal T2 Col2;
		internal T3 Col3;

		public Row (T1 col1, T2 col2, T3 col3)
		{
			Col1 = col1;
			Col2 = col2;
			Col3 = col3;
		}
	}

	struct Row<T1, T2, T3, T4> {
		internal T1 Col1;
		internal T2 Col2;
		internal T3 Col3;
		internal T4 Col4;

		public Row (T1 col1, T2 col2, T3 col3, T4 col4)
		{
			Col1 = col1;
			Col2 = col2;
			Col3 = col3;
			Col4 = col4;
		}
	}

	struct Row<T1, T2, T3, T4, T5> {
		internal T1 Col1;
		internal T2 Col2;
		internal T3 Col3;
		internal T4 Col4;
		internal T5 Col5;

		public Row (T1 col1, T2 col2, T3 col3, T4 col4, T5 col5)
		{
			Col1 = col1;
			Col2 = col2;
			Col3 = col3;
			Col4 = col4;
			Col5 = col5;
		}
	}

	struct Row<T1, T2, T3, T4, T5, T6> {
		internal T1 Col1;
		internal T2 Col2;
		internal T3 Col3;
		internal T4 Col4;
		internal T5 Col5;
		internal T6 Col6;

		public Row (T1 col1, T2 col2, T3 col3, T4 col4, T5 col5, T6 col6)
		{
			Col1 = col1;
			Col2 = col2;
			Col3 = col3;
			Col4 = col4;
			Col5 = col5;
			Col6 = col6;
		}
	}

	struct Row<T1, T2, T3, T4, T5, T6, T7, T8, T9> {
		internal T1 Col1;
		internal T2 Col2;
		internal T3 Col3;
		internal T4 Col4;
		internal T5 Col5;
		internal T6 Col6;
		internal T7 Col7;
		internal T8 Col8;
		internal T9 Col9;

		public Row (T1 col1, T2 col2, T3 col3, T4 col4, T5 col5, T6 col6, T7 col7, T8 col8, T9 col9)
		{
			Col1 = col1;
			Col2 = col2;
			Col3 = col3;
			Col4 = col4;
			Col5 = col5;
			Col6 = col6;
			Col7 = col7;
			Col8 = col8;
			Col9 = col9;
		}
	}

	sealed class RowEqualityComparer : IEqualityComparer<Row<string, string>>, IEqualityComparer<Row<uint, uint>>, IEqualityComparer<Row<uint, uint, uint>> {

		public bool Equals (Row<string, string> x, Row<string, string> y)
		{
			return x.Col1 == y.Col1
				&& x.Col2 == y.Col2;
		}

		public int GetHashCode (Row<string, string> obj)
		{
			string x = obj.Col1, y = obj.Col2;
			return (x != null ? x.GetHashCode () : 0) ^ (y != null ? y.GetHashCode () : 0);
		}

		public bool Equals (Row<uint, uint> x, Row<uint, uint> y)
		{
			return x.Col1 == y.Col1
				&& x.Col2 == y.Col2;
		}

		public int GetHashCode (Row<uint, uint> obj)
		{
			return (int) (obj.Col1 ^ obj.Col2);
		}

		public bool Equals (Row<uint, uint, uint> x, Row<uint, uint, uint> y)
		{
			return x.Col1 == y.Col1
				&& x.Col2 == y.Col2
				&& x.Col3 == y.Col3;
		}

		public int GetHashCode (Row<uint, uint, uint> obj)
		{
			return (int) (obj.Col1 ^ obj.Col2 ^ obj.Col3);
		}
	}
}
