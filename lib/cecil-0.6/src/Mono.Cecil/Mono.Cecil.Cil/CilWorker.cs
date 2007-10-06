//
// CilWorker.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// (C) 2005 Jb Evain
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

namespace Mono.Cecil.Cil {

	using System;
	using SR = System.Reflection;

	public sealed class CilWorker {

		MethodBody m_mbody;
		InstructionCollection m_instrs;

		internal CilWorker (MethodBody body)
		{
			m_mbody = body;
			m_instrs = m_mbody.Instructions;
		}

		public MethodBody GetBody ()
		{
			return m_mbody;
		}

		public Instruction Create (OpCode opcode)
		{
			if (opcode.OperandType != OperandType.InlineNone)
				throw new ArgumentException ("opcode");

			return FinalCreate (opcode);
		}

		public Instruction Create (OpCode opcode, TypeReference type)
		{
			if (opcode.OperandType != OperandType.InlineType &&
				opcode.OperandType != OperandType.InlineTok)
				throw new ArgumentException ("opcode");

			return FinalCreate (opcode, type);
		}

		public Instruction Create (OpCode opcode, MethodReference meth)
		{
			if (opcode.OperandType != OperandType.InlineMethod &&
				opcode.OperandType != OperandType.InlineTok)
				throw new ArgumentException ("opcode");

			return FinalCreate (opcode, meth);
		}

		public Instruction Create (OpCode opcode, FieldReference field)
		{
			if (opcode.OperandType != OperandType.InlineField &&
				opcode.OperandType != OperandType.InlineTok)
				throw new ArgumentException ("opcode");

			return FinalCreate (opcode, field);
		}

		public Instruction Create (OpCode opcode, string str)
		{
			if (opcode.OperandType != OperandType.InlineString)
				throw new ArgumentException ("opcode");

			return FinalCreate (opcode, str);
		}

		public Instruction Create (OpCode opcode, sbyte b)
		{
			if (opcode.OperandType != OperandType.ShortInlineI &&
				opcode != OpCodes.Ldc_I4_S)
				throw new ArgumentException ("opcode");

			return FinalCreate (opcode, b);
		}

		public Instruction Create (OpCode opcode, byte b)
		{
			if (opcode.OperandType != OperandType.ShortInlineI ||
				opcode == OpCodes.Ldc_I4_S)
				throw new ArgumentException ("opcode");

			return FinalCreate (opcode, b);
		}

		public Instruction Create (OpCode opcode, int i)
		{
			if (opcode.OperandType != OperandType.InlineI)
				throw new ArgumentException ("opcode");

			return FinalCreate (opcode, i);
		}

		public Instruction Create (OpCode opcode, long l)
		{
			if (opcode.OperandType != OperandType.InlineI8)
				throw new ArgumentException ("opcode");

			return FinalCreate (opcode, l);
		}

		public Instruction Create (OpCode opcode, float f)
		{
			if (opcode.OperandType != OperandType.ShortInlineR)
				throw new ArgumentException ("opcode");

			return FinalCreate (opcode, f);
		}

		public Instruction Create (OpCode opcode, double d)
		{
			if (opcode.OperandType != OperandType.InlineR)
				throw new ArgumentException ("opcode");

			return FinalCreate (opcode, d);
		}

		public Instruction Create (OpCode opcode, Instruction label)
		{
			if (opcode.OperandType != OperandType.InlineBrTarget &&
				opcode.OperandType != OperandType.ShortInlineBrTarget)
				throw new ArgumentException ("opcode");

			return FinalCreate (opcode, label);
		}

		public Instruction Create (OpCode opcode, Instruction [] labels)
		{
			if (opcode.OperandType != OperandType.InlineSwitch)
				throw new ArgumentException ("opcode");

			return FinalCreate (opcode, labels);
		}

		public Instruction Create (OpCode opcode, VariableDefinition var)
		{
			if (opcode.OperandType != OperandType.ShortInlineVar &&
				opcode.OperandType != OperandType.InlineVar)
				throw new ArgumentException ("opcode");

			return FinalCreate (opcode, var);
		}

		public Instruction Create (OpCode opcode, ParameterDefinition param)
		{
			if (opcode.OperandType != OperandType.ShortInlineParam &&
				opcode.OperandType != OperandType.InlineParam)
				throw new ArgumentException ("opcode");

			return FinalCreate (opcode, param);
		}

