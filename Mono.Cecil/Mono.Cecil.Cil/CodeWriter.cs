//
// CodeWriter.cs
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

using Mono.Collections.Generic;

using Mono.Cecil.Metadata;
using Mono.Cecil.PE;

using RVA = System.UInt32;

#if !READ_ONLY

namespace Mono.Cecil.Cil {

	sealed class CodeWriter : ByteBuffer {

		readonly RVA code_base;
		internal readonly MetadataBuilder metadata;
		readonly Dictionary<uint, MetadataToken> standalone_signatures;

		RVA current;
		MethodBody body;

		public CodeWriter (MetadataBuilder metadata)
			: base (0)
		{
			this.code_base = metadata.text_map.GetNextRVA (TextSegment.CLIHeader);
			this.current = code_base;
			this.metadata = metadata;
			this.standalone_signatures = new Dictionary<uint, MetadataToken> ();
		}

		public RVA WriteMethodBody (MethodDefinition method)
		{
			var rva = BeginMethod ();

			if (IsUnresolved (method)) {
				if (method.rva == 0)
					return 0;

				WriteUnresolvedMethodBody (method);
			} else {
				if (IsEmptyMethodBody (method.Body))
					return 0;

				WriteResolvedMethodBody (method);
			}

			Align (4);

			EndMethod ();
			return rva;
		}

		static bool IsEmptyMethodBody (MethodBody body)
		{
			return body.instructions.IsNullOrEmpty ()
				&& body.variables.IsNullOrEmpty ();
		}

		static bool IsUnresolved (MethodDefinition method)
		{
			return method.HasBody && method.HasImage && method.body == null;
		}

		void WriteUnresolvedMethodBody (MethodDefinition method)
		{
			var code_reader = metadata.module.Read (method, (_, reader) => reader.code);

			MethodSymbols symbols;
			var buffer = code_reader.PatchRawMethodBody (method, this, out symbols);

			WriteBytes (buffer);

			if (symbols.instructions.IsNullOrEmpty ())
				return;

			symbols.method_token = method.token;
			symbols.local_var_token = GetLocalVarToken (buffer, symbols);

			var symbol_writer = metadata.symbol_writer;
			if (symbol_writer != null)
				symbol_writer.Write (symbols);
		}

		static MetadataToken GetLocalVarToken (ByteBuffer buffer, MethodSymbols symbols)
		{
			if (symbols.variables.IsNullOrEmpty ())
				return MetadataToken.Zero;

			buffer.position = 8;
			return new MetadataToken (buffer.ReadUInt32 ());
		}

		void WriteResolvedMethodBody (MethodDefinition method)
		{
			body = method.Body;
			ComputeHeader ();
			if (RequiresFatHeader ())
				WriteFatHeader ();
			else
				WriteByte ((byte) (0x2 | (body.CodeSize << 2))); // tiny

			WriteInstructions ();

			if (body.HasExceptionHandlers)
				WriteExceptionHandlers ();

			var symbol_writer = metadata.symbol_writer;
			if (symbol_writer != null)
				symbol_writer.Write (body);
		}

		void WriteFatHeader ()
		{
			var body = this.body;
			byte flags = 0x3;	// fat
			if (body.InitLocals)
				flags |= 0x10;	// init locals
			if (body.HasExceptionHandlers)
				flags |= 0x8;	// more sections

			WriteByte (flags);
			WriteByte (0x30);
			WriteInt16 ((short) body.max_stack_size);
			WriteInt32 (body.code_size);
			body.local_var_token = body.HasVariables
				? GetStandAloneSignature (body.Variables)
				: MetadataToken.Zero;
			WriteMetadataToken (body.local_var_token);
		}

		void WriteInstructions ()
		{
			var instructions = body.Instructions;
			var items = instructions.items;
			var size = instructions.size;

			for (int i = 0; i < size; i++) {
				var instruction = items [i];
				WriteOpCode (instruction.opcode);
				WriteOperand (instruction);
			}
		}

		void WriteOpCode (OpCode opcode)
		{
			if (opcode.Size == 1) {
				WriteByte (opcode.Op2);
			} else {
				WriteByte (opcode.Op1);
				WriteByte (opcode.Op2);
			}
		}

