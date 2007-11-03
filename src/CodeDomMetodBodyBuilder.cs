using System;

using ICSharpCode.NRefactory.Ast;

using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Decompiler
{
	public class ASTMetodBodyBuilder
	{
		public static BlockStatement CreateMetodBody(MethodDefinition methodDef)
		{
			CodeStatementCollection codeStmtCol = new CodeStatementCollection();
			
			methodDef.Body.Simplify();
			
			foreach(Instruction instr in methodDef.Body.Instructions) {
				OpCode opCode = instr.OpCode;
				string decription = 
					string.Format("IL_{0:X2}: {1, -22} # {2}->{3} {4} {5}",
					              instr.Offset,
					              opCode + " " + FormatInstructionOperand(instr.Operand),
					              opCode.StackBehaviourPop,
					              opCode.StackBehaviourPush,
					              opCode.FlowControl == FlowControl.Next ? string.Empty : "Flow=" + opCode.FlowControl,
					              opCode.OpCodeType == OpCodeType.Macro ? "(macro)" : string.Empty);
				
				codeStmtCol.Add(new CodeCommentStatement(decription));
				try {
					object codeExpr = MakeCodeDomExpression(
						instr,
						new CodeVariableReferenceExpression("arg1"),
						new CodeVariableReferenceExpression("arg2"),
						new CodeVariableReferenceExpression("arg3"));
					if (codeExpr is CodeStatement) {
						codeStmtCol.Add((CodeStatement)codeExpr);
					} else if (codeExpr is CodeExpression) {
						codeStmtCol.Add((CodeExpression)codeExpr);
					}
				} catch (NotImplementedException) {
					codeStmtCol.Add(new CodeSnippetExpression("/* Not implemented */"));
				}
				codeStmtCol.Add(new CodeSnippetStatement(""));
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
		
		static object MakeCodeDomExpression(Instruction inst, params Expression[] args)
		{
			OpCode opCode = inst.OpCode;
			object operand = inst.Operand;
			CodeExpression arg1 = args.Length >= 1 ? args[0] : null;
			CodeExpression arg2 = args.Length >= 2 ? args[1] : null;
			CodeExpression arg3 = args.Length >= 3 ? args[2] : null;
			
			switch(opCode.Code) {
				#region Arithmetic
					case Code.Add:        return new CodeBinaryOperatorExpression(arg1, CodeBinaryOperatorType.Add, arg2);
					case Code.Add_Ovf:    return new CodeBinaryOperatorExpression(arg1, CodeBinaryOperatorType.Add, arg2);
					case Code.Add_Ovf_Un: return new CodeBinaryOperatorExpression(arg1, CodeBinaryOperatorType.Add, arg2);
					case Code.Div:        return new CodeBinaryOperatorExpression(arg1, CodeBinaryOperatorType.Divide, arg2);
					case Code.Div_Un:     return new CodeBinaryOperatorExpression(arg1, CodeBinaryOperatorType.Divide, arg2);
					case Code.Mul:        return new CodeBinaryOperatorExpression(arg1, CodeBinaryOperatorType.Multiply, arg2);
					case Code.Mul_Ovf:    return new CodeBinaryOperatorExpression(arg1, CodeBinaryOperatorType.Multiply, arg2);
					case Code.Mul_Ovf_Un: return new CodeBinaryOperatorExpression(arg1, CodeBinaryOperatorType.Multiply, arg2);
					case Code.Rem:        return new CodeBinaryOperatorExpression(arg1, CodeBinaryOperatorType.Modulus, arg2);
					case Code.Rem_Un:     return new CodeBinaryOperatorExpression(arg1, CodeBinaryOperatorType.Modulus, arg2);
					case Code.Sub:        return new CodeBinaryOperatorExpression(arg1, CodeBinaryOperatorType.Subtract, arg2);
					case Code.Sub_Ovf:    return new CodeBinaryOperatorExpression(arg1, CodeBinaryOperatorType.Subtract, arg2);
					case Code.Sub_Ovf_Un: return new CodeBinaryOperatorExpression(arg1, CodeBinaryOperatorType.Subtract, arg2);
					case Code.And:        return new CodeBinaryOperatorExpression(arg1, CodeBinaryOperatorType.BitwiseAnd, arg2);
					case Code.Xor:        throw new NotImplementedException();
					case Code.Shl:        throw new NotImplementedException();
					case Code.Shr:        throw new NotImplementedException();
					case Code.Shr_Un:     throw new NotImplementedException();
					
					case Code.Neg:        throw new NotImplementedException();
					case Code.Not:        throw new NotImplementedException();
				#endregion
				#region Arrays
					case Code.Newarr: throw new NotImplementedException();
					
					case Code.Ldlen: return new CodePropertyReferenceExpression(arg1, "Length");
					
					case Code.Ldelem_I:   throw new NotImplementedException();
					case Code.Ldelem_I1:  throw new NotImplementedException();
					case Code.Ldelem_I2:  throw new NotImplementedException();
					case Code.Ldelem_I4:  throw new NotImplementedException();
					case Code.Ldelem_I8:  throw new NotImplementedException();
					case Code.Ldelem_U1:  throw new NotImplementedException();
					case Code.Ldelem_U2:  throw new NotImplementedException();
					case Code.Ldelem_U4:  throw new NotImplementedException();
					case Code.Ldelem_R4:  throw new NotImplementedException();
					case Code.Ldelem_R8:  throw new NotImplementedException();
					case Code.Ldelem_Ref: throw new NotImplementedException();
					case Code.Ldelem_Any: throw new NotImplementedException();
					case Code.Ldelema:    throw new NotImplementedException();
					
					case Code.Stelem_I:   throw new NotImplementedException();
					case Code.Stelem_I1:  throw new NotImplementedException();
					case Code.Stelem_I2:  throw new NotImplementedException();
					case Code.Stelem_I4:  throw new NotImplementedException();
					case Code.Stelem_I8:  throw new NotImplementedException();
					case Code.Stelem_R4:  throw new NotImplementedException();
					case Code.Stelem_R8:  throw new NotImplementedException();
					case Code.Stelem_Ref: throw new NotImplementedException();
					case Code.Stelem_Any: throw new NotImplementedException();
				#endregion
				#region Branching
					case Code.Br: throw new NotImplementedException();
					case Code.Brfalse: throw new NotImplementedException();
					case Code.Brtrue: throw new NotImplementedException();
					case Code.Beq: throw new NotImplementedException();
					case Code.Bge: throw new NotImplementedException();
					case Code.Bge_Un: throw new NotImplementedException();
					case Code.Bgt: throw new NotImplementedException();
					case Code.Bgt_Un: throw new NotImplementedException();
					case Code.Ble: throw new NotImplementedException();
					case Code.Ble_Un: throw new NotImplementedException();
					case Code.Blt: throw new NotImplementedException();
					case Code.Blt_Un: throw new NotImplementedException();
					case Code.Bne_Un: throw new NotImplementedException();
				#endregion
				#region Comparison
					case Code.Ceq: throw new NotImplementedException();
					case Code.Cgt: throw new NotImplementedException();
					case Code.Cgt_Un: throw new NotImplementedException();
					case Code.Clt: throw new NotImplementedException();
					case Code.Clt_Un: throw new NotImplementedException();
				#endregion
				#region Conversions
					case Code.Conv_I:  return new CodeCastExpression(typeof(int), arg1); // TODO
					case Code.Conv_I1: return new CodeCastExpression(typeof(SByte), arg1);
					case Code.Conv_I2: return new CodeCastExpression(typeof(Int16), arg1);
					case Code.Conv_I4: return new CodeCastExpression(typeof(Int32), arg1);
					case Code.Conv_I8: return new CodeCastExpression(typeof(Int64), arg1);
					case Code.Conv_U:  return new CodeCastExpression(typeof(uint), arg1); // TODO
					case Code.Conv_U1: return new CodeCastExpression(typeof(Byte), arg1);
					case Code.Conv_U2: return new CodeCastExpression(typeof(UInt16), arg1);
					case Code.Conv_U4: return new CodeCastExpression(typeof(UInt32), arg1);
					case Code.Conv_U8: return new CodeCastExpression(typeof(UInt64), arg1);
					case Code.Conv_R4: return new CodeCastExpression(typeof(float), arg1);
					case Code.Conv_R8: return new CodeCastExpression(typeof(double), arg1);
					case Code.Conv_R_Un: return new CodeCastExpression(typeof(double), arg1); // TODO
					
					case Code.Conv_Ovf_I:  return new CodeCastExpression(typeof(int), arg1); // TODO
					case Code.Conv_Ovf_I1: return new CodeCastExpression(typeof(SByte), arg1);
					case Code.Conv_Ovf_I2: return new CodeCastExpression(typeof(Int16), arg1);
					case Code.Conv_Ovf_I4: return new CodeCastExpression(typeof(Int32), arg1);
					case Code.Conv_Ovf_I8: return new CodeCastExpression(typeof(Int64), arg1);
					case Code.Conv_Ovf_U:  return new CodeCastExpression(typeof(uint), arg1); // TODO
					case Code.Conv_Ovf_U1: return new CodeCastExpression(typeof(Byte), arg1);
					case Code.Conv_Ovf_U2: return new CodeCastExpression(typeof(UInt16), arg1);
					case Code.Conv_Ovf_U4: return new CodeCastExpression(typeof(UInt32), arg1);
					case Code.Conv_Ovf_U8: return new CodeCastExpression(typeof(UInt64), arg1);
					
					case Code.Conv_Ovf_I_Un:  return new CodeCastExpression(typeof(int), arg1); // TODO
					case Code.Conv_Ovf_I1_Un: return new CodeCastExpression(typeof(SByte), arg1);
					case Code.Conv_Ovf_I2_Un: return new CodeCastExpression(typeof(Int16), arg1);
					case Code.Conv_Ovf_I4_Un: return new CodeCastExpression(typeof(Int32), arg1);
					case Code.Conv_Ovf_I8_Un: return new CodeCastExpression(typeof(Int64), arg1);
					case Code.Conv_Ovf_U_Un:  return new CodeCastExpression(typeof(uint), arg1); // TODO
					case Code.Conv_Ovf_U1_Un: return new CodeCastExpression(typeof(Byte), arg1);
					case Code.Conv_Ovf_U2_Un: return new CodeCastExpression(typeof(UInt16), arg1);
					case Code.Conv_Ovf_U4_Un: return new CodeCastExpression(typeof(UInt32), arg1);
					case Code.Conv_Ovf_U8_Un: return new CodeCastExpression(typeof(UInt64), arg1);
				#endregion
				#region Indirect
					case Code.Ldind_I: throw new NotImplementedException();
					case Code.Ldind_I1: throw new NotImplementedException();
					case Code.Ldind_I2: throw new NotImplementedException();
					case Code.Ldind_I4: throw new NotImplementedException();
					case Code.Ldind_I8: throw new NotImplementedException();
					case Code.Ldind_U1: throw new NotImplementedException();
					case Code.Ldind_U2: throw new NotImplementedException();
					case Code.Ldind_U4: throw new NotImplementedException();
					case Code.Ldind_R4: throw new NotImplementedException();
					case Code.Ldind_R8: throw new NotImplementedException();
					case Code.Ldind_Ref: throw new NotImplementedException();
					
					case Code.Stind_I: throw new NotImplementedException();
					case Code.Stind_I1: throw new NotImplementedException();
					case Code.Stind_I2: throw new NotImplementedException();
					case Code.Stind_I4: throw new NotImplementedException();
					case Code.Stind_I8: throw new NotImplementedException();
					case Code.Stind_R4: throw new NotImplementedException();
					case Code.Stind_R8: throw new NotImplementedException();
					case Code.Stind_Ref: throw new NotImplementedException();
				#endregion
				case Code.Arglist: throw new NotImplementedException();
				case Code.Box: throw new NotImplementedException();
				case Code.Break: throw new NotImplementedException();
				case Code.Call: throw new NotImplementedException();
				case Code.Calli: throw new NotImplementedException();
				case Code.Callvirt: throw new NotImplementedException();
				case Code.Castclass: throw new NotImplementedException();
				case Code.Ckfinite: throw new NotImplementedException();
				case Code.Constrained: throw new NotImplementedException();
				case Code.Cpblk: throw new NotImplementedException();
				case Code.Cpobj: throw new NotImplementedException();
				case Code.Dup: throw new NotImplementedException();
				case Code.Endfilter: throw new NotImplementedException();
				case Code.Endfinally: throw new NotImplementedException();
				case Code.Initblk: throw new NotImplementedException();
				case Code.Initobj: throw new NotImplementedException();
				case Code.Isinst: throw new NotImplementedException();
				case Code.Jmp: throw new NotImplementedException();
				case Code.Ldarg: return new CodeArgumentReferenceExpression(((ParameterDefinition)operand).Name);
				case Code.Ldarga: throw new NotImplementedException();
				case Code.Ldc_I4: 
				case Code.Ldc_I8: 
				case Code.Ldc_R4: 
				case Code.Ldc_R8: return new CodePrimitiveExpression(operand);
				case Code.Ldfld: throw new NotImplementedException();
				case Code.Ldflda: throw new NotImplementedException();
				case Code.Ldftn: throw new NotImplementedException();
				case Code.Ldloc: return new CodeVariableReferenceExpression(((VariableDefinition)operand).Name);
				case Code.Ldloca: throw new NotImplementedException();
				case Code.Ldnull: return new CodePrimitiveExpression(null);
				case Code.Ldobj: throw new NotImplementedException();
				case Code.Ldsfld: throw new NotImplementedException();
				case Code.Ldsflda: throw new NotImplementedException();
				case Code.Ldstr: return new CodePrimitiveExpression(operand);
				case Code.Ldtoken: throw new NotImplementedException();
				case Code.Ldvirtftn: throw new NotImplementedException();
				case Code.Leave: throw new NotImplementedException();
				case Code.Localloc: throw new NotImplementedException();
				case Code.Mkrefany: throw new NotImplementedException();
				case Code.Newobj: throw new NotImplementedException();
				case Code.No: throw new NotImplementedException();
				case Code.Nop: return new CodeSnippetExpression("/* No-op */");
				case Code.Or: throw new NotImplementedException();
				case Code.Pop: throw new NotImplementedException();
				case Code.Readonly: throw new NotImplementedException();
				case Code.Refanytype: throw new NotImplementedException();
				case Code.Refanyval: throw new NotImplementedException();
				case Code.Ret: throw new NotImplementedException();
				case Code.Rethrow: throw new NotImplementedException();
				case Code.Sizeof: throw new NotImplementedException();
				case Code.Starg: throw new NotImplementedException();
				case Code.Stfld: throw new NotImplementedException();
				case Code.Stloc: return new CodeAssignStatement(new CodeVariableReferenceExpression(((VariableDefinition)operand).Name), arg1);
				case Code.Stobj: throw new NotImplementedException();
				case Code.Stsfld: throw new NotImplementedException();
				case Code.Switch: throw new NotImplementedException();
				case Code.Tail: throw new NotImplementedException();
				case Code.Throw: throw new NotImplementedException();
				case Code.Unaligned: throw new NotImplementedException();
				case Code.Unbox: throw new NotImplementedException();
				case Code.Unbox_Any: throw new NotImplementedException();
				case Code.Volatile: throw new NotImplementedException();
				default: throw new Exception("Unknown OpCode: " + opCode);
			}
		}
	}
}
