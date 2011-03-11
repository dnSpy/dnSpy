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
			Test(this);
			Test(ref this);
		}
		
		static void Test(S byVal)
		{
		}
		
		static void Test(ref S byRef)
		{
		}
	}
	
	public static S InitObj1()
	{
		S s = default(S);
		return s;
	}
	
	public static S InitObj2()
	{
		return default(S);
	}
	
	public static void InitObj3(out S p)
	{
		p = default(S);
	}
	
	public static S CallValueTypeCtor1()
	{
		return new S(10);
	}
	
	public static S CallValueTypeCtor2()
	{
		S s = new S(10);
		return s;
	}
	
	public static S Copy1(S p)
	{
		return p;
	}
	
	public static S Copy2(ref S p)
	{
		return p;
	}
	
	public static void Copy3(S p, out S o)
	{
		o = p;
	}
	
	public static void Copy4(ref S p, out S o)
	{
		o = p;
	}
	
	public static void Copy4b(ref S p, out S o)
	{
		// test passing through by-ref arguments
		Copy4(ref p, out o);
	}
	
	public static void Issue56(int i, out string str)
	{
		str = "qq";
		str += i.ToString();
	}
	
	public static void CopyAroundAndModifyField(S s)
	{
		S locS = s;
		locS.Field += 10;
		s = locS;
	}
	
	static int[] MakeArray()
	{
		return null;
	}
	
	public static void IncrementArrayLocation()
	{
		MakeArray()[Environment.TickCount]++;
	}
}
