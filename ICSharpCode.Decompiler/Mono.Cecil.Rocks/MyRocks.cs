/*
 * Created by SharpDevelop.
 * User: User
 * Date: 05/02/2011
 * Time: 10:10
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Decompiler.Rocks
{
	static class MyRocks
	{
		public static List<T> CutRange<T>(this List<T> list, int start, int count)
		{
			List<T> ret = new List<T>(count);
			for (int i = 0; i < count; i++) {
				ret.Add(list[start + i]);
			}
			list.RemoveRange(start, count);
			return ret;
		}
		
		public static bool CanFallThough(this OpCode opCode)
		{
			switch(opCode.FlowControl) {
				case FlowControl.Branch:			return false;
				case FlowControl.Cond_Branch:	return true;
				case FlowControl.Next:			return true;
				case FlowControl.Call:			return true;
				case FlowControl.Return:			return false;
				case FlowControl.Throw:			return false;
				case FlowControl.Meta:			return true;
				default: throw new NotImplementedException();
			}
		}
		
		public static bool IsBranch(this OpCode opCode)
		{
			return opCode.FlowControl == FlowControl.Branch || opCode.FlowControl == FlowControl.Cond_Branch;
		}
		
		public static int? GetPopCount(this Instruction inst)
		{
			switch(inst.OpCode.StackBehaviourPop) {
				case StackBehaviour.Pop0:   				return 0;
				case StackBehaviour.Pop1:   				return 1;
				case StackBehaviour.Popi:   				return 1;
				case StackBehaviour.Popref: 				return 1;
				case StackBehaviour.Pop1_pop1:   		return 2;
				case StackBehaviour.Popi_pop1:   		return 2;
				case StackBehaviour.Popi_popi:   		return 2;
				case StackBehaviour.Popi_popi8:  		return 2;
				case StackBehaviour.Popi_popr4:  		return 2;
				case StackBehaviour.Popi_popr8:  		return 2;
				case StackBehaviour.Popref_pop1: 		return 2;
				case StackBehaviour.Popref_popi: 		return 2;
				case StackBehaviour.Popi_popi_popi:     return 3;
				case StackBehaviour.Popref_popi_popi:   return 3;
				case StackBehaviour.Popref_popi_popi8:  return 3;
				case StackBehaviour.Popref_popi_popr4:  return 3;
				case StackBehaviour.Popref_popi_popr8:  return 3;
				case StackBehaviour.Popref_popi_popref: return 3;
				case StackBehaviour.PopAll: 				return null;
				case StackBehaviour.Varpop: 
					switch(inst.OpCode.Code) {
						case Code.Call:
						case Code.Callvirt:
							MethodReference cecilMethod = ((MethodReference)inst.Operand);
							if (cecilMethod.HasThis) {
								return cecilMethod.Parameters.Count + 1 /* this */;
							} else {
								return cecilMethod.Parameters.Count;
							}
						case Code.Calli:    throw new NotImplementedException();
						case Code.Ret:		return null;
						case Code.Newobj:
							MethodReference ctorMethod = ((MethodReference)inst.Operand);
							return ctorMethod.Parameters.Count;
						default: throw new Exception("Unknown Varpop opcode");
					}
				default: throw new Exception("Unknown pop behaviour: " + inst.OpCode.StackBehaviourPop);
			}
		}
		
		public static int GetPushCount(this Instruction inst)
		{
			switch(inst.OpCode.StackBehaviourPush) {
				case StackBehaviour.Push0:       return 0;
				case StackBehaviour.Push1:       return 1;
				case StackBehaviour.Push1_push1: return 2;
				case StackBehaviour.Pushi:       return 1;
				case StackBehaviour.Pushi8:      return 1;
				case StackBehaviour.Pushr4:      return 1;
				case StackBehaviour.Pushr8:      return 1;
				case StackBehaviour.Pushref:     return 1;
				case StackBehaviour.Varpush:     // Happens only for calls
					switch(inst.OpCode.Code) {
						case Code.Call:
						case Code.Callvirt:
							MethodReference cecilMethod = ((MethodReference)inst.Operand);
							if (cecilMethod.ReturnType.FullName == Constants.Void) {
								return 0;
							} else {
								return 1;
							}
						case Code.Calli:    throw new NotImplementedException();
						default: throw new Exception("Unknown Varpush opcode");
					}
				default: throw new Exception("Unknown push behaviour: " + inst.OpCode.StackBehaviourPush);
			}
		}
	}
}
