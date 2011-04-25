//
// MdbWriter.cs
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
using System.Collections.Generic;
using System.IO;

using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using Mono.CompilerServices.SymbolWriter;

namespace Mono.Cecil.Mdb {

#if !READ_ONLY
	public class MdbWriterProvider : ISymbolWriterProvider {

		public ISymbolWriter GetSymbolWriter (ModuleDefinition module, string fileName)
		{
			return new MdbWriter (module.Mvid, fileName);
		}

		public ISymbolWriter GetSymbolWriter (ModuleDefinition module, Stream symbolStream)
		{
			throw new NotImplementedException ();
		}
	}

	public class MdbWriter : ISymbolWriter {

		readonly Guid mvid;
		readonly MonoSymbolWriter writer;
		readonly Dictionary<string, SourceFile> source_files;

		public MdbWriter (Guid mvid, string assembly)
		{
			this.mvid = mvid;
			this.writer = new MonoSymbolWriter (assembly);
			this.source_files = new Dictionary<string, SourceFile> ();
		}

		static Collection<Instruction> GetInstructions (MethodBody body)
		{
			var instructions = new Collection<Instruction> ();
			foreach (var instruction in body.Instructions)
				if (instruction.SequencePoint != null)
					instructions.Add (instruction);

			return instructions;
		}

		SourceFile GetSourceFile (Document document)
		{
			var url = document.Url;

			SourceFile source_file;
			if (source_files.TryGetValue (url, out source_file))
				return source_file;

			var entry = writer.DefineDocument (url);
			var compile_unit = writer.DefineCompilationUnit (entry);

			source_file = new SourceFile (compile_unit, entry);
			source_files.Add (url, source_file);
			return source_file;
		}

		void Populate (Collection<Instruction> instructions, int [] offsets,
			int [] startRows, int [] startCols, out SourceFile file)
		{
			SourceFile source_file = null;

			for (int i = 0; i < instructions.Count; i++) {
				var instruction = instructions [i];
				offsets [i] = instruction.Offset;

				var sequence_point = instruction.SequencePoint;
				if (source_file == null)
					source_file = GetSourceFile (sequence_point.Document);

				startRows [i] = sequence_point.StartLine;
				startCols [i] = sequence_point.StartColumn;
			}

			file = source_file;
		}

		public void Write (MethodBody body)
		{
			var method = new SourceMethod (body.Method);

			var instructions = GetInstructions (body);
			int count = instructions.Count;
			if (count == 0)
				return;

			var offsets = new int [count];
			var start_rows = new int [count];
			var start_cols = new int [count];

			SourceFile file;
			Populate (instructions, offsets, start_rows, start_cols, out file);

			var builder = writer.OpenMethod (file.CompilationUnit, 0, method);

			for (int i = 0; i < count; i++)
				builder.MarkSequencePoint (
					offsets [i],
					file.CompilationUnit.SourceFile,
					start_rows [i],
					start_cols [i],
					false);

			if (body.HasVariables)
				AddVariables (body.Variables);

			writer.CloseMethod ();
		}

		readonly static byte [] empty_header = new byte [0];

		public bool GetDebugHeader (out ImageDebugDirectory directory, out byte [] header)
		{
			directory = new ImageDebugDirectory ();
			header = empty_header;
			return false;
		}

		void AddVariables (IList<VariableDefinition> variables)
		{
			for (int i = 0; i < variables.Count; i++) {
				var variable = variables [i];
				writer.DefineLocalVariable (i, variable.Name);
			}
		}

		public void Write (MethodSymbols symbols)
		{
			var method = new SourceMethodSymbol (symbols);

			var file = GetSourceFile (symbols.Instructions [0].SequencePoint.Document);
			var builder = writer.OpenMethod (file.CompilationUnit, 0, method);
			var count = symbols.Instructions.Count;

			for (int i = 0; i < count; i++) {
				var instruction = symbols.Instructions [i];
				var sequence_point = instruction.SequencePoint;

				builder.MarkSequencePoint (
					instruction.Offset,
					GetSourceFile (sequence_point.Document).CompilationUnit.SourceFile,
					sequence_point.StartLine,
					sequence_point.EndLine,
					false);
			}

			if (symbols.HasVariables)
				AddVariables (symbols.Variables);

			writer.CloseMethod ();
		}

		public void Dispose ()
		{
			writer.WriteSymbolFile (mvid);
		}

		class SourceFile : ISourceFile {

			readonly CompileUnitEntry compilation_unit;
			readonly SourceFileEntry entry;

			public SourceFileEntry Entry {
				get { return entry; }
			}

			public CompileUnitEntry CompilationUnit {
				get { return compilation_unit; }
			}

			public SourceFile (CompileUnitEntry comp_unit, SourceFileEntry entry)
			{
				this.compilation_unit = comp_unit;
				this.entry = entry;
			}
		}

		class SourceMethodSymbol : IMethodDef {

			readonly string name;
			readonly int token;

			public string Name {
				get { return name;}
			}

			public int Token {
				get { return token; }
			}

			public SourceMethodSymbol (MethodSymbols symbols)
			{
				name = symbols.MethodName;
				token = symbols.MethodToken.ToInt32 ();
			}
		}

		class SourceMethod : IMethodDef {

			readonly MethodDefinition method;

			public string Name {
				get { return method.Name; }
			}

			public int Token {
				get { return method.MetadataToken.ToInt32 (); }
			}

			public SourceMethod (MethodDefinition method)
			{
				this.method = method;
			}
		}
	}
#endif
}
