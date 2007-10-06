//
// IMetadataImport.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// (C) 2006 Jb Evain
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

// from: http://blogs.msdn.com/jmstall/articles/sample_pdb2xml.aspx

namespace Mono.Cecil.Pdb {

	using System;
	using System.Runtime.InteropServices;

	[Guid ("809c652e-7396-11d2-9771-00a0c9b4d50c")]
	[InterfaceType (ComInterfaceType.InterfaceIsIUnknown)]
	[ComVisible (true)]
	interface IMetaDataDispenser {

		void DefineScope_Placeholder();

		void OpenScope (
			[In, MarshalAs (UnmanagedType.LPWStr)] string szScope,
			[In] int dwOpenFlags,
			[In] ref Guid riid,
			[Out, MarshalAs (UnmanagedType.IUnknown)] out object punk);

		void OpenScopeOnMemory (
			[In] IntPtr pData,
			[In] uint cbData,
			[In] uint dwOpenFlags,
			[In] ref Guid riid,
			[Out, MarshalAs (UnmanagedType.Interface)] out object punk);
	}
}
