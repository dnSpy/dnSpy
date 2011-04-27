//
// MethodAttributes.cs
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
	public enum MethodAttributes : ushort {
		MemberAccessMask	= 0x0007,
		CompilerControlled	= 0x0000,	// Member not referenceable
		Private				= 0x0001,	// Accessible only by the parent type
		FamANDAssem			= 0x0002,	// Accessible by sub-types only in this Assembly
		Assembly				= 0x0003,	// Accessibly by anyone in the Assembly
		Family				= 0x0004,	// Accessible only by type and sub-types
		FamORAssem			= 0x0005,	// Accessibly by sub-types anywhere, plus anyone in assembly
		Public				= 0x0006,	// Accessibly by anyone who has visibility to this scope

		Static				= 0x0010,	// Defined on type, else per instance
		Final				= 0x0020,	// Method may not be overridden
		Virtual				= 0x0040,	// Method is virtual
		HideBySig			= 0x0080,	// Method hides by name+sig, else just by name

		VtableLayoutMask	= 0x0100,	// Use this mask to retrieve vtable attributes
		ReuseSlot			= 0x0000,	// Method reuses existing slot in vtable
		NewSlot				= 0x0100,	// Method always gets a new slot in the vtable

		CheckAccessOnOverride = 0x0200,   // Method can only be overriden if also accessible
		Abstract			= 0x0400,	// Method does not provide an implementation
		SpecialName			= 0x0800,	// Method is special

		// Interop Attributes
		PInvokeImpl			= 0x2000,	// Implementation is forwarded through PInvoke
		UnmanagedExport		= 0x0008,	// Reserved: shall be zero for conforming implementations

		// Additional flags
		RTSpecialName		= 0x1000,	// CLI provides 'special' behavior, depending upon the name of the method
		HasSecurity			= 0x4000,	// Method has security associate with it
		RequireSecObject	= 0x8000	 // Method calls another method containing security code
	}
}
