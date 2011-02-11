//
// TokenType.cs
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

namespace Mono.Cecil.Metadata {

	public enum TokenType : uint {
		Module			  = 0x00000000,
		TypeRef			 = 0x01000000,
		TypeDef			 = 0x02000000,
		Field			   = 0x04000000,
		Method			  = 0x06000000,
		Param			   = 0x08000000,
		InterfaceImpl	   = 0x09000000,
		MemberRef		   = 0x0a000000,
		CustomAttribute	 = 0x0c000000,
		Permission		  = 0x0e000000,
		Signature		   = 0x11000000,
		Event			   = 0x14000000,
		Property			= 0x17000000,
		ModuleRef		   = 0x1a000000,
		TypeSpec			= 0x1b000000,
		Assembly			= 0x20000000,
		AssemblyRef		 = 0x23000000,
		File				= 0x26000000,
		ExportedType		= 0x27000000,
		ManifestResource	= 0x28000000,
		GenericParam			= 0x2a000000,
		MethodSpec			= 0x2b000000,
		String			  = 0x70000000,
		Name				= 0x71000000,
		BaseType			= 0x72000000
	}
}
