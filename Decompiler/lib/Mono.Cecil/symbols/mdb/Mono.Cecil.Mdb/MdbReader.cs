//
// MdbReader.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2010 Jb Evain
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
using System.IO;

using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using Mono.CompilerServices.SymbolWriter;

namespace Mono.Cecil.Mdb {

	public class MdbReaderProvider : ISymbolReaderProvider {

		public ISymbolReader GetSymbolReader (ModuleDefinition module, string fileName)
		{
			return new MdbReader (MonoSymbolFile.ReadSymbolFile (module, fileName));
		}

		public ISymbolReader GetSymbolReader (ModuleDefinition module, Stream symbolStream)
		{
			throw new NotImplementedException ();
		}
	}

	public class MdbReader : ISymbolReader {

		readonly MonoSymbolFile symbol_file;
		readonly Dictionary<string, Document> documents;

		public MdbReader (MonoSymbolFile symFile)
		{
			symbol_file = symFile;
			documents = new Dictionary<string, Document> ();
		}

		public bool ProcessDebugHeader (ImageDebugDirectory directory, byte [] header)
		{
			return true;
		}

		public void Read (MethodBody body, InstructionMapper mapper)
		{
			var method_token = body.Method.MetadataToken;
			var entry = symbol_file.GetMethodByToken (method_token.ToInt32	());
			if (entry == null)
				return;

			var scopes = ReadScopes (entry, body, mapper);
			ReadLineNumbers (entry, mapper);
			ReadLocalVariables (entry, body, scopes);
		}

		static void ReadLocalVariables (MethodEntry entry, MethodBody body, Scope [] scopes)
		{
			var locals = entry.GetLocals ();
			foreach (var local in locals) {
				var variable = body.Variables [local.Index];
				variable.Name = local.Name;

				var index = local.BlockIndex;
				if (index < 0 || index >= scopes.Length)
					continue;

				var scope = scopes [index];
				if (scope == null)
					continue;

				scope.Variables.Add (variable);
			}
		}

		void ReadLineNumbers (MethodEntry entry, InstructionMapper mapper)
		{
			Document document = null;
			var table = entry.GetLineNumberTable ();

			foreach (var line in table.LineNumbers) {
				var instruction = mapper (line.Offset);
				if (instruction == null)
					continue;

				if (document == null)
					document = GetDocument (entry.CompileUnit.SourceFile);

				instruction.SequencePoint = new SequencePoint (document) {
					StartLine = line.Row,
					EndLine = line.Row,
				};
			}
		}

		Document GetDocument (SourceFileEntry file)
		{
			var file_name = file.FileName;

			Document document;
			if (documents.TryGetValue (file_name, out document))
				return document;

			document = new Document (file_name);
			documents.Add (file_name, document);

			return document;
		}

		static Scope [] ReadScopes (MethodEntry entry, MethodBody body, InstructionMapper mapper)
		{
			var blocks = entry.GetCodeBlocks ();
			var scopes = new Scope [blocks.Length];

			foreach (var block in blocks) {
				if (block.BlockType != CodeBlockEntry.Type.Lexical)
					continue;

				var scope = new Scope ();
				scope.Start = mapper (block.StartOffset);
				scope.End = mapper (block.EndOffset);

				scopes [block.Index] = scope;

				if (body.Scope == null)
					body.Scope = scope;

				if (!AddScope (body.Scope, scope))
					body.Scope = scope;
			}

			return scopes;
		}

		static bool AddScope (Scope provider, Scope scope)
		{
			foreach (var sub_scope in provider.Scopes) {
				if (AddScope (sub_scope, scope))
					return true;

				if (scope.Start.Offset >= sub_scope.Start.Offset && scope.End.Offset <= sub_scope.End.Offset) {
					sub_scope.Scopes.Add (scope);
					return true;
				}
			}

			return false;
		}

		public void Read (MethodSymbols symbols)
		{
			var entry = symbol_file.GetMethodByToken (symbols.MethodToken.ToInt32 ());
			if (entry == null)
				return;

			ReadLineNumbers (entry, symbols);
			ReadLocalVariables (entry, symbols);
		}

		void ReadLineNumbers (MethodEntry entry, MethodSymbols symbols)
		{
			var table = entry.GetLineNumberTable ();
			var lines = table.LineNumbers;

			var instructions = symbols.instructions = new Collection<InstructionSymbol> (lines.Length);

			for (int i = 0; i < lines.Length; i++) {
				var line = lines [i];

				instructions.Add (new InstructionSymbol (line.Offset, new SequencePoint (GetDocument (entry.CompileUnit.SourceFile)) {
					StartLine = line.Row,
					EndLine = line.Row,
				}));
			}
		}

		static void ReadLocalVariables (MethodEntry entry, MethodSymbols symbols)
		{
			foreach (var local in entry.GetLocals ()) {
				var variable = symbols.Variables [local.Index];
				variable.Name = local.Name;
			}
		}

		public void Dispose ()
		{
			symbol_file.Dispose ();
		}
	}
}
