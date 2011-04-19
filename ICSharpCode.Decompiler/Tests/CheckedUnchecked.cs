// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

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
}
