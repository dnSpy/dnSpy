using System;
using System.Collections.Generic;

using Cecil = Mono.Cecil;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Decompiler
{
	public partial class ByteCode
	{
		ByteCode previous;
		ByteCode next;
		
		List<ByteCode> nestedByteCodes = new List<ByteCode>();
		
		MethodDefinition methodDef;
		int offset;
		OpCode opCode;
		object operand;
		
		public ByteCode Previous {
			get { return previous; }
			set { previous = value; }
		}
		
		public ByteCode Next {
			get { return next; }
			set { next = value; }
		}
		
		public List<ByteCode> NestedByteCodes {
			get { return nestedByteCodes; }
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
		
		public ByteCode(MethodDefinition methodDef, Instruction inst)
		{
			this.methodDef = methodDef;
			this.offset = inst.Offset;
			this.opCode = inst.OpCode;
			this.operand = inst.Operand;
		}
	}
}
