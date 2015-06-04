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
	
	public unsafe double ConvertLongToDouble(long d)
	{
		return *(double*)(&d);
	}
	
	public unsafe int ConvertFloatToInt(float d)
	{
		return *(int*)(&d);
	}
	
	public unsafe float ConvertIntToFloat(int d)
	{
		return *(float*)(&d);
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
	
	public unsafe void AddressInMultiDimensionalArray(double[,] matrix)
	{
		fixed (double* ptr = &matrix[1, 2])
		{
			this.PointerReferenceExpression(ptr);
		}
	}
	
	public unsafe void FixedStringAccess(string text)
	{
		fixed (char* ptr = text)
		{
			char* ptr2 = ptr;
			while (*ptr2 != '\0')
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

	public unsafe int* PointerArithmetic(int* p)
	{
		return p + 2;
	}

	public unsafe byte* PointerArithmetic2(long* p, int y, int x)
	{
		return (byte*)((short*)p + (y * x));
	}

	public unsafe long* PointerArithmetic3(long* p)
	{
		return (long*)((byte*)p + 3);
	}

	public unsafe long* PointerArithmetic4(void* p)
	{
		return (long*)((byte*)p + 3);
	}

	public unsafe int PointerArithmetic5(void* p, byte* q, int i)
	{
		return (int)(q[i] + *(byte*)p);
	}

	public unsafe int PointerSubtraction(long* p, long* q)
	{
		return (int)((long)(p - q));
	}

	public unsafe int PointerSubtraction2(long* p, short* q)
	{
		return (int)((long)((byte*)p - (byte*)q));
	}

	public unsafe int PointerSubtraction3(void* p, void* q)
	{
		return (int)((long)((byte*)p - (byte*)q));
	}

	unsafe ~UnsafeCode()
	{
		this.PassPointerAsRefParameter(this.NullPointer);
	}
}
