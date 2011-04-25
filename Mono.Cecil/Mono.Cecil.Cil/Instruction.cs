//
// Instruction.cs
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
using System.Text;

namespace Mono.Cecil.Cil {

	public sealed class Instruction {

		internal int offset;
		internal OpCode opcode;
		internal object operand;

		internal Instruction previous;
		internal Instruction next;

		SequencePoint sequence_point;

		public int Offset {
			get { return offset; }
			set { offset = value; }
		}

		public OpCode OpCode {
			get { return opcode; }
			set { opcode = value; }
		}

		public object Operand {
			get { return operand; }
			set { operand = value; }
		}

		public Instruction Previous {
			get { return previous; }
			set { previous = value; }
		}

		public Instruction Next {
			get { return next; }
			set { next = value; }
		}

		public SequencePoint SequencePoint {
			get { return sequence_point; }
			set { sequence_point = value; }
		}

		internal Instruction (int offset, OpCode opCode)
		{
			this.offset = offset;
			this.opcode = opCode;
		}

		internal Instruction (OpCode opcode, object operand)
		{
			this.opcode = opcode;
			this.operand = operand;
		}

		public int GetSize ()
		{
			int size = opcode.Size;

			switch (opcode.OperandType) {
			case OperandType.InlineSwitch:
				return size + (1 + ((Instruction []) operand).Length) * 4;
			case OperandType.InlineI8:
			case OperandType.InlineR:
				return size + 8;
			case OperandType.InlineBrTarget:
			case OperandType.InlineField:
			case OperandType.InlineI:
			case OperandType.InlineMethod:
			case OperandType.InlineString:
			case OperandType.InlineTok:
			case OperandType.InlineType:
			case OperandType.ShortInlineR:
			case OperandType.InlineSig:
				return size + 4;
			case OperandType.InlineArg:
			case OperandType.InlineVar:
				return size + 2;
			case OperandType.ShortInlineBrTarget:
			case OperandType.ShortInlineI:
			case OperandType.ShortInlineArg:
			case OperandType.ShortInlineVar:
				return size + 1;
			default:
				return size;
			}
		}

		public override string ToString ()
		{
			var instruction = new StringBuilder ();

			AppendLabel (instruction, this);
			instruction.Append (':');
			instruction.Append (' ');
			instruction.Append (opcode.Name);

			if (operand == null)
				return instruction.ToString ();

			instruction.Append (' ');

			switch (opcode.OperandType) {
			case OperandType.ShortInlineBrTarget:
			case OperandType.InlineBrTarget:
				AppendLabel (instruction, (Instruction) operand);
				break;
			case OperandType.InlineSwitch:
				var labels = (Instruction []) operand;
				for (int i = 0; i < labels.Length; i++) {
					if (i > 0)
						instruction.Append (',');

					AppendLabel (instruction, labels [i]);
				}
				break;
			case OperandType.InlineString:
				instruction.Append ('\"');
				instruction.Append (operand);
				instruction.Append ('\"');
				break;
			default:
				instruction.Append (operand);
				break;
			}

			return instruction.ToString ();
		}

		static void AppendLabel (StringBuilder builder, Instruction instruction)
		{
			builder.Append ("IL_");
			builder.Append (instruction.offset.ToString ("x4"));
		}

		public static Instruction Create (OpCode opcode)
		{
			if (opcode.OperandType != OperandType.InlineNone)
				throw new ArgumentException ("opcode");

			return new Instruction (opcode, null);
		}

		public static Instruction Create (OpCode opcode, TypeReference type)
		{
			if (type == null)
				throw new ArgumentNullException ("type");
			if (opcode.OperandType != OperandType.InlineType &&
				opcode.OperandType != OperandType.InlineTok)
				throw new ArgumentException ("opcode");

			return new Instruction (opcode, type);
		}

