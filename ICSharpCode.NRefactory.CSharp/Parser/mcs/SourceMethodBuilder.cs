//
// SourceMethodBuilder.cs
//
// Authors:
//   Martin Baulig (martin@ximian.com)
//   Marek Safar (marek.safar@gmail.com)
//
// (C) 2002 Ximian, Inc.  http://www.ximian.com
// Copyright (C) 2012 Xamarin Inc (http://www.xamarin.com)
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

using System.Collections.Generic;

namespace Mono.CompilerServices.SymbolWriter
{
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
		readonly List<LineNumberEntry> method_lines;

		readonly ICompileUnit _comp_unit;
		readonly int ns_id;
		readonly IMethodDef method;

		public SourceMethodBuilder (ICompileUnit comp_unit)
		{
			this._comp_unit = comp_unit;
			method_lines = new List<LineNumberEntry> ();
		}

		public SourceMethodBuilder (ICompileUnit comp_unit, int ns_id, IMethodDef method)
			: this (comp_unit)
		{
			this.ns_id = ns_id;
			this.method = method;
		}

		public void MarkSequencePoint (int offset, SourceFileEntry file, int line, int column, bool is_hidden)
		{
			int file_idx = file != null ? file.Index : 0;
			var lne = new LineNumberEntry (file_idx, line, column, offset, is_hidden);

			if (method_lines.Count > 0) {
				var prev = method_lines[method_lines.Count - 1];

				//
				// Same offset cannot be used for multiple lines
				// 
				if (prev.Offset == offset) {
					//
					// Use the new location because debugger will adjust
					// the breakpoint to next line with sequence point
					//
					if (LineNumberEntry.LocationComparer.Default.Compare (lne, prev) > 0)
						method_lines[method_lines.Count - 1] = lne;

					return;
				}
			}

			method_lines.Add (lne);
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
					return _locals.ToArray ();
				}
			}
		}

		public ICompileUnit SourceFile {
			get {
				return _comp_unit;
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

				return _scope_vars.ToArray ();
			}
		}

		public void AddScopeVariable (int scope, int index)
		{
			if (_scope_vars == null)
				_scope_vars = new List<ScopeVariable> ();
			_scope_vars.Add (
				new ScopeVariable (scope, index));
		}

		public void DefineMethod (MonoSymbolFile file)
		{
			DefineMethod (file, method.Token);
		}

		public void DefineMethod (MonoSymbolFile file, int token)
		{
			MethodEntry entry = new MethodEntry (
				file, _comp_unit.Entry, token, ScopeVariables,
				Locals, method_lines.ToArray (), Blocks, null, MethodEntry.Flags.ColumnsInfoIncluded, ns_id);

			file.AddMethod (entry);
		}
	}
}