		void WriteOperand (Instruction instruction)
		{
			var opcode = instruction.opcode;
			var operand_type = opcode.OperandType;
			if (operand_type == OperandType.InlineNone)
				return;

			var operand = instruction.operand;
			if (operand == null)
				throw new ArgumentException ();

			switch (operand_type) {
			case OperandType.InlineSwitch: {
				var targets = (Instruction []) operand;
				WriteInt32 (targets.Length);
				var diff = instruction.Offset + opcode.Size + (4 * (targets.Length + 1));
				for (int i = 0; i < targets.Length; i++)
					WriteInt32 (GetTargetOffset (targets [i]) - diff);
				break;
			}
			case OperandType.ShortInlineBrTarget: {
				var target = (Instruction) operand;
				WriteSByte ((sbyte) (GetTargetOffset (target) - (instruction.Offset + opcode.Size + 1)));
				break;
			}
			case OperandType.InlineBrTarget: {
				var target = (Instruction) operand;
				WriteInt32 (GetTargetOffset (target) - (instruction.Offset + opcode.Size + 4));
				break;
			}
			case OperandType.ShortInlineVar:
				WriteByte ((byte) GetVariableIndex ((VariableDefinition) operand));
				break;
			case OperandType.ShortInlineArg:
				WriteByte ((byte) GetParameterIndex ((ParameterDefinition) operand));
				break;
			case OperandType.InlineVar:
				WriteInt16 ((short) GetVariableIndex ((VariableDefinition) operand));
				break;
			case OperandType.InlineArg:
				WriteInt16 ((short) GetParameterIndex ((ParameterDefinition) operand));
				break;
			case OperandType.InlineSig:
				WriteMetadataToken (GetStandAloneSignature ((CallSite) operand));
				break;
			case OperandType.ShortInlineI:
				if (opcode == OpCodes.Ldc_I4_S)
					WriteSByte ((sbyte) operand);
				else
					WriteByte ((byte) operand);
				break;
			case OperandType.InlineI:
				WriteInt32 ((int) operand);
				break;
			case OperandType.InlineI8:
				WriteInt64 ((long) operand);
				break;
			case OperandType.ShortInlineR:
				WriteSingle ((float) operand);
				break;
			case OperandType.InlineR:
				WriteDouble ((double) operand);
				break;
			case OperandType.InlineString:
				WriteMetadataToken (
					new MetadataToken (
						TokenType.String,
						GetUserStringIndex ((string) operand)));
				break;
			case OperandType.InlineType:
			case OperandType.InlineField:
			case OperandType.InlineMethod:
			case OperandType.InlineTok:
				WriteMetadataToken (metadata.LookupToken ((IMetadataTokenProvider) operand));
				break;
			default:
				throw new ArgumentException ();
			}
		}

		int GetTargetOffset (Instruction instruction)
		{
			if (instruction == null) {
				var last = body.instructions [body.instructions.size - 1];
				return last.offset + last.GetSize ();
			}

			return instruction.offset;
		}

		uint GetUserStringIndex (string @string)
		{
			if (@string == null)
				return 0;

			return metadata.user_string_heap.GetStringIndex (@string);
		}

		static int GetVariableIndex (VariableDefinition variable)
		{
			return variable.Index;
		}

		int GetParameterIndex (ParameterDefinition parameter)
		{
			if (body.method.HasThis) {
				if (parameter == body.this_parameter)
					return 0;

				return parameter.Index + 1;
			}

			return parameter.Index;
		}

		bool RequiresFatHeader ()
		{
			var body = this.body;
			return body.CodeSize >= 64
				|| body.InitLocals
				|| body.HasVariables
				|| body.HasExceptionHandlers
				|| body.MaxStackSize > 8;
		}

		void ComputeHeader ()
		{
			int offset = 0;
			var instructions = body.instructions;
			var items = instructions.items;
			var count = instructions.size;
			var stack_size = 0;
			var max_stack = 0;
			Dictionary<Instruction, int> stack_sizes = null;

			if (body.HasExceptionHandlers)
				ComputeExceptionHandlerStackSize (ref stack_sizes);

			for (int i = 0; i < count; i++) {
				var instruction = items [i];
				instruction.offset = offset;
				offset += instruction.GetSize ();

				ComputeStackSize (instruction, ref stack_sizes, ref stack_size, ref max_stack);
			}

			body.code_size = offset;
			body.max_stack_size = max_stack;
		}

		void ComputeExceptionHandlerStackSize (ref Dictionary<Instruction, int> stack_sizes)
		{
			var exception_handlers = body.ExceptionHandlers;

			for (int i = 0; i < exception_handlers.Count; i++) {
				var exception_handler = exception_handlers [i];

				switch (exception_handler.HandlerType) {
				case ExceptionHandlerType.Catch:
					AddExceptionStackSize (exception_handler.HandlerStart, ref stack_sizes);
					break;
				case ExceptionHandlerType.Filter:
					AddExceptionStackSize (exception_handler.FilterStart, ref stack_sizes);
					AddExceptionStackSize (exception_handler.HandlerStart, ref stack_sizes);
					break;
				}
			}
		}

