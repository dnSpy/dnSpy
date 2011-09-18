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
using System.Runtime.InteropServices;

public static class LiftedOperators
{
	// C# uses 4 different patterns of IL for lifted operators: bool, other primitive types, decimal, other structs.
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

	public static void BoolComplex(bool? a, Func<bool> x)
	{
		if (a == x())
		{
			Console.WriteLine();
		}
		if (a != x())
		{
			Console.WriteLine();
		}

		if (x() == a)
		{
			Console.WriteLine();
		}
		if (x() != a)
		{
			Console.WriteLine();
		}

		if (!(a == x()))
		{
			Console.WriteLine();
		}
		if (!(a != x()))
		{
			Console.WriteLine();
		}
		if (!(x() == a))
		{
			Console.WriteLine();
		}
		if (!(x() != a))
		{
			Console.WriteLine();
		}
	}

	public static void BoolConst(bool? a)
	{
		if (a == true) 
		{
			Console.WriteLine();
		}
		if (a != true)
		{
			Console.WriteLine();
		}
		if (a == false)
		{
			Console.WriteLine();
		}
		if (a != false)
		{
			Console.WriteLine();
		}
		if (a ?? true) 
		{
		  Console.WriteLine();
		}
		if (a ?? false) 
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

		Console.WriteLine(a & b);
		Console.WriteLine(a | b);
		Console.WriteLine(a ^ b);
		Console.WriteLine(a ?? b);
		Console.WriteLine(!a);
		a &= b;
		a |= b;
		a ^= b;
	}

	public static void BoolValueComplex(bool? a, Func<bool> x)
	{
		Console.WriteLine(a == x());
		Console.WriteLine(a != x());

		Console.WriteLine(x() == a);
		Console.WriteLine(x() != a);

		Console.WriteLine(!(a == x()));
		Console.WriteLine(!(a != x()));

		Console.WriteLine(a & x());
		Console.WriteLine(a | x());
		Console.WriteLine(a ^ x());
		Console.WriteLine(a ?? x());
		a &= x();
		a |= x();
		a ^= x();

		Console.WriteLine(x() ^ a);
		(new bool?[0])[0] ^= x();
	}

