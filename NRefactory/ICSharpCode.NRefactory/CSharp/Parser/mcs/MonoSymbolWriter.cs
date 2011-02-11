//
// Mono.CSharp.Debugger/MonoSymbolWriter.cs
//
// Author:
//   Martin Baulig (martin@ximian.com)
//
// This is the default implementation of the System.Diagnostics.SymbolStore.ISymbolWriter
// interface.
//
// (C) 2002 Ximian, Inc.  http://www.ximian.com
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
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.IO;
	
namespace Mono.CompilerServices.SymbolWriter
{
	public class MonoSymbolWriter
	{
		List<SourceMethodBuilder> methods;
		List<SourceFileEntry> sources;
		List<CompileUnitEntry> comp_units;
		protected readonly MonoSymbolFile file;
		string filename;
		
		private SourceMethodBuilder current_method;
#if NET_2_1
		System.Collections.Stack current_method_stack = new System.Collections.Stack ();
#else
		Stack<SourceMethodBuilder> current_method_stack = new Stack<SourceMethodBuilder> ();
#endif

		public MonoSymbolWriter (string filename)
		{
			this.methods = new List<SourceMethodBuilder> ();
			this.sources = new List<SourceFileEntry> ();
			this.comp_units = new List<CompileUnitEntry> ();
			this.file = new MonoSymbolFile ();

			this.filename = filename + ".mdb";
		}

		public MonoSymbolFile SymbolFile {
			get { return file; }
		}

		public void CloseNamespace ()
		{ }

		public void DefineLocalVariable (int index, string name)
		{
			if (current_method == null)
				return;

			current_method.AddLocal (index, name);
		}

		public void DefineCapturedLocal (int scope_id, string name, string captured_name)
		{
			file.DefineCapturedVariable (scope_id, name, captured_name,
						     CapturedVariable.CapturedKind.Local);
		}

		public void DefineCapturedParameter (int scope_id, string name, string captured_name)
		{
			file.DefineCapturedVariable (scope_id, name, captured_name,
						     CapturedVariable.CapturedKind.Parameter);
		}

		public void DefineCapturedThis (int scope_id, string captured_name)
		{
			file.DefineCapturedVariable (scope_id, "this", captured_name,
						     CapturedVariable.CapturedKind.This);
		}

		public void DefineCapturedScope (int scope_id, int id, string captured_name)
		{
			file.DefineCapturedScope (scope_id, id, captured_name);
		}

		public void DefineScopeVariable (int scope, int index)
		{
			if (current_method == null)
				return;

			current_method.AddScopeVariable (scope, index);
		}

		public void MarkSequencePoint (int offset, SourceFileEntry file, int line, int column,
					       bool is_hidden)
		{
			if (current_method == null)
				return;

			current_method.MarkSequencePoint (offset, file, line, column, is_hidden);
		}

		public SourceMethodBuilder OpenMethod (ICompileUnit file, int ns_id, IMethodDef method)
		{
			SourceMethodBuilder builder = new SourceMethodBuilder (file, ns_id, method);
			current_method_stack.Push (current_method);
			current_method = builder;
			methods.Add (current_method);
			return builder;
		}

		public void CloseMethod ()
		{
			current_method = (SourceMethodBuilder) current_method_stack.Pop ();
		}

		public SourceFileEntry DefineDocument (string url)
		{
			SourceFileEntry entry = new SourceFileEntry (file, url);
			sources.Add (entry);
			return entry;
		}

		public SourceFileEntry DefineDocument (string url, byte[] guid, byte[] checksum)
		{
			SourceFileEntry entry = new SourceFileEntry (file, url, guid, checksum);
			sources.Add (entry);
			return entry;
		}

		public CompileUnitEntry DefineCompilationUnit (SourceFileEntry source)
		{
			CompileUnitEntry entry = new CompileUnitEntry (file, source);
			comp_units.Add (entry);
			return entry;
		}

		public int DefineNamespace (string name, CompileUnitEntry unit,
					    string[] using_clauses, int parent)
		{
			if ((unit == null) || (using_clauses == null))
				throw new NullReferenceException ();

			return unit.DefineNamespace (name, using_clauses, parent);
		}

		public int OpenScope (int start_offset)
		{
			if (current_method == null)
				return 0;

			current_method.StartBlock (CodeBlockEntry.Type.Lexical, start_offset);
			return 0;
		}

		public void CloseScope (int end_offset)
		{
			if (current_method == null)
				return;

			current_method.EndBlock (end_offset);
		}

		public void OpenCompilerGeneratedBlock (int start_offset)
		{
			if (current_method == null)
				return;

			current_method.StartBlock (CodeBlockEntry.Type.CompilerGenerated,
						   start_offset);
		}

		public void CloseCompilerGeneratedBlock (int end_offset)
		{
			if (current_method == null)
				return;

			current_method.EndBlock (end_offset);
		}

		public void StartIteratorBody (int start_offset)
		{
			current_method.StartBlock (CodeBlockEntry.Type.IteratorBody,
						   start_offset);
		}

		public void EndIteratorBody (int end_offset)
		{
			current_method.EndBlock (end_offset);
		}

		public void StartIteratorDispatcher (int start_offset)
		{
			current_method.StartBlock (CodeBlockEntry.Type.IteratorDispatcher,
						   start_offset);
		}

