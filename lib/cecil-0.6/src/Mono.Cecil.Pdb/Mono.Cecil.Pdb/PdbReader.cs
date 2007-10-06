//
// PdbReader.cs
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

namespace Mono.Cecil.Pdb {

	using System;
	using System.Collections;
	using System.Diagnostics.SymbolStore;
	using System.Runtime.InteropServices;

	public class PdbReader : Cil.ISymbolReader {

		ISymbolReader m_reader;

		Hashtable m_documents;

		internal PdbReader (ISymbolReader reader)
		{
			m_reader = reader;
			m_documents = new Hashtable ();
		}

		public void Read (Cil.MethodBody body)
		{
			try {
				ISymbolMethod method = m_reader.GetMethod (new SymbolToken ((int) body.Method.MetadataToken.ToUInt ()));
				Hashtable instructions = GetInstructions (body);

				ReadSequencePoints (method, instructions);
				ReadScopeAndLocals (method.RootScope, null, body, instructions);
			} catch (COMException) {}
		}

		static Hashtable GetInstructions (Cil.MethodBody body)
		{
			Hashtable instructions = new Hashtable (body.Instructions.Count);
			foreach (Cil.Instruction i in body.Instructions)
				instructions.Add (i.Offset, i);

			return instructions;
		}

		static Cil.Instruction GetInstruction (Cil.MethodBody body, Hashtable instructions, int offset)
		{
			Cil.Instruction instr = (Cil.Instruction) instructions [offset];
			if (instr != null)
				return instr;

			return body.Instructions.Outside;
		}

		static void ReadScopeAndLocals (ISymbolScope scope, Cil.Scope parent, Cil.MethodBody body, Hashtable instructions)
		{
			Cil.Scope s = new Cil.Scope ();
			s.Start = GetInstruction (body, instructions, scope.StartOffset);
			s.End = GetInstruction (body, instructions, scope.EndOffset);

			if (parent != null)
				parent.Scopes.Add (s);
			else
				body.Scopes.Add (s);

			foreach (ISymbolVariable local in scope.GetLocals ()) {
				Cil.VariableDefinition variable = body.Variables [local.AddressField1];
				variable.Name = local.Name;

				s.Variables.Add (variable);
			}

			foreach (ISymbolScope child in scope.GetChildren ())
				ReadScopeAndLocals (child, s, body, instructions);
		}

		void ReadSequencePoints(ISymbolMethod method, Hashtable instructions)
		{
			int count = method.SequencePointCount;
			int [] offsets = new int [count];
			ISymbolDocument [] docs = new ISymbolDocument [count];
			int [] startColumn = new int [count];
			int [] endColumn = new int [count];
			int [] startRow = new int [count];
			int [] endRow = new int [count];
			method.GetSequencePoints (offsets, docs, startRow, startColumn, endRow, endColumn);

			for (int i = 0; i < offsets.Length; i++) {
				Cil.Instruction instr = (Cil.Instruction) instructions [offsets [i]];

				Cil.SequencePoint sp = new Cil.SequencePoint (GetDocument (docs [i]));
				sp.StartLine = startRow [i];
				sp.StartColumn = startColumn [i];
				sp.EndLine = endRow [i];
				sp.EndColumn = endColumn [i];

				instr.SequencePoint = sp;
			}
		}

		Cil.Document GetDocument (ISymbolDocument document)
		{
			Cil.Document doc = m_documents [document.URL] as Cil.Document;
			if (doc != null)
				return doc;

			doc = new Cil.Document (document.URL);
			doc.Type = (Cil.DocumentType) Cil.GuidAttribute.GetValueFromGuid (
				document.DocumentType, typeof (Cil.DocumentType));
			doc.Language = (Cil.DocumentLanguage) Cil.GuidAttribute.GetValueFromGuid (
				document.Language, typeof (Cil.DocumentLanguage));
			doc.LanguageVendor = (Cil.DocumentLanguageVendor) Cil.GuidAttribute.GetValueFromGuid (
				document.LanguageVendor, typeof (Cil.DocumentLanguageVendor));

			m_documents [doc.Url] = doc;
			return doc;
		}

		public void Dispose ()
		{
			m_reader = null;
			// force the release of the pdb lock
			GC.Collect ();
		}
	}
}
