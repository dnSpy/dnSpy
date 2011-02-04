//
// CodeReader.cs
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

using Mono.Cecil.PE;
using Mono.Collections.Generic;

using RVA = System.UInt32;

namespace Mono.Cecil.Cil {

	sealed class CodeReader : ByteBuffer {

		readonly internal MetadataReader reader;

		int start;
		Section code_section;

		MethodDefinition method;
		MethodBody body;

		int Offset {
			get { return base.position - start; }
		}

		CodeReader (Section section, MetadataReader reader)
			: base (section.Data)
		{
			this.code_section = section;
			this.reader = reader;
		}

		public static CodeReader CreateCodeReader (MetadataReader metadata)
		{
			return new CodeReader (metadata.image.MetadataSection, metadata);
		}

		public MethodBody ReadMethodBody (MethodDefinition method)
		{
			this.method = method;
			this.body = new MethodBody (method);

			reader.context = method;

			ReadMethodBody ();

			return this.body;
		}

		public void MoveTo (int rva)
		{
			if (!IsInSection (rva)) {
				code_section = reader.image.GetSectionAtVirtualAddress ((uint) rva);
				Reset (code_section.Data);
			}

			base.position = rva - (int) code_section.VirtualAddress;
		}

		bool IsInSection (int rva)
		{
			return code_section.VirtualAddress <= rva && rva < code_section.VirtualAddress + code_section.SizeOfRawData;
		}

		void ReadMethodBody ()
		{
			MoveTo (method.RVA);

			var flags = ReadByte ();
			switch (flags & 0x3) {
			case 0x2: // tiny
				body.code_size = flags >> 2;
				body.MaxStackSize = 8;
				ReadCode ();
				break;
			case 0x3: // fat
				base.position--;
				ReadFatMethod ();
				break;
			default:
				throw new InvalidOperationException ();
			}

			var symbol_reader = reader.module.SymbolReader;

			if (symbol_reader != null) {
				var instructions = body.Instructions;
				symbol_reader.Read (body, offset => GetInstruction (instructions, offset));
			}
		}

		void ReadFatMethod ()
		{
			var flags = ReadUInt16 ();
			body.max_stack_size = ReadUInt16 ();
			body.code_size = (int) ReadUInt32 ();
			body.local_var_token = new MetadataToken (ReadUInt32 ());
			body.init_locals = (flags & 0x10) != 0;

			if (body.local_var_token.RID != 0)
				body.variables = ReadVariables (body.local_var_token);

			ReadCode ();

			if ((flags & 0x8) != 0)
				ReadSection ();
		}

		public VariableDefinitionCollection ReadVariables (MetadataToken local_var_token)
		{
			var position = reader.position;
			var variables = reader.ReadVariables (local_var_token);
			reader.position = position;

			return variables;
		}

		void ReadCode ()
		{
			start = position;
			var code_size = body.code_size;

			if (code_size < 0 || buffer.Length <= (uint) (code_size + position))
				code_size = 0;

			var end = start + code_size;
			var instructions = body.instructions = new InstructionCollection (code_size / 3);

			while (position < end) {
				var offset = base.position - start;
				var opcode = ReadOpCode ();
				var current = new Instruction (offset, opcode);

				if (opcode.OperandType != OperandType.InlineNone)
					current.operand = ReadOperand (current);

				instructions.Add (current);
			}

			ResolveBranches (instructions);
		}

		OpCode ReadOpCode ()
		{
			var il_opcode = ReadByte ();
			return il_opcode != 0xfe
				? OpCodes.OneByteOpCode [il_opcode]
				: OpCodes.TwoBytesOpCode [ReadByte ()];
		}

