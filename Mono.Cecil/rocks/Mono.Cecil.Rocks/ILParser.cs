//
// ILParser.cs
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

using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace Mono.Cecil.Rocks {

#if INSIDE_ROCKS
	public
#endif
	interface IILVisitor {
		void OnInlineNone (OpCode opcode);
		void OnInlineSByte (OpCode opcode, sbyte value);
		void OnInlineByte (OpCode opcode, byte value);
		void OnInlineInt32 (OpCode opcode, int value);
		void OnInlineInt64 (OpCode opcode, long value);
		void OnInlineSingle (OpCode opcode, float value);
		void OnInlineDouble (OpCode opcode, double value);
		void OnInlineString (OpCode opcode, string value);
		void OnInlineBranch (OpCode opcode, int offset);
		void OnInlineSwitch (OpCode opcode, int [] offsets);
		void OnInlineVariable (OpCode opcode, VariableDefinition variable);
		void OnInlineArgument (OpCode opcode, ParameterDefinition parameter);
		void OnInlineSignature (OpCode opcode, CallSite callSite);
		void OnInlineType (OpCode opcode, TypeReference type);
		void OnInlineField (OpCode opcode, FieldReference field);
		void OnInlineMethod (OpCode opcode, MethodReference method);
	}

#if INSIDE_ROCKS
	public
#endif
	static class ILParser {

		class ParseContext {
			public CodeReader Code { get; set; }
			public MetadataReader Metadata { get; set; }
			public Collection<VariableDefinition> Variables { get; set; }
			public IILVisitor Visitor { get; set; }
		}

		public static void Parse (MethodDefinition method, IILVisitor visitor)
		{
			if (method == null)
				throw new ArgumentNullException ("method");
			if (visitor == null)
				throw new ArgumentNullException ("visitor");
			if (!method.HasBody || !method.HasImage)
				throw new ArgumentException ();

			var context = CreateContext (method, visitor);
			var code = context.Code;

			code.MoveTo (method.RVA);

			var flags = code.ReadByte ();

			switch (flags & 0x3) {
			case 0x2: // tiny
				int code_size = flags >> 2;
				ParseCode (code_size, context);
				break;
			case 0x3: // fat
				code.position--;
				ParseFatMethod (context);
				break;
			default:
				throw new NotSupportedException ();
			}
		}

		static ParseContext CreateContext (MethodDefinition method, IILVisitor visitor)
		{
			var code = method.Module.Read (method, (_, reader) => new CodeReader (reader.image.MetadataSection, reader));

			return new ParseContext {
				Code = code,
				Metadata = code.reader,
				Visitor = visitor,
			};
		}

		static void ParseFatMethod (ParseContext context)
		{
			var code = context.Code;

			code.Advance (4);
			var code_size = code.ReadInt32 ();
			var local_var_token = code.ReadToken ();

			if (local_var_token != MetadataToken.Zero)
				context.Variables = code.ReadVariables (local_var_token);

			ParseCode (code_size, context);
		}

		static void ParseCode (int code_size, ParseContext context)
		{
			var code = context.Code;
			var metadata = context.Metadata;
			var visitor = context.Visitor;

			var start = code.position;
			var end = start + code_size;

			while (code.position < end) {
				var il_opcode = code.ReadByte ();
				var opcode = il_opcode != 0xfe
					? OpCodes.OneByteOpCode [il_opcode]
					: OpCodes.TwoBytesOpCode [code.ReadByte ()];

				switch (opcode.OperandType) {
				case OperandType.InlineNone:
					visitor.OnInlineNone (opcode);
					break;
				case OperandType.InlineSwitch:
					var length = code.ReadInt32 ();
					var branches = new int [length];
					for (int i = 0; i < length; i++)
						branches [i] = code.ReadInt32 ();
					visitor.OnInlineSwitch (opcode, branches);
					break;
				case OperandType.ShortInlineBrTarget:
					visitor.OnInlineBranch (opcode, code.ReadSByte ());
					break;
				case OperandType.InlineBrTarget:
					visitor.OnInlineBranch (opcode, code.ReadInt32 ());
					break;
				case OperandType.ShortInlineI:
					if (opcode == OpCodes.Ldc_I4_S)
						visitor.OnInlineSByte (opcode, code.ReadSByte ());
					else
						visitor.OnInlineByte (opcode, code.ReadByte ());
					break;
				case OperandType.InlineI:
					visitor.OnInlineInt32 (opcode, code.ReadInt32 ());
					break;
				case OperandType.InlineI8:
					visitor.OnInlineInt64 (opcode, code.ReadInt64 ());
					break;
				case OperandType.ShortInlineR:
					visitor.OnInlineSingle (opcode, code.ReadSingle ());
					break;
				case OperandType.InlineR:
					visitor.OnInlineDouble (opcode, code.ReadDouble ());
					break;
				case OperandType.InlineSig:
					visitor.OnInlineSignature (opcode, code.GetCallSite (code.ReadToken ()));
					break;
				case OperandType.InlineString:
					visitor.OnInlineString (opcode, code.GetString (code.ReadToken ()));
					break;
				case OperandType.ShortInlineArg:
					visitor.OnInlineArgument (opcode, code.GetParameter (code.ReadByte ()));
					break;
				case OperandType.InlineArg:
					visitor.OnInlineArgument (opcode, code.GetParameter (code.ReadInt16 ()));
					break;
				case OperandType.ShortInlineVar:
					visitor.OnInlineVariable (opcode, GetVariable (context, code.ReadByte ()));
					break;
				case OperandType.InlineVar:
					visitor.OnInlineVariable (opcode, GetVariable (context, code.ReadInt16 ()));
					break;
				case OperandType.InlineTok:
				case OperandType.InlineField:
				case OperandType.InlineMethod:
				case OperandType.InlineType:
					var member = metadata.LookupToken (code.ReadToken ());
					switch (member.MetadataToken.TokenType) {
					case TokenType.TypeDef:
					case TokenType.TypeRef:
					case TokenType.TypeSpec:
						visitor.OnInlineType (opcode, (TypeReference) member);
						break;
					case TokenType.Method:
					case TokenType.MethodSpec:
						visitor.OnInlineMethod (opcode, (MethodReference) member);
						break;
					case TokenType.Field:
						visitor.OnInlineField (opcode, (FieldReference) member);
						break;
					case TokenType.MemberRef:
						var field_ref = member as FieldReference;
						if (field_ref != null) {
							visitor.OnInlineField (opcode, field_ref);
							break;
						}

						var method_ref = member as MethodReference;
						if (method_ref != null) {
							visitor.OnInlineMethod (opcode, method_ref);
							break;
						}

						throw new InvalidOperationException ();
					}
					break;
				}
			}
		}

		static VariableDefinition GetVariable (ParseContext context, int index)
		{
			return context.Variables [index];
		}
	}
}
