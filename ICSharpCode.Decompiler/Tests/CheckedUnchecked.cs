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

public class CheckedUnchecked
{
	public int Operators(int a, int b)
	{
		int num = checked(a + b);
		int num2 = a + b;
		int num3 = checked(a - b);
		int num4 = a - b;
		int num5 = checked(a * b);
		int num6 = a * b;
		int num7 = a / b;
		int num8 = a % b;
		// The division operators / and % only exist in one form (checked vs. unchecked doesn't matter for them)
		return num * num2 * num3 * num4 * num5 * num6 * num7 * num8;
	}
	
	public int Cast(int a)
	{
		short num = checked((short)a);
		short num2 = (short)a;
		byte b = checked((byte)a);
		byte b2 = (byte)a;
		return num * num2 * b * b2;
	}
	
	public void ForWithCheckedIteratorAndUncheckedBody(int n)
	{
		checked
		{
			for (int i = n + 1; i < n + 1; i++)
			{
				n = unchecked(i * i);
			}
		}
	}
	
	public void ForWithCheckedInitializerAndUncheckedIterator(int n)
	{
		checked
		{
			int i = n;
			for (i -= 10; i < n; i = unchecked(i + 1))
			{
				n--;
			}
		}
	}
	public void ObjectCreationInitializerChecked() 
	{
		this.TestHelp(new
		{
			x = 0,
			l = 0
		}, n => checked(new
		{
			x = n.x + 1, 
			l = n.l + 1
		}));
	}

	public void ObjectCreationWithOneFieldChecked() 
	{
		this.TestHelp(new 
		{
			x = 0,
			l = 0
		}, n => new 
		{
			x = checked(n.x + 1), 
			l = n.l + 1 
		});
	}

	
	public void ArrayInitializerChecked() 
	{
		this.TestHelp<int[]>(new int[]
		{
			1,
			2
		}, (int[] n) => checked(new int[]
		{
			n[0] + 1,
			n[1] + 1
		}));
	}

	public T TestHelp<T>(T t, Func<T, T> f)
	{
		return f(t);
	}
	
	public void CheckedInArrayCreationArgument(int a, int b)
	{
		Console.WriteLine(new int[checked(a + b)]);
	}
}