		object ReadOperand (Instruction instruction)
		{
			switch (instruction.opcode.OperandType) {
			case OperandType.InlineSwitch:
				var length = ReadInt32 ();
				var base_offset = Offset + (4 * length);
				var branches = new int [length];
				for (int i = 0; i < length; i++)
					branches [i] = base_offset + ReadInt32 ();
				return branches;
			case OperandType.ShortInlineBrTarget:
				return ReadSByte () + Offset;
			case OperandType.InlineBrTarget:
				return ReadInt32 () + Offset;
			case OperandType.ShortInlineI:
				if (instruction.opcode == OpCodes.Ldc_I4_S)
					return ReadSByte ();

				return ReadByte ();
			case OperandType.InlineI:
				return ReadInt32 ();
			case OperandType.ShortInlineR:
				return ReadSingle ();
			case OperandType.InlineR:
				return ReadDouble ();
			case OperandType.InlineI8:
				return ReadInt64 ();
			case OperandType.ShortInlineVar:
				return GetVariable (ReadByte ());
			case OperandType.InlineVar:
				return GetVariable (ReadUInt16 ());
			case OperandType.ShortInlineArg:
				return GetParameter (ReadByte ());
			case OperandType.InlineArg:
				return GetParameter (ReadUInt16 ());
			case OperandType.InlineSig:
				return GetCallSite (ReadToken ());
			case OperandType.InlineString:
				return GetString (ReadToken ());
			case OperandType.InlineTok:
			case OperandType.InlineType:
			case OperandType.InlineMethod:
			case OperandType.InlineField:
				return reader.LookupToken (ReadToken ());
			default:
				throw new NotSupportedException ();
			}
		}

		public string GetString (MetadataToken token)
		{
			return reader.image.UserStringHeap.Read (token.RID);
		}

		public ParameterDefinition GetParameter (int index)
		{
			return body.GetParameter (index);
		}

		public VariableDefinition GetVariable (int index)
		{
			return body.GetVariable (index);
		}

		public CallSite GetCallSite (MetadataToken token)
		{
			return reader.ReadCallSite (token);
		}

		void ResolveBranches (Collection<Instruction> instructions)
		{
			var items = instructions.items;
			var size = instructions.size;

			for (int i = 0; i < size; i++) {
				var instruction = items [i];
				switch (instruction.opcode.OperandType) {
				case OperandType.ShortInlineBrTarget:
				case OperandType.InlineBrTarget:
					instruction.operand = GetInstruction ((int) instruction.operand);
					break;
				case OperandType.InlineSwitch:
					var offsets = (int []) instruction.operand;
					var branches = new Instruction [offsets.Length];
					for (int j = 0; j < offsets.Length; j++)
						branches [j] = GetInstruction (offsets [j]);

					instruction.operand = branches;
					break;
				}
			}
		}

		Instruction GetInstruction (int offset)
		{
			return GetInstruction (body.Instructions, offset);
		}

		static Instruction GetInstruction (Collection<Instruction> instructions, int offset)
		{
			var size = instructions.size;
			var items = instructions.items;
			if (offset < 0 || offset > items [size - 1].offset)
				return null;

			int min = 0;
			int max = size - 1;
			while (min <= max) {
				int mid = min + ((max - min) / 2);
				var instruction = items [mid];
				var instruction_offset = instruction.offset;

				if (offset == instruction_offset)
					return instruction;

				if (offset < instruction_offset)
					max = mid - 1;
				else
					min = mid + 1;
			}

			return null;
		}

		void ReadSection ()
		{
			Align (4);

			const byte fat_format = 0x40;
			const byte more_sects = 0x80;

			var flags = ReadByte ();
			if ((flags & fat_format) == 0)
				ReadSmallSection ();
			else
				ReadFatSection ();

			if ((flags & more_sects) != 0)
				ReadSection ();
		}

		void ReadSmallSection ()
		{
			var count = ReadByte () / 12;
			Advance (2);

			ReadExceptionHandlers (
				count,
				() => (int) ReadUInt16 (),
				() => (int) ReadByte ());
		}

		void ReadFatSection ()
		{
			position--;
			var count = (ReadInt32 () >> 8) / 24;

			ReadExceptionHandlers (
				count,
				ReadInt32,
				ReadInt32);
		}

