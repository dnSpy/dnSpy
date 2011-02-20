using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using ICSharpCode.NRefactory.Utils;
using Ast = ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp;
using Cecil = Mono.Cecil;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Decompiler.ControlFlow;

namespace Decompiler
{
	public class AstMethodBodyBuilder
	{
		MethodDefinition methodDef;
		TypeSystem typeSystem;
		DecompilerContext context;
		HashSet<ILVariable> localVariablesToDefine = new HashSet<ILVariable>(); // local variables that are missing a definition
		
		public static BlockStatement CreateMethodBody(MethodDefinition methodDef, DecompilerContext context)
		{
			MethodDefinition oldCurrentMethod = context.CurrentMethod;
			Debug.Assert(oldCurrentMethod == null || oldCurrentMethod == methodDef);
			context.CurrentMethod = methodDef;
			try {
				AstMethodBodyBuilder builder = new AstMethodBodyBuilder();
				builder.methodDef = methodDef;
				builder.context = context;
				builder.typeSystem = methodDef.Module.TypeSystem;
				if (Debugger.IsAttached) {
					return builder.CreateMethodBody();
				} else {
					try {
						return builder.CreateMethodBody();
					} catch (OperationCanceledException) {
						throw;
					} catch (Exception ex) {
						throw new ICSharpCode.Decompiler.DecompilerException(methodDef, ex);
					}
				}
			} finally {
				context.CurrentMethod = oldCurrentMethod;
			}
		}
		
		public BlockStatement CreateMethodBody()
		{
			if (methodDef.Body == null) return null;
			
			context.CancellationToken.ThrowIfCancellationRequested();
			ILBlock ilMethod = new ILBlock();
			ILAstBuilder astBuilder = new ILAstBuilder();
			ilMethod.Body = astBuilder.Build(methodDef, true);
			
			context.CancellationToken.ThrowIfCancellationRequested();
			ILAstOptimizer bodyGraph = new ILAstOptimizer();
			bodyGraph.Optimize(context, ilMethod);
			context.CancellationToken.ThrowIfCancellationRequested();
			
			NameVariables.AssignNamesToVariables(methodDef.Parameters.Select(p => p.Name), astBuilder.Variables, ilMethod);
			
			context.CancellationToken.ThrowIfCancellationRequested();
			Ast.BlockStatement astBlock = TransformBlock(ilMethod);
			CommentStatement.ReplaceAll(astBlock); // convert CommentStatements to Comments
			foreach (ILVariable v in localVariablesToDefine) {
				DeclareVariableInSmallestScope.DeclareVariable(astBlock, AstBuilder.ConvertType(v.Type), v.Name);
			}
			
			return astBlock;
		}
		
