//
// ElementType.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2010 Jb Evain
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

namespace Mono.Cecil.Metadata {

	enum ElementType : byte {
		None = 0x00,
		Void = 0x01,
		Boolean = 0x02,
		Char = 0x03,
		I1 = 0x04,
		U1 = 0x05,
		I2 = 0x06,
		U2 = 0x07,
		I4 = 0x08,
		U4 = 0x09,
		I8 = 0x0a,
		U8 = 0x0b,
		R4 = 0x0c,
		R8 = 0x0d,
		String = 0x0e,
		Ptr = 0x0f,   // Followed by <type> token
		ByRef = 0x10,   // Followed by <type> token
		ValueType = 0x11,   // Followed by <type> token
		Class = 0x12,   // Followed by <type> token
		Var = 0x13,   // Followed by generic parameter number
		Array = 0x14,   // <type> <rank> <boundsCount> <bound1>  <loCount> <lo1>
		GenericInst = 0x15,   // <type> <type-arg-count> <type-1> ... <type-n> */
		TypedByRef = 0x16,
		I = 0x18,   // System.IntPtr
		U = 0x19,   // System.UIntPtr
		FnPtr = 0x1b,   // Followed by full method signature
		Object = 0x1c,   // System.Object
		SzArray = 0x1d,   // Single-dim array with 0 lower bound
		MVar = 0x1e,   // Followed by generic parameter number
		CModReqD = 0x1f,   // Required modifier : followed by a TypeDef or TypeRef token
		CModOpt = 0x20,   // Optional modifier : followed by a TypeDef or TypeRef token
		Internal = 0x21,   // Implemented within the CLI
		Modifier = 0x40,   // Or'd with following element types
		Sentinel = 0x41,   // Sentinel for varargs method signature
		Pinned = 0x45,   // Denotes a local variable that points at a pinned object

		// special undocumented constants
		Type = 0x50,
		Boxed = 0x51,
		Enum = 0x55
	}
}
