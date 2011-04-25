//
// NativeType.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2011 Jb Evain
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

namespace Mono.Cecil {

	public enum NativeType {
		None = 0x66,

		Boolean = 0x02,
		I1 = 0x03,
		U1 = 0x04,
		I2 = 0x05,
		U2 = 0x06,
		I4 = 0x07,
		U4 = 0x08,
		I8 = 0x09,
		U8 = 0x0a,
		R4 = 0x0b,
		R8 = 0x0c,
		LPStr = 0x14,
		Int = 0x1f,
		UInt = 0x20,
		Func = 0x26,
		Array = 0x2a,

		// Msft specific
		Currency = 0x0f,
		BStr = 0x13,
		LPWStr = 0x15,
		LPTStr = 0x16,
		FixedSysString = 0x17,
		IUnknown = 0x19,
		IDispatch = 0x1a,
		Struct = 0x1b,
		IntF = 0x1c,
		SafeArray = 0x1d,
		FixedArray = 0x1e,
		ByValStr = 0x22,
		ANSIBStr = 0x23,
		TBStr = 0x24,
		VariantBool = 0x25,
		ASAny = 0x28,
		LPStruct = 0x2b,
		CustomMarshaler = 0x2c,
		Error = 0x2d,
		Max = 0x50
	}
}
