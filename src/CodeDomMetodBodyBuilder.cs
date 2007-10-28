using System;
using System.CodeDom;

using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Decompiler
{
	public class CodeDomMetodBodyBuilder
	{
		public static CodeStatementCollection CreateMetodBody(MethodDefinition methodDef)
		{
			CodeStatementCollection codeStmtCol = new CodeStatementCollection();
			
			methodDef.Body.Simplify();
			
			foreach(Instruction instr in methodDef.Body.Instructions) {
				OpCode opCode = instr.OpCode;
				string decription = 
					string.Format("IL_{0:X2}: {1, -11} {2, -15}  # {3}->{4} {5} {6}", 
					              instr.Offset,
					              opCode,
					              FormatInstructionOperand(instr.Operand),
					              opCode.StackBehaviourPop,
					              opCode.StackBehaviourPush,
					              opCode.FlowControl == FlowControl.Next ? string.Empty : "Flow=" + opCode.FlowControl,
					              opCode.OpCodeType == OpCodeType.Macro ? "(macro)" : string.Empty);
				codeStmtCol.Add(new CodeCommentStatement(decription));
			}
			
			return codeStmtCol;
		}
		
		static object FormatInstructionOperand(object operand)
		{
			if (operand == null) {
				return string.Empty;
			} else if (operand is Instruction) {
				return string.Format("IL_{0:X2}", ((Instruction)operand).Offset);
			} else if (operand is MethodReference) {
				return ((MethodReference)operand).Name + "()";
			} else if (operand is TypeReference) {
				return ((TypeReference)operand).FullName;
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
		
		static object MakeCodeDomExpression(Instruction inst, params CodeExpression[] stackArgs)
		{
			OpCode opCode = inst.OpCode;
			object operand = inst.Operand;
			CodeExpression stackArg1 = stackArgs.Length >= 1 ? stackArgs[0] : null;
			CodeExpression stackArg2 = stackArgs.Length >= 2 ? stackArgs[1] : null;
			CodeExpression stackArg3 = stackArgs.Length >= 3 ? stackArgs[2] : null;
			
			switch(opCode.Code) {
				case Code.Nop: throw new NotImplementedException();
				case Code.Break: throw new NotImplementedException();
				case Code.Ldnull: throw new NotImplementedException();
				case Code.Ldc_I4: throw new NotImplementedException();
				case Code.Ldc_I8: throw new NotImplementedException();
				case Code.Ldc_R4: throw new NotImplementedException();
				case Code.Ldc_R8: throw new NotImplementedException();
				case Code.Dup: throw new NotImplementedException();
				case Code.Pop: throw new NotImplementedException();
				case Code.Jmp: throw new NotImplementedException();
				case Code.Call: throw new NotImplementedException();
				case Code.Calli: throw new NotImplementedException();
				case Code.Ret: throw new NotImplementedException();
				case Code.Br: throw new NotImplementedException();
				case Code.Brfalse: throw new NotImplementedException();
				case Code.Brtrue: throw new NotImplementedException();
				case Code.Beq: throw new NotImplementedException();
				case Code.Bge: throw new NotImplementedException();
				case Code.Bgt: throw new NotImplementedException();
				case Code.Ble: throw new NotImplementedException();
				case Code.Blt: throw new NotImplementedException();
				case Code.Bne_Un: throw new NotImplementedException();
				case Code.Bge_Un: throw new NotImplementedException();
				case Code.Bgt_Un: throw new NotImplementedException();
				case Code.Ble_Un: throw new NotImplementedException();
				case Code.Blt_Un: throw new NotImplementedException();
				case Code.Switch: throw new NotImplementedException();
				case Code.Ldind_I1: throw new NotImplementedException();
				case Code.Ldind_U1: throw new NotImplementedException();
				case Code.Ldind_I2: throw new NotImplementedException();
				case Code.Ldind_U2: throw new NotImplementedException();
				case Code.Ldind_I4: throw new NotImplementedException();
				case Code.Ldind_U4: throw new NotImplementedException();
				case Code.Ldind_I8: throw new NotImplementedException();
				case Code.Ldind_I: throw new NotImplementedException();
				case Code.Ldind_R4: throw new NotImplementedException();
				case Code.Ldind_R8: throw new NotImplementedException();
				case Code.Ldind_Ref: throw new NotImplementedException();
				case Code.Stind_Ref: throw new NotImplementedException();
				case Code.Stind_I1: throw new NotImplementedException();
				case Code.Stind_I2: throw new NotImplementedException();
				case Code.Stind_I4: throw new NotImplementedException();
				case Code.Stind_I8: throw new NotImplementedException();
				case Code.Stind_R4: throw new NotImplementedException();
				case Code.Stind_R8: throw new NotImplementedException();
				case Code.Add: throw new NotImplementedException();
				case Code.Sub: throw new NotImplementedException();
				case Code.Mul: throw new NotImplementedException();
				case Code.Div: throw new NotImplementedException();
				case Code.Div_Un: throw new NotImplementedException();
				case Code.Rem: throw new NotImplementedException();
				case Code.Rem_Un: throw new NotImplementedException();
				case Code.And: throw new NotImplementedException();
				case Code.Or: throw new NotImplementedException();
				case Code.Xor: throw new NotImplementedException();
				case Code.Shl: throw new NotImplementedException();
				case Code.Shr: throw new NotImplementedException();
				case Code.Shr_Un: throw new NotImplementedException();
				case Code.Neg: throw new NotImplementedException();
				case Code.Not: throw new NotImplementedException();
				case Code.Conv_I1: throw new NotImplementedException();
				case Code.Conv_I2: throw new NotImplementedException();
				case Code.Conv_I4: throw new NotImplementedException();
				case Code.Conv_I8: throw new NotImplementedException();
				case Code.Conv_R4: throw new NotImplementedException();
				case Code.Conv_R8: throw new NotImplementedException();
				case Code.Conv_U4: throw new NotImplementedException();
				case Code.Conv_U8: throw new NotImplementedException();
				case Code.Callvirt: throw new NotImplementedException();
				case Code.Cpobj: throw new NotImplementedException();
				case Code.Ldobj: throw new NotImplementedException();
				case Code.Ldstr: throw new NotImplementedException();
				case Code.Newobj: throw new NotImplementedException();
				case Code.Castclass: throw new NotImplementedException();
				case Code.Isinst: throw new NotImplementedException();
				case Code.Conv_R_Un: throw new NotImplementedException();
				case Code.Unbox: throw new NotImplementedException();
				case Code.Throw: throw new NotImplementedException();
				case Code.Ldfld: throw new NotImplementedException();
				case Code.Ldflda: throw new NotImplementedException();
				case Code.Stfld: throw new NotImplementedException();
				case Code.Ldsfld: throw new NotImplementedException();
				case Code.Ldsflda: throw new NotImplementedException();
				case Code.Stsfld: throw new NotImplementedException();
				case Code.Stobj: throw new NotImplementedException();
				case Code.Conv_Ovf_I1_Un: throw new NotImplementedException();
				case Code.Conv_Ovf_I2_Un: throw new NotImplementedException();
				case Code.Conv_Ovf_I4_Un: throw new NotImplementedException();
				case Code.Conv_Ovf_I8_Un: throw new NotImplementedException();
				case Code.Conv_Ovf_U1_Un: throw new NotImplementedException();
				case Code.Conv_Ovf_U2_Un: throw new NotImplementedException();
				case Code.Conv_Ovf_U4_Un: throw new NotImplementedException();
				case Code.Conv_Ovf_U8_Un: throw new NotImplementedException();
				case Code.Conv_Ovf_I_Un: throw new NotImplementedException();
				case Code.Conv_Ovf_U_Un: throw new NotImplementedException();
				case Code.Box: throw new NotImplementedException();
				case Code.Newarr: throw new NotImplementedException();
				case Code.Ldlen: throw new NotImplementedException();
				case Code.Ldelema: throw new NotImplementedException();
				case Code.Ldelem_I1: throw new NotImplementedException();
				case Code.Ldelem_U1: throw new NotImplementedException();
				case Code.Ldelem_I2: throw new NotImplementedException();
				case Code.Ldelem_U2: throw new NotImplementedException();
				case Code.Ldelem_I4: throw new NotImplementedException();
				case Code.Ldelem_U4: throw new NotImplementedException();
				case Code.Ldelem_I8: throw new NotImplementedException();
				case Code.Ldelem_I: throw new NotImplementedException();
				case Code.Ldelem_R4: throw new NotImplementedException();
				case Code.Ldelem_R8: throw new NotImplementedException();
				case Code.Ldelem_Ref: throw new NotImplementedException();
				case Code.Stelem_I: throw new NotImplementedException();
				case Code.Stelem_I1: throw new NotImplementedException();
				case Code.Stelem_I2: throw new NotImplementedException();
				case Code.Stelem_I4: throw new NotImplementedException();
				case Code.Stelem_I8: throw new NotImplementedException();
				case Code.Stelem_R4: throw new NotImplementedException();
				case Code.Stelem_R8: throw new NotImplementedException();
				case Code.Stelem_Ref: throw new NotImplementedException();
				case Code.Ldelem_Any: throw new NotImplementedException();
				case Code.Stelem_Any: throw new NotImplementedException();
				case Code.Unbox_Any: throw new NotImplementedException();
				case Code.Conv_Ovf_I1: throw new NotImplementedException();
				case Code.Conv_Ovf_U1: throw new NotImplementedException();
				case Code.Conv_Ovf_I2: throw new NotImplementedException();
				case Code.Conv_Ovf_U2: throw new NotImplementedException();
				case Code.Conv_Ovf_I4: throw new NotImplementedException();
				case Code.Conv_Ovf_U4: throw new NotImplementedException();
				case Code.Conv_Ovf_I8: throw new NotImplementedException();
				case Code.Conv_Ovf_U8: throw new NotImplementedException();
				case Code.Refanyval: throw new NotImplementedException();
				case Code.Ckfinite: throw new NotImplementedException();
				case Code.Mkrefany: throw new NotImplementedException();
				case Code.Ldtoken: throw new NotImplementedException();
				case Code.Conv_U2: throw new NotImplementedException();
				case Code.Conv_U1: throw new NotImplementedException();
				case Code.Conv_I: throw new NotImplementedException();
				case Code.Conv_Ovf_I: throw new NotImplementedException();
				case Code.Conv_Ovf_U: throw new NotImplementedException();
				case Code.Add_Ovf: throw new NotImplementedException();
				case Code.Add_Ovf_Un: throw new NotImplementedException();
				case Code.Mul_Ovf: throw new NotImplementedException();
				case Code.Mul_Ovf_Un: throw new NotImplementedException();
				case Code.Sub_Ovf: throw new NotImplementedException();
				case Code.Sub_Ovf_Un: throw new NotImplementedException();
				case Code.Endfinally: throw new NotImplementedException();
				case Code.Leave: throw new NotImplementedException();
				case Code.Stind_I: throw new NotImplementedException();
				case Code.Conv_U: throw new NotImplementedException();
				case Code.Arglist: throw new NotImplementedException();
				case Code.Ceq: throw new NotImplementedException();
				case Code.Cgt: throw new NotImplementedException();
				case Code.Cgt_Un: throw new NotImplementedException();
				case Code.Clt: throw new NotImplementedException();
				case Code.Clt_Un: throw new NotImplementedException();
				case Code.Ldftn: throw new NotImplementedException();
				case Code.Ldvirtftn: throw new NotImplementedException();
				case Code.Ldarg: throw new NotImplementedException();
				case Code.Ldarga: throw new NotImplementedException();
				case Code.Starg: throw new NotImplementedException();
				case Code.Ldloc: throw new NotImplementedException();
				case Code.Ldloca: throw new NotImplementedException();
				case Code.Stloc: throw new NotImplementedException();
				case Code.Localloc: throw new NotImplementedException();
				case Code.Endfilter: throw new NotImplementedException();
				case Code.Unaligned: throw new NotImplementedException();
				case Code.Volatile: throw new NotImplementedException();
				case Code.Tail: throw new NotImplementedException();
				case Code.Initobj: throw new NotImplementedException();
				case Code.Constrained: throw new NotImplementedException();
				case Code.Cpblk: throw new NotImplementedException();
				case Code.Initblk: throw new NotImplementedException();
				case Code.No: throw new NotImplementedException();
				case Code.Rethrow: throw new NotImplementedException();
				case Code.Sizeof: throw new NotImplementedException();
				case Code.Refanytype: throw new NotImplementedException();
				case Code.Readonly: throw new NotImplementedException();
				default: throw new Exception("Unknown OpCode: " + opCode);
			}
		}
	}
}
