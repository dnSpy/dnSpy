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

public class TypeAnalysisTests
{
	public byte SubtractFrom256(byte b)
	{
		return (byte)(256 - (int)b);
	}
	
	#region Shift
	public int LShiftInteger(int num1, int num2)
	{
		return num1 << num2;
	}
	
	public uint LShiftUnsignedInteger(uint num1, uint num2)
	{
		return num1 << (int)num2;
	}
	
	public long LShiftLong(long num1, long num2)
	{
		return num1 << (int)num2;
	}
	
	public ulong LShiftUnsignedLong(ulong num1, ulong num2)
	{
		return num1 << (int)num2;
	}
	
	public int RShiftInteger(int num1, int num2)
	{
		return num1 >> num2;
	}
	
	public uint RShiftUnsignedInteger(uint num1, int num2)
	{
		return num1 >> num2;
	}
	
	public long RShiftLong(long num1, long num2)
	{
		return num1 >> (int)num2;
	}
	
	public ulong RShiftUnsignedLong(ulong num1, ulong num2)
	{
		return num1 >> (int)num2;
	}
	
	public int ShiftByte(byte num)
	{
		return (int)num << 8;
	}
	
	public int RShiftByte(byte num)
	{
		return num >> 8;
	}
	
	public uint RShiftByteWithZeroExtension(byte num)
	{
		return (uint)num >> 8;
	}
	
	public int RShiftByteAsSByte(byte num)
	{
		return (sbyte)num >> 8;
	}
	
	public int RShiftSByte(sbyte num)
	{
		return num >> 8;
	}
	
	public uint RShiftSByteWithZeroExtension(sbyte num)
	{
		return (uint)num >> 8;
	}
	
	public int RShiftSByteAsByte(sbyte num)
	{
		return (byte)num >> 8;
	}
	#endregion
	
	public int GetHashCode(long num)
	{
		return (int)num ^ (int)(num >> 32);
	}

	public void TernaryOp(Random a, Random b, bool c)
	{
		if ((c ? a : b) == null)
		{
			Console.WriteLine();
		}
	}

	public void OperatorIs(object o)
	{
		Console.WriteLine(o is Random);
		Console.WriteLine(!(o is Random));
	}
	
	public byte[] CreateArrayWithInt(int length)
	{
		return new byte[length];
	}
	
	public byte[] CreateArrayWithLong(long length)
	{
		return new byte[length];
	}
	
	public byte[] CreateArrayWithUInt(uint length)
	{
		return new byte[length];
	}
	
	public byte[] CreateArrayWithULong(ulong length)
	{
		return new byte[length];
	}
	
	public StringComparison EnumDiffNumber(StringComparison data)
	{
		return data - 1;
	}
	
	public int EnumDiff(StringComparison a, StringComparison b)
	{
		return Math.Abs(a - b);
	}
}
