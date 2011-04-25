// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

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
	
	public static void MethodWithConstraint<T, S>() where T : class, S where S : ICloneable, new()
	{
	}
	
	public static void MethodWithStructConstraint<T>() where T : struct
	{
	}
	
	public static Dictionary<string, string>.KeyCollection.Enumerator GetEnumerator(Dictionary<string, string> d, Generics.MyArray<string>.NestedClass<int> nc)
	{
		// Tests references to inner classes in generic classes
		return d.Keys.GetEnumerator();
	}
}
