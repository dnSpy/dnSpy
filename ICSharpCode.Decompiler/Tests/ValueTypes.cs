// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

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
}