		public static Instruction Create (OpCode opcode, CallSite site)
		{
			if (site == null)
				throw new ArgumentNullException ("site");
			if (opcode.Code != Code.Calli)
				throw new ArgumentException ("code");

			return new Instruction (opcode, site);
		}

		public static Instruction Create (OpCode opcode, MethodReference method)
		{
			if (method == null)
				throw new ArgumentNullException ("method");
			if (opcode.OperandType != OperandType.InlineMethod &&
				opcode.OperandType != OperandType.InlineTok)
				throw new ArgumentException ("opcode");

			return new Instruction (opcode, method);
		}

		public static Instruction Create (OpCode opcode, FieldReference field)
		{
			if (field == null)
				throw new ArgumentNullException ("field");
			if (opcode.OperandType != OperandType.InlineField &&
				opcode.OperandType != OperandType.InlineTok)
				throw new ArgumentException ("opcode");

			return new Instruction (opcode, field);
		}

		public static Instruction Create (OpCode opcode, string value)
		{
			if (value == null)
				throw new ArgumentNullException ("value");
			if (opcode.OperandType != OperandType.InlineString)
				throw new ArgumentException ("opcode");

			return new Instruction (opcode, value);
		}

		public static Instruction Create (OpCode opcode, sbyte value)
		{
			if (opcode.OperandType != OperandType.ShortInlineI &&
				opcode != OpCodes.Ldc_I4_S)
				throw new ArgumentException ("opcode");

			return new Instruction (opcode, value);
		}

		public static Instruction Create (OpCode opcode, byte value)
		{
			if (opcode.OperandType != OperandType.ShortInlineI ||
				opcode == OpCodes.Ldc_I4_S)
				throw new ArgumentException ("opcode");

			return new Instruction (opcode, value);
		}

		public static Instruction Create (OpCode opcode, int value)
		{
			if (opcode.OperandType != OperandType.InlineI)
				throw new ArgumentException ("opcode");

			return new Instruction (opcode, value);
		}

		public static Instruction Create (OpCode opcode, long value)
		{
			if (opcode.OperandType != OperandType.InlineI8)
				throw new ArgumentException ("opcode");

			return new Instruction (opcode, value);
		}

		public static Instruction Create (OpCode opcode, float value)
		{
			if (opcode.OperandType != OperandType.ShortInlineR)
				throw new ArgumentException ("opcode");

			return new Instruction (opcode, value);
		}

		public static Instruction Create (OpCode opcode, double value)
		{
			if (opcode.OperandType != OperandType.InlineR)
				throw new ArgumentException ("opcode");

			return new Instruction (opcode, value);
		}

		public static Instruction Create (OpCode opcode, Instruction target)
		{
			if (target == null)
				throw new ArgumentNullException ("target");
			if (opcode.OperandType != OperandType.InlineBrTarget &&
				opcode.OperandType != OperandType.ShortInlineBrTarget)
				throw new ArgumentException ("opcode");

			return new Instruction (opcode, target);
		}

		public static Instruction Create (OpCode opcode, Instruction [] targets)
		{
			if (targets == null)
				throw new ArgumentNullException ("targets");
			if (opcode.OperandType != OperandType.InlineSwitch)
				throw new ArgumentException ("opcode");

			return new Instruction (opcode, targets);
		}

		public static Instruction Create (OpCode opcode, VariableDefinition variable)
		{
			if (variable == null)
				throw new ArgumentNullException ("variable");
			if (opcode.OperandType != OperandType.ShortInlineVar &&
				opcode.OperandType != OperandType.InlineVar)
				throw new ArgumentException ("opcode");

			return new Instruction (opcode, variable);
		}

		public static Instruction Create (OpCode opcode, ParameterDefinition parameter)
		{
			if (parameter == null)
				throw new ArgumentNullException ("parameter");
			if (opcode.OperandType != OperandType.ShortInlineArg &&
				opcode.OperandType != OperandType.InlineArg)
				throw new ArgumentException ("opcode");

			return new Instruction (opcode, parameter);
		}
	}
}
