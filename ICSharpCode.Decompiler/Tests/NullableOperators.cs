// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
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

public static class NullableOperators
{
	// C# uses 4 different patterns of IL for operators involving nullable values: bool, other primitive types, decimal, other structs.
	// Different patterns are used depending on whether both of the operands are nullable or only the left/right operand is nullable.
	// Negation must not be pushed through such comparisons because it would change the semantics.
	// A comparison used in a condition differs somewhat from a comparison used as a simple value.

	public static void BoolBasic(bool? a, bool? b)
	{
		if (a == b)
		{
			Console.WriteLine();
		}
		if (a != b)
		{
			Console.WriteLine();
		}

		if (!(a == b))
		{
			Console.WriteLine();
		}
		if (!(a != b))
		{
			Console.WriteLine();
		}
	}

	public static void BoolComplex(bool? a, bool x)
	{
		if (a == x)
		{
			Console.WriteLine();
		}
		if (a != x)
		{
			Console.WriteLine();
		}

		if (x == a)
		{
			Console.WriteLine();
		}
		if (x != a)
		{
			Console.WriteLine();
		}

		if (!(a == x))
		{
			Console.WriteLine();
		}
		if (!(a != x))
		{
			Console.WriteLine();
		}
		if (!(x == a))
		{
			Console.WriteLine();
		}
		if (!(x != a))
		{
			Console.WriteLine();
		}
	}

	public static void BoolValueBasic(bool? a, bool? b)
	{
		Console.WriteLine(a == b);
		Console.WriteLine(a != b);

		Console.WriteLine(!(a == b));
		Console.WriteLine(!(a != b));
	}

	public static void BoolValueComplex(bool? a, bool x)
	{
		Console.WriteLine(a == x);
		Console.WriteLine(a != x);

		Console.WriteLine(x == a);
		Console.WriteLine(x != a);

		Console.WriteLine(!(a == x));
		Console.WriteLine(!(a != x));
		Console.WriteLine(!(x == a));
		Console.WriteLine(!(x != a));
	}

	public static void IntBasic(int? a, int? b)
	{
		if (a == b)
		{
			Console.WriteLine();
		}
		if (a != b)
		{
			Console.WriteLine();
		}
		if (a > b)
		{
			Console.WriteLine();
		}
		if (a < b)
		{
			Console.WriteLine();
		}
		if (a >= b)
		{
			Console.WriteLine();
		}
		if (a <= b)
		{
			Console.WriteLine();
		}

		if (!(a == b))
		{
			Console.WriteLine();
		}
		if (!(a != b))
		{
			Console.WriteLine();
		}
		if (!(a > b))
		{
			Console.WriteLine();
		}
	}

	public static void IntComplex(int? a, int x)
	{
		if (a == x)
		{
			Console.WriteLine();
		}
		if (a != x)
		{
			Console.WriteLine();
		}
		if (a > x)
		{
			Console.WriteLine();
		}
		if (a < x)
		{
			Console.WriteLine();
		}
		if (a >= x)
		{
			Console.WriteLine();
		}
		if (a <= x)
		{
			Console.WriteLine();
		}

		if (x == a)
		{
			Console.WriteLine();
		}
		if (x != a)
		{
			Console.WriteLine();
		}
		if (x > a)
		{
			Console.WriteLine();
		}
		if (x < a)
		{
			Console.WriteLine();
		}
		if (x >= a)
		{
			Console.WriteLine();
		}
		if (x <= a)
		{
			Console.WriteLine();
		}

		if (!(a == x))
		{
			Console.WriteLine();
		}
		if (!(a != x))
		{
			Console.WriteLine();
		}
		if (!(a > x))
		{
			Console.WriteLine();
		}
	}

	public static void IntValueBasic(int? a, int? b)
	{
		Console.WriteLine(a == b);
		Console.WriteLine(a != b);
		Console.WriteLine(a > b);
		Console.WriteLine(a < b);
		Console.WriteLine(a >= b);
		Console.WriteLine(a <= b);

		Console.WriteLine(!(a == b));
		Console.WriteLine(!(a != b));
		Console.WriteLine(!(a > b));
	}

	public static void IntValueComplex(int? a, int x)
	{
		Console.WriteLine(a == x);
		Console.WriteLine(a != x);
		Console.WriteLine(a > x);
		Console.WriteLine(a < x);
		Console.WriteLine(a >= x);
		Console.WriteLine(a <= x);

		Console.WriteLine(x == a);
		Console.WriteLine(x != a);
		Console.WriteLine(x > a);
		Console.WriteLine(x < a);
		Console.WriteLine(x >= a);
		Console.WriteLine(x <= a);

		Console.WriteLine(!(a == x));
		Console.WriteLine(!(a != x));
		Console.WriteLine(!(a > x));
	}

	public static void NumberBasic(decimal? a, decimal? b)
	{
		if (a == b)
		{
			Console.WriteLine();
		}
		if (a != b)
		{
			Console.WriteLine();
		}
		if (a > b)
		{
			Console.WriteLine();
		}
		if (a < b)
		{
			Console.WriteLine();
		}
		if (a >= b)
		{
			Console.WriteLine();
		}
		if (a <= b)
		{
			Console.WriteLine();
		}

		if (!(a == b))
		{
			Console.WriteLine();
		}
		if (!(a != b))
		{
			Console.WriteLine();
		}
		if (!(a > b))
		{
			Console.WriteLine();
		}
	}

