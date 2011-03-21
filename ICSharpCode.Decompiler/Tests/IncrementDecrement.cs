// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

public class IncrementDecrement
{
	public class MutableClass
	{
		public int Field;
	}
	
	private IncrementDecrement.MutableClass M()
	{
		return new IncrementDecrement.MutableClass();
	}
	
	public int PreIncrementInAddition(int i, int j)
	{
		return i + ++j;
	}
	
	public int PreDecrementArrayElement(int[] array, int pos)
	{
		return --array[pos];
	}
	
	public int PreIncrementInstanceField()
	{
		return ++this.M().Field;
	}
	
	public int PreIncrementInstanceField2(IncrementDecrement.MutableClass m)
	{
		return ++m.Field;
	}
	
	public int CompoundMultiplyInstanceField()
	{
		return this.M().Field *= 10;
	}
	
	public int CompoundMultiplyArrayElement1(int[] array, int pos)
	{
		return array[pos] *= 10;
	}
	
	public int CompoundMultiplyArrayElement2(int[] array)
	{
		return array[Environment.TickCount] *= 10;
	}
	
//	public int PostIncrementInAddition(int i, int j)
//	{
//		return i++ + j;
//	}
//	
//	public int PostDecrementArrayElement(int[] array, int pos)
//	{
//		return array[pos]--;
//	}
}
