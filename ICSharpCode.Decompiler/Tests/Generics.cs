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

public static class Generics
{
	public class MyArray<T>
	{
		public class NestedClass<Y>
		{
			public T Item1;
			public Y Item2;
		}
		
		public enum NestedEnum
		{
			A,
			B
		}
		
		private T[] arr;
		
		public MyArray(int capacity)
		{
			this.arr = new T[capacity];
		}
		
		public void Size(int capacity)
		{
			Array.Resize<T>(ref this.arr, capacity);
		}
		
		public void Grow(int capacity)
		{
			if (capacity >= this.arr.Length)
			{
				this.Size(capacity);
			}
		}
	}
	
	public interface IInterface
	{
		void Method1<T>() where T : class;
		void Method2<T>() where T : class;
	}
	
	public abstract class Base : Generics.IInterface
	{
		// constraints must be repeated on implicit interface implementation
		public abstract void Method1<T>() where T : class;
		
		// constraints must not be specified on explicit interface implementation
		void Generics.IInterface.Method2<T>()
		{
		}
	}
	
	public class Derived : Generics.Base
	{
		// constraints are inherited automatically and must not be specified
		public override void Method1<T>()
		{
		}
	}
	
	private const Generics.MyArray<string>.NestedEnum enumVal = Generics.MyArray<string>.NestedEnum.A;
	private static Type type1 = typeof(List<>);
	private static Type type2 = typeof(Generics.MyArray<>);
	private static Type type3 = typeof(List<>.Enumerator);
	private static Type type4 = typeof(Generics.MyArray<>.NestedClass<>);
	private static Type type5 = typeof(List<int>[]);
	private static Type type6 = typeof(Generics.MyArray<>.NestedEnum);
	
	public static void MethodWithConstraint<T, S>() where T : class, S where S : ICloneable, new()
	{
	}
	
	public static void MethodWithStructConstraint<T>() where T : struct
	{
	}
	
	private static void MultidimensionalArray<T>(T[,] array)
	{
		array[0, 0] = array[0, 1];
	}

	public static Dictionary<string, string>.KeyCollection.Enumerator GetEnumerator(Dictionary<string, string> d, Generics.MyArray<string>.NestedClass<int> nc)
	{
		// Tests references to inner classes in generic classes
		return d.Keys.GetEnumerator();
	}
	
	public static bool IsString<T>(T input)
	{
		return input is string;
	}
	
	public static string AsString<T>(T input)
	{
		return input as string;
	}
	
	public static string CastToString<T>(T input)
	{
		return (string)((object)input);
	}
	
	public static T CastFromString<T>(string input)
	{
		return (T)((object)input);
	}
	
	public static bool IsInt<T>(T input)
	{
		return input is int;
	}
	
	public static int CastToInt<T>(T input)
	{
		return (int)((object)input);
	}
	
	public static T CastFromInt<T>(int input)
	{
		return (T)((object)input);
	}
	
	public static bool IsNullableInt<T>(T input)
	{
		return input is int?;
	}
	
	public static int? AsNullableInt<T>(T input)
	{
		return input as int?;
	}
	
	public static int? CastToNullableInt<T>(T input)
	{
		return (int?)((object)input);
	}
	
	public static T CastFromNullableInt<T>(int? input)
	{
		return (T)((object)input);
	}
}
