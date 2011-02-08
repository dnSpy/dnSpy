//
// SymbolWriterImpl.cs
//
// Author:
//   Lluis Sanchez Gual (lluis@novell.com)
//
// (C) 2005 Novell, Inc.  http://www.novell.com
//
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
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Collections;
using System.IO;
using System.Diagnostics.SymbolStore;

namespace Mono.CompilerServices.SymbolWriter
{
	public class SymbolWriterImpl: ISymbolWriter
	{
		MonoSymbolWriter msw;

		int nextLocalIndex;
		int currentToken;
		string methodName;
		Stack namespaceStack = new Stack ();
		bool methodOpened;

		Hashtable documents = new Hashtable ();

#if !CECIL
		ModuleBuilder mb;
		delegate Guid GetGuidFunc (ModuleBuilder mb);
		GetGuidFunc get_guid_func;

		public SymbolWriterImpl (ModuleBuilder mb)
		{
			this.mb = mb;
		}

		public void Close ()
		{
			MethodInfo mi = typeof (ModuleBuilder).GetMethod (
				"Mono_GetGuid",
				BindingFlags.Static | BindingFlags.NonPublic);
			if (mi == null)
				return;

			get_guid_func = (GetGuidFunc) System.Delegate.CreateDelegate (
				typeof (GetGuidFunc), mi);

			msw.WriteSymbolFile (get_guid_func (mb));
		}
#else
		Guid guid;

		public SymbolWriterImpl (Guid guid)
		{
			this.guid = guid;
		}

		public void Close ()
		{
			msw.WriteSymbolFile (guid);
		}
#endif

		public void CloseMethod ()
		{
			if (methodOpened) {
				methodOpened = false;
				nextLocalIndex = 0;
				msw.CloseMethod ();
			}
		}

		public void CloseNamespace ()
		{
			namespaceStack.Pop ();
			msw.CloseNamespace ();
		}

		public void CloseScope (int endOffset)
		{
			msw.CloseScope (endOffset);
		}

		public ISymbolDocumentWriter DefineDocument (
			string url,
			Guid language,
			Guid languageVendor,
			Guid documentType)
		{
			SymbolDocumentWriterImpl doc = (SymbolDocumentWriterImpl) documents [url];
			if (doc == null) {
				SourceFileEntry entry = msw.DefineDocument (url);
				CompileUnitEntry comp_unit = msw.DefineCompilationUnit (entry);
				doc = new SymbolDocumentWriterImpl (comp_unit);
				documents [url] = doc;
			}
			return doc;
		}

		public void DefineField (
			SymbolToken parent,
			string name,
			FieldAttributes attributes,
			byte[] signature,
			SymAddressKind addrKind,
			int addr1,
			int addr2,
			int addr3)
		{
		}

		public void DefineGlobalVariable (
			string name,
			FieldAttributes attributes,
			byte[] signature,
			SymAddressKind addrKind,
			int addr1,
			int addr2,
			int addr3)
		{
		}

		public void DefineLocalVariable (
			string name,
			FieldAttributes attributes,
			byte[] signature,
			SymAddressKind addrKind,
			int addr1,
			int addr2,
			int addr3,
			int startOffset,
			int endOffset)
		{
			msw.DefineLocalVariable (nextLocalIndex++, name);
		}

		public void DefineParameter (
			string name,
			ParameterAttributes attributes,
			int sequence,
			SymAddressKind addrKind,
			int addr1,
			int addr2,
			int addr3)
		{
		}

		public void DefineSequencePoints (
			ISymbolDocumentWriter document,
			int[] offsets,
			int[] lines,
			int[] columns,
			int[] endLines,
			int[] endColumns)
		{
			SymbolDocumentWriterImpl doc = (SymbolDocumentWriterImpl) document;
			SourceFileEntry file = doc != null ? doc.Entry.SourceFile : null;

			for (int n=0; n<offsets.Length; n++) {
				if (n > 0 && offsets[n] == offsets[n-1] && lines[n] == lines[n-1] && columns[n] == columns[n-1])
					continue;
				msw.MarkSequencePoint (offsets[n], file, lines[n], columns[n], false);
			}
		}

