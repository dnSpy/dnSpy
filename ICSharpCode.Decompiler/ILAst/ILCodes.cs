// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2010 Jb Evain
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
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ICSharpCode.Decompiler.ILAst
{
	public enum ILCode
	{
		// For convenience, the start is exactly identical to Mono.Cecil.Cil.Code
		// Instructions that should not be used are prepended by __
		Nop,
		Break,
		__Ldarg_0,
		__Ldarg_1,
		__Ldarg_2,
		__Ldarg_3,
		__Ldloc_0,
		__Ldloc_1,
		__Ldloc_2,
		__Ldloc_3,
		__Stloc_0,
		__Stloc_1,
		__Stloc_2,
		__Stloc_3,
		__Ldarg_S,
		__Ldarga_S,
		__Starg_S,
		__Ldloc_S,
		__Ldloca_S,
		__Stloc_S,
		Ldnull,
		__Ldc_I4_M1,
		__Ldc_I4_0,
		__Ldc_I4_1,
		__Ldc_I4_2,
		__Ldc_I4_3,
		__Ldc_I4_4,
		__Ldc_I4_5,
		__Ldc_I4_6,
		__Ldc_I4_7,
		__Ldc_I4_8,
		__Ldc_I4_S,
		Ldc_I4,
		Ldc_I8,
		Ldc_R4,
		Ldc_R8,
		Dup,
		Pop,
		Jmp,
		Call,
		Calli,
		Ret,
		__Br_S,
		__Brfalse_S,
		__Brtrue_S,
		__Beq_S,
		__Bge_S,
		__Bgt_S,
		__Ble_S,
		__Blt_S,
		__Bne_Un_S,
		__Bge_Un_S,
		__Bgt_Un_S,
		__Ble_Un_S,
		__Blt_Un_S,
		Br,
		__Brfalse,
		Brtrue,
		__Beq,
		__Bge,
		__Bgt,
		__Ble,
		__Blt,
		__Bne_Un,
		__Bge_Un,
		__Bgt_Un,
		__Ble_Un,
		__Blt_Un,
		Switch,
		Ldind_I1,
		Ldind_U1,
		Ldind_I2,
		Ldind_U2,
		Ldind_I4,
		Ldind_U4,
		Ldind_I8,
		Ldind_I,
		Ldind_R4,
		Ldind_R8,
		Ldind_Ref,
		Stind_Ref,
		Stind_I1,
		Stind_I2,
		Stind_I4,
		Stind_I8,
		Stind_R4,
		Stind_R8,
		Add,
		Sub,
		Mul,
		Div,
		Div_Un,
		Rem,
		Rem_Un,
		And,
		Or,
		Xor,
		Shl,
		Shr,
		Shr_Un,
		Neg,
		Not,
		Conv_I1,
		Conv_I2,
		Conv_I4,
		Conv_I8,
		Conv_R4,
		Conv_R8,
		Conv_U4,
		Conv_U8,
		Callvirt,
		Cpobj,
		Ldobj,
		Ldstr,
		Newobj,
		Castclass,
		Isinst,
		Conv_R_Un,
		Unbox,
		Throw,
		Ldfld,
		Ldflda,
		Stfld,
		Ldsfld,
		Ldsflda,
		Stsfld,
		Stobj,
		Conv_Ovf_I1_Un,
		Conv_Ovf_I2_Un,
		Conv_Ovf_I4_Un,
		Conv_Ovf_I8_Un,
		Conv_Ovf_U1_Un,
		Conv_Ovf_U2_Un,
		Conv_Ovf_U4_Un,
		Conv_Ovf_U8_Un,
		Conv_Ovf_I_Un,
		Conv_Ovf_U_Un,
		Box,
		Newarr,
		Ldlen,
		Ldelema,
		Ldelem_I1,
		Ldelem_U1,
		Ldelem_I2,
		Ldelem_U2,
		Ldelem_I4,
		Ldelem_U4,
		Ldelem_I8,
		Ldelem_I,
		Ldelem_R4,
		Ldelem_R8,
		Ldelem_Ref,
		Stelem_I,
		Stelem_I1,
		Stelem_I2,
		Stelem_I4,
		Stelem_I8,
		Stelem_R4,
		Stelem_R8,
		Stelem_Ref,
		Ldelem_Any,
		Stelem_Any,
		Unbox_Any,
		Conv_Ovf_I1,
		Conv_Ovf_U1,
		Conv_Ovf_I2,
		Conv_Ovf_U2,
		Conv_Ovf_I4,
		Conv_Ovf_U4,
		Conv_Ovf_I8,
		Conv_Ovf_U8,
		Refanyval,
		Ckfinite,
		Mkrefany,
		Ldtoken,
		Conv_U2,
		Conv_U1,
		Conv_I,
		Conv_Ovf_I,
		Conv_Ovf_U,
		Add_Ovf,
		Add_Ovf_Un,
		Mul_Ovf,
		Mul_Ovf_Un,
		Sub_Ovf,
		Sub_Ovf_Un,
		Endfinally,
		Leave,
		__Leave_S,
		Stind_I,
		Conv_U,
		Arglist,
		Ceq,
		Cgt,
		Cgt_Un,
		Clt,
		Clt_Un,
		Ldftn,
		Ldvirtftn,
		Ldarg,
		Ldarga,
		Starg,
		Ldloc,
		Ldloca,
		Stloc,
		Localloc,
		Endfilter,
		Unaligned,
		Volatile,
		Tail,
		Initobj,
		Constrained,
		Cpblk,
		Initblk,
		No,
		Rethrow,
		Sizeof,
		Refanytype,
		Readonly,
		
		// Virtual codes - defined for convenience
		Ldexception,  // Operand holds the CatchType for catch handler, null for filter
		LogicNot,
		LogicAnd,
		LogicOr,
		InitArray, // Array Initializer
		TernaryOp, // ?:
		LoopOrSwitchBreak,
		LoopContinue,
		Ldc_Decimal,
		
		Pattern // used for ILAst pattern nodes
	}
	
	public static class ILCodeUtil
	{
		public static string GetName(this ILCode code)
		{
			return code.ToString().ToLowerInvariant().TrimStart('_').Replace('_','.');
		}
		
		public static bool CanFallThough(this ILCode code)
		{
			switch(code) {
				case ILCode.Br:
				case ILCode.__Br_S:
				case ILCode.Leave:
				case ILCode.__Leave_S:
				case ILCode.Ret:
				case ILCode.Endfilter:
				case ILCode.Endfinally:
				case ILCode.Throw:
				case ILCode.Rethrow:
					return false;
				default:
					return true;
			}
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
							if (cecilMethod.ReturnType.FullName == "System.Void") {
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
		
		public static void ExpandMacro(ref ILCode code, ref object operand, MethodBody methodBody)
		{
			switch (code) {
					case ILCode.__Ldarg_0: 		code = ILCode.Ldarg; operand = methodBody.GetParameter(0); break;
					case ILCode.__Ldarg_1: 		code = ILCode.Ldarg; operand = methodBody.GetParameter(1); break;
					case ILCode.__Ldarg_2: 		code = ILCode.Ldarg; operand = methodBody.GetParameter(2); break;
					case ILCode.__Ldarg_3: 		code = ILCode.Ldarg; operand = methodBody.GetParameter(3); break;
					case ILCode.__Ldloc_0: 		code = ILCode.Ldloc; operand = methodBody.Variables[0]; break;
					case ILCode.__Ldloc_1: 		code = ILCode.Ldloc; operand = methodBody.Variables[1]; break;
					case ILCode.__Ldloc_2: 		code = ILCode.Ldloc; operand = methodBody.Variables[2]; break;
					case ILCode.__Ldloc_3: 		code = ILCode.Ldloc; operand = methodBody.Variables[3]; break;
					case ILCode.__Stloc_0: 		code = ILCode.Stloc; operand = methodBody.Variables[0]; break;
					case ILCode.__Stloc_1: 		code = ILCode.Stloc; operand = methodBody.Variables[1]; break;
					case ILCode.__Stloc_2: 		code = ILCode.Stloc; operand = methodBody.Variables[2]; break;
					case ILCode.__Stloc_3: 		code = ILCode.Stloc; operand = methodBody.Variables[3]; break;
					case ILCode.__Ldarg_S: 		code = ILCode.Ldarg; break;
					case ILCode.__Ldarga_S: 		code = ILCode.Ldarga; break;
					case ILCode.__Starg_S: 		code = ILCode.Starg; break;
					case ILCode.__Ldloc_S: 		code = ILCode.Ldloc; break;
					case ILCode.__Ldloca_S: 		code = ILCode.Ldloca; break;
					case ILCode.__Stloc_S: 		code = ILCode.Stloc; break;
					case ILCode.__Ldc_I4_M1: 	code = ILCode.Ldc_I4; operand = -1; break;
					case ILCode.__Ldc_I4_0: 		code = ILCode.Ldc_I4; operand = 0; break;
					case ILCode.__Ldc_I4_1: 		code = ILCode.Ldc_I4; operand = 1; break;
					case ILCode.__Ldc_I4_2: 		code = ILCode.Ldc_I4; operand = 2; break;
					case ILCode.__Ldc_I4_3: 		code = ILCode.Ldc_I4; operand = 3; break;
					case ILCode.__Ldc_I4_4: 		code = ILCode.Ldc_I4; operand = 4; break;
					case ILCode.__Ldc_I4_5: 		code = ILCode.Ldc_I4; operand = 5; break;
					case ILCode.__Ldc_I4_6: 		code = ILCode.Ldc_I4; operand = 6; break;
					case ILCode.__Ldc_I4_7: 		code = ILCode.Ldc_I4; operand = 7; break;
					case ILCode.__Ldc_I4_8: 		code = ILCode.Ldc_I4; operand = 8; break;
					case ILCode.__Ldc_I4_S: 		code = ILCode.Ldc_I4; operand = (int) (sbyte) operand; break;
					case ILCode.__Br_S: 			code = ILCode.Br; break;
					case ILCode.__Brfalse_S: 	code = ILCode.__Brfalse; break;
					case ILCode.__Brtrue_S: 		code = ILCode.Brtrue; break;
					case ILCode.__Beq_S: 		code = ILCode.__Beq; break;
					case ILCode.__Bge_S: 		code = ILCode.__Bge; break;
					case ILCode.__Bgt_S: 		code = ILCode.__Bgt; break;
					case ILCode.__Ble_S: 		code = ILCode.__Ble; break;
					case ILCode.__Blt_S: 		code = ILCode.__Blt; break;
					case ILCode.__Bne_Un_S: 		code = ILCode.__Bne_Un; break;
					case ILCode.__Bge_Un_S: 		code = ILCode.__Bge_Un; break;
					case ILCode.__Bgt_Un_S: 		code = ILCode.__Bgt_Un; break;
					case ILCode.__Ble_Un_S: 		code = ILCode.__Ble_Un; break;
					case ILCode.__Blt_Un_S:		code = ILCode.__Blt_Un; break;
					case ILCode.__Leave_S:		code = ILCode.Leave; break;
			}
		}
		
		public static ParameterDefinition GetParameter (this MethodBody self, int index)
		{
			var method = self.Method;

			if (method.HasThis) {
				if (index == 0)
					return self.ThisParameter;

				index--;
			}

			var parameters = method.Parameters;

			if (index < 0 || index >= parameters.Count)
				return null;

			return parameters [index];
		}
	}
}