	public static void BoolValueConst(bool? a)
	{
		Console.WriteLine(a == true);
		Console.WriteLine(a != true);
		Console.WriteLine(a == false);
		Console.WriteLine(a != false);
		Console.WriteLine(a ?? true);
		Console.WriteLine(a ?? false);
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

	public static void IntComplex(int? a, Func<int> x)
	{
		if (a == x())
		{
			Console.WriteLine();
		}
		if (a != x())
		{
			Console.WriteLine();
		}
		if (a > x())
		{
			Console.WriteLine();
		}

		if (x() == a)
		{
			Console.WriteLine();
		}
		if (x() != a)
		{
			Console.WriteLine();
		}
		if (x() > a)
		{
			Console.WriteLine();
		}

		if (!(a == x()))
		{
			Console.WriteLine();
		}
		if (!(a != x()))
		{
			Console.WriteLine();
		}
		if (!(a > x()))
		{
			Console.WriteLine();
		}
	}

	public static void IntConst(int? a)
	{
		if (a == 2)
		{
			Console.WriteLine();
		}
		if (a != 2)
		{
			Console.WriteLine();
		}
		if (a > 2)
		{
			Console.WriteLine();
		}

		if (2 == a)
		{
			Console.WriteLine();
		}
		if (2 != a)
		{
			Console.WriteLine();
		}
		if (2 > a)
		{
			Console.WriteLine();
		}
	}

	public static void IntValueBasic(int? a, int? b)
	{
		Console.WriteLine(a == b);
		Console.WriteLine(a != b);
		Console.WriteLine(a > b);

		Console.WriteLine(!(a == b));
		Console.WriteLine(!(a != b));
		Console.WriteLine(!(a > b));

		Console.WriteLine(a + b);
		Console.WriteLine(a - b);
		Console.WriteLine(a * b);
		Console.WriteLine(a / b);
		Console.WriteLine(a % b);
		Console.WriteLine(a & b);
		Console.WriteLine(a | b);
		Console.WriteLine(a ^ b);
		Console.WriteLine(a << b);
		Console.WriteLine(a >> b);
		Console.WriteLine(a ?? b);
		Console.WriteLine(-a);
		Console.WriteLine(~a);
		// TODO:
		//Console.WriteLine(a++);
		//Console.WriteLine(a--);
		Console.WriteLine(++a);
		Console.WriteLine(--a);
		a += b;
		a -= b;
		a *= b;
		a /= b;
		a %= b;
		a &= b;
		a |= b;
		a ^= b;
		a <<= b;
		a >>= b;
	}

	public static void IntValueComplex(int? a, Func<int> x)
	{
		Console.WriteLine(a == x());
		Console.WriteLine(a != x());
		Console.WriteLine(a > x());

		Console.WriteLine(x() == a);
		Console.WriteLine(x() != a);
		Console.WriteLine(x() > a);

		Console.WriteLine(a + x());
		Console.WriteLine(a - x());
		Console.WriteLine(a * x());
		Console.WriteLine(a / x());
		Console.WriteLine(a % x());
		Console.WriteLine(a & x());
		Console.WriteLine(a | x());
		Console.WriteLine(a ^ x());
		Console.WriteLine(a << x());
		Console.WriteLine(a >> x());
		Console.WriteLine(a ?? x());
		a += x();
		a -= x();
		a *= x();
		a /= x();
		a %= x();
		a &= x();
		a |= x();
		a ^= x();
		a <<= x();
		a >>= x();

		Console.WriteLine(x() + a);
		(new int?[0])[0] += x();
	}

	public static void IntValueConst(int? a)
	{
		Console.WriteLine(a == 2);
		Console.WriteLine(a != 2);
		Console.WriteLine(a > 2);

		Console.WriteLine(2 == a);
		Console.WriteLine(2 != a);
		Console.WriteLine(2 > a);

		Console.WriteLine(a + 2);
		Console.WriteLine(a - 2);
		Console.WriteLine(a * 2);
		Console.WriteLine(a / 2);
		Console.WriteLine(a % 2);
		Console.WriteLine(a & 2);
		Console.WriteLine(a | 2);
		Console.WriteLine(a ^ 2);
		Console.WriteLine(a << 2);
		Console.WriteLine(a >> 2);
		Console.WriteLine(a ?? 2);
		a += 2;
		a -= 2;
		a *= 2;
		a /= 2;
		a %= 2;
		a &= 2;
		a |= 2;
		a ^= 2;
		a <<= 2;
		a >>= 2;

		Console.WriteLine(2 + a);
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

	public static void NumberComplex(decimal? a, Func<decimal> x)
	{
		if (a == x())
		{
			Console.WriteLine();
		}
		if (a != x())
		{
			Console.WriteLine();
		}
		if (a > x())
		{
			Console.WriteLine();
		}

		if (x() == a)
		{
			Console.WriteLine();
		}
		if (x() != a)
		{
			Console.WriteLine();
		}
		if (x() > a)
		{
			Console.WriteLine();
		}
	}

	public static void NumberConst(decimal? a)
	{
		if (a == 2m)
		{
			Console.WriteLine();
		}
		if (a != 2m)
		{
			Console.WriteLine();
		}
		if (a > 2m)
		{
			Console.WriteLine();
		}

		if (2m == a)
		{
			Console.WriteLine();
		}
		if (2m != a)
		{
			Console.WriteLine();
		}
		if (2m > a)
		{
			Console.WriteLine();
		}
	}

	public static void NumberValueBasic(decimal? a, decimal? b)
	{
		Console.WriteLine(a == b);
		Console.WriteLine(a != b);
		Console.WriteLine(a > b);

		Console.WriteLine(!(a == b));
		Console.WriteLine(!(a != b));
		Console.WriteLine(!(a > b));

		Console.WriteLine(a + b);
		Console.WriteLine(a - b);
		Console.WriteLine(a * b);
		Console.WriteLine(a / b);
		Console.WriteLine(a % b);
		Console.WriteLine(a ?? b);
		Console.WriteLine(-a);
		// TODO:
		//Console.WriteLine(a++);
		//Console.WriteLine(a--);
		//Console.WriteLine(++a);
		//Console.WriteLine(--a);
		a += b;
		a -= b;
		a *= b;
		a /= b;
		a %= b;
	}

	public static void NumberValueComplex(decimal? a, Func<decimal> x)
	{
		Console.WriteLine(a == x());
		Console.WriteLine(a != x());
		Console.WriteLine(a > x());

		Console.WriteLine(x() == a);
		Console.WriteLine(x() != a);
		Console.WriteLine(x() > a);

		Console.WriteLine(a + x());
		Console.WriteLine(a - x());
		Console.WriteLine(a * x());
		Console.WriteLine(a / x());
		Console.WriteLine(a % x());
		Console.WriteLine(a ?? x());
		a += x();
		a -= x();
		a *= x();
		a /= x();
		a %= x();

		Console.WriteLine(x() + a);
		(new decimal?[0])[0] += x();
	}

	public static void NumberValueConst(decimal? a)
	{
		Console.WriteLine(a == 2m);
		Console.WriteLine(a != 2m);
		Console.WriteLine(a > 2m);

		Console.WriteLine(2m == a);
		Console.WriteLine(2m != a);
		Console.WriteLine(2m > a);

		Console.WriteLine(a + 2m);
		Console.WriteLine(a - 2m);
		Console.WriteLine(a * 2m);
		Console.WriteLine(a / 2m);
		Console.WriteLine(a % 2m);
		Console.WriteLine(a ?? 2m);
		a += 2m;
		a -= 2m;
		a *= 2m;
		a /= 2m;
		a %= 2m;

		Console.WriteLine(2m + a);
	}

	public static void StructBasic(TS? a, TS? b)
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

	public static void StructComplex(TS? a, Func<TS> x)
	{
		if (a == x())
		{
			Console.WriteLine();
		}
		if (a != x())
		{
			Console.WriteLine();
		}
		if (a > x())
		{
			Console.WriteLine();
		}

		if (x() == a)
		{
			Console.WriteLine();
		}
		if (x() != a)
		{
			Console.WriteLine();
		}
		if (x() > a)
		{
			Console.WriteLine();
		}
	}

	public static void StructValueBasic(TS? a, TS? b, int? i)
	{
		Console.WriteLine(a == b);
		Console.WriteLine(a != b);
		Console.WriteLine(a > b);

		Console.WriteLine(!(a == b));
		Console.WriteLine(!(a != b));
		Console.WriteLine(!(a > b));

		Console.WriteLine(a + b);
		Console.WriteLine(a - b);
		Console.WriteLine(a * b);
		Console.WriteLine(a / b);
		Console.WriteLine(a % b);
		Console.WriteLine(a & b);
		Console.WriteLine(a | b);
		Console.WriteLine(a ^ b);
		Console.WriteLine(a << i);
		Console.WriteLine(a >> i);
		Console.WriteLine(a ?? b);
		Console.WriteLine(+a);
		Console.WriteLine(-a);
		Console.WriteLine(!a);
		Console.WriteLine(~a);
		// TODO:
		//Console.WriteLine(a++);
		//Console.WriteLine(a--);
		//Console.WriteLine(++a);
		//Console.WriteLine(--a);
		//Console.WriteLine((int?)a);
		a += b;
		a -= b;
		a *= b;
		a /= b;
		a %= b;
		a &= b;
		a |= b;
		a ^= b;
		a <<= i;
		a >>= i;
	}

	public static void StructValueComplex(TS? a, Func<TS> x, Func<int> i)
	{
		Console.WriteLine(a == x());
		Console.WriteLine(a != x());
		Console.WriteLine(a > x());

		Console.WriteLine(x() == a);
		Console.WriteLine(x() != a);
		Console.WriteLine(x() > a);

		Console.WriteLine(a + x());
		Console.WriteLine(a - x());
		Console.WriteLine(a * x());
		Console.WriteLine(a / x());
		Console.WriteLine(a % x());
		Console.WriteLine(a & x());
		Console.WriteLine(a | x());
		Console.WriteLine(a ^ x());
		Console.WriteLine(a << i());
		Console.WriteLine(a >> i());
		Console.WriteLine(a ?? x());
		a += x();
		a -= x();
		a *= x();
		a /= x();
		a %= x();
		a &= x();
		a |= x();
		a ^= x();
		a <<= i();
		a >>= i();

		Console.WriteLine(x() + a);
		(new TS?[0])[0] += x();
	}
}

// dummy structure for testing custom operators
[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct TS
{
	// unary
	public static TS operator +(TS a)
	{
		throw null;
	}
	public static TS operator -(TS a)
	{
		throw null;
	}
	public static TS operator !(TS a)
	{
		throw null;
	}
	public static TS operator ~(TS a)
	{
		throw null;
	}
	public static TS operator ++(TS a)
	{
		throw null;
	}
	public static TS operator --(TS a)
	{
		throw null;
	}

	public static explicit operator int(TS a)
	{
		throw null;
	}

	// binary
	public static TS operator +(TS a, TS b)
	{
		throw null;
	}
	public static TS operator -(TS a, TS b)
	{
		throw null;
	}
	public static TS operator *(TS a, TS b)
	{
		throw null;
	}
	public static TS operator /(TS a, TS b)
	{
		throw null;
	}
	public static TS operator %(TS a, TS b)
	{
		throw null;
	}
	public static TS operator &(TS a, TS b)
	{
		throw null;
	}
	public static TS operator |(TS a, TS b)
	{
		throw null;
	}
	public static TS operator ^(TS a, TS b)
	{
		throw null;
	}
	public static TS operator <<(TS a, int b)
	{
		throw null;
	}
	public static TS operator >>(TS a, int b)
	{
		throw null;
	}

	// comparisons
	public static bool operator ==(TS a, TS b)
	{
		throw null;
	}
	public static bool operator !=(TS a, TS b)
	{
		throw null;
	}
	public static bool operator <(TS a, TS b)
	{
		throw null;
	}
	public static bool operator <=(TS a, TS b)
	{
		throw null;
	}
	public static bool operator >(TS a, TS b)
	{
		throw null;
	}
	public static bool operator >=(TS a, TS b)
	{
		throw null;
	}

	public override bool Equals(object obj)
	{
		throw null;
	}
	public override int GetHashCode()
	{
		throw null;
	}
}
