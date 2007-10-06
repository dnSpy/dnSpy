//
// NativeType.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// (C) 2005 Jb Evain
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
		NONE = 0x66,

		BOOLEAN = 0x02,
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
		LPSTR = 0x14,
		INT = 0x1f,
		UINT = 0x20,
		FUNC = 0x26,
		ARRAY = 0x2a,

		// Msft specific
		CURRENCY = 0x0f,
		BSTR = 0x13,
		LPWSTR = 0x15,
		LPTSTR = 0x16,
		FIXEDSYSSTRING = 0x17,
		IUNKNOWN = 0x19,
		IDISPATCH = 0x1a,
		STRUCT = 0x1b,
		INTF = 0x1c,
		SAFEARRAY = 0x1d,
		FIXEDARRAY = 0x1e,
		BYVALSTR = 0x22,
		ANSIBSTR = 0x23,
		TBSTR = 0x24,
		VARIANTBOOL = 0x25,
		ASANY = 0x28,
		LPSTRUCT = 0x2b,
		CUSTOMMARSHALER = 0x2c,
		ERROR = 0x2d,
		MAX = 0x50
	}
}
