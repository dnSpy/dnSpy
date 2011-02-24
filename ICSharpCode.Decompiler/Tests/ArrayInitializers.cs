// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

public class ArrayInitializers
{
	// Helper methods used to ensure array initializers used within expressions work correctly
	static void X(object a, object b)
	{
	}
	
	static object Y()
	{
		return null;
	}
	
	public static void Array1()
	{
		X(Y(), new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
	}
	
	public static void Array2(int a, int b, int c)
	{
		X(Y(), new int[] { a, b, c });
	}
	
	public static void NestedArray(int a, int b, int c)
	{
		X(Y(), new int[][] {
		  	new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 },
		  	new int[] { a, b, c },
		  	new int[] { 1, 2, 3, 4, 5, 6 }
		  });
	}
}
