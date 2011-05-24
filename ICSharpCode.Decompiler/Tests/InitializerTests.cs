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
using System.Collections.Generic;

public class InitializerTests
{
	enum MyEnum
	{
		a,
		b
	}
	
	enum MyEnum2
	{
		c,
		d
	}
	
	class Data
	{
		public InitializerTests.MyEnum a
		{
			get;
			set;
		}
		public List<InitializerTests.MyEnum2> PropertyList
		{
			get;
			set;
		}
		public List<InitializerTests.MyEnum2> FieldList = new List<InitializerTests.MyEnum2>();
		
		public InitializerTests.Data MoreData { get; set; }
	}

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
	
	public static void ArrayEnum()
	{
		X(Y(), new InitializerTests.MyEnum[] { InitializerTests.MyEnum.a, InitializerTests.MyEnum.b, InitializerTests.MyEnum.a, InitializerTests.MyEnum.b });
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
	
	public static void CollectionInitializerDictionaryWithEnumTypes()
	{
		X(Y(), new Dictionary<InitializerTests.MyEnum, InitializerTests.MyEnum2> {
		  	{ InitializerTests.MyEnum.a, InitializerTests.MyEnum2.c },
		  	{ InitializerTests.MyEnum.b, InitializerTests.MyEnum2.d }
		  });
	}
	
	public static void NotACollectionInitializer()
	{
		List<int> list = new List<int>();
		list.Add(1);
		list.Add(2);
		list.Add(3);
		X(Y(), list);
	}
	
	public static void ObjectInitializer()
	{
		X(Y(), new Data
		  {
		  	a = InitializerTests.MyEnum.a
		  });
	}
	
	public static void NotAObjectInitializer()
	{
		Data data = new InitializerTests.Data();
		data.a = InitializerTests.MyEnum.a;
		X(Y(), data);
	}
	
	public static void ObjectInitializerAssignCollectionToField()
	{
		X(Y(), new InitializerTests.Data
		  {
		  	a = InitializerTests.MyEnum.a,
		  	FieldList = new List<InitializerTests.MyEnum2>
		  	{
		  		InitializerTests.MyEnum2.c,
		  		InitializerTests.MyEnum2.d
		  	}
		  });
	}
	
	public static void ObjectInitializerAddToCollectionInField()
	{
		X(Y(), new InitializerTests.Data
		  {
		  	a = InitializerTests.MyEnum.a,
		  	FieldList =
		  	{
		  		InitializerTests.MyEnum2.c,
		  		InitializerTests.MyEnum2.d
		  	}
		  });
	}
	
	public static void ObjectInitializerAssignCollectionToProperty()
	{
		X(Y(), new InitializerTests.Data
		  {
		  	a = InitializerTests.MyEnum.a,
		  	PropertyList = new List<InitializerTests.MyEnum2>
		  	{
		  		InitializerTests.MyEnum2.c,
		  		InitializerTests.MyEnum2.d
		  	}
		  });
	}
	
	public static void ObjectInitializerAddToCollectionInProperty()
	{
		X(Y(), new InitializerTests.Data
		  {
		  	a = InitializerTests.MyEnum.a,
		  	PropertyList =
		  	{
		  		InitializerTests.MyEnum2.c,
		  		InitializerTests.MyEnum2.d
		  	}
		  });
	}
	
	public static void ObjectInitializerWithInitializationOfNestedObjects()
	{
		X(Y(), new InitializerTests.Data
		  {
		  	MoreData =
		  	{
		  		a = InitializerTests.MyEnum.a
		  	}
		  });
	}
}