		static void AddExceptionStackSize (Instruction handler_start, ref Dictionary<Instruction, int> stack_sizes)
		{
			if (handler_start == null)
				return;

			if (stack_sizes == null)
				stack_sizes = new Dictionary<Instruction, int> ();

			stack_sizes [handler_start] = 1;
		}

		static void ComputeStackSize (Instruction instruction, ref Dictionary<Instruction, int> stack_sizes, ref int stack_size, ref int max_stack)
		{
			int computed_size;
			if (stack_sizes != null && stack_sizes.TryGetValue (instruction, out computed_size))
				stack_size = computed_size;

			max_stack = System.Math.Max (max_stack, stack_size);
			ComputeStackDelta (instruction, ref stack_size);
			max_stack = System.Math.Max (max_stack, stack_size);

			CopyBranchStackSize (instruction, ref stack_sizes, stack_size);
			ComputeStackSize (instruction, ref stack_size);
		}

		static void CopyBranchStackSize (Instruction instruction, ref Dictionary<Instruction, int> stack_sizes, int stack_size)
		{
			if (stack_size == 0)
				return;

			switch (instruction.opcode.OperandType) {
			case OperandType.ShortInlineBrTarget:
			case OperandType.InlineBrTarget:
				CopyBranchStackSize (ref stack_sizes, (Instruction) instruction.operand, stack_size);
				break;
			case OperandType.InlineSwitch:
				var targets = (Instruction[]) instruction.operand;
				for (int i = 0; i < targets.Length; i++)
					CopyBranchStackSize (ref stack_sizes, targets [i], stack_size);
				break;
			}
		}

		static void CopyBranchStackSize (ref Dictionary<Instruction, int> stack_sizes, Instruction target, int stack_size)
		{
			if (stack_sizes == null)
				stack_sizes = new Dictionary<Instruction, int> ();

			int branch_stack_size = stack_size;

			int computed_size;
			if (stack_sizes.TryGetValue (target, out computed_size))
				branch_stack_size = System.Math.Max (branch_stack_size, computed_size);

			stack_sizes [target] = branch_stack_size;
		}

		static void ComputeStackSize (Instruction instruction, ref int stack_size)
		{
			switch (instruction.opcode.FlowControl) {
			case FlowControl.Branch:
			case FlowControl.Break:
			case FlowControl.Throw:
			case FlowControl.Return:
				stack_size = 0;
				break;
			}
		}

		static void ComputeStackDelta (Instruction instruction, ref int stack_size)
		{
			switch (instruction.opcode.FlowControl) {
			case FlowControl.Call: {
				var method = (IMethodSignature) instruction.operand;
				// pop 'this' argument
				if (method.HasImplicitThis() && instruction.opcode.Code != Code.Newobj)
					stack_size--;
				// pop normal arguments
				if (method.HasParameters)
					stack_size -= method.Parameters.Count;
				// pop function pointer
				if (instruction.opcode.Code == Code.Calli)
					stack_size--;
				// push return value
				if (method.ReturnType.etype != ElementType.Void || instruction.opcode.Code == Code.Newobj)
					stack_size++;
				break;
			}
			default:
				ComputePopDelta (instruction.opcode.StackBehaviourPop, ref stack_size);
				ComputePushDelta (instruction.opcode.StackBehaviourPush, ref stack_size);
				break;
			}
		}

		static void ComputePopDelta (StackBehaviour pop_behavior, ref int stack_size)
		{
			switch (pop_behavior) {
			case StackBehaviour.Popi:
			case StackBehaviour.Popref:
			case StackBehaviour.Pop1:
				stack_size--;
				break;
			case StackBehaviour.Pop1_pop1:
			case StackBehaviour.Popi_pop1:
			case StackBehaviour.Popi_popi:
			case StackBehaviour.Popi_popi8:
			case StackBehaviour.Popi_popr4:
			case StackBehaviour.Popi_popr8:
			case StackBehaviour.Popref_pop1:
			case StackBehaviour.Popref_popi:
				stack_size -= 2;
				break;
			case StackBehaviour.Popi_popi_popi:
			case StackBehaviour.Popref_popi_popi:
			case StackBehaviour.Popref_popi_popi8:
			case StackBehaviour.Popref_popi_popr4:
			case StackBehaviour.Popref_popi_popr8:
			case StackBehaviour.Popref_popi_popref:
				stack_size -= 3;
				break;
			case StackBehaviour.PopAll:
				stack_size = 0;
				break;
			}
		}

