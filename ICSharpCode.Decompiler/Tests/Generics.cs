// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;

public static class Generics
{
	class MyArray<T>
	{
		private T[] arr;
		
		public MyArray(int capacity)
		{
			this.arr = new T[capacity];
		}
		
		public void Size(int capacity)
		{
			Array.Resize(ref this.arr, capacity);
		}
		
		public void Grow(int capacity)
		{
			if (capacity >= this.arr.Length)
			{
				this.Size(capacity);
			}
		}
	}
	
	public static void MethodWithConstraint<T, S>() where T : class, S where S : ICloneable, new()
	{
	}
	
	public static Dictionary<string, string>.KeyCollection.Enumerator GetEnumerator(Dictionary<string, string> d)
	{
		return d.Keys.GetEnumerator();
	}
}
