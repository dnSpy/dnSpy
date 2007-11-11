using System;
using System.Collections.Generic;

using Cecil = Mono.Cecil;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Decompiler
{
	public class ByteCode
	{
		ByteCode next;
		
		int offset;
		OpCode opCode;
		object operand;
		
		public ByteCode Next {
			get { return next; }
			set { next = value; }
		}
		
		public int Offset {
			get { return offset; }
			set { offset = value; }
		}
		
		public OpCode OpCode {
			get { return opCode; }
			set { opCode = value; }
		}
		
		public object Operand {
			get { return operand; }
			set { operand = value; }
		}
		
		public ByteCode BranchTarget {
			get {
				return (ByteCode)operand;
			}
		}
		
		public bool CanBranch {
			get {
				return OpCode.FlowControl == FlowControl.Branch ||
				       OpCode.FlowControl == FlowControl.Cond_Branch;
			}
		}
		
		public ByteCode(int offset, OpCode opCode, object operand)
		{
			this.offset = offset;
			this.opCode = opCode;
			this.operand = operand;
		}
		
		public ByteCode(Instruction inst)
		{
			this.offset = inst.Offset;
			this.opCode = inst.OpCode;
			this.operand = inst.Operand;
		}
	}
}
