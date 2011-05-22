// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

public class IncrementDecrement
{
	[Flags]
	private enum MyEnum
	{
		None = 0,
		One = 1,
		Two = 2,
		Four = 4
	}
	
	public class MutableClass
	{
		public int Field;
		
		public int Property
		{
			get;
			set;
		}
		
		public uint this[string name]
		{
			get
			{
				return 0u;
			}
			set
			{
			}
		}
	}
	
	private IncrementDecrement.MyEnum enumField;
	public static int StaticField;
	
	public static int StaticProperty
	{
		get;
		set;
	}
	
	private IncrementDecrement.MutableClass M()
	{
		return new IncrementDecrement.MutableClass();
	}
	
	private int[,] Array()
	{
		return null;
	}
	
	private unsafe int* GetPointer()
	{
		return null;
	}
	
	public int PreIncrementInAddition(int i, int j)
	{
		return i + ++j;
	}
	
	public int PreIncrementArrayElement(int[] array, int pos)
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
	
	public int PreIncrementInstanceProperty()
	{
		return ++this.M().Property;
	}
	
	public int PreIncrementStaticField()
	{
		return ++IncrementDecrement.StaticField;
	}
	
	public int PreIncrementStaticProperty()
	{
		return ++IncrementDecrement.StaticProperty;
	}
	
//	public uint PreIncrementIndexer(string name)
//	{
//		return ++this.M()[name];
//	}
	
	public int PreIncrementByRef(ref int i)
	{
		return ++i;
	}
	
	public unsafe int PreIncrementByPointer()
	{
		return ++(*this.GetPointer());
	}
	
	public int PreIncrement2DArray()
	{
		return ++this.Array()[1, 2];
	}
	
	public int CompoundAssignInstanceField()
	{
		return this.M().Field *= 10;
	}
	
	public int CompoundAssignInstanceProperty()
	{
		return this.M().Property *= 10;
	}
	
	public int CompoundAssignStaticField()
	{
		return IncrementDecrement.StaticField ^= 100;
	}
	
	public int CompoundAssignStaticProperty()
	{
		return IncrementDecrement.StaticProperty &= 10;
	}
	
	public int CompoundAssignArrayElement1(int[] array, int pos)
	{
		return array[pos] *= 10;
	}
	
	public int CompoundAssignArrayElement2(int[] array)
	{
		return array[Environment.TickCount] *= 10;
	}
	
//	public uint CompoundAssignIndexer(string name)
//	{
//		return this.M()[name] -= 2;
//	}
	
	public int CompoundAssignIncrement2DArray()
	{
		return this.Array()[1, 2] %= 10;
	}
	
	public int CompoundAssignByRef(ref int i)
	{
		return i <<= 2;
	}
	
	public unsafe double CompoundAssignByPointer(double* ptr)
	{
		return *ptr /= 1.5;
	}
	
	public void CompoundAssignEnum()
	{
		this.enumField |= IncrementDecrement.MyEnum.Two;
		this.enumField &= ~IncrementDecrement.MyEnum.Four;
	}
	
	public int PostIncrementInAddition(int i, int j)
	{
		return i++ + j;
	}
	
	public void PostIncrementInlineLocalVariable(Func<int, int> f)
	{
		int num = 0;
		f(num++);
	}
	
	public int PostIncrementArrayElement(int[] array, int pos)
	{
		return array[pos]--;
	}
	
	public int PostIncrementStaticField()
	{
		return IncrementDecrement.StaticField++;
	}
	
	public int PostIncrementStaticProperty()
	{
		return IncrementDecrement.StaticProperty++;
	}
	
	public int PostIncrementInstanceField(IncrementDecrement.MutableClass m)
	{
		return m.Field++;
	}
	
//	public uint PostIncrementIndexer(string name)
//	{
//		return this.M()[name]++;
//	}

//	public unsafe int PostIncrementOfPointer(int* ptr)
//	{
//		return *(ptr++);
//	}

	public int PostIncrementInstanceField()
	{
		return this.M().Field--;
	}
	
	public int PostIncrementInstanceProperty()
	{
		return this.M().Property--;
	}
	
	public int PostIncrement2DArray()
	{
		return this.Array()[IncrementDecrement.StaticField, IncrementDecrement.StaticProperty]++;
	}
	
	public int PostIncrementByRef(ref int i)
	{
		return i++;
	}
	
	public unsafe int PostIncrementByPointer()
	{
		return (*this.GetPointer())++;
	}
}
