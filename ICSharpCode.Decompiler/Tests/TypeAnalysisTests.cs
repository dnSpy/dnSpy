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
	
	public int GetHashCode(long num)
	{
		return (int)num ^ (int)(num >> 32);
	}
	
	public int ShiftByte(byte num)
	{
		return (int)num << 8;
	}
	
	public int RShiftByte(byte num)
	{
		return num >> 8;
	}
	
	public int RShiftByteWithSignExtension(byte num)
	{
		return (sbyte)num >> 8;
	}
	
	public int RShiftSByte(sbyte num)
	{
		return num >> 8;
	}
	
	public int RShiftSByteWithZeroExtension(sbyte num)
	{
		return (byte)num >> 8;
	}
}