		public void EndIteratorDispatcher (int end_offset)
		{
			current_method.EndBlock (end_offset);
		}

		public void DefineAnonymousScope (int id)
		{
			file.DefineAnonymousScope (id);
		}

		public void WriteSymbolFile (Guid guid)
		{
			foreach (SourceMethodBuilder method in methods)
				method.DefineMethod (file);

			try {
				// We mmap the file, so unlink the previous version since it may be in use
				File.Delete (filename);
			} catch {
				// We can safely ignore
			}
			using (FileStream fs = new FileStream (filename, FileMode.Create, FileAccess.Write)) {
				file.CreateSymbolFile (guid, fs);
			}
		}
	}

	public class SourceMethodBuilder
	{
		List<LocalVariableEntry> _locals;
		List<CodeBlockEntry> _blocks;
		List<ScopeVariable> _scope_vars;
#if NET_2_1
		System.Collections.Stack _block_stack;
#else		
		Stack<CodeBlockEntry> _block_stack;
#endif
		string _real_name;
		IMethodDef _method;
		ICompileUnit _comp_unit;
//		MethodEntry.Flags _method_flags;
		int _ns_id;

		public SourceMethodBuilder (ICompileUnit comp_unit, int ns_id, IMethodDef method)
		{
			this._comp_unit = comp_unit;
			this._method = method;
			this._ns_id = ns_id;

			method_lines = new LineNumberEntry [32];
		}

		private LineNumberEntry [] method_lines;
		private int method_lines_pos = 0;

		public void MarkSequencePoint (int offset, SourceFileEntry file, int line, int column,
					       bool is_hidden)
		{
			if (method_lines_pos == method_lines.Length) {
				LineNumberEntry [] tmp = method_lines;
				method_lines = new LineNumberEntry [method_lines.Length * 2];
				Array.Copy (tmp, method_lines, method_lines_pos);
			}

			int file_idx = file != null ? file.Index : 0;
			method_lines [method_lines_pos++] = new LineNumberEntry (
				file_idx, line, offset, is_hidden);
		}

		public void StartBlock (CodeBlockEntry.Type type, int start_offset)
		{
			if (_block_stack == null) {
#if NET_2_1
				_block_stack = new System.Collections.Stack ();
#else				
				_block_stack = new Stack<CodeBlockEntry> ();
#endif
			}
			
			if (_blocks == null)
				_blocks = new List<CodeBlockEntry> ();

			int parent = CurrentBlock != null ? CurrentBlock.Index : -1;

			CodeBlockEntry block = new CodeBlockEntry (
				_blocks.Count + 1, parent, type, start_offset);

			_block_stack.Push (block);
			_blocks.Add (block);
		}

		public void EndBlock (int end_offset)
		{
			CodeBlockEntry block = (CodeBlockEntry) _block_stack.Pop ();
			block.Close (end_offset);
		}

		public CodeBlockEntry[] Blocks {
			get {
				if (_blocks == null)
					return new CodeBlockEntry [0];

				CodeBlockEntry[] retval = new CodeBlockEntry [_blocks.Count];
				_blocks.CopyTo (retval, 0);
				return retval;
			}
		}

		public CodeBlockEntry CurrentBlock {
			get {
				if ((_block_stack != null) && (_block_stack.Count > 0))
					return (CodeBlockEntry) _block_stack.Peek ();
				else
					return null;
			}
		}

		public LocalVariableEntry[] Locals {
			get {
				if (_locals == null)
					return new LocalVariableEntry [0];
				else {
					LocalVariableEntry[] retval =
						new LocalVariableEntry [_locals.Count];
					_locals.CopyTo (retval, 0);
					return retval;
				}
			}
		}

		public void AddLocal (int index, string name)
		{
			if (_locals == null)
				_locals = new List<LocalVariableEntry> ();
			int block_idx = CurrentBlock != null ? CurrentBlock.Index : 0;
			_locals.Add (new LocalVariableEntry (index, name, block_idx));
		}

		public ScopeVariable[] ScopeVariables {
			get {
				if (_scope_vars == null)
					return new ScopeVariable [0];

				ScopeVariable[] retval = new ScopeVariable [_scope_vars.Count];
				_scope_vars.CopyTo (retval);
				return retval;
			}
		}

		public void AddScopeVariable (int scope, int index)
		{
			if (_scope_vars == null)
				_scope_vars = new List<ScopeVariable> ();
			_scope_vars.Add (
				new ScopeVariable (scope, index));
		}

		public string RealMethodName {
			get { return _real_name; }
		}

		public void SetRealMethodName (string name)
		{
			_real_name = name;
		}

		public ICompileUnit SourceFile {
			get { return _comp_unit; }
		}

		public IMethodDef Method {
			get { return _method; }
		}

		public void DefineMethod (MonoSymbolFile file)
		{
			LineNumberEntry[] lines = new LineNumberEntry [method_lines_pos];
			Array.Copy (method_lines, lines, method_lines_pos);

			MethodEntry entry = new MethodEntry (
				file, _comp_unit.Entry, _method.Token, ScopeVariables,
				Locals, lines, Blocks, RealMethodName, 0, //_method_flags,
				_ns_id);

			file.AddMethod (entry);
		}
	}
}
