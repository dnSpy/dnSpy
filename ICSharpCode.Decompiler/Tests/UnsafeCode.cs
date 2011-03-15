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
}