		// inline ?
		void ReadExceptionHandlers (int count, Func<int> read_entry, Func<int> read_length)
		{
			for (int i = 0; i < count; i++) {
				var handler = new ExceptionHandler (
					(ExceptionHandlerType) (read_entry () & 0x7));

				handler.TryStart = GetInstruction (read_entry ());
				handler.TryEnd = GetInstruction (handler.TryStart.Offset + read_length ());

				handler.HandlerStart = GetInstruction (read_entry ());
				handler.HandlerEnd = GetInstruction (handler.HandlerStart.Offset + read_length ());

				ReadExceptionHandlerSpecific (handler);

				this.body.ExceptionHandlers.Add (handler);
			}
		}

		void ReadExceptionHandlerSpecific (ExceptionHandler handler)
		{
			switch (handler.HandlerType) {
			case ExceptionHandlerType.Catch:
				handler.CatchType = (TypeReference) reader.LookupToken (ReadToken ());
				break;
			case ExceptionHandlerType.Filter:
				handler.FilterStart = GetInstruction (ReadInt32 ());
				handler.FilterEnd = handler.HandlerStart.Previous;
				break;
			default:
				Advance (4);
				break;
			}
		}

		void Align (int align)
		{
			align--;
			Advance (((position + align) & ~align) - position);
		}

		public MetadataToken ReadToken ()
		{
			return new MetadataToken (ReadUInt32 ());
		}

#if !READ_ONLY

		public ByteBuffer PatchRawMethodBody (MethodDefinition method, CodeWriter writer, out MethodSymbols symbols)
		{
			var buffer = new ByteBuffer ();
			symbols = new MethodSymbols (method.Name);

			this.method = method;
			reader.context = method;

			MoveTo (method.RVA);

			var flags = ReadByte ();

			MetadataToken local_var_token;

			switch (flags & 0x3) {
			case 0x2: // tiny
				buffer.WriteByte (flags);
				local_var_token = MetadataToken.Zero;
				symbols.code_size = flags >> 2;
				PatchRawCode (buffer, symbols.code_size, writer);
				break;
			case 0x3: // fat
				base.position--;

				PatchRawFatMethod (buffer, symbols, writer, out local_var_token);
				break;
			default:
				throw new NotSupportedException ();
			}

			var symbol_reader = reader.module.SymbolReader;
			if (symbol_reader != null && writer.metadata.write_symbols) {
				symbols.method_token = GetOriginalToken (writer.metadata, method);
				symbols.local_var_token = local_var_token;
				symbol_reader.Read (symbols);
			}

			return buffer;
		}

		void PatchRawFatMethod (ByteBuffer buffer, MethodSymbols symbols, CodeWriter writer, out MetadataToken local_var_token)
		{
			var flags = ReadUInt16 ();
			buffer.WriteUInt16 (flags);
			buffer.WriteUInt16 (ReadUInt16 ());
			symbols.code_size = ReadInt32 ();
			buffer.WriteInt32 (symbols.code_size);
			local_var_token = ReadToken ();

			if (local_var_token.RID > 0) {
				var variables = symbols.variables = ReadVariables (local_var_token);
				buffer.WriteUInt32 (variables != null
					? writer.GetStandAloneSignature (symbols.variables).ToUInt32 ()
					: 0);
			} else
				buffer.WriteUInt32 (0);

			PatchRawCode (buffer, symbols.code_size, writer);

			if ((flags & 0x8) != 0)
				PatchRawSection (buffer, writer.metadata);
		}

		static MetadataToken GetOriginalToken (MetadataBuilder metadata, MethodDefinition method)
		{
			MetadataToken original;
			if (metadata.TryGetOriginalMethodToken (method.token, out original))
				return original;

			return MetadataToken.Zero;
		}

