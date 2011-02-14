using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Ast = ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp;
using Cecil = Mono.Cecil;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Decompiler.ControlFlow;

namespace Decompiler
{
	public class AstMethodBodyBuilder
	{
		MethodDefinition methodDef;
		static Dictionary<string, Cecil.TypeReference> localVarTypes = new Dictionary<string, Cecil.TypeReference>();
		static Dictionary<string, bool> localVarDefined = new Dictionary<string, bool>();
		
		public static BlockStatement CreateMethodBody(MethodDefinition methodDef)
		{
			AstMethodBodyBuilder builder = new AstMethodBodyBuilder();
			builder.methodDef = methodDef;
			return builder.CreateMethodBody();
		}
		
		static readonly Dictionary<string, string> typeNameToVariableNameDict = new Dictionary<string, string> {
			{ "System.Boolean", "flag" },
			{ "System.Byte", "b" },
			{ "System.SByte", "b" },
			{ "System.Int16", "num" },
			{ "System.Int32", "num" },
			{ "System.Int64", "num" },
			{ "System.UInt16", "num" },
			{ "System.UInt32", "num" },
			{ "System.UInt64", "num" },
			{ "System.Single", "num" },
			{ "System.Double", "num" },
			{ "System.Decimal", "num" },
			{ "System.String", "text" },
			{ "System.Object", "obj" },
		};
		
		public BlockStatement CreateMethodBody()
		{
			if (methodDef.Body == null) return null;
			
			List<ILNode> body = new ILAstBuilder().Build(methodDef, true);
			
			ILAstOptimizer bodyGraph = new ILAstOptimizer();
			bodyGraph.Optimize(ref body);
			
			List<string> intNames = new List<string>(new string[] {"i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t"});
			Dictionary<string, int> typeNames = new Dictionary<string, int>();
			foreach(VariableDefinition varDef in methodDef.Body.Variables) {
				if (string.IsNullOrEmpty(varDef.Name)) {
					if (varDef.VariableType.FullName == Constants.Int32 && intNames.Count > 0) {
						varDef.Name = intNames[0];
						intNames.RemoveAt(0);
					} else {
						string name;
						if (varDef.VariableType.IsArray) {
							name = "array";
						} else if (!typeNameToVariableNameDict.TryGetValue(varDef.VariableType.FullName, out name)) {
							name = varDef.VariableType.Name;
							// remove the 'I' for interfaces
							if (name.Length >= 3 && name[0] == 'I' && char.IsUpper(name[1]) && char.IsLower(name[2]))
								name = name.Substring(1);
							// remove the backtick (generics)
							int pos = name.IndexOf('`');
							if (pos >= 0)
								name = name.Substring(0, pos);
							if (name.Length == 0)
								name = "obj";
							else
								name = char.ToLower(name[0]) + name.Substring(1);
						}
						if (!typeNames.ContainsKey(name)) {
							typeNames.Add(name, 0);
						}
						int count = typeNames[name];
						if (count > 0) {
							name += count.ToString();
						}
						varDef.Name = name;
					}
				}
				localVarTypes[varDef.Name] = varDef.VariableType;
				localVarDefined[varDef.Name] = false;
				
//				Ast.VariableDeclaration astVar = new Ast.VariableDeclaration(varDef.Name);
//				Ast.LocalVariableDeclaration astLocalVar = new Ast.LocalVariableDeclaration(astVar);
//				astLocalVar.TypeReference = new Ast.TypeReference(varDef.VariableType.FullName);
//				astBlock.Children.Add(astLocalVar);
			}
			
			Ast.BlockStatement astBlock = TransformBlock(new ILBlock(body));
			CommentStatement.ReplaceAll(astBlock); // convert CommentStatements to Comments
			return astBlock;
		}
		
		Ast.BlockStatement TransformBlock(ILBlock block)
		{
			Ast.BlockStatement astBlock = new BlockStatement();
			if (block != null) {
				foreach(ILNode node in block.Body) {
					astBlock.AddStatements(TransformNode(node));
				}
			}
			return astBlock;
		}
		
