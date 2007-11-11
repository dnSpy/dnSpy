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
		
		public string Description {
			get {
				return string.Format(
					" {1, -22} # {2}->{3} {4} {5}",
					this.Offset,
					this.OpCode + " " + FormatByteCodeOperand(this.Operand),
					this.OpCode.StackBehaviourPop,
					this.OpCode.StackBehaviourPush,
					this.OpCode.FlowControl == FlowControl.Next ? string.Empty : "Flow=" + opCode.FlowControl,
					this.OpCode.OpCodeType == OpCodeType.Macro ? "(macro)" : string.Empty
				);
			}
		}
		
		static object FormatByteCodeOperand(object operand)
		{
			if (operand == null) {
				return string.Empty;
			} else if (operand is ByteCode) {
				return string.Format("IL_{0:X2}", ((ByteCode)operand).Offset);
			} else if (operand is MethodReference) {
				return ((MethodReference)operand).Name + "()";
			} else if (operand is Cecil.TypeReference) {
				return ((Cecil.TypeReference)operand).FullName;
			} else if (operand is VariableDefinition) {
				return ((VariableDefinition)operand).Name;
			} else if (operand is ParameterDefinition) {
				return ((ParameterDefinition)operand).Name;
			} else if (operand is string) {
				return "\"" + operand + "\"";
			} else if (operand is int) {
				return operand.ToString();
			} else {
				return "(" + operand.GetType() + ")";
			}
		}
	}
}
