//
// SymWriter.cs
//
// Author:
//   Juerg Billeter (j@bitron.ch)
//
// (C) 2008 Juerg Billeter
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
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Runtime.InteropServices;

using Mono.Cecil.Cil;
using Mono.Collections.Generic;

#if !READ_ONLY

namespace Mono.Cecil.Pdb
{
	internal class SymWriter
	{
		[DllImport("ole32.dll")]
		static extern int CoCreateInstance (
			[In] ref Guid rclsid,
			[In, MarshalAs (UnmanagedType.IUnknown)] object pUnkOuter,
			[In] uint dwClsContext,
			[In] ref Guid riid,
			[Out, MarshalAs (UnmanagedType.Interface)] out object ppv);

		static Guid s_symUnmangedWriterIID = new Guid("0b97726e-9e6d-4f05-9a26-424022093caa");
		static Guid s_CorSymWriter_SxS_ClassID = new Guid ("108296c1-281e-11d3-bd22-0000f80849bd");

		readonly ISymUnmanagedWriter2 m_writer;
		readonly Collection<ISymUnmanagedDocumentWriter> documents;

		public SymWriter ()
		{
			object objWriter;
			CoCreateInstance (ref s_CorSymWriter_SxS_ClassID, null, 1, ref s_symUnmangedWriterIID, out objWriter);

			m_writer = (ISymUnmanagedWriter2) objWriter;
			documents = new Collection<ISymUnmanagedDocumentWriter> ();
		}

		public byte[] GetDebugInfo (out ImageDebugDirectory idd)
		{
			int size;

			// get size of debug info
			m_writer.GetDebugInfo (out idd, 0, out size, null);

			byte[] debug_info = new byte[size];
			m_writer.GetDebugInfo (out idd, size, out size, debug_info);

			return debug_info;
		}

		public void DefineLocalVariable2 (
			string name,
			FieldAttributes attributes,
			SymbolToken sigToken,
			SymAddressKind addrKind,
			int addr1,
			int addr2,
			int addr3,
			int startOffset,
			int endOffset)
		{
			m_writer.DefineLocalVariable2 (name, (int)attributes, sigToken, (int)addrKind, addr1, addr2, addr3, startOffset, endOffset);
		}

		public void Close ()
		{
			m_writer.Close ();
			Marshal.ReleaseComObject (m_writer);

			foreach (var document in documents)
				Marshal.ReleaseComObject (document);
		}

		public void CloseMethod ()
		{
			m_writer.CloseMethod ();
		}

		public void CloseNamespace ()
		{
			m_writer.CloseNamespace ();
		}

		public void CloseScope (int endOffset)
		{
			m_writer.CloseScope (endOffset);
		}

		public SymDocumentWriter DefineDocument (string url, Guid language, Guid languageVendor, Guid documentType)
		{
			ISymUnmanagedDocumentWriter unmanagedDocumentWriter;
			m_writer.DefineDocument (url, ref language, ref languageVendor, ref documentType, out unmanagedDocumentWriter);

			documents.Add (unmanagedDocumentWriter);
			return new SymDocumentWriter (unmanagedDocumentWriter);
		}

		public void DefineParameter (string name, ParameterAttributes attributes, int sequence, SymAddressKind addrKind, int addr1, int addr2, int addr3)
		{
			throw new Exception ("The method or operation is not implemented.");
		}

		public void DefineSequencePoints (SymDocumentWriter document, int[] offsets, int[] lines, int[] columns, int[] endLines, int[] endColumns)
		{
			m_writer.DefineSequencePoints (document.GetUnmanaged(), offsets.Length, offsets, lines, columns, endLines, endColumns);
		}

		public void Initialize (object emitter, string filename, bool fFullBuild)
		{
			m_writer.Initialize (emitter, filename, null, fFullBuild);
		}

		public void SetUserEntryPoint (SymbolToken method)
		{
			m_writer.SetUserEntryPoint (method);
		}

		public void OpenMethod (SymbolToken method)
		{
			m_writer.OpenMethod (method);
		}

		public void OpenNamespace (string name)
		{
			m_writer.OpenNamespace (name);
		}

		public int OpenScope (int startOffset)
		{
			int result;
			m_writer.OpenScope (startOffset, out result);
			return result;
		}

		public void UsingNamespace (string fullName)
		{
			m_writer.UsingNamespace (fullName);
		}
	}
}

#endif