		Ast.BlockStatement TransformBlock(ILBlock block)
		{
			Ast.BlockStatement astBlock = new BlockStatement();
			if (block != null) {
				if (block.EntryGoto != null)
					astBlock.AddStatement((Statement)TransformExpression(block.EntryGoto));
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
				List<ILRange> ilRanges = ((ILExpression)node).GetILRanges();
				AstNode codeExpr = TransformExpression((ILExpression)node);
				if (codeExpr != null) {
					codeExpr = codeExpr.WithAnnotation(ilRanges);
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
			} else if (node is ILCondition) {
				ILCondition conditionalNode = (ILCondition)node;
				if (conditionalNode.FalseBlock.Body.Any()) {
					// Swap bodies
					yield return new Ast.IfElseStatement {
						Condition = new UnaryOperatorExpression(UnaryOperatorType.Not, MakeBranchCondition(conditionalNode.Condition)),
						TrueStatement = TransformBlock(conditionalNode.FalseBlock),
						FalseStatement = TransformBlock(conditionalNode.TrueBlock)
					};
				} else {
					yield return new Ast.IfElseStatement {
						Condition = MakeBranchCondition(conditionalNode.Condition),
						TrueStatement = TransformBlock(conditionalNode.TrueBlock),
						FalseStatement = TransformBlock(conditionalNode.FalseBlock)
					};
				}
			} else if (node is ILSwitch) {
				ILSwitch ilSwitch = (ILSwitch)node;
				SwitchStatement switchStmt = new SwitchStatement() { Expression = (Expression)TransformExpression(ilSwitch.Condition.Arguments[0]) };
				for (int i = 0; i < ilSwitch.CaseBlocks.Count; i++) {
					SwitchSection section = new SwitchSection();
					section.CaseLabels.Add(new CaseLabel() { Expression = new PrimitiveExpression(i) });
					section.Statements.Add(TransformBlock(ilSwitch.CaseBlocks[i]));
					switchStmt.SwitchSections.Add(section);
				}
				yield return switchStmt;
				if (ilSwitch.DefaultGoto != null)
					yield return (Statement)TransformExpression(ilSwitch.DefaultGoto);
			} else if (node is ILTryCatchBlock) {
				ILTryCatchBlock tryCatchNode = ((ILTryCatchBlock)node);
				var tryCatchStmt = new Ast.TryCatchStatement();
				tryCatchStmt.TryBlock = TransformBlock(tryCatchNode.TryBlock);
				foreach (var catchClause in tryCatchNode.CatchBlocks) {
					tryCatchStmt.CatchClauses.Add(
						new Ast.CatchClause {
							Type = AstBuilder.ConvertType(catchClause.ExceptionType),
							VariableName = catchClause.ExceptionVariable == null ? null : catchClause.ExceptionVariable.Name,
							Body = TransformBlock(catchClause)
						});
				}
				if (tryCatchNode.FinallyBlock != null)
					tryCatchStmt.FinallyBlock = TransformBlock(tryCatchNode.FinallyBlock);
				yield return tryCatchStmt;
			} else if (node is ILBlock) {
				yield return TransformBlock((ILBlock)node);
			} else if (node is ILComment) {
				yield return new CommentStatement(((ILComment)node).Text).WithAnnotation(((ILComment)node).ILRanges);
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
		
		AstNode TransformExpression(ILExpression expr)
		{
			List<Ast.Expression> args = TransformExpressionArguments(expr);
			return TransformByteCode(expr, args);
		}
		
		Ast.Expression MakeBranchCondition(ILExpression expr)
		{
			switch(expr.Code) {
				case ILCode.LogicNot:
					return new Ast.UnaryOperatorExpression(UnaryOperatorType.Not, MakeBranchCondition(expr.Arguments[0]));
				case ILCode.LogicAnd:
					return new Ast.BinaryOperatorExpression(
						MakeBranchCondition(expr.Arguments[0]),
						BinaryOperatorType.ConditionalAnd,
						MakeBranchCondition(expr.Arguments[1])
					);
				case ILCode.LogicOr:
					return new Ast.BinaryOperatorExpression(
						MakeBranchCondition(expr.Arguments[0]),
						BinaryOperatorType.ConditionalOr,
						MakeBranchCondition(expr.Arguments[1])
					);
			}
			
			List<Ast.Expression> args = TransformExpressionArguments(expr);
			Ast.Expression arg1 = args.Count >= 1 ? args[0] : null;
			Ast.Expression arg2 = args.Count >= 2 ? args[1] : null;
			TypeReference arg1Type = args.Count >= 1 ? expr.Arguments[0].InferredType : null;
			switch((Code)expr.Code) {
				case Code.Brfalse:
					if (arg1Type == typeSystem.Boolean)
						return new Ast.UnaryOperatorExpression(UnaryOperatorType.Not, arg1);
					else if (TypeAnalysis.IsIntegerOrEnum(typeSystem, arg1Type))
						return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Equality, new PrimitiveExpression(0));
					else
						return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Equality, new NullReferenceExpression());
				case Code.Brtrue:
					if (arg1Type == typeSystem.Boolean)
						return arg1;
					else if (TypeAnalysis.IsIntegerOrEnum(typeSystem, arg1Type))
						return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.InEquality, new PrimitiveExpression(0));
					else
						return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.InEquality, new NullReferenceExpression());
				case Code.Beq:
					return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Equality, arg2);
				case Code.Bge:
					return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.GreaterThanOrEqual, arg2);
				case Code.Bge_Un:
					return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.GreaterThanOrEqual, arg2);
				case Code.Bgt:
					return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.GreaterThan, arg2);
				case Code.Bgt_Un:
					return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.GreaterThan, arg2);
				case Code.Ble:
					return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.LessThanOrEqual, arg2);
				case Code.Ble_Un:
					return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.LessThanOrEqual, arg2);
				case Code.Blt:
					return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.LessThan, arg2);
				case Code.Blt_Un:
					return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.LessThan, arg2);
				case Code.Bne_Un:
					return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.InEquality, arg2);
				default:
					throw new Exception("Bad opcode");
			}
		}
		
		AstNode TransformByteCode(ILExpression byteCode, List<Ast.Expression> args)
		{
			try {
				AstNode ret = TransformByteCode_Internal(byteCode, args);
				// ret.UserData["Type"] = byteCode.Type;
				return ret;
			} catch (NotImplementedException) {
				// Output the operand of the unknown IL code as well
				if (byteCode.Operand != null) {
					args.Insert(0, new IdentifierExpression(FormatByteCodeOperand(byteCode.Operand)));
				}
				return new IdentifierExpression(byteCode.Code.GetName()).Invoke(args);
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
		
		AstNode TransformByteCode_Internal(ILExpression byteCode, List<Ast.Expression> args)
		{
			ILCode opCode = byteCode.Code;
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
			
			switch((Code)opCode) {
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
					case Code.Or:         return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.BitwiseOr, arg2);
					case Code.Xor:        return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.ExclusiveOr, arg2);
					case Code.Shl:        return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.ShiftLeft, arg2);
					case Code.Shr:        return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.ShiftRight, arg2);
					case Code.Shr_Un:     return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.ShiftRight, arg2);
					
					case Code.Neg:        return new Ast.UnaryOperatorExpression(UnaryOperatorType.Minus, arg1);
					case Code.Not:        return new Ast.UnaryOperatorExpression(UnaryOperatorType.BitNot, arg1);
					#endregion
					#region Arrays
				case Code.Newarr:
					{
						var ace = new Ast.ArrayCreateExpression();
						ace.Type = operandAsTypeRef;
						ace.Arguments.Add(arg1);
						return ace;
					}
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
					return InlineAssembly(byteCode, args);
				case Code.Ldelema:
					return MakeRef(arg1.Indexer(arg2));
					
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
					return InlineAssembly(byteCode, args);
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
				case Code.Castclass:
				case Code.Unbox_Any:
					return arg1.CastTo(operandAsTypeRef);
				case Code.Isinst:
					return arg1.CastAs(operandAsTypeRef);
				case Code.Box:
					return arg1;
				case Code.Unbox:
					return InlineAssembly(byteCode, args);
					#endregion
					#region Indirect
					case Code.Ldind_I: return InlineAssembly(byteCode, args);
					case Code.Ldind_I1: return InlineAssembly(byteCode, args);
					case Code.Ldind_I2: return InlineAssembly(byteCode, args);
					case Code.Ldind_I4: return InlineAssembly(byteCode, args);
					case Code.Ldind_I8: return InlineAssembly(byteCode, args);
					case Code.Ldind_U1: return InlineAssembly(byteCode, args);
					case Code.Ldind_U2: return InlineAssembly(byteCode, args);
					case Code.Ldind_U4: return InlineAssembly(byteCode, args);
					case Code.Ldind_R4: return InlineAssembly(byteCode, args);
					case Code.Ldind_R8: return InlineAssembly(byteCode, args);
					case Code.Ldind_Ref: return InlineAssembly(byteCode, args);
					
					case Code.Stind_I: return InlineAssembly(byteCode, args);
					case Code.Stind_I1: return InlineAssembly(byteCode, args);
					case Code.Stind_I2: return InlineAssembly(byteCode, args);
					case Code.Stind_I4: return InlineAssembly(byteCode, args);
					case Code.Stind_I8: return InlineAssembly(byteCode, args);
					case Code.Stind_R4: return InlineAssembly(byteCode, args);
					case Code.Stind_R8: return InlineAssembly(byteCode, args);
					case Code.Stind_Ref: return InlineAssembly(byteCode, args);
					#endregion
					case Code.Arglist: return InlineAssembly(byteCode, args);
					case Code.Break: return InlineAssembly(byteCode, args);
				case Code.Call:
					return TransformCall(false, operand, methodDef, args);
				case Code.Callvirt:
					return TransformCall(true, operand, methodDef, args);
				case Code.Ldftn:
					{
						Cecil.MethodReference cecilMethod = ((MethodReference)operand);
						var expr = new Ast.IdentifierExpression(cecilMethod.Name);
						expr.TypeArguments.AddRange(ConvertTypeArguments(cecilMethod));
						expr.AddAnnotation(cecilMethod);
						return new IdentifierExpression("ldftn").Invoke(expr)
							.WithAnnotation(new Transforms.DelegateConstruction.Annotation(false));
					}
				case Code.Ldvirtftn:
					{
						Cecil.MethodReference cecilMethod = ((MethodReference)operand);
						var expr = new Ast.IdentifierExpression(cecilMethod.Name);
						expr.TypeArguments.AddRange(ConvertTypeArguments(cecilMethod));
						expr.AddAnnotation(cecilMethod);
						return new IdentifierExpression("ldvirtftn").Invoke(expr)
							.WithAnnotation(new Transforms.DelegateConstruction.Annotation(true));
					}
					
					case Code.Calli: return InlineAssembly(byteCode, args);
					case Code.Ckfinite: return InlineAssembly(byteCode, args);
					case Code.Constrained: return InlineAssembly(byteCode, args);
					case Code.Cpblk: return InlineAssembly(byteCode, args);
					case Code.Cpobj: return InlineAssembly(byteCode, args);
					case Code.Dup: return arg1;
					case Code.Endfilter: return InlineAssembly(byteCode, args);
					case Code.Endfinally: return null;
					case Code.Initblk: return InlineAssembly(byteCode, args);
					case Code.Initobj: return InlineAssembly(byteCode, args);
					case Code.Jmp: return InlineAssembly(byteCode, args);
				case Code.Ldarg:
					if (methodDef.HasThis && ((ParameterDefinition)operand).Index < 0) {
						return new Ast.ThisReferenceExpression();
					} else {
						return new Ast.IdentifierExpression(((ParameterDefinition)operand).Name).WithAnnotation(operand);
					}
				case Code.Ldarga:
					if (methodDef.HasThis && ((ParameterDefinition)operand).Index < 0) {
						return MakeRef(new Ast.ThisReferenceExpression());
					} else {
						return MakeRef(new Ast.IdentifierExpression(((ParameterDefinition)operand).Name).WithAnnotation(operand));
					}
				case Code.Ldc_I4:
					if (byteCode.InferredType == typeSystem.Boolean && (int)operand == 0)
						return new Ast.PrimitiveExpression(false);
					else if (byteCode.InferredType == typeSystem.Boolean && (int)operand == 1)
						return new Ast.PrimitiveExpression(true);
					if (byteCode.InferredType != null) { // cannot rely on IsValueType, it's not set for typerefs (but is set for typespecs)
						TypeDefinition enumDefinition = byteCode.InferredType.Resolve();
						if (enumDefinition != null && enumDefinition.IsEnum) {
							foreach (FieldDefinition field in enumDefinition.Fields) {
								if (field.IsStatic && object.Equals(CSharpPrimitiveCast.Cast(TypeCode.Int32, field.Constant, false), operand))
									return AstBuilder.ConvertType(enumDefinition).Member(field.Name).WithAnnotation(field);
							}
						}
					}
					return new Ast.PrimitiveExpression(operand);
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
					return MakeRef(arg1.Member(((FieldReference) operand).Name).WithAnnotation(operand));
				case Code.Ldsflda:
					return MakeRef(
						AstBuilder.ConvertType(((FieldReference)operand).DeclaringType)
						.Member(((FieldReference)operand).Name).WithAnnotation(operand));
				case Code.Ldloc:
					return new Ast.IdentifierExpression(((ILVariable)operand).Name).WithAnnotation(operand);
				case Code.Ldloca:
					return MakeRef(new Ast.IdentifierExpression(((ILVariable)operand).Name).WithAnnotation(operand));
					case Code.Ldnull: return new Ast.NullReferenceExpression();
					case Code.Ldobj: return InlineAssembly(byteCode, args);
					case Code.Ldstr: return new Ast.PrimitiveExpression(operand);
				case Code.Ldtoken:
					if (operand is Cecil.TypeReference) {
						return new Ast.TypeOfExpression { Type = operandAsTypeRef }.Member("TypeHandle");
					} else {
						return InlineAssembly(byteCode, args);
					}
					case Code.Leave: return null;
					case Code.Localloc: return InlineAssembly(byteCode, args);
					case Code.Mkrefany: return InlineAssembly(byteCode, args);
				case Code.Newobj:
					{
						Cecil.TypeReference declaringType = ((MethodReference)operand).DeclaringType;
						// TODO: Ensure that the corrent overloaded constructor is called
						
						/*if (declaringType is ArrayType) { shouldn't this be newarr?
						return new Ast.ArrayCreateExpression {
							Type = AstBuilder.ConvertType((ArrayType)declaringType),
							Arguments = args
						};
					}*/
						var oce = new Ast.ObjectCreateExpression();
						oce.Type = AstBuilder.ConvertType(declaringType);
						oce.Arguments.AddRange(args);
						return oce.WithAnnotation(operand);
					}
					case Code.No: return InlineAssembly(byteCode, args);
					case Code.Nop: return null;
					case Code.Pop: return arg1;
					case Code.Readonly: return InlineAssembly(byteCode, args);
					case Code.Refanytype: return InlineAssembly(byteCode, args);
					case Code.Refanyval: return InlineAssembly(byteCode, args);
					case Code.Ret: {
						if (methodDef.ReturnType.FullName != "System.Void") {
							arg1 = Convert(arg1, methodDef.ReturnType);
							return new Ast.ReturnStatement { Expression = arg1 };
						} else {
							return new Ast.ReturnStatement();
						}
					}
					case Code.Rethrow: return new Ast.ThrowStatement();
					case Code.Sizeof: return new Ast.SizeOfExpression { Type = AstBuilder.ConvertType(operand as TypeReference) };
				case Code.Starg:
					return new Ast.AssignmentExpression(new Ast.IdentifierExpression(((ParameterDefinition)operand).Name).WithAnnotation(operand), arg1);
					case Code.Stloc: {
						ILVariable locVar = (ILVariable)operand;
						if (!localVariablesToDefine.Contains(locVar)) {
							localVariablesToDefine.Add(locVar);
						}
						return new Ast.AssignmentExpression(new Ast.IdentifierExpression(locVar.Name).WithAnnotation(locVar), arg1);
					}
					case Code.Stobj: return InlineAssembly(byteCode, args);
					case Code.Switch: return InlineAssembly(byteCode, args);
					case Code.Tail: return InlineAssembly(byteCode, args);
					case Code.Throw: return new Ast.ThrowStatement { Expression = arg1 };
					case Code.Unaligned: return InlineAssembly(byteCode, args);
					case Code.Volatile: return InlineAssembly(byteCode, args);
					default: throw new Exception("Unknown OpCode: " + opCode);
			}
		}
		
		static AstNode TransformCall(bool isVirtual, object operand, MethodDefinition methodDef, List<Ast.Expression> args)
		{
			Cecil.MethodReference cecilMethod = ((MethodReference)operand);
			Ast.Expression target;
			List<Ast.Expression> methodArgs = new List<Ast.Expression>(args);
			if (cecilMethod.HasThis) {
				target = methodArgs[0];
				methodArgs.RemoveAt(0);
				
				// Unpack any DirectionExpression that is used as target for the call
				// (calling methods on value types implicitly passes the first argument by reference)
				if (target is DirectionExpression) {
					target = ((DirectionExpression)target).Expression;
					target.Remove(); // detach from DirectionExpression
				}
			} else {
				target = new TypeReferenceExpression { Type = AstBuilder.ConvertType(cecilMethod.DeclaringType) };
			}
			if (target is ThisReferenceExpression && !isVirtual) {
				// a non-virtual call on "this" might be a "base"-call.
				if (cecilMethod.DeclaringType != methodDef.DeclaringType) {
					// If we're not calling a method in the current class; we must be calling one in the base class.
					target = new BaseReferenceExpression();
				}
			}
			
			// Resolve the method to figure out whether it is an accessor:
			Cecil.MethodDefinition cecilMethodDef = cecilMethod.Resolve();
			if (cecilMethodDef != null) {
				if (cecilMethodDef.IsGetter && methodArgs.Count == 0) {
					foreach (var prop in cecilMethodDef.DeclaringType.Properties) {
						if (prop.GetMethod == cecilMethodDef)
							return target.Member(prop.Name).WithAnnotation(prop);
					}
				} else if (cecilMethodDef.IsSetter && methodArgs.Count == 1) {
					foreach (var prop in cecilMethodDef.DeclaringType.Properties) {
						if (prop.SetMethod == cecilMethodDef)
							return new Ast.AssignmentExpression(target.Member(prop.Name).WithAnnotation(prop), methodArgs[0]);
					}
				} else if (cecilMethodDef.IsAddOn && methodArgs.Count == 1) {
					foreach (var ev in cecilMethodDef.DeclaringType.Events) {
						if (ev.AddMethod == cecilMethodDef) {
							return new Ast.AssignmentExpression {
								Left = target.Member(ev.Name).WithAnnotation(ev),
								Operator = AssignmentOperatorType.Add,
								Right = methodArgs[0]
							};
						}
					}
				} else if (cecilMethodDef.IsRemoveOn && methodArgs.Count == 1) {
					foreach (var ev in cecilMethodDef.DeclaringType.Events) {
						if (ev.RemoveMethod == cecilMethodDef) {
							return new Ast.AssignmentExpression {
								Left = target.Member(ev.Name).WithAnnotation(ev),
								Operator = AssignmentOperatorType.Subtract,
								Right = methodArgs[0]
							};
						}
					}
				}
			}
			// Default invocation
			return target.Invoke(cecilMethod.Name, ConvertTypeArguments(cecilMethod), methodArgs).WithAnnotation(cecilMethod);
		}
		
		#if DEBUG
		static readonly ConcurrentDictionary<ILCode, int> unhandledOpcodes = new ConcurrentDictionary<ILCode, int>();
		#endif
		
		[Conditional("DEBUG")]
		public static void ClearUnhandledOpcodes()
		{
			#if DEBUG
			unhandledOpcodes.Clear();
			#endif
		}
		
		[Conditional("DEBUG")]
		public static void PrintNumberOfUnhandledOpcodes()
		{
			#if DEBUG
			foreach (var pair in unhandledOpcodes) {
				Debug.WriteLine("AddMethodBodyBuilder unhandled opcode: {1}x {0}", pair.Key, pair.Value);
			}
			#endif
		}
		
		static Expression InlineAssembly(ILExpression byteCode, List<Ast.Expression> args)
		{
			#if DEBUG
			unhandledOpcodes.AddOrUpdate(byteCode.Code, c => 1, (c, n) => n+1);
			#endif
			// Output the operand of the unknown IL code as well
			if (byteCode.Operand != null) {
				args.Insert(0, new IdentifierExpression(FormatByteCodeOperand(byteCode.Operand)));
			}
			return new IdentifierExpression(byteCode.Code.GetName()).Invoke(args);
		}
		
		static IEnumerable<AstType> ConvertTypeArguments(MethodReference cecilMethod)
		{
			GenericInstanceMethod g = cecilMethod as GenericInstanceMethod;
			if (g == null)
				return null;
			return g.GenericArguments.Select(t => AstBuilder.ConvertType(t));
		}
		
		static Ast.DirectionExpression MakeRef(Ast.Expression expr)
		{
			return new DirectionExpression { Expression = expr, FieldDirection = FieldDirection.Ref };
		}
		
		static Ast.Expression Convert(Ast.Expression expr, Cecil.TypeReference reqType)
		{
			if (reqType == null) {
				return expr;
			} else {
				return expr;
			}
		}
		
		static Ast.Expression ConvertIntToBool(Ast.Expression astInt)
		{
			return astInt;
			// return new Ast.ParenthesizedExpression(new Ast.BinaryOperatorExpression(astInt, BinaryOperatorType.InEquality, new Ast.PrimitiveExpression(0, "0")));
		}
	}
}
