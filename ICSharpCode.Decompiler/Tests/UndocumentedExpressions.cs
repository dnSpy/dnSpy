// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

public class UndocumentedExpressions
{
	public static int GetArgCount(__arglist)
	{
		ArgIterator argIterator = new ArgIterator(__arglist);
		return argIterator.GetRemainingCount();
	}
	
	public static void MakeTypedRef(object o)
	{
		TypedReference tr = __makeref(o);
		UndocumentedExpressions.AcceptTypedRef(tr);
	}
	
	private static void AcceptTypedRef(TypedReference tr)
	{
		Console.WriteLine("Value is: " + __refvalue(tr, object).ToString());
		Console.WriteLine("Type is: " + __reftype(tr).Name);
		__refvalue(tr, object) = 1;
	}
}
