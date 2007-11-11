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
		
		public int PopCount {
			get {
				int popCount;
				int pushCount;
				SimulateStackSize(out popCount, out pushCount);
				return popCount;
			}
		}
		
		public int PushCount {
			get {
				int popCount;
				int pushCount;
				SimulateStackSize(out popCount, out pushCount);
				return pushCount;
			}
		}
		
		void SimulateStackSize(out int popCount, out int pushCount)
		{
			int stackSize = 0;
			int minStackSize = 0;
			foreach(ByteCode bc in nestedByteCodes) {
				stackSize -= bc.PopCount;
				minStackSize = Math.Min(minStackSize, stackSize);
				stackSize += bc.PushCount;
			}
			{
				stackSize -= GetPopCount(methodDef, this);
				minStackSize = Math.Min(minStackSize, stackSize);
				stackSize += GetPushCount(methodDef, this);
			}
			popCount = -minStackSize;
			pushCount = stackSize - minStackSize;
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
		
		static int GetPopCount(MethodDefinition methodDef, ByteCode byteCode)
		{
			switch(byteCode.OpCode.StackBehaviourPop) {
				case StackBehaviour.Pop0:   return 0;
				case StackBehaviour.Pop1:   return 1;
				case StackBehaviour.Popi:   return 1;
				case StackBehaviour.Popref: return 1;
				case StackBehaviour.Pop1_pop1:   return 2;
				case StackBehaviour.Popi_pop1:   return 2;
				case StackBehaviour.Popi_popi:   return 2;
				case StackBehaviour.Popi_popi8:  return 2;
				case StackBehaviour.Popi_popr4:  return 2;
				case StackBehaviour.Popi_popr8:  return 2;
				case StackBehaviour.Popref_pop1: return 2;
				case StackBehaviour.Popref_popi: return 2;
				case StackBehaviour.Popi_popi_popi:     return 3;
				case StackBehaviour.Popref_popi_popi:   return 3;
				case StackBehaviour.Popref_popi_popi8:  return 3;
				case StackBehaviour.Popref_popi_popr4:  return 3;
				case StackBehaviour.Popref_popi_popr8:  return 3;
				case StackBehaviour.Popref_popi_popref: return 3;
				case StackBehaviour.PopAll: throw new Exception("PopAll");
				case StackBehaviour.Varpop: 
					switch(byteCode.OpCode.Code) {
						case Code.Call:     
							Cecil.MethodReference cecilMethod = ((MethodReference)byteCode.Operand);
							if (cecilMethod.HasThis) {
								return cecilMethod.Parameters.Count + 1 /* this */;
							} else {
								return cecilMethod.Parameters.Count;
							}
						case Code.Calli:    throw new NotImplementedException();
						case Code.Callvirt: throw new NotImplementedException();
						case Code.Ret:
							if (methodDef.ReturnType.ReturnType.FullName == Constants.Void) {
								return 0;
							} else {
								return 1;
							}
						case Code.Newobj:   throw new NotImplementedException();
						default: throw new Exception("Unknown Varpop opcode");
					}
				default: throw new Exception("Unknown pop behaviour: " + byteCode.OpCode.StackBehaviourPop);
			}
		}
		
		static int GetPushCount(MethodDefinition methodDef, ByteCode byteCode)
		{
			switch(byteCode.OpCode.StackBehaviourPush) {
				case StackBehaviour.Push0:       return 0;
				case StackBehaviour.Push1:       return 1;
				case StackBehaviour.Push1_push1: return 2;
				case StackBehaviour.Pushi:       return 1;
				case StackBehaviour.Pushi8:      return 1;
				case StackBehaviour.Pushr4:      return 1;
				case StackBehaviour.Pushr8:      return 1;
				case StackBehaviour.Pushref:     return 1;
				case StackBehaviour.Varpush:     // Happens only for calls
					switch(byteCode.OpCode.Code) {
						case Code.Call:     
							Cecil.MethodReference cecilMethod = ((MethodReference)byteCode.Operand);
							if (cecilMethod.ReturnType.ReturnType.FullName == Constants.Void) {
								return 0;
							} else {
								return 1;
							}
						case Code.Calli:    throw new NotImplementedException();
						case Code.Callvirt: throw new NotImplementedException();
						default: throw new Exception("Unknown Varpush opcode");
					}
				default: throw new Exception("Unknown push behaviour: " + byteCode.OpCode.StackBehaviourPush);
			}
		}
	}
}
