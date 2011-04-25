//
// PInvokeAttributes.cs
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
	public enum PInvokeAttributes : ushort {
		NoMangle			= 0x0001,	// PInvoke is to use the member name as specified

		// Character set
		CharSetMask			= 0x0006,
		CharSetNotSpec		= 0x0000,
		CharSetAnsi			= 0x0002,
		CharSetUnicode		= 0x0004,
		CharSetAuto			= 0x0006,

		SupportsLastError	= 0x0040,	// Information about target function. Not relevant for fields

		// Calling convetion
		CallConvMask		= 0x0700,
		CallConvWinapi		= 0x0100,
		CallConvCdecl		= 0x0200,
		CallConvStdCall		= 0x0300,
		CallConvThiscall	= 0x0400,
		CallConvFastcall	= 0x0500,

		BestFitMask			= 0x0030,
		BestFitEnabled		= 0x0010,
		BestFitDisabled		= 0x0020,

		ThrowOnUnmappableCharMask = 0x3000,
		ThrowOnUnmappableCharEnabled = 0x1000,
		ThrowOnUnmappableCharDisabled = 0x2000,
	}
}