		IEnumerable<Statement> TransformNode(ILNode node)
		{
			if (node is ILLabel) {
				yield return new Ast.LabelStatement { Label = ((ILLabel)node).Name };
			} else if (node is ILExpression) {
				object codeExpr = TransformExpression((ILExpression)node);
				if (codeExpr != null) {
					if (codeExpr is Ast.Expression) {
						yield return new Ast.ExpressionStatement { Expression = (Ast.Expression)codeExpr };
					} else if (codeExpr is Ast.Statement) {
						yield return (Ast.Statement)codeExpr;
					} else {
						throw new Exception();
					}
				}
			} else if (node is ILLoop) {
				yield return new Ast.ForStatement {
					EmbeddedStatement = TransformBlock(((ILLoop)node).ContentBlock)
				};
			/*
			} else if (node is Branch) {
				yield return new Ast.LabelStatement { Label = ((Branch)node).FirstBasicBlock.Label };
				
				Ast.BlockStatement trueBlock = new Ast.BlockStatement();
				trueBlock.AddStatement(new Ast.GotoStatement(((Branch)node).TrueSuccessor.Label));
				
				Ast.BlockStatement falseBlock = new Ast.BlockStatement();
				falseBlock.AddStatement(new Ast.GotoStatement(((Branch)node).FalseSuccessor.Label));
				
				Ast.IfElseStatement ifElseStmt = new Ast.IfElseStatement {
					Condition = MakeBranchCondition((Branch)node),
					TrueStatement = trueBlock,
					FalseStatement = falseBlock
				};
				
				yield return ifElseStmt;
			*/
			} else if (node is ILCondition) {
				ILCondition conditionalNode = (ILCondition)node;
				// Swap bodies
				yield return new Ast.IfElseStatement {
					Condition = new UnaryOperatorExpression(UnaryOperatorType.Not, MakeBranchCondition(conditionalNode.Condition)),
					TrueStatement = TransformBlock(conditionalNode.FalseBlock),
					FalseStatement = TransformBlock(conditionalNode.TrueBlock)
				};
			} else if (node is ILTryCatchBlock) {
				ILTryCatchBlock tryCatchNode = ((ILTryCatchBlock)node);
				List<Ast.CatchClause> catchClauses = new List<CatchClause>();
				foreach (var catchClause in tryCatchNode.CatchBlocks) {
					catchClauses.Add(new Ast.CatchClause {
						Type = AstBuilder.ConvertType(catchClause.ExceptionType),
						VariableName = "exception",
						Body = TransformBlock(catchClause)
					});
				}
				yield return new Ast.TryCatchStatement {
					TryBlock = TransformBlock(tryCatchNode.TryBlock),
					CatchClauses = catchClauses,
					FinallyBlock = tryCatchNode.FinallyBlock != null ? TransformBlock(tryCatchNode.FinallyBlock) : null
				};
			} else if (node is ILBlock) {
				yield return TransformBlock((ILBlock)node);
			} else {
				throw new Exception("Unknown node type");
			}
		}
		
		List<Ast.Expression> TransformExpressionArguments(ILExpression expr)
		{
			List<Ast.Expression> args = new List<Ast.Expression>();
			// Args generated by nested expressions (which must be closed)
			foreach(ILExpression arg in expr.Arguments) {
				args.Add((Ast.Expression)TransformExpression(arg));
			}
			return args;
		}
		
		object TransformExpression(ILExpression expr)
		{
			List<Ast.Expression> args = TransformExpressionArguments(expr);
			return TransformByteCode(methodDef, expr, args);
		}
		