		static void ComputePushDelta (StackBehaviour push_behaviour, ref int stack_size)
		{
			switch (push_behaviour) {
			case StackBehaviour.Push1:
			case StackBehaviour.Pushi:
			case StackBehaviour.Pushi8:
			case StackBehaviour.Pushr4:
			case StackBehaviour.Pushr8:
			case StackBehaviour.Pushref:
				stack_size++;
				break;
			case StackBehaviour.Push1_push1:
				stack_size += 2;
				break;
			}
		}

		void WriteExceptionHandlers ()
		{
			Align (4);

			var handlers = body.ExceptionHandlers;

			if (handlers.Count < 0x15 && !RequiresFatSection (handlers))
				WriteSmallSection (handlers);
			else
				WriteFatSection (handlers);
		}

		static bool RequiresFatSection (Collection<ExceptionHandler> handlers)
		{
			for (int i = 0; i < handlers.Count; i++) {
				var handler = handlers [i];

				if (IsFatRange (handler.TryStart, handler.TryEnd))
					return true;

				if (IsFatRange (handler.HandlerStart, handler.HandlerEnd))
					return true;

				if (handler.HandlerType == ExceptionHandlerType.Filter
					&& IsFatRange (handler.FilterStart, handler.HandlerStart))
					return true;
			}

			return false;
		}

		static bool IsFatRange (Instruction start, Instruction end)
		{
			if (start == null)
				throw new ArgumentException ();

			if (end == null)
				return true;

			return end.Offset - start.Offset > 255 || start.Offset > 65535;
		}

		void WriteSmallSection (Collection<ExceptionHandler> handlers)
		{
			const byte eh_table = 0x1;

			WriteByte (eh_table);
			WriteByte ((byte) (handlers.Count * 12 + 4));
			WriteBytes (2);

			WriteExceptionHandlers (
				handlers,
				i => WriteUInt16 ((ushort) i),
				i => WriteByte ((byte) i));
		}

		void WriteFatSection (Collection<ExceptionHandler> handlers)
		{
			const byte eh_table = 0x1;
			const byte fat_format = 0x40;

			WriteByte (eh_table | fat_format);

			int size = handlers.Count * 24 + 4;
			WriteByte ((byte) (size & 0xff));
			WriteByte ((byte) ((size >> 8) & 0xff));
			WriteByte ((byte) ((size >> 16) & 0xff));

			WriteExceptionHandlers (handlers, WriteInt32, WriteInt32);
		}

		void WriteExceptionHandlers (Collection<ExceptionHandler> handlers, Action<int> write_entry, Action<int> write_length)
		{
			for (int i = 0; i < handlers.Count; i++) {
				var handler = handlers [i];

				write_entry ((int) handler.HandlerType);

				write_entry (handler.TryStart.Offset);
				write_length (GetTargetOffset (handler.TryEnd) - handler.TryStart.Offset);

				write_entry (handler.HandlerStart.Offset);
				write_length (GetTargetOffset (handler.HandlerEnd) - handler.HandlerStart.Offset);

				WriteExceptionHandlerSpecific (handler);
			}
		}

		void WriteExceptionHandlerSpecific (ExceptionHandler handler)
		{
			switch (handler.HandlerType) {
			case ExceptionHandlerType.Catch:
				WriteMetadataToken (metadata.LookupToken (handler.CatchType));
				break;
			case ExceptionHandlerType.Filter:
				WriteInt32 (handler.FilterStart.Offset);
				break;
			default:
				WriteInt32 (0);
				break;
			}
		}

		public MetadataToken GetStandAloneSignature (Collection<VariableDefinition> variables)
		{
			var signature = metadata.GetLocalVariableBlobIndex (variables);

			return GetStandAloneSignatureToken (signature);
		}

		public MetadataToken GetStandAloneSignature (CallSite call_site)
		{
			var signature = metadata.GetCallSiteBlobIndex (call_site);
			var token = GetStandAloneSignatureToken (signature);
			call_site.MetadataToken = token;
			return token;
		}

		MetadataToken GetStandAloneSignatureToken (uint signature)
		{
			MetadataToken token;
			if (standalone_signatures.TryGetValue (signature, out token))
				return token;

			token = new MetadataToken (TokenType.Signature, metadata.AddStandAloneSignature (signature));
			standalone_signatures.Add (signature, token);
			return token;
		}

		RVA BeginMethod ()
		{
			return current;
		}

		void WriteMetadataToken (MetadataToken token)
		{
			WriteUInt32 (token.ToUInt32 ());
		}

		void Align (int align)
		{
			align--;
			WriteBytes (((position + align) & ~align) - position);
		}

		void EndMethod ()
		{
			current = (RVA) (code_base + position);
		}
	}
}

#endif