		void PatchRawCode (ByteBuffer buffer, int code_size, CodeWriter writer)
		{
			var metadata = writer.metadata;
			buffer.WriteBytes (ReadBytes (code_size));
			var end = buffer.position;
			buffer.position -= code_size;

			while (buffer.position < end) {
				OpCode opcode;
				var il_opcode = buffer.ReadByte ();
				if (il_opcode != 0xfe) {
					opcode = OpCodes.OneByteOpCode [il_opcode];
				} else {
					var il_opcode2 = buffer.ReadByte ();
					opcode = OpCodes.TwoBytesOpCode [il_opcode2];
				}

				switch (opcode.OperandType) {
				case OperandType.ShortInlineI:
				case OperandType.ShortInlineBrTarget:
				case OperandType.ShortInlineVar:
				case OperandType.ShortInlineArg:
					buffer.position += 1;
					break;
				case OperandType.InlineVar:
				case OperandType.InlineArg:
					buffer.position += 2;
					break;
				case OperandType.InlineBrTarget:
				case OperandType.ShortInlineR:
				case OperandType.InlineI:
					buffer.position += 4;
					break;
				case OperandType.InlineI8:
				case OperandType.InlineR:
					buffer.position += 8;
					break;
				case OperandType.InlineSwitch:
					var length = buffer.ReadInt32 ();
					buffer.position += length * 4;
					break;
				case OperandType.InlineString:
					var @string = GetString (new MetadataToken (buffer.ReadUInt32 ()));
					buffer.position -= 4;
					buffer.WriteUInt32 (
						new MetadataToken (
							TokenType.String,
							metadata.user_string_heap.GetStringIndex (@string)).ToUInt32 ());
					break;
				case OperandType.InlineSig:
					var call_site = GetCallSite (new MetadataToken (buffer.ReadUInt32 ()));
					buffer.position -= 4;
					buffer.WriteUInt32 (writer.GetStandAloneSignature (call_site).ToUInt32 ());
					break;
				case OperandType.InlineTok:
				case OperandType.InlineType:
				case OperandType.InlineMethod:
				case OperandType.InlineField:
					var provider = reader.LookupToken (new MetadataToken (buffer.ReadUInt32 ()));
					buffer.position -= 4;
					buffer.WriteUInt32 (metadata.LookupToken (provider).ToUInt32 ());
					break;
				}
			}
		}

		void PatchRawSection (ByteBuffer buffer, MetadataBuilder metadata)
		{
			var position = base.position;
			Align (4);
			buffer.WriteBytes (base.position - position);

			const byte fat_format = 0x40;
			const byte more_sects = 0x80;

			var flags = ReadByte ();
			if ((flags & fat_format) == 0) {
				buffer.WriteByte (flags);
				PatchRawSmallSection (buffer, metadata);
			} else
				PatchRawFatSection (buffer, metadata);

			if ((flags & more_sects) != 0)
				PatchRawSection (buffer, metadata);
		}

		void PatchRawSmallSection (ByteBuffer buffer, MetadataBuilder metadata)
		{
			var length = ReadByte ();
			buffer.WriteByte (length);
			Advance (2);

			buffer.WriteUInt16 (0);

			var count = length / 12;

			PatchRawExceptionHandlers (buffer, metadata, count, false);
		}

		void PatchRawFatSection (ByteBuffer buffer, MetadataBuilder metadata)
		{
			position--;
			var length = ReadInt32 ();
			buffer.WriteInt32 (length);

			var count = (length >> 8) / 24;

			PatchRawExceptionHandlers (buffer, metadata, count, true);
		}

		void PatchRawExceptionHandlers (ByteBuffer buffer, MetadataBuilder metadata, int count, bool fat_entry)
		{
			const int fat_entry_size = 16;
			const int small_entry_size = 6;

			for (int i = 0; i < count; i++) {
				ExceptionHandlerType handler_type;
				if (fat_entry) {
					var type = ReadUInt32 ();
					handler_type = (ExceptionHandlerType) (type & 0x7);
					buffer.WriteUInt32 (type);
				} else {
					var type = ReadUInt16 ();
					handler_type = (ExceptionHandlerType) (type & 0x7);
					buffer.WriteUInt16 (type);
				}

				buffer.WriteBytes (ReadBytes (fat_entry ? fat_entry_size : small_entry_size));

				switch (handler_type) {
				case ExceptionHandlerType.Catch:
					var exception = reader.LookupToken (ReadToken ());
					buffer.WriteUInt32 (metadata.LookupToken (exception).ToUInt32 ());
					break;
				default:
					buffer.WriteUInt32 (ReadUInt32 ());
					break;
				}
			}
		}

#endif

	}
}