		Ast.Expression MakeBranchCondition(ILExpression expr)
		{
			List<Ast.Expression> args = TransformExpressionArguments(expr);
			Ast.Expression arg1 = args.Count >= 1 ? args[0] : null;
			Ast.Expression arg2 = args.Count >= 2 ? args[1] : null;
			switch(expr.OpCode.Code) {
				case Code.Brfalse: return new Ast.UnaryOperatorExpression(UnaryOperatorType.Not, arg1);
				case Code.Brtrue:  return arg1;
				case Code.Beq:     return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Equality, arg2);
				case Code.Bge:     return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.GreaterThanOrEqual, arg2);
				case Code.Bge_Un:  return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.GreaterThanOrEqual, arg2);
				case Code.Bgt:     return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.GreaterThan, arg2);
				case Code.Bgt_Un:  return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.GreaterThan, arg2);
				case Code.Ble:     return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.LessThanOrEqual, arg2);
				case Code.Ble_Un:  return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.LessThanOrEqual, arg2);
				case Code.Blt:     return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.LessThan, arg2);
				case Code.Blt_Un:  return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.LessThan, arg2);
				case Code.Bne_Un:  return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.InEquality, arg2);
				default: throw new Exception("Bad opcode");
			}
			/*
			} else if (branch is ShortCircuitBranch) {
				ShortCircuitBranch scBranch = (ShortCircuitBranch)branch;
				switch(scBranch.Operator) {
					case ShortCircuitOperator.LeftAndRight:
						return new BinaryOperatorExpression(
							MakeBranchCondition(scBranch.Left),
							BinaryOperatorType.ConditionalAnd,
							MakeBranchCondition(scBranch.Right)
						);
					case ShortCircuitOperator.LeftOrRight:
						return new BinaryOperatorExpression(
							MakeBranchCondition(scBranch.Left),
							BinaryOperatorType.ConditionalOr,
							MakeBranchCondition(scBranch.Right)
						);
					case ShortCircuitOperator.NotLeftAndRight:
						return new BinaryOperatorExpression(
							new UnaryOperatorExpression(UnaryOperatorType.Not, MakeBranchCondition(scBranch.Left)),
							BinaryOperatorType.ConditionalAnd,
							MakeBranchCondition(scBranch.Right)
						);
					case ShortCircuitOperator.NotLeftOrRight:
						return new BinaryOperatorExpression(
							new UnaryOperatorExpression(UnaryOperatorType.Not, MakeBranchCondition(scBranch.Left)),
							BinaryOperatorType.ConditionalOr,
							MakeBranchCondition(scBranch.Right)
						);
					default:
						throw new Exception("Bad operator");
				}
			} else {
				throw new Exception("Bad type");
			}
			*/
		}
		
		static object TransformByteCode(MethodDefinition methodDef, ILExpression byteCode, List<Ast.Expression> args)
		{
			try {
				AstNode ret = TransformByteCode_Internal(methodDef, byteCode, args);
				// ret.UserData["Type"] = byteCode.Type;
				return ret;
			} catch (NotImplementedException) {
				// Output the operand of the unknown IL code as well
				if (byteCode.Operand != null) {
					args.Insert(0, new IdentifierExpression(FormatByteCodeOperand(byteCode.Operand)));
				}
				return new IdentifierExpression(byteCode.OpCode.Name).Invoke(args);
			}
		}
		
		static string FormatByteCodeOperand(object operand)
		{
			if (operand == null) {
				return string.Empty;
				//} else if (operand is ILExpression) {
				//	return string.Format("IL_{0:X2}", ((ILExpression)operand).Offset);
			} else if (operand is MethodReference) {
				return ((MethodReference)operand).Name + "()";
			} else if (operand is Cecil.TypeReference) {
				return ((Cecil.TypeReference)operand).FullName;
			} else if (operand is VariableDefinition) {
				return ((VariableDefinition)operand).Name;
			} else if (operand is ParameterDefinition) {
				return ((ParameterDefinition)operand).Name;
			} else if (operand is FieldReference) {
				return ((FieldReference)operand).Name;
			} else if (operand is string) {
				return "\"" + operand + "\"";
			} else if (operand is int) {
				return operand.ToString();
			} else {
				return operand.ToString();
			}
		}
		
		static AstNode TransformByteCode_Internal(MethodDefinition methodDef, ILExpression byteCode, List<Ast.Expression> args)
		{
			// throw new NotImplementedException();
			
			OpCode opCode = byteCode.OpCode;
			object operand = byteCode.Operand;
			AstType operandAsTypeRef = AstBuilder.ConvertType(operand as Cecil.TypeReference);
			ILExpression operandAsByteCode = operand as ILExpression;
			Ast.Expression arg1 = args.Count >= 1 ? args[0] : null;
			Ast.Expression arg2 = args.Count >= 2 ? args[1] : null;
			Ast.Expression arg3 = args.Count >= 3 ? args[2] : null;
			
			BlockStatement branchCommand = null;
			if (byteCode.Operand is ILLabel) {
				branchCommand = new BlockStatement();
				branchCommand.AddStatement(new Ast.GotoStatement(((ILLabel)byteCode.Operand).Name));
			}
			
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
					
					case Code.Neg:        return new Ast.UnaryOperatorExpression(UnaryOperatorType.Minus, arg1);
					case Code.Not:        return new Ast.UnaryOperatorExpression(UnaryOperatorType.BitNot, arg1);
					#endregion
					#region Arrays
				case Code.Newarr:
					operandAsTypeRef = operandAsTypeRef.MakeArrayType(0);
					return new Ast.ArrayCreateExpression {
						Type = operandAsTypeRef,
						Arguments = new Expression[] {arg1}
					};
					
				case Code.Ldlen:
					return arg1.Member("Length");
					
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
				case Code.Ldelem_Ref:
					return arg1.Indexer(arg2);
				case Code.Ldelem_Any:
					throw new NotImplementedException();
				case Code.Ldelema:
					return arg1.Indexer(arg2);
					
				case Code.Stelem_I:
				case Code.Stelem_I1:
				case Code.Stelem_I2:
				case Code.Stelem_I4:
				case Code.Stelem_I8:
				case Code.Stelem_R4:
				case Code.Stelem_R8:
				case Code.Stelem_Ref:
					return new Ast.AssignmentExpression(arg1.Indexer(arg2), arg3);
				case Code.Stelem_Any:
					throw new NotImplementedException();
					#endregion
					#region Branching
					case Code.Br:      return new Ast.GotoStatement(((ILLabel)byteCode.Operand).Name);
					case Code.Brfalse: return new Ast.IfElseStatement(new Ast.UnaryOperatorExpression(UnaryOperatorType.Not, arg1), branchCommand);
					case Code.Brtrue:  return new Ast.IfElseStatement(arg1, branchCommand);
					case Code.Beq:     return new Ast.IfElseStatement(new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Equality, arg2), branchCommand);
					case Code.Bge:     return new Ast.IfElseStatement(new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.GreaterThanOrEqual, arg2), branchCommand);
					case Code.Bge_Un:  return new Ast.IfElseStatement(new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.GreaterThanOrEqual, arg2), branchCommand);
					case Code.Bgt:     return new Ast.IfElseStatement(new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.GreaterThan, arg2), branchCommand);
					case Code.Bgt_Un:  return new Ast.IfElseStatement(new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.GreaterThan, arg2), branchCommand);
					case Code.Ble:     return new Ast.IfElseStatement(new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.LessThanOrEqual, arg2), branchCommand);
					case Code.Ble_Un:  return new Ast.IfElseStatement(new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.LessThanOrEqual, arg2), branchCommand);
					case Code.Blt:     return new Ast.IfElseStatement(new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.LessThan, arg2), branchCommand);
					case Code.Blt_Un:  return new Ast.IfElseStatement(new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.LessThan, arg2), branchCommand);
					case Code.Bne_Un:  return new Ast.IfElseStatement(new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.InEquality, arg2), branchCommand);
					#endregion
					#region Comparison
					case Code.Ceq:    return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Equality, ConvertIntToBool(arg2));
					case Code.Cgt:    return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.GreaterThan, arg2);
					case Code.Cgt_Un: return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.GreaterThan, arg2);
					case Code.Clt:    return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.LessThan, arg2);
					case Code.Clt_Un: return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.LessThan, arg2);
					#endregion
					#region Conversions
					case Code.Conv_I:    return arg1.CastTo(typeof(int)); // TODO
					case Code.Conv_I1:   return arg1.CastTo(typeof(SByte));
					case Code.Conv_I2:   return arg1.CastTo(typeof(Int16));
					case Code.Conv_I4:   return arg1.CastTo(typeof(Int32));
					case Code.Conv_I8:   return arg1.CastTo(typeof(Int64));
					case Code.Conv_U:    return arg1.CastTo(typeof(uint)); // TODO
					case Code.Conv_U1:   return arg1.CastTo(typeof(Byte));
					case Code.Conv_U2:   return arg1.CastTo(typeof(UInt16));
					case Code.Conv_U4:   return arg1.CastTo(typeof(UInt32));
					case Code.Conv_U8:   return arg1.CastTo(typeof(UInt64));
					case Code.Conv_R4:   return arg1.CastTo(typeof(float));
					case Code.Conv_R8:   return arg1.CastTo(typeof(double));
					case Code.Conv_R_Un: return arg1.CastTo(typeof(double)); // TODO
					
					case Code.Conv_Ovf_I:  return arg1.CastTo(typeof(int));
					case Code.Conv_Ovf_I1: return arg1.CastTo(typeof(SByte));
					case Code.Conv_Ovf_I2: return arg1.CastTo(typeof(Int16));
					case Code.Conv_Ovf_I4: return arg1.CastTo(typeof(Int32));
					case Code.Conv_Ovf_I8: return arg1.CastTo(typeof(Int64));
					case Code.Conv_Ovf_U:  return arg1.CastTo(typeof(uint));
					case Code.Conv_Ovf_U1: return arg1.CastTo(typeof(Byte));
					case Code.Conv_Ovf_U2: return arg1.CastTo(typeof(UInt16));
					case Code.Conv_Ovf_U4: return arg1.CastTo(typeof(UInt32));
					case Code.Conv_Ovf_U8: return arg1.CastTo(typeof(UInt64));
					
					case Code.Conv_Ovf_I_Un:  return arg1.CastTo(typeof(int));
					case Code.Conv_Ovf_I1_Un: return arg1.CastTo(typeof(SByte));
					case Code.Conv_Ovf_I2_Un: return arg1.CastTo(typeof(Int16));
					case Code.Conv_Ovf_I4_Un: return arg1.CastTo(typeof(Int32));
					case Code.Conv_Ovf_I8_Un: return arg1.CastTo(typeof(Int64));
					case Code.Conv_Ovf_U_Un:  return arg1.CastTo(typeof(uint));
					case Code.Conv_Ovf_U1_Un: return arg1.CastTo(typeof(Byte));
					case Code.Conv_Ovf_U2_Un: return arg1.CastTo(typeof(UInt16));
					case Code.Conv_Ovf_U4_Un: return arg1.CastTo(typeof(UInt32));
					case Code.Conv_Ovf_U8_Un: return arg1.CastTo(typeof(UInt64));
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
				case Code.Callvirt:
					// TODO: Diferentiate vitual and non-vitual dispach
					Cecil.MethodReference cecilMethod = ((MethodReference)operand);
					Ast.Expression target;
					List<Ast.Expression> methodArgs = new List<Ast.Expression>(args);
					if (cecilMethod.HasThis) {
						target = methodArgs[0];
						methodArgs.RemoveAt(0);
					} else {
						target = new TypeReferenceExpression { Type = AstBuilder.ConvertType(cecilMethod.DeclaringType)};
					}
					
					// TODO: Constructors are ignored
					if (cecilMethod.Name == ".ctor") {
						return new CommentStatement("Constructor");
					}
					
					// TODO: Hack, detect properties properly
					if (cecilMethod.Name.StartsWith("get_")) {
						return target.Member(cecilMethod.Name.Remove(0, 4)).WithAnnotation(cecilMethod);
					} else if (cecilMethod.Name.StartsWith("set_")) {
						return new Ast.AssignmentExpression(
							target.Member(cecilMethod.Name.Remove(0, 4)).WithAnnotation(cecilMethod),
							methodArgs[0]
						);
					}
					
					// Multi-dimensional array acces // TODO: do properly
					/*
					if (cecilMethod.Name == "Get") {
						return new Ast.IndexerExpression(target, methodArgs);
					} else if (cecilMethod.Name == "Set") {
						Expression val = methodArgs[methodArgs.Count - 1];
						methodArgs.RemoveAt(methodArgs.Count - 1);
						return new Ast.AssignmentExpression(
							new Ast.IndexerExpression(target, methodArgs),
							AssignmentOperatorType.Assign,
							Convert(val, ((Cecil.ArrayType)target.UserData["Type"]).ElementType)
						);
					}*/
					
					// Default invocation
					return target.Invoke(cecilMethod.Name, methodArgs).WithAnnotation(cecilMethod);
					case Code.Calli: throw new NotImplementedException();
					case Code.Castclass: return arg1.CastTo(operandAsTypeRef);
					case Code.Ckfinite: throw new NotImplementedException();
					case Code.Constrained: throw new NotImplementedException();
					case Code.Cpblk: throw new NotImplementedException();
					case Code.Cpobj: throw new NotImplementedException();
					case Code.Dup: return arg1;
					case Code.Endfilter: throw new NotImplementedException();
					case Code.Endfinally: return null;
					case Code.Initblk: throw new NotImplementedException();
					case Code.Initobj: throw new NotImplementedException();
					case Code.Isinst: return arg1.IsType(AstBuilder.ConvertType((Cecil.TypeReference)operand));
					case Code.Jmp: throw new NotImplementedException();
				case Code.Ldarg:
					if (methodDef.HasThis && ((ParameterDefinition)operand).Index < 0) {
						return new Ast.ThisReferenceExpression();
					} else {
						return new Ast.IdentifierExpression(((ParameterDefinition)operand).Name);
					}
					case Code.Ldarga: throw new NotImplementedException();
				case Code.Ldc_I4:
				case Code.Ldc_I8:
				case Code.Ldc_R4:
				case Code.Ldc_R8:
					return new Ast.PrimitiveExpression(operand);
				case Code.Ldfld:
					return arg1.Member(((FieldReference) operand).Name).WithAnnotation(operand);
				case Code.Ldsfld:
					return AstBuilder.ConvertType(((FieldReference)operand).DeclaringType)
						.Member(((FieldReference)operand).Name).WithAnnotation(operand);
				case Code.Stfld:
					return new AssignmentExpression(arg1.Member(((FieldReference) operand).Name).WithAnnotation(operand), arg2);
				case Code.Stsfld:
					return new AssignmentExpression(
						AstBuilder.ConvertType(((FieldReference)operand).DeclaringType)
						.Member(((FieldReference)operand).Name).WithAnnotation(operand),
						arg1);
				case Code.Ldflda:
					case Code.Ldsflda: throw new NotImplementedException();
					case Code.Ldftn: throw new NotImplementedException();
				case Code.Ldloc:
					if (operand is ILVariable) {
						return new Ast.IdentifierExpression(((ILVariable)operand).Name);
					} else {
						return new Ast.IdentifierExpression(((VariableDefinition)operand).Name);
					}
					case Code.Ldloca: throw new NotImplementedException();
					case Code.Ldnull: return new Ast.PrimitiveExpression(null);
					case Code.Ldobj: throw new NotImplementedException();
					case Code.Ldstr: return new Ast.PrimitiveExpression(operand);
				case Code.Ldtoken:
					if (operand is Cecil.TypeReference) {
						return new Ast.TypeOfExpression { Type = operandAsTypeRef }.Member("TypeHandle");
					} else {
						throw new NotImplementedException();
					}
					case Code.Ldvirtftn: throw new NotImplementedException();
					case Code.Leave: return null;
					case Code.Localloc: throw new NotImplementedException();
					case Code.Mkrefany: throw new NotImplementedException();
				case Code.Newobj:
					Cecil.TypeReference declaringType = ((MethodReference)operand).DeclaringType;
					// TODO: Ensure that the corrent overloaded constructor is called
					if (declaringType is ArrayType) {
						return new Ast.ArrayCreateExpression {
							Type = AstBuilder.ConvertType((ArrayType)declaringType),
							Arguments = args
						};
					}
					return new Ast.ObjectCreateExpression {
						Type = AstBuilder.ConvertType(declaringType),
						Arguments = args
					};
					case Code.No: throw new NotImplementedException();
					case Code.Nop: return null;
					case Code.Or: throw new NotImplementedException();
					case Code.Pop: return arg1;
					case Code.Readonly: throw new NotImplementedException();
					case Code.Refanytype: throw new NotImplementedException();
					case Code.Refanyval: throw new NotImplementedException();
					case Code.Ret: {
						if (methodDef.ReturnType.FullName != Constants.Void) {
							arg1 = Convert(arg1, methodDef.ReturnType);
							return new Ast.ReturnStatement { Expression = arg1 };
						} else {
							return new Ast.ReturnStatement();
						}
					}
					case Code.Rethrow: return new Ast.ThrowStatement();
					case Code.Sizeof: throw new NotImplementedException();
					case Code.Starg: throw new NotImplementedException();
					case Code.Stloc: {
						if (operand is ILVariable) {
							var astLocalVar = new Ast.VariableDeclarationStatement();
							astLocalVar.Type = new Ast.PrimitiveType("var");
							astLocalVar.Variables = new [] {
								new Ast.VariableInitializer(((ILVariable)operand).Name, arg1)
							};
							return astLocalVar;
						}
						VariableDefinition locVar = (VariableDefinition)operand;
						string name = locVar.Name;
						arg1 = Convert(arg1, locVar.VariableType);
						if (localVarDefined.ContainsKey(name)) {
							if (localVarDefined[name]) {
								return new Ast.AssignmentExpression(new Ast.IdentifierExpression(name), arg1);
							} else {
								var astLocalVar = new Ast.VariableDeclarationStatement();
								astLocalVar.Type = AstBuilder.ConvertType(localVarTypes[name]);
								astLocalVar.Variables = new[] { new Ast.VariableInitializer(name, arg1) };
								localVarDefined[name] = true;
								return astLocalVar;
							}
						} else {
							return new Ast.AssignmentExpression(new Ast.IdentifierExpression(name), arg1);
						}
					}
					case Code.Stobj: throw new NotImplementedException();
					case Code.Switch: throw new NotImplementedException();
					case Code.Tail: throw new NotImplementedException();
					case Code.Throw: return new Ast.ThrowStatement { Expression = arg1 };
					case Code.Unaligned: throw new NotImplementedException();
					case Code.Unbox: throw new NotImplementedException();
					case Code.Unbox_Any: throw new NotImplementedException();
					case Code.Volatile: throw new NotImplementedException();
					default: throw new Exception("Unknown OpCode: " + opCode);
			}
		}
		
		static Ast.Expression Convert(Ast.Expression expr, Cecil.TypeReference reqType)
		{
			if (reqType == null) {
				return expr;
			} else {
				return Convert(expr, reqType.FullName);
			}
		}
		
		static Ast.Expression Convert(Ast.Expression expr, string reqType)
		{
//			if (expr.UserData.ContainsKey("Type")) {
//			    Cecil.TypeReference exprType = (Cecil.TypeReference)expr.UserData["Type"];
//				if (exprType == ByteCode.TypeZero &&
//				    reqType == ByteCode.TypeBool.FullName) {
//					return new PrimitiveExpression(false, "false");
//				}
//				if (exprType == ByteCode.TypeOne &&
//				    reqType == ByteCode.TypeBool.FullName) {
//					return new PrimitiveExpression(true, "true");
//				}
//			}
			return expr;
		}
		
		static Ast.Expression ConvertIntToBool(Ast.Expression astInt)
		{
			return astInt;
			// return new Ast.ParenthesizedExpression(new Ast.BinaryOperatorExpression(astInt, BinaryOperatorType.InEquality, new Ast.PrimitiveExpression(0, "0")));
		}
	}
}
