// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;

public class InitializerTests
{
	// Helper methods used to ensure initializers used within expressions work correctly
	static void X(object a, object b)
	{
	}
	
	static object Y()
	{
		return null;
	}
	
	#region Array Initializers
	public static void Array1()
	{
		X(Y(), new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
	}
	
	public static void Array2(int a, int b, int c)
	{
		X(Y(), new int[] { a, 0, b, 0, c });
	}
	
	public static void NestedArray(int a, int b, int c)
	{
		X(Y(), new int[][] {
		  	new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 },
		  	new int[] { a, b, c },
		  	new int[] { 1, 2, 3, 4, 5, 6 }
		  });
	}
	
	public static void ArrayBoolean()
	{
		X(Y(), new bool[] { true, false, true, false, false, false, true, true });
	}
	
	public static void ArrayByte()
	{
		X(Y(), new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 254, 255 });
	}
	
	public static void ArraySByte()
	{
		X(Y(), new sbyte[] { -128, -127, 0, 1, 2, 3, 4, 127 });
	}
	
	public static void ArrayShort()
	{
		X(Y(), new short[] { -32768, -1, 0, 1, 32767 });
	}
	
	public static void ArrayUShort()
	{
		X(Y(), new ushort[] { 0, 1, 32767, 32768, 65534, 65535 });
	}
	
	public static void ArrayInt()
	{
		X(Y(), new int[] { 1, -2, 2000000000, 4, 5, -6, 7, 8, 9, 10 });
	}
	
	public static void ArrayUInt()
	{
		X(Y(), new uint[] { 1, 2000000000, 3000000000, 4, 5, 6, 7, 8, 9, 10 });
	}
	
	public static void ArrayLong()
	{
		X(Y(), new long[] { -4999999999999999999, -1, 0, 1, 4999999999999999999 });
	}
	
	public static void ArrayULong()
	{
		X(Y(), new ulong[] { 1, 2000000000, 3000000000, 4, 5, 6, 7, 8, 4999999999999999999, 9999999999999999999 });
	}
	
	public static void ArrayFloat()
	{
		X(Y(), new float[] { -1.5f, 0f, 1.5f, float.NegativeInfinity, float.PositiveInfinity, float.NaN });
	}
	
	public static void ArrayDouble()
	{
		X(Y(), new double[] { -1.5, 0.0, 1.5, double.NegativeInfinity, double.PositiveInfinity, double.NaN });
	}
	
	public static void ArrayDecimal()
	{
		X(Y(), new decimal[] { -100m, 0m, 100m, decimal.MinValue, decimal.MaxValue, 0.0000001m });
	}
	
	public static void ArrayString()
	{
		X(Y(), new string[] { "", null, "Hello", "World" });
	}
	#endregion
	
	public static void CollectionInitializerList()
	{
		X(Y(), new List<int> { 1, 2, 3 });
	}
	
	public static void CollectionInitializerDictionary()
	{
		X(Y(), new Dictionary<string, int> {
		  	{ "First", 1 },
		  	{ "Second", 2 },
		  	{ "Third" , 3 }
		  });
	}
}
