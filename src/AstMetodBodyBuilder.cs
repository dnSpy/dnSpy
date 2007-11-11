using System;
using System.Collections.Generic;

using Ast = ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.Ast;

using Cecil = Mono.Cecil;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Decompiler
{
	public class AstMetodBodyBuilder
	{
		public static BlockStatement CreateMetodBody(MethodDefinition methodDef)
		{
			Ast.BlockStatement astBlock = new Ast.BlockStatement();
			
			methodDef.Body.Simplify();
			
			ByteCodeCollection body = new ByteCodeCollection(methodDef);
			
			foreach(VariableDefinition varDef in methodDef.Body.Variables) {
				Ast.VariableDeclaration astVar = new Ast.VariableDeclaration(varDef.Name);
				Ast.LocalVariableDeclaration astLocalVar = new Ast.LocalVariableDeclaration(astVar);
				astLocalVar.TypeReference = new Ast.TypeReference(varDef.VariableType.FullName);
				astBlock.Children.Add(astLocalVar);
			}
			
			foreach(ByteCode byteCode in body) {
				OpCode opCode = byteCode.OpCode;
				string description = 
					string.Format(" {1, -22} # {2}->{3} {4} {5}",
					              byteCode.Offset,
					              opCode + " " + FormatByteCodeOperand(byteCode.Operand),
					              opCode.StackBehaviourPop,
					              opCode.StackBehaviourPush,
					              opCode.FlowControl == FlowControl.Next ? string.Empty : "Flow=" + opCode.FlowControl,
					              opCode.OpCodeType == OpCodeType.Macro ? "(macro)" : string.Empty);
				
				Ast.Statement astStatement = null;
				try {
					int argCount = byteCode.PopCount;
					Ast.Expression[] args = new Ast.Expression[argCount];
					for(int i = 0; i < argCount; i++) {
						ByteCode allocBy = byteCode.StackBefore.Peek(argCount - i).AllocadedBy;
						string name = string.Format("expr{0:X2}", allocBy.Offset);
						args[i] = new Ast.IdentifierExpression(name);
					}
					object codeExpr = MakeCodeDomExpression(
						methodDef,
						byteCode,
						args);
					if (codeExpr is Ast.Expression) {
						if (byteCode.PushCount == 1) {
							string type = byteCode.Type.FullName;
							string name = string.Format("expr{0:X2}", byteCode.Offset);
							Ast.LocalVariableDeclaration astLocal = new Ast.LocalVariableDeclaration(new Ast.TypeReference(type.ToString()));
							astLocal.Variables.Add(new Ast.VariableDeclaration(name, (Ast.Expression)codeExpr));
							astStatement = astLocal;
						} else {
							astStatement = new ExpressionStatement((Ast.Expression)codeExpr);
						}
					} else if (codeExpr is Ast.Statement) {
						astStatement = (Ast.Statement)codeExpr;
					}
				} catch (NotImplementedException) {
					astStatement = MakeComment(description);
				}
				//astBlock.Children.Add(MakeComment(description));
				if (byteCode.BranchesHere.Count > 0) {
					astBlock.Children.Add(new Ast.LabelStatement(string.Format("IL_{0:X2}", byteCode.Offset)));
				}
				astBlock.Children.Add(astStatement);
				//astBlock.Children.Add(MakeComment(" " + stackAnalysis.StackAfter[instr].ToString()));
			}
			
			return astBlock;
		}
		
		static Ast.ExpressionStatement MakeComment(string text)
		{
			text = "/*" + text + "*/";
			return new Ast.ExpressionStatement(new PrimitiveExpression(text, text));
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
		
		static object MakeCodeDomExpression(MethodDefinition methodDef, ByteCode byteCode, params Ast.Expression[] args)
		{
			OpCode opCode = byteCode.OpCode;
			object operand = byteCode.Operand;
			Ast.TypeReference operandAsTypeRef = operand is Cecil.TypeReference ? new Ast.TypeReference(((Cecil.TypeReference)operand).FullName) : null;
			ByteCode operandAsByteCode = operand as ByteCode;
			string operandAsByteCodeLabel = operand is ByteCode ? String.Format("IL_{0:X2}", ((ByteCode)operand).Offset) : null;
			Ast.Expression arg1 = args.Length >= 1 ? args[0] : null;
			Ast.Expression arg2 = args.Length >= 2 ? args[1] : null;
			Ast.Expression arg3 = args.Length >= 3 ? args[2] : null;
			
			switch(opCode.Code) {
				#region Arithmetic
					case Code.Add:        return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Add, arg2);
					case Code.Add_Ovf:    return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Add, arg2);
					case Code.Add_Ovf_Un: return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Add, arg2);
					case Code.Div:        return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Divide, arg2);
					case Code.Div_Un:     return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Divide, arg2);
					case Code.Mul:        return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Multiply, arg2);
					case Code.Mul_Ovf:    return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Multiply, arg2);
					case Code.Mul_Ovf_Un: return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Multiply, arg2);
					case Code.Rem:        return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Modulus, arg2);
					case Code.Rem_Un:     return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Modulus, arg2);
					case Code.Sub:        return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Subtract, arg2);
					case Code.Sub_Ovf:    return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Subtract, arg2);
					case Code.Sub_Ovf_Un: return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Subtract, arg2);
					case Code.And:        return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.BitwiseAnd, arg2);
					case Code.Xor:        return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.ExclusiveOr, arg2);
					case Code.Shl:        return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.ShiftLeft, arg2);
					case Code.Shr:        return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.ShiftRight, arg2);
					case Code.Shr_Un:     return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.ShiftRight, arg2);
					
					case Code.Neg:        return new Ast.UnaryOperatorExpression(arg1, UnaryOperatorType.Minus);
					case Code.Not:        return new Ast.UnaryOperatorExpression(arg1, UnaryOperatorType.BitNot);
				#endregion
				#region Arrays
					case Code.Newarr:
						operandAsTypeRef.RankSpecifier = new int[] {0};
						return new Ast.ArrayCreateExpression(operandAsTypeRef, new List<Expression>(new Expression[] {arg1}));
					
					case Code.Ldlen: return new Ast.MemberReferenceExpression(arg1, "Length");
					
					case Code.Ldelem_I:   
					case Code.Ldelem_I1:  
					case Code.Ldelem_I2:  
					case Code.Ldelem_I4:  
					case Code.Ldelem_I8:  
					case Code.Ldelem_U1:  
					case Code.Ldelem_U2:  
					case Code.Ldelem_U4:  
					case Code.Ldelem_R4:  
					case Code.Ldelem_R8:  
					case Code.Ldelem_Ref: return new Ast.IndexerExpression(arg1, new List<Expression>(new Expression[] {arg2}));
					case Code.Ldelem_Any: throw new NotImplementedException();
					case Code.Ldelema:    return new Ast.IndexerExpression(arg1, new List<Expression>(new Expression[] {arg2}));
					
					case Code.Stelem_I:   
					case Code.Stelem_I1:  
					case Code.Stelem_I2:  
					case Code.Stelem_I4:  
					case Code.Stelem_I8:  
					case Code.Stelem_R4:  
					case Code.Stelem_R8:  
					case Code.Stelem_Ref: return new Ast.AssignmentExpression(new Ast.IndexerExpression(arg1, new List<Expression>(new Expression[] {arg2})), AssignmentOperatorType.Assign, arg3);
					case Code.Stelem_Any: throw new NotImplementedException();
				#endregion
				#region Branching
					case Code.Br:      return new Ast.GotoStatement(operandAsByteCodeLabel);
					case Code.Brfalse: return new Ast.IfElseStatement(new Ast.UnaryOperatorExpression(arg1, UnaryOperatorType.Not), new Ast.GotoStatement(operandAsByteCodeLabel));
					case Code.Brtrue:  return new Ast.IfElseStatement(arg1, new Ast.GotoStatement(operandAsByteCodeLabel));
					case Code.Beq:     return new Ast.IfElseStatement(new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Equality, arg2), new Ast.GotoStatement(operandAsByteCodeLabel));
					case Code.Bge:     return new Ast.IfElseStatement(new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.GreaterThanOrEqual, arg2), new Ast.GotoStatement(operandAsByteCodeLabel));
					case Code.Bge_Un:  return new Ast.IfElseStatement(new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.GreaterThanOrEqual, arg2), new Ast.GotoStatement(operandAsByteCodeLabel));
					case Code.Bgt:     return new Ast.IfElseStatement(new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.GreaterThan, arg2), new Ast.GotoStatement(operandAsByteCodeLabel));
					case Code.Bgt_Un:  return new Ast.IfElseStatement(new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.GreaterThan, arg2), new Ast.GotoStatement(operandAsByteCodeLabel));
					case Code.Ble:     return new Ast.IfElseStatement(new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.LessThanOrEqual, arg2), new Ast.GotoStatement(operandAsByteCodeLabel));
					case Code.Ble_Un:  return new Ast.IfElseStatement(new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.LessThanOrEqual, arg2), new Ast.GotoStatement(operandAsByteCodeLabel));
					case Code.Blt:     return new Ast.IfElseStatement(new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.LessThan, arg2), new Ast.GotoStatement(operandAsByteCodeLabel));
					case Code.Blt_Un:  return new Ast.IfElseStatement(new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.LessThan, arg2), new Ast.GotoStatement(operandAsByteCodeLabel));
					case Code.Bne_Un:  return new Ast.IfElseStatement(new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.InEquality, arg2), new Ast.GotoStatement(operandAsByteCodeLabel));
				#endregion
				#region Comparison
					case Code.Ceq:    return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Equality, ConvertIntToBool(arg2));
					case Code.Cgt:    return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.GreaterThan, arg2);
					case Code.Cgt_Un: return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.GreaterThan, arg2);
					case Code.Clt:    return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.LessThan, arg2);
					case Code.Clt_Un: return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.LessThan, arg2);
				#endregion
				#region Conversions
					case Code.Conv_I:    return new Ast.CastExpression(new Ast.TypeReference(typeof(int).Name), arg1, CastType.Cast); // TODO
					case Code.Conv_I1:   return new Ast.CastExpression(new Ast.TypeReference(typeof(SByte).Name), arg1, CastType.Cast);
					case Code.Conv_I2:   return new Ast.CastExpression(new Ast.TypeReference(typeof(Int16).Name), arg1, CastType.Cast);
					case Code.Conv_I4:   return new Ast.CastExpression(new Ast.TypeReference(typeof(Int32).Name), arg1, CastType.Cast);
					case Code.Conv_I8:   return new Ast.CastExpression(new Ast.TypeReference(typeof(Int64).Name), arg1, CastType.Cast);
					case Code.Conv_U:    return new Ast.CastExpression(new Ast.TypeReference(typeof(uint).Name), arg1, CastType.Cast); // TODO
					case Code.Conv_U1:   return new Ast.CastExpression(new Ast.TypeReference(typeof(Byte).Name), arg1, CastType.Cast);
					case Code.Conv_U2:   return new Ast.CastExpression(new Ast.TypeReference(typeof(UInt16).Name), arg1, CastType.Cast);
					case Code.Conv_U4:   return new Ast.CastExpression(new Ast.TypeReference(typeof(UInt32).Name), arg1, CastType.Cast);
					case Code.Conv_U8:   return new Ast.CastExpression(new Ast.TypeReference(typeof(UInt64).Name), arg1, CastType.Cast);
					case Code.Conv_R4:   return new Ast.CastExpression(new Ast.TypeReference(typeof(float).Name), arg1, CastType.Cast);
					case Code.Conv_R8:   return new Ast.CastExpression(new Ast.TypeReference(typeof(double).Name), arg1, CastType.Cast);
					case Code.Conv_R_Un: return new Ast.CastExpression(new Ast.TypeReference(typeof(double).Name), arg1, CastType.Cast); // TODO
					
					case Code.Conv_Ovf_I:  return new Ast.CastExpression(new Ast.TypeReference(typeof(int).Name), arg1, CastType.Cast); // TODO
					case Code.Conv_Ovf_I1: return new Ast.CastExpression(new Ast.TypeReference(typeof(SByte).Name), arg1, CastType.Cast);
					case Code.Conv_Ovf_I2: return new Ast.CastExpression(new Ast.TypeReference(typeof(Int16).Name), arg1, CastType.Cast);
					case Code.Conv_Ovf_I4: return new Ast.CastExpression(new Ast.TypeReference(typeof(Int32).Name), arg1, CastType.Cast);
					case Code.Conv_Ovf_I8: return new Ast.CastExpression(new Ast.TypeReference(typeof(Int64).Name), arg1, CastType.Cast);
					case Code.Conv_Ovf_U:  return new Ast.CastExpression(new Ast.TypeReference(typeof(uint).Name), arg1, CastType.Cast); // TODO
					case Code.Conv_Ovf_U1: return new Ast.CastExpression(new Ast.TypeReference(typeof(Byte).Name), arg1, CastType.Cast);
					case Code.Conv_Ovf_U2: return new Ast.CastExpression(new Ast.TypeReference(typeof(UInt16).Name), arg1, CastType.Cast);
					case Code.Conv_Ovf_U4: return new Ast.CastExpression(new Ast.TypeReference(typeof(UInt32).Name), arg1, CastType.Cast);
					case Code.Conv_Ovf_U8: return new Ast.CastExpression(new Ast.TypeReference(typeof(UInt64).Name), arg1, CastType.Cast);
					
					case Code.Conv_Ovf_I_Un:  return new Ast.CastExpression(new Ast.TypeReference(typeof(int).Name), arg1, CastType.Cast); // TODO
					case Code.Conv_Ovf_I1_Un: return new Ast.CastExpression(new Ast.TypeReference(typeof(SByte).Name), arg1, CastType.Cast);
					case Code.Conv_Ovf_I2_Un: return new Ast.CastExpression(new Ast.TypeReference(typeof(Int16).Name), arg1, CastType.Cast);
					case Code.Conv_Ovf_I4_Un: return new Ast.CastExpression(new Ast.TypeReference(typeof(Int32).Name), arg1, CastType.Cast);
					case Code.Conv_Ovf_I8_Un: return new Ast.CastExpression(new Ast.TypeReference(typeof(Int64).Name), arg1, CastType.Cast);
					case Code.Conv_Ovf_U_Un:  return new Ast.CastExpression(new Ast.TypeReference(typeof(uint).Name), arg1, CastType.Cast); // TODO
					case Code.Conv_Ovf_U1_Un: return new Ast.CastExpression(new Ast.TypeReference(typeof(Byte).Name), arg1, CastType.Cast);
					case Code.Conv_Ovf_U2_Un: return new Ast.CastExpression(new Ast.TypeReference(typeof(UInt16).Name), arg1, CastType.Cast);
					case Code.Conv_Ovf_U4_Un: return new Ast.CastExpression(new Ast.TypeReference(typeof(UInt32).Name), arg1, CastType.Cast);
					case Code.Conv_Ovf_U8_Un: return new Ast.CastExpression(new Ast.TypeReference(typeof(UInt64).Name), arg1, CastType.Cast);
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
				case Code.Call:
					Cecil.MethodReference cecilMethod = ((MethodReference)operand);
					Ast.IdentifierExpression astType = new Ast.IdentifierExpression(cecilMethod.DeclaringType.FullName);
					List<Ast.Expression> methodArgs = new List<Ast.Expression>(args);
					if (cecilMethod.HasThis) {
						methodArgs.RemoveAt(0); // Remove 'this'
						return new Ast.InvocationExpression(new Ast.MemberReferenceExpression(arg1, cecilMethod.Name), methodArgs);
					} else {
						return new Ast.InvocationExpression(new Ast.MemberReferenceExpression(astType, cecilMethod.Name), methodArgs);
					}
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
				case Code.Ldarg: return new Ast.IdentifierExpression(((ParameterDefinition)operand).Name);
				case Code.Ldarga: throw new NotImplementedException();
				case Code.Ldc_I4: 
				case Code.Ldc_I8: 
				case Code.Ldc_R4: 
				case Code.Ldc_R8: return new Ast.PrimitiveExpression(operand, null);
				case Code.Ldfld: throw new NotImplementedException();
				case Code.Ldflda: throw new NotImplementedException();
				case Code.Ldftn: throw new NotImplementedException();
				case Code.Ldloc: return new Ast.IdentifierExpression(((VariableDefinition)operand).Name);
				case Code.Ldloca: throw new NotImplementedException();
				case Code.Ldnull: return new Ast.PrimitiveExpression(null, null);
				case Code.Ldobj: throw new NotImplementedException();
				case Code.Ldsfld: throw new NotImplementedException();
				case Code.Ldsflda: throw new NotImplementedException();
				case Code.Ldstr: return new Ast.PrimitiveExpression(operand, null);
				case Code.Ldtoken: throw new NotImplementedException();
				case Code.Ldvirtftn: throw new NotImplementedException();
				case Code.Leave: throw new NotImplementedException();
				case Code.Localloc: throw new NotImplementedException();
				case Code.Mkrefany: throw new NotImplementedException();
				case Code.Newobj: throw new NotImplementedException();
				case Code.No: throw new NotImplementedException();
				case Code.Nop: return new Ast.PrimitiveExpression("/* No-op */", "/* No-op */");
				case Code.Or: throw new NotImplementedException();
				case Code.Pop: throw new NotImplementedException();
				case Code.Readonly: throw new NotImplementedException();
				case Code.Refanytype: throw new NotImplementedException();
				case Code.Refanyval: throw new NotImplementedException();
				case Code.Ret: return new Ast.ReturnStatement(methodDef.ReturnType.ReturnType.FullName != Cecil.Constants.Void ? arg1 : null);
				case Code.Rethrow: throw new NotImplementedException();
				case Code.Sizeof: throw new NotImplementedException();
				case Code.Starg: throw new NotImplementedException();
				case Code.Stfld: throw new NotImplementedException();
				case Code.Stloc: return new Ast.AssignmentExpression(new Ast.IdentifierExpression(((VariableDefinition)operand).Name), AssignmentOperatorType.Assign, arg1);
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
		
		static Ast.Expression ConvertIntToBool(Ast.Expression astInt)
		{
			return new Ast.ParenthesizedExpression(new Ast.BinaryOperatorExpression(astInt, BinaryOperatorType.InEquality, new Ast.PrimitiveExpression(0, "0")));
		}
	}
}
