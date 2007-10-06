//
// PdbHelper.cs
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
	using System.Diagnostics.SymbolStore;
	using System.IO;
	using System.Runtime.InteropServices;

	internal class PdbHelper {

		[DllImport("ole32.dll")]
		static extern int CoCreateInstance (
			[In] ref Guid rclsid,
			[In, MarshalAs (UnmanagedType.IUnknown)] object pUnkOuter,
			[In] uint dwClsContext,
			[In] ref Guid riid,
			[Out, MarshalAs(UnmanagedType.Interface)] out object ppv);

		static Guid s_dispenserClassID = new Guid (0xe5cb7a31, 0x7512, 0x11d2, 0x89, 0xce, 0x00, 0x80, 0xc7, 0x92, 0xe5, 0xd8);
		static Guid s_dispenserIID = new Guid (0x809c652e, 0x7396, 0x11d2, 0x97, 0x71, 0x00, 0xa0, 0xc9, 0xb4, 0xd5, 0x0c);
		static Guid s_importerIID = new Guid (0x7dac8207, 0xd3ae, 0x4c75, 0x9b, 0x67, 0x92, 0x80, 0x1a, 0x49, 0x7d, 0x44);

		public static ISymbolReader CreateReader (string filename)
		{
			SymBinder binder;
			object objImporter;

			IMetaDataDispenser dispenser = InstantiateDispenser (out binder);
			dispenser.OpenScope (filename, 0, ref s_importerIID, out objImporter);

			return InstantiateReader (binder, filename, objImporter);
		}

		public static ISymbolReader CreateReader (string filename, byte [] binaryFile)
		{
			SymBinder binder;
			object objImporter;

			IntPtr filePtr = Marshal.AllocHGlobal (binaryFile.Length);
			Marshal.Copy (binaryFile, 0, filePtr, binaryFile.Length);

			IMetaDataDispenser dispenser = InstantiateDispenser (out binder);
			dispenser.OpenScopeOnMemory (filePtr, (uint) binaryFile.Length, 0, ref s_importerIID, out objImporter);

			return InstantiateReader (binder, filename, objImporter);
		}

		static IMetaDataDispenser InstantiateDispenser (out SymBinder binder)
		{
			binder = new SymBinder ();
			object dispenser;
			CoCreateInstance (ref s_dispenserClassID, null, 1, ref s_dispenserIID, out dispenser);
			return (IMetaDataDispenser) dispenser;
		}

		static ISymbolReader InstantiateReader (SymBinder binder, string filename, object objImporter)
		{
			IntPtr importerPtr = IntPtr.Zero;
			ISymbolReader reader;
			try {
				importerPtr = Marshal.GetComInterfaceForObject (objImporter, typeof (IMetadataImport));

				reader = binder.GetReader (importerPtr, filename, null);
			} finally {
				if (importerPtr != IntPtr.Zero)
					Marshal.Release (importerPtr);
			}

			return reader;
		}

		public static ISymbolWriter CreateWriter (string assembly, string pdb)
		{
			SymWriter writer = new SymWriter (false);

			object objDispenser, objImporter;
			CoCreateInstance (ref s_dispenserClassID, null, 1, ref s_dispenserIID, out objDispenser);

			IMetaDataDispenser dispenser = (IMetaDataDispenser) objDispenser;
			dispenser.OpenScope (assembly, 1, ref s_importerIID, out objImporter);

			IntPtr importerPtr = Marshal.GetComInterfaceForObject (objImporter, typeof (IMetadataImport));

			try {
				if (File.Exists (pdb))
					File.Delete (pdb);

				writer.Initialize (importerPtr, pdb, false);
			} finally {
				if (importerPtr != IntPtr.Zero) {
					Marshal.Release (importerPtr);
					Marshal.ReleaseComObject (objDispenser);
					Marshal.ReleaseComObject (objImporter);
					Marshal.ReleaseComObject (dispenser);
				}
			}

			return writer;
		}
	}
}
