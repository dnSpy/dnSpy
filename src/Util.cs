using System;

using Ast = ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.Ast;

using Cecil = Mono.Cecil;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Decompiler
{
	public static class Util
	{
		public static int GetNumberOfInputs(MethodDefinition methodDef, ByteCode byteCode)
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
		
		public static int GetNumberOfOutputs(MethodDefinition methodDef, ByteCode byteCode)
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