		Instruction FinalCreate (OpCode opcode)
		{
			return FinalCreate (opcode, null);
		}

		Instruction FinalCreate (OpCode opcode, object operand)
		{
			return new Instruction (opcode, operand);
		}

		public Instruction Emit (OpCode opcode)
		{
			Instruction instr = Create (opcode);
			Append (instr);
			return instr;
		}

		public Instruction Emit (OpCode opcode, TypeReference type)
		{
			Instruction instr = Create (opcode, type);
			Append (instr);
			return instr;
		}

		public Instruction Emit (OpCode opcode, MethodReference meth)
		{
			Instruction instr = Create (opcode, meth);
			Append (instr);
			return instr;
		}

		public Instruction Emit (OpCode opcode, FieldReference field)
		{
			Instruction instr = Create (opcode, field);
			Append (instr);
			return instr;
		}

		public Instruction Emit (OpCode opcode, string str)
		{
			Instruction instr = Create (opcode, str);
			Append (instr);
			return instr;
		}

		public Instruction Emit (OpCode opcode, byte b)
		{
			Instruction instr = Create (opcode, b);
			Append (instr);
			return instr;
		}

		public Instruction Emit (OpCode opcode, sbyte b)
		{
			Instruction instr = Create (opcode, b);
			Append (instr);
			return instr;
		}

		public Instruction Emit (OpCode opcode, int i)
		{
			Instruction instr = Create (opcode, i);
			Append (instr);
			return instr;
		}

		public Instruction Emit (OpCode opcode, long l)
		{
			Instruction instr = Create (opcode, l);
			Append (instr);
			return instr;
		}

		public Instruction Emit (OpCode opcode, float f)
		{
			Instruction instr = Create (opcode, f);
			Append (instr);
			return instr;
		}

		public Instruction Emit (OpCode opcode, double d)
		{
			Instruction instr = Create (opcode, d);
			Append (instr);
			return instr;
		}

		public Instruction Emit (OpCode opcode, Instruction target)
		{
			Instruction instr = Create (opcode, target);
			Append (instr);
			return instr;
		}

		public Instruction Emit (OpCode opcode, Instruction [] targets)
		{
			Instruction instr = Create (opcode, targets);
			Append (instr);
			return instr;
		}

		public Instruction Emit (OpCode opcode, VariableDefinition var)
		{
			Instruction instr = Create (opcode, var);
			Append (instr);
			return instr;
		}

		public Instruction Emit (OpCode opcode, ParameterDefinition param)
		{
			Instruction instr = Create (opcode, param);
			Append (instr);
			return instr;
		}

		public void InsertBefore (Instruction target, Instruction instr)
		{
			int index = m_instrs.IndexOf (target);
			if (index == -1)
				throw new ArgumentOutOfRangeException ("Target instruction not in method body");

			m_instrs.Insert (index, instr);
			instr.Previous = target.Previous;
			if (target.Previous != null)
				target.Previous.Next = instr;
			target.Previous = instr;
			instr.Next = target;
		}

		public void InsertAfter (Instruction target, Instruction instr)
		{
			int index = m_instrs.IndexOf (target);
			if (index == -1)
				throw new ArgumentOutOfRangeException ("Target instruction not in method body");

			m_instrs.Insert (index + 1, instr);
			instr.Next = target.Next;
			if (target.Next != null)
				target.Next.Previous = instr;
			target.Next = instr;
			instr.Previous = target;
		}

		public void Append (Instruction instr)
		{
			Instruction last = null, current = instr;
			if (m_instrs.Count > 0)
				last = m_instrs [m_instrs.Count - 1];

			if (last != null) {
				last.Next = instr;
				current.Previous = last;
			}

			m_instrs.Add (current);
		}

		public void Replace (Instruction old, Instruction instr)
		{
			int index = m_instrs.IndexOf (old);
			if (index == -1)
				throw new ArgumentOutOfRangeException ("Target instruction not in method body");

			InsertAfter (old, instr);
			Remove (old);
		}

		public void Remove (Instruction instr)
		{
			if (!m_instrs.Contains (instr))
				throw new ArgumentException ("Instruction not in method body");

			if (instr.Previous != null)
				instr.Previous.Next = instr.Next;
			if (instr.Next != null)
				instr.Next.Previous = instr.Previous;
			m_instrs.Remove (instr);
		}
	}
}
