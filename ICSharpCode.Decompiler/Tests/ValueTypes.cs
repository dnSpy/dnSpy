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

public static class ValueTypes
{
	public struct S
	{
		public int Field;
		
		public S(int field)
		{
			this.Field = field;
		}
		
		public void SetField()
		{
			this.Field = 5;
		}
		
		public void MethodCalls()
		{
			this.SetField();
			ValueTypes.S.Test(this);
			ValueTypes.S.Test(ref this);
		}
		
		private static void Test(ValueTypes.S byVal)
		{
		}
		
		private static void Test(ref ValueTypes.S byRef)
		{
		}
	}
	
	private static readonly ValueTypes.S ReadOnlyS = default(ValueTypes.S);
	private static ValueTypes.S MutableS = default(ValueTypes.S);
	private static volatile int VolatileInt;
	
	public static void CallMethodViaField()
	{
		ValueTypes.ReadOnlyS.SetField();
		ValueTypes.MutableS.SetField();
		ValueTypes.S mutableS = ValueTypes.MutableS;
		mutableS.SetField();
	}
	
	public static ValueTypes.S InitObj1()
	{
		ValueTypes.S result = default(ValueTypes.S);
		ValueTypes.MakeArray();
		return result;
	}
	
	public static ValueTypes.S InitObj2()
	{
		return default(ValueTypes.S);
	}
	
	public static void InitObj3(out ValueTypes.S p)
	{
		p = default(ValueTypes.S);
	}
	
	public static ValueTypes.S CallValueTypeCtor1()
	{
		return new ValueTypes.S(10);
	}
	
	public static ValueTypes.S CallValueTypeCtor2()
	{
		ValueTypes.S result = new ValueTypes.S(10);
		return result;
	}
	
	public static ValueTypes.S Copy1(ValueTypes.S p)
	{
		return p;
	}
	
	public static ValueTypes.S Copy2(ref ValueTypes.S p)
	{
		return p;
	}
	
	public static void Copy3(ValueTypes.S p, out ValueTypes.S o)
	{
		o = p;
	}
	
	public static void Copy4(ref ValueTypes.S p, out ValueTypes.S o)
	{
		o = p;
	}
	
	public static void Copy4b(ref ValueTypes.S p, out ValueTypes.S o)
	{
		// test passing through by-ref arguments
		ValueTypes.Copy4(ref p, out o);
	}
	
	public static void Issue56(int i, out string str)
	{
		str = "qq";
		str += i.ToString();
	}
	
	public static void CopyAroundAndModifyField(ValueTypes.S s)
	{
		ValueTypes.S s2 = s;
		s2.Field += 10;
		s = s2;
	}
	
	private static int[] MakeArray()
	{
		return null;
	}
	
	public static void IncrementArrayLocation()
	{
		ValueTypes.MakeArray()[Environment.TickCount]++;
	}
	
	public static bool Is(object obj)
	{
		return obj is ValueTypes.S;
	}
	
	public static bool IsNullable(object obj)
	{
		return obj is ValueTypes.S?;
	}
	
	public static ValueTypes.S? As(object obj)
	{
		return obj as ValueTypes.S?;
	}
	
	public static ValueTypes.S OnlyChangeTheCopy(ValueTypes.S p)
	{
		ValueTypes.S s = p;
		s.SetField();
		return p;
	}
	
	public static void UseRefBoolInCondition(ref bool x)
	{
		if (x) 
		{
			Console.WriteLine("true");
		}
	}

	public static void CompareNotEqual0IsReallyNotEqual(IComparable<int> a)
	{
		if (a.CompareTo(0) != 0)
		{
			Console.WriteLine("true");
		}
	}

	public static void CompareEqual0IsReallyEqual(IComparable<int> a)
	{
		if (a.CompareTo(0) == 0)
		{
			Console.WriteLine("true");
		}
	}

}