		public void Initialize (IntPtr emitter, string filename, bool fFullBuild)
		{
			msw = new MonoSymbolWriter (filename);
		}

		public void OpenMethod (SymbolToken method)
		{
			currentToken = method.GetToken ();
		}

		public void OpenNamespace (string name)
		{
			NamespaceInfo n = new NamespaceInfo ();
			n.NamespaceID = -1;
			n.Name = name;
			namespaceStack.Push (n);
		}

		public int OpenScope (int startOffset)
		{
			return msw.OpenScope (startOffset);
		}

		public void SetMethodSourceRange (
			ISymbolDocumentWriter startDoc,
			int startLine,
			int startColumn,
			ISymbolDocumentWriter endDoc,
			int endLine,
			int endColumn)
		{
			int nsId = GetCurrentNamespace (startDoc);
			SourceMethodImpl sm = new SourceMethodImpl (methodName, currentToken, nsId);
			msw.OpenMethod (((ICompileUnit)startDoc).Entry, nsId, sm);
			methodOpened = true;
		}

		public void SetScopeRange (int scopeID, int startOffset, int endOffset)
		{
		}

		public void SetSymAttribute (SymbolToken parent, string name, byte[] data)
		{
			// This is a hack! but MonoSymbolWriter needs the method name
			// and ISymbolWriter does not have any method for providing it
			if (name == "__name")
				methodName = System.Text.Encoding.UTF8.GetString (data);
		}

		public void SetUnderlyingWriter (IntPtr underlyingWriter)
		{
		}

		public void SetUserEntryPoint (SymbolToken entryMethod)
		{
		}

		public void UsingNamespace (string fullName)
		{
			if (namespaceStack.Count == 0) {
				OpenNamespace ("");
			}

			NamespaceInfo ni = (NamespaceInfo) namespaceStack.Peek ();
			if (ni.NamespaceID != -1) {
				NamespaceInfo old = ni;
				CloseNamespace ();
				OpenNamespace (old.Name);
				ni = (NamespaceInfo) namespaceStack.Peek ();
				ni.UsingClauses = old.UsingClauses;
			}
			ni.UsingClauses.Add (fullName);
		}

		int GetCurrentNamespace (ISymbolDocumentWriter doc)
		{
			if (namespaceStack.Count == 0) {
				OpenNamespace ("");
			}

			NamespaceInfo ni = (NamespaceInfo) namespaceStack.Peek ();
			if (ni.NamespaceID == -1)
			{
				string[] usings = (string[]) ni.UsingClauses.ToArray (typeof(string));

				int parentId = 0;
				if (namespaceStack.Count > 1) {
					namespaceStack.Pop ();
					parentId = ((NamespaceInfo) namespaceStack.Peek ()).NamespaceID;
					namespaceStack.Push (ni);
				}

				ni.NamespaceID = msw.DefineNamespace (ni.Name, ((ICompileUnit)doc).Entry, usings, parentId);
			}
			return ni.NamespaceID;
		}

	}

	class SymbolDocumentWriterImpl: ISymbolDocumentWriter, ISourceFile, ICompileUnit
	{
		CompileUnitEntry comp_unit;

		public SymbolDocumentWriterImpl (CompileUnitEntry comp_unit)
		{
			this.comp_unit = comp_unit;
		}

		public void SetCheckSum (Guid algorithmId, byte[] checkSum)
		{
		}

		public void SetSource (byte[] source)
		{
		}

		SourceFileEntry ISourceFile.Entry {
			get { return comp_unit.SourceFile; }
		}

		public CompileUnitEntry Entry {
			get { return comp_unit; }
		}
	}

	class SourceMethodImpl: IMethodDef
	{
		string name;
		int token;
		int namespaceID;

		public SourceMethodImpl (string name, int token, int namespaceID)
		{
			this.name = name;
			this.token = token;
			this.namespaceID = namespaceID;
		}

		public string Name {
			get { return name; }
		}

		public int NamespaceID {
			get { return namespaceID; }
		}

		public int Token {
			get { return token; }
		}
	}

	class NamespaceInfo
	{
		public string Name;
		public int NamespaceID;
		public ArrayList UsingClauses = new ArrayList ();
	}
}
