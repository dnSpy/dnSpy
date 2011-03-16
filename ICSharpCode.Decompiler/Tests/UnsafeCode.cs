// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

public class UnsafeCode
{
	public unsafe long ConvertDoubleToLong(double d)
	{
		return *(long*)&d;
	}
	
	public unsafe int* NullPointer {
		get {
			return null;
		}
	}
	
	public unsafe void PassRefParameterAsPointer(ref int p)
	{
		fixed (int* ptr = &p)
			PassPointerAsRefParameter(ptr);
	}
	
	public unsafe void PassPointerAsRefParameter(int* p)
	{
		PassRefParameterAsPointer(ref *p);
	}
	
	public unsafe void FixedStringAccess(string text)
	{
		fixed (char* c = text) {
			char* tmp = c;
			while (*tmp != 0) {
				*tmp = 'A';
				tmp++;
			}
		}
	}
	
	public unsafe void PutDoubleIntoLongArray1(long[] array, int index, double val)
	{
		fixed (long* l = array) {
			((double*)l)[index] = val;
		}
	}
	
	public unsafe void PutDoubleIntoLongArray2(long[] array, int index, double val)
	{
		fixed (long* l = &array[index]) {
			*(double*)l = val;
		}
	}
	
	public unsafe string PointerReferenceExpression(double* d)
	{
		return d->ToString();
	}
	
	public unsafe void FixMultipleStrings(string text)
	{
		fixed (char* c = text, d = Environment.UserName, e = text) {
			*c = 'c';
			*d = 'd';
			*e = 'e';
		}
	}
	
	public unsafe string StackAlloc(int count)
	{
		char* a = stackalloc char[count];
		for (int i = 0; i < count; i++) {
			a[i] = (char)i;
		}
		return PointerReferenceExpression((double*)a);
	}
}
