// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
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
using System.Runtime.InteropServices;

// P/Invoke and marshalling attribute tests
public class PInvoke
{
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 2)]
	public struct MarshalAsTest
	{
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
		public uint[] FixedArray;
		
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4, ArraySubType = UnmanagedType.Bool)]
		public int[] FixedBoolArray;
		
		[MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BSTR)]
		public string[] SafeBStrArray;
		
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 8)]
		public string FixedString;
	}
	
	[StructLayout(LayoutKind.Explicit)]
	public struct Rect
	{
		[FieldOffset(0)]
		public int left;
		[FieldOffset(4)]
		public int top;
		[FieldOffset(8)]
		public int right;
		[FieldOffset(12)]
		public int bottom;
	}
	
	public static decimal MarshalAttributesOnPropertyAccessors
	{
		[return: MarshalAs(UnmanagedType.Currency)]
		get
		{
			return 0m;
		}
		[param: MarshalAs(UnmanagedType.Currency)]
		set
		{
		}
	}
	
	[DllImport("xyz.dll", CharSet = CharSet.Auto)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool Method([MarshalAs(UnmanagedType.LPStr)] string input);
	
	[DllImport("xyz.dll")]
	private static extern void New1(int ElemCnt, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] int[] ar);
	
	[DllImport("xyz.dll")]
	private static extern void New2([MarshalAs(UnmanagedType.LPArray, SizeConst = 128)] int[] ar);
	
	[DllImport("xyz.dll")]
	private static extern void New3([MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.Bool, SizeConst = 64, SizeParamIndex = 1)] int[] ar);
	
	public void CustomMarshal1([MarshalAs(UnmanagedType.CustomMarshaler, MarshalType = "MyCompany.MyMarshaler")] object o)
	{
	}
	
	public void CustomMarshal2([MarshalAs(UnmanagedType.CustomMarshaler, MarshalType = "MyCompany.MyMarshaler", MarshalCookie = "Cookie")] object o)
	{
	}
	
	[DllImport("ws2_32.dll", SetLastError = true)]
	internal static extern IntPtr ioctlsocket([In] IntPtr socketHandle, [In] int cmd, [In] [Out] ref int argp);
	
	public void CallMethodWithInOutParameter()
	{
		int num = 0;
		PInvoke.ioctlsocket(IntPtr.Zero, 0, ref num);
	}
}
