// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

public class CheckedUnchecked
{
	public void Operators(int a, int b)
	{
		int c1 = checked(a + b);
		int u1 = unchecked(a + b);
		int c2 = checked(a - b);
		int u2 = unchecked(a - b);
		int c3 = checked(a * b);
		int u3 = unchecked(a * b);
		int c4 = checked(a / b);
		int u4 = unchecked(a / b);
		int c5 = checked(a % b);
		int u5 = unchecked(a % b);
	}
	
	public void ForWithCheckedIteratorAndUncheckedBody(int n)
	{
		checked {
			for (int i = n + 1; i < n + 1; i++) {
				unchecked {
					n = i * i;
				}
			}
		}
	}
}
