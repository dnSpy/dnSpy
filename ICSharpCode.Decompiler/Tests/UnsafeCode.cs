// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

public class UnsafeCode
{
	public unsafe int* NullPointer
	{
		get
		{
			return null;
		}
	}
	
	public unsafe long ConvertDoubleToLong(double d)
	{
		return *(long*)(&d);
	}
	
	public unsafe void PassRefParameterAsPointer(ref int p)
	{
		fixed (int* ptr = &p)
		{
			this.PassPointerAsRefParameter(ptr);
		}
	}
	
	public unsafe void PassPointerAsRefParameter(int* p)
	{
		this.PassRefParameterAsPointer(ref *p);
	}
	
	public unsafe void FixedStringAccess(string text)
	{
		fixed (char* ptr = text)
		{
			char* ptr2 = ptr;
			while (*ptr2 != 0)
			{
				*ptr2 = 'A';
				ptr2++;
			}
		}
	}
	
	public unsafe void PutDoubleIntoLongArray1(long[] array, int index, double val)
	{
		fixed (long* ptr = array) 
		{
			((double*)ptr)[index] = val;
		}
	}
	
	public unsafe void PutDoubleIntoLongArray2(long[] array, int index, double val)
	{
		fixed (long* ptr = &array[index]) 
		{
			*(double*)ptr = val;
		}
	}
	
	public unsafe string PointerReferenceExpression(double* d)
	{
		return d->ToString();
	}
	
	public unsafe void FixMultipleStrings(string text)
	{
		fixed (char* ptr = text, userName = Environment.UserName, ptr2 = text) 
		{
			*ptr = 'c';
			*userName = 'd';
			*ptr2 = 'e';
		}
	}
	
	public unsafe string StackAlloc(int count)
	{
		char* ptr = stackalloc char[count];
		for (int i = 0; i < count; i++) 
		{
			ptr[i] = (char)i;
		}
		return this.PointerReferenceExpression((double*)ptr);
	}
	
	unsafe ~UnsafeCode()
	{
		this.PassPointerAsRefParameter(this.NullPointer);
	}
}