	public static void NumberComplex(decimal? a, decimal x)
	{
		if (a == x)
		{
			Console.WriteLine();
		}
		if (a != x)
		{
			Console.WriteLine();
		}
		if (a > x)
		{
			Console.WriteLine();
		}
		if (a < x)
		{
			Console.WriteLine();
		}
		if (a >= x)
		{
			Console.WriteLine();
		}
		if (a <= x)
		{
			Console.WriteLine();
		}

		if (x == a)
		{
			Console.WriteLine();
		}
		if (x != a)
		{
			Console.WriteLine();
		}
		if (x > a)
		{
			Console.WriteLine();
		}
		if (x < a)
		{
			Console.WriteLine();
		}
		if (x >= a)
		{
			Console.WriteLine();
		}
		if (x <= a)
		{
			Console.WriteLine();
		}

		if (!(a == x))
		{
			Console.WriteLine();
		}
		if (!(a != x))
		{
			Console.WriteLine();
		}
		if (!(a > x))
		{
			Console.WriteLine();
		}
	}

	public static void NumberValueBasic(decimal? a, decimal? b)
	{
		Console.WriteLine(a == b);
		Console.WriteLine(a != b);
		Console.WriteLine(a > b);
		Console.WriteLine(a < b);
		Console.WriteLine(a >= b);
		Console.WriteLine(a <= b);

		Console.WriteLine(!(a == b));
		Console.WriteLine(!(a != b));
		Console.WriteLine(!(a > b));
	}

	public static void NumberValueComplex(decimal? a, decimal x)
	{
		Console.WriteLine(a == x);
		Console.WriteLine(a != x);
		Console.WriteLine(a > x);
		Console.WriteLine(a < x);
		Console.WriteLine(a >= x);
		Console.WriteLine(a <= x);

		Console.WriteLine(x == a);
		Console.WriteLine(x != a);
		Console.WriteLine(x > a);
		Console.WriteLine(x < a);
		Console.WriteLine(x >= a);
		Console.WriteLine(x <= a);

		Console.WriteLine(!(a == x));
		Console.WriteLine(!(a != x));
		Console.WriteLine(!(a > x));
	}

	public static void StructBasic(DateTime? a, DateTime? b)
	{
		if (a == b)
		{
			Console.WriteLine();
		}
		if (a != b)
		{
			Console.WriteLine();
		}
		if (a > b)
		{
			Console.WriteLine();
		}
		if (a < b)
		{
			Console.WriteLine();
		}
		if (a >= b)
		{
			Console.WriteLine();
		}
		if (a <= b)
		{
			Console.WriteLine();
		}

		if (!(a == b))
		{
			Console.WriteLine();
		}
		if (!(a != b))
		{
			Console.WriteLine();
		}
		if (!(a > b))
		{
			Console.WriteLine();
		}
	}

	public static void StructComplex(DateTime? a, DateTime x)
	{
		if (a == x)
		{
			Console.WriteLine();
		}
		if (a != x)
		{
			Console.WriteLine();
		}
		if (a > x)
		{
			Console.WriteLine();
		}
		if (a < x)
		{
			Console.WriteLine();
		}
		if (a >= x)
		{
			Console.WriteLine();
		}
		if (a <= x)
		{
			Console.WriteLine();
		}

		if (x == a)
		{
			Console.WriteLine();
		}
		if (x != a)
		{
			Console.WriteLine();
		}
		if (x > a)
		{
			Console.WriteLine();
		}
		if (x < a)
		{
			Console.WriteLine();
		}
		if (x >= a)
		{
			Console.WriteLine();
		}
		if (x <= a)
		{
			Console.WriteLine();
		}

		if (!(a == x))
		{
			Console.WriteLine();
		}
		if (!(a != x))
		{
			Console.WriteLine();
		}
		if (!(a > x))
		{
			Console.WriteLine();
		}
	}

	public static void StructValueBasic(DateTime? a, DateTime? b)
	{
		Console.WriteLine(a == b);
		Console.WriteLine(a != b);
		Console.WriteLine(a > b);
		Console.WriteLine(a < b);
		Console.WriteLine(a >= b);
		Console.WriteLine(a <= b);

		Console.WriteLine(!(a == b));
		Console.WriteLine(!(a != b));
		Console.WriteLine(!(a > b));
	}

	public static void StructValueComplex(DateTime? a, DateTime x)
	{
		Console.WriteLine(a == x);
		Console.WriteLine(a != x);
		Console.WriteLine(a > x);
		Console.WriteLine(a < x);
		Console.WriteLine(a >= x);
		Console.WriteLine(a <= x);

		Console.WriteLine(x == a);
		Console.WriteLine(x != a);
		Console.WriteLine(x > a);
		Console.WriteLine(x < a);
		Console.WriteLine(x >= a);
		Console.WriteLine(x <= a);

		Console.WriteLine(!(a == x));
		Console.WriteLine(!(a != x));
		Console.WriteLine(!(a > x));
	}
}
