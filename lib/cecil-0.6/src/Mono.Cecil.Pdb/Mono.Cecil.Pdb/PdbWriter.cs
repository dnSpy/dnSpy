//
// PdbWriter.cs
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

using Mono.Cecil.Cil;

namespace Mono.Cecil.Pdb {

	using System;
	using System.Collections;
	using System.Diagnostics.SymbolStore;
	using System.IO;

	public class PdbWriter : Cil.ISymbolWriter {

		ModuleDefinition m_module;
		ISymbolWriter m_writer;
		Hashtable m_documents;
		string m_pdb;

		internal PdbWriter (ISymbolWriter writer, ModuleDefinition module, string pdb)
		{
			m_writer = writer;
			m_module = module;
			m_documents = new Hashtable ();
			m_pdb = pdb;
		}

		public void Write (MethodBody body, byte [][] variables)
		{
			CreateDocuments (body);
			m_writer.OpenMethod (new SymbolToken ((int) body.Method.MetadataToken.ToUInt ()));
			CreateScopes (body, body.Scopes, variables);
			m_writer.CloseMethod ();
		}

		void CreateScopes (MethodBody body, ScopeCollection scopes, byte [][] variables)
		{
			foreach (Scope s in scopes) {
				int startOffset = s.Start.Offset;
				int endOffset = s.End == body.Instructions.Outside ?
					body.Instructions[body.Instructions.Count - 1].Offset + 1 :
					s.End.Offset;

				m_writer.OpenScope (startOffset);
				m_writer.UsingNamespace (body.Method.DeclaringType.Namespace);
				m_writer.OpenNamespace (body.Method.DeclaringType.Namespace);

				int start = body.Instructions.IndexOf (s.Start);
				int end = s.End == body.Instructions.Outside ?
					body.Instructions.Count - 1 :
					body.Instructions.IndexOf (s.End);

				ArrayList instructions = new ArrayList();
				for (int i = start; i <= end; i++)
					if (body.Instructions [i].SequencePoint != null)
						instructions.Add (body.Instructions [i]);

				Document doc = null;

				int [] offsets = new int [instructions.Count];
				int [] startRows = new int [instructions.Count];
				int [] startCols = new int [instructions.Count];
				int [] endRows = new int [instructions.Count];
				int [] endCols = new int [instructions.Count];

				for (int i = 0; i < instructions.Count; i++) {
					Instruction instr = (Instruction) instructions [i];
					offsets [i] = instr.Offset;

					if (doc == null)
						doc = instr.SequencePoint.Document;

					startRows [i] = instr.SequencePoint.StartLine;
					startCols [i] = instr.SequencePoint.StartColumn;
					endRows [i] = instr.SequencePoint.EndLine;
					endCols [i] = instr.SequencePoint.EndColumn;
				}

				m_writer.DefineSequencePoints (GetDocument (doc),
					offsets, startRows, startCols, endRows, endCols);

				CreateLocalVariable (s, startOffset, endOffset, variables);

				CreateScopes (body, s.Scopes, variables);
				m_writer.CloseNamespace ();

				m_writer.CloseScope (endOffset);
			}
		}

		void CreateLocalVariable (IVariableDefinitionProvider provider, int startOffset, int endOffset, byte [][] variables)
		{
			for (int i = 0; i < provider.Variables.Count; i++) {
				VariableDefinition var = provider.Variables [i];
				m_writer.DefineLocalVariable (
					var.Name,
					0,
					variables [i],
					SymAddressKind.ILOffset,
					i,
					0,
					0,
					startOffset,
					endOffset);
			}
		}

		void CreateDocuments (MethodBody body)
		{
			foreach (Instruction instr in body.Instructions) {
				if (instr.SequencePoint == null)
					continue;

				GetDocument (instr.SequencePoint.Document);
			}
		}

		ISymbolDocumentWriter GetDocument (Document document)
		{
			if (document == null)
				return null;

			ISymbolDocumentWriter docWriter = m_documents [document.Url] as ISymbolDocumentWriter;
			if (docWriter != null)
				return docWriter;

			docWriter = m_writer.DefineDocument (
				document.Url,
				GuidAttribute.GetGuidFromValue ((int) document.Language, typeof (DocumentLanguage)),
				GuidAttribute.GetGuidFromValue ((int) document.LanguageVendor, typeof (DocumentLanguageVendor)),
				GuidAttribute.GetGuidFromValue ((int) document.Type, typeof (DocumentType)));

			m_documents [document.Url] = docWriter;
			return docWriter;
		}

		public void Dispose ()
		{
			m_writer.Close ();
			Patch ();
		}

		void Patch ()
		{
			FileStream fs = new FileStream (m_pdb, FileMode.Open, FileAccess.ReadWrite);
			uint age = m_module.Image.DebugHeader.Age;
			Guid g = m_module.Image.DebugHeader.Signature;

			BinaryReader reader = new BinaryReader (fs);
			reader.BaseStream.Position = 32;

			uint pageSize = reader.ReadUInt32 ();
			reader.BaseStream.Position += 4;

			uint pageCount = reader.ReadUInt32 ();
			reader.BaseStream.Position += pageSize - 44;

			uint magic = 0x1312e94;
			int page = 0;
			for (int i = 1; i < pageCount; i++) {
				if (magic == reader.ReadUInt32 ()) {
					page = i;
					break;
				}
				reader.BaseStream.Position += pageSize - 4;
			}

			BinaryWriter writer = new BinaryWriter (fs);
			writer.BaseStream.Position = page * pageSize + 8;

			writer.Write (age);
			writer.Write (g.ToByteArray ());

			fs.Close ();
		}
	}
}
