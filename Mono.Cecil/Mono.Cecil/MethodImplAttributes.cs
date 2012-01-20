//
// MethodImplAttributes.cs
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

using System;

namespace Mono.Cecil {

	[Flags]
	public enum MethodImplAttributes : ushort {
		CodeTypeMask		= 0x0003,
		IL					= 0x0000,	// Method impl is CIL
		Native				= 0x0001,	// Method impl is native
		OPTIL				= 0x0002,	// Reserved: shall be zero in conforming implementations
		Runtime				= 0x0003,	// Method impl is provided by the runtime

		ManagedMask			= 0x0004,	// Flags specifying whether the code is managed or unmanaged
		Unmanaged			= 0x0004,	// Method impl is unmanaged, otherwise managed
		Managed				= 0x0000,	// Method impl is managed

		// Implementation info and interop
		ForwardRef			= 0x0010,	// Indicates method is defined; used primarily in merge scenarios
		PreserveSig			= 0x0080,	// Reserved: conforming implementations may ignore
		InternalCall		= 0x1000,	// Reserved: shall be zero in conforming implementations
		Synchronized		= 0x0020,	// Method is single threaded through the body
		NoOptimization		= 0x0040,	// Method is not optimized by the JIT.
		NoInlining			= 0x0008,	// Method may not be inlined
	}
}
