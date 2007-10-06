//
// OpCode.cs
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

	public struct OpCode {

		string m_name;
		byte m_op1;
		byte m_op2;
		int m_size;

		Code m_code;
		FlowControl m_flowControl;
		OpCodeType m_opCodeType;
		OperandType m_operandType;
		StackBehaviour m_stackBehaviourPop;
		StackBehaviour m_stackBehaviourPush;

		public string Name {
			get { return m_name; }
		}

		public int Size {
			get { return m_size; }
		}

		public byte Op1 {
			get { return m_op1; }
		}

		public byte Op2 {
			get { return m_op2; }
		}

		public short Value {
			get { return m_size == 1 ? m_op2 : (short) ((m_op1 << 8) | m_op2); }
		}

		public Code Code {
			get { return m_code; }
		}

		public FlowControl FlowControl {
			get { return m_flowControl; }
		}

		public OpCodeType OpCodeType {
			get { return m_opCodeType; }
		}

		public OperandType OperandType {
			get { return m_operandType; }
		}

		public StackBehaviour StackBehaviourPop {
			get { return m_stackBehaviourPop; }
		}

		public StackBehaviour StackBehaviourPush {
			get { return m_stackBehaviourPush; }
		}

		internal OpCode (string name, byte op1, byte op2, int size,
			Code code, FlowControl flowControl,
			OpCodeType opCodeType, OperandType operandType,
			StackBehaviour pop, StackBehaviour push)
		{
			m_name = name;
			m_op1 = op1;
			m_op2 = op2;
			m_size = size;
			m_code = code;
			m_flowControl = flowControl;
			m_opCodeType = opCodeType;
			m_operandType = operandType;
			m_stackBehaviourPop = pop;
			m_stackBehaviourPush = push;

			if (op1 == 0xff)
				OpCodes.OneByteOpCode [op2] = this;
			else
				OpCodes.TwoBytesOpCode [op2] = this;
		}

		public override int GetHashCode ()
		{
			return this.Value;
		}

		public override bool Equals (object obj)
		{
			if (!(obj is OpCode))
				return false;
			OpCode v = (OpCode) obj;
			return v.m_op1 == m_op1 && v.m_op2 == m_op2;
		}

		public static bool operator == (OpCode one, OpCode other)
		{
			return one.Equals (other);
		}

		public static bool operator != (OpCode one, OpCode other)
		{
			return !one.Equals (other);
		}

		public override string ToString ()
		{
			return m_name;
		}
	}
}
