//
// Instruction.cs
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

	public sealed class Instruction : ICodeVisitable {

		int m_offset;
		OpCode m_opCode;
		object m_operand;

		Instruction m_previous;
		Instruction m_next;

		SequencePoint m_sequencePoint;

		public int Offset {
			get { return m_offset; }
			set { m_offset = value; }
		}

		public OpCode OpCode {
			get { return m_opCode; }
			set { m_opCode = value; }
		}

		public object Operand {
			get { return m_operand; }
			set { m_operand = value; }
		}

		public Instruction Previous {
			get { return m_previous; }
			set { m_previous = value; }
		}

		public Instruction Next {
			get { return m_next; }
			set { m_next = value; }
		}

		public SequencePoint SequencePoint {
			get { return m_sequencePoint; }
			set { m_sequencePoint = value; }
		}

		internal Instruction (int offset, OpCode opCode, object operand) : this (offset, opCode)
		{
			m_operand = operand;
		}

		internal Instruction (int offset, OpCode opCode)
		{
			m_offset = offset;
			m_opCode = opCode;
		}

		internal Instruction (OpCode opCode, object operand) : this (0, opCode, operand)
		{
		}

		internal Instruction (OpCode opCode) : this (0, opCode)
		{
		}

		public void Accept (ICodeVisitor visitor)
		{
			visitor.VisitInstruction (this);
		}
	}
}
