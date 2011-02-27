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
					astBlock.Add((Statement)TransformExpression(block.EntryGoto));
				foreach(ILNode node in block.Body) {
					astBlock.AddRange(TransformNode(node));
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
			} else if (node is ILWhileLoop) {
				ILWhileLoop ilLoop = (ILWhileLoop)node;
				WhileStatement whileStmt = new WhileStatement() {
					Condition = ilLoop.Condition != null ? MakeBranchCondition(ilLoop.Condition) : new PrimitiveExpression(true),
					EmbeddedStatement = TransformBlock(ilLoop.BodyBlock)
				};
				yield return whileStmt;
			} else if (node is ILCondition) {
				ILCondition conditionalNode = (ILCondition)node;
				bool hasFalseBlock = conditionalNode.FalseBlock.EntryGoto != null || conditionalNode.FalseBlock.Body.Count > 0;
				yield return new Ast.IfElseStatement {
					Condition = MakeBranchCondition(conditionalNode.Condition),
					TrueStatement = TransformBlock(conditionalNode.TrueBlock),
					FalseStatement = hasFalseBlock ? TransformBlock(conditionalNode.FalseBlock) : null
				};
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
		
		Ast.Expression MakeBranchCondition(ILExpression expr)
		{
			switch(expr.Code) {
				case ILCode.LogicNot:
					return new Ast.UnaryOperatorExpression(UnaryOperatorType.Not, MakeBranchCondition(expr.Arguments[0]));
				case ILCode.BrLogicAnd:
					return new Ast.BinaryOperatorExpression(
						MakeBranchCondition(expr.Arguments[0]),
						BinaryOperatorType.ConditionalAnd,
						MakeBranchCondition(expr.Arguments[1])
					);
				case ILCode.BrLogicOr:
					return new Ast.BinaryOperatorExpression(
						MakeBranchCondition(expr.Arguments[0]),
						BinaryOperatorType.ConditionalOr,
						MakeBranchCondition(expr.Arguments[1])
					);
			}
			
			List<Ast.Expression> args = TransformExpressionArguments(expr);
			Ast.Expression arg1 = args.Count >= 1 ? args[0] : null;
			Ast.Expression arg2 = args.Count >= 2 ? args[1] : null;
			switch((Code)expr.Code) {
				case Code.Brfalse:
					return new Ast.UnaryOperatorExpression(UnaryOperatorType.Not, arg1);
				case Code.Brtrue:
					return arg1;
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
		
		AstNode TransformExpression(ILExpression expr)
		{
			AstNode node = TransformByteCode(expr);
			Expression astExpr = node as Expression;
			if (astExpr != null)
				return Convert(astExpr, expr.InferredType, expr.ExpectedType);
			else
				return node;
		}
		
		AstNode TransformByteCode(ILExpression byteCode)
		{
			ILCode opCode = byteCode.Code;
			object operand = byteCode.Operand;
			AstType operandAsTypeRef = AstBuilder.ConvertType(operand as Cecil.TypeReference);
			ILExpression operandAsByteCode = operand as ILExpression;

			// Do branches first because TransformExpressionArguments does not work on arguments that are branches themselfs
			// TODO:  We should probably have virtual instructions for these and not abuse branch codes as expressions
			switch(opCode) {
					case ILCode.Br: return new Ast.GotoStatement(((ILLabel)byteCode.Operand).Name);
				case ILCode.Brfalse:
				case ILCode.Brtrue:
				case ILCode.Beq:
				case ILCode.Bge:
				case ILCode.Bge_Un:
				case ILCode.Bgt:
				case ILCode.Bgt_Un:
				case ILCode.Ble:
				case ILCode.Ble_Un:
				case ILCode.Blt:
				case ILCode.Blt_Un:
				case ILCode.Bne_Un:
				case ILCode.BrLogicAnd:
				case ILCode.BrLogicOr:
					return new Ast.IfElseStatement() {
						Condition = MakeBranchCondition(byteCode),
						TrueStatement = new BlockStatement() {
							new Ast.GotoStatement(((ILLabel)byteCode.Operand).Name)
						}
					};
				case ILCode.TernaryOp:
					return new Ast.ConditionalExpression() {
						Condition = MakeBranchCondition(byteCode.Arguments[0]),
						TrueExpression = (Expression)TransformExpression(byteCode.Arguments[1]),
						FalseExpression = (Expression)TransformExpression(byteCode.Arguments[2]),
					};
				case ILCode.LoopBreak:
					return new Ast.BreakStatement();
				case ILCode.LoopContinue:
					return new Ast.ContinueStatement();
			}
			
			List<Ast.Expression> args = TransformExpressionArguments(byteCode);
			Ast.Expression arg1 = args.Count >= 1 ? args[0] : null;
			Ast.Expression arg2 = args.Count >= 2 ? args[1] : null;
			Ast.Expression arg3 = args.Count >= 3 ? args[2] : null;
			
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
				case (Code)ILCode.InitArray:
					{
						var ace = new Ast.ArrayCreateExpression();
						ace.Type = operandAsTypeRef;
						ComposedType ct = operandAsTypeRef as ComposedType;
						if (ct != null) {
							// change "new (int[,])[10] to new int[10][,]"
							ct.ArraySpecifiers.MoveTo(ace.AdditionalArraySpecifiers);
						}
						if (opCode == ILCode.InitArray) {
							ace.Initializer = new ArrayInitializerExpression();
							ace.Initializer.Elements.AddRange(args);
						} else {
							ace.Arguments.Add(arg1);
						}
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
				case Code.Ldelem_Any:
					return arg1.Indexer(arg2);
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
				case Code.Stelem_Any:
					return new Ast.AssignmentExpression(arg1.Indexer(arg2), arg3);
					#endregion
					#region Comparison
					case Code.Ceq:    return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Equality, arg2);
					case Code.Cgt:    return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.GreaterThan, arg2);
				case Code.Cgt_Un:
					// can also mean Inequality, when used with object references
					{
						TypeReference arg1Type = byteCode.Arguments[0].InferredType;
						if (arg1Type != null && !arg1Type.IsValueType)
							return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.InEquality, arg2);
						else
							return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.GreaterThan, arg2);
					}
					case Code.Clt:    return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.LessThan, arg2);
					case Code.Clt_Un: return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.LessThan, arg2);
					#endregion
					#region Conversions
				case Code.Conv_I1:
				case Code.Conv_I2:
				case Code.Conv_I4:
				case Code.Conv_I8:
				case Code.Conv_U1:
				case Code.Conv_U2:
				case Code.Conv_U4:
				case Code.Conv_U8:
					return arg1; // conversion is handled by Convert() function using the info from type analysis
					case Code.Conv_I:    return arg1.CastTo(typeof(IntPtr)); // TODO
					case Code.Conv_U:    return arg1.CastTo(typeof(UIntPtr)); // TODO
					case Code.Conv_R4:   return arg1.CastTo(typeof(float));
					case Code.Conv_R8:   return arg1.CastTo(typeof(double));
					case Code.Conv_R_Un: return arg1.CastTo(typeof(double)); // TODO
					
				case Code.Conv_Ovf_I1:
				case Code.Conv_Ovf_I2:
				case Code.Conv_Ovf_I4:
				case Code.Conv_Ovf_I8:
				case Code.Conv_Ovf_U1:
				case Code.Conv_Ovf_U2:
				case Code.Conv_Ovf_U4:
				case Code.Conv_Ovf_U8:
				case Code.Conv_Ovf_I1_Un:
				case Code.Conv_Ovf_I2_Un:
				case Code.Conv_Ovf_I4_Un:
				case Code.Conv_Ovf_I8_Un:
				case Code.Conv_Ovf_U1_Un:
				case Code.Conv_Ovf_U2_Un:
				case Code.Conv_Ovf_U4_Un:
				case Code.Conv_Ovf_U8_Un:
					return arg1; // conversion was handled by Convert() function using the info from type analysis
					case Code.Conv_Ovf_I:  return arg1.CastTo(typeof(IntPtr)); // TODO
					case Code.Conv_Ovf_U:  return arg1.CastTo(typeof(UIntPtr));
					case Code.Conv_Ovf_I_Un:  return arg1.CastTo(typeof(IntPtr));
					case Code.Conv_Ovf_U_Un:  return arg1.CastTo(typeof(UIntPtr));
					
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
				case Code.Ldind_I:
				case Code.Ldind_I1:
				case Code.Ldind_I2:
				case Code.Ldind_I4:
				case Code.Ldind_I8:
				case Code.Ldind_U1:
				case Code.Ldind_U2:
				case Code.Ldind_U4:
				case Code.Ldind_R4:
				case Code.Ldind_R8:
				case Code.Ldind_Ref:
				case Code.Ldobj:
					if (args[0] is DirectionExpression)
						return ((DirectionExpression)args[0]).Expression.Detach();
					else
						return InlineAssembly(byteCode, args);
					
				case Code.Stind_I:
				case Code.Stind_I1:
				case Code.Stind_I2:
				case Code.Stind_I4:
				case Code.Stind_I8:
				case Code.Stind_R4:
				case Code.Stind_R8:
				case Code.Stind_Ref:
				case Code.Stobj:
					if (args[0] is DirectionExpression)
						return new AssignmentExpression(((DirectionExpression)args[0]).Expression.Detach(), args[1]);
					else
						return InlineAssembly(byteCode, args);
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
				case Code.Initobj:
					if (args[0] is DirectionExpression)
						return new AssignmentExpression(((DirectionExpression)args[0]).Expression.Detach(), new DefaultValueExpression { Type = operandAsTypeRef });
					else
						return InlineAssembly(byteCode, args);
					case Code.Jmp: return InlineAssembly(byteCode, args);
				case Code.Ldarg:
					if (methodDef.HasThis && ((ParameterDefinition)operand).Index < 0) {
						if (context.CurrentMethod.DeclaringType.IsValueType)
							return MakeRef(new Ast.ThisReferenceExpression());
						else
							return new Ast.ThisReferenceExpression();
					} else {
						var expr = new Ast.IdentifierExpression(((ParameterDefinition)operand).Name).WithAnnotation(operand);
						if (((ParameterDefinition)operand).ParameterType is ByReferenceType)
							return MakeRef(expr);
						else
							return expr;
					}
				case Code.Ldarga:
					if (methodDef.HasThis && ((ParameterDefinition)operand).Index < 0) {
						return MakeRef(new Ast.ThisReferenceExpression());
					} else {
						return MakeRef(new Ast.IdentifierExpression(((ParameterDefinition)operand).Name).WithAnnotation(operand));
					}
				case Code.Ldc_I4:
					return MakePrimitive((int)operand, byteCode.InferredType);
				case Code.Ldc_I8:
				case Code.Ldc_R4:
				case Code.Ldc_R8:
					return new Ast.PrimitiveExpression(operand);
				case Code.Ldfld:
					if (arg1 is DirectionExpression)
						arg1 = ((DirectionExpression)arg1).Expression.Detach();
					return arg1.Member(((FieldReference) operand).Name).WithAnnotation(operand);
				case Code.Ldsfld:
					return AstBuilder.ConvertType(((FieldReference)operand).DeclaringType)
						.Member(((FieldReference)operand).Name).WithAnnotation(operand);
				case Code.Stfld:
					if (arg1 is DirectionExpression)
						arg1 = ((DirectionExpression)arg1).Expression.Detach();
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
					localVariablesToDefine.Add((ILVariable)operand);
					return new Ast.IdentifierExpression(((ILVariable)operand).Name).WithAnnotation(operand);
				case Code.Ldloca:
					localVariablesToDefine.Add((ILVariable)operand);
					return MakeRef(new Ast.IdentifierExpression(((ILVariable)operand).Name).WithAnnotation(operand));
				case Code.Ldnull:
					return new Ast.NullReferenceExpression();
					case Code.Ldstr: return new Ast.PrimitiveExpression(operand);
				case Code.Ldtoken:
					if (operand is Cecil.TypeReference) {
						return new Ast.TypeOfExpression { Type = operandAsTypeRef }.Member("TypeHandle");
					} else {
						return InlineAssembly(byteCode, args);
					}
					case Code.Leave: return new GotoStatement() { Label = ((ILLabel)operand).Name };
					case Code.Localloc: return InlineAssembly(byteCode, args);
					case Code.Mkrefany: return InlineAssembly(byteCode, args);
				case Code.Newobj:
					{
						Cecil.TypeReference declaringType = ((MethodReference)operand).DeclaringType;
						
						if (declaringType is ArrayType) {
							ComposedType ct = AstBuilder.ConvertType((ArrayType)declaringType) as ComposedType;
							if (ct != null && ct.ArraySpecifiers.Count >= 1) {
								var ace = new Ast.ArrayCreateExpression();
								ct.ArraySpecifiers.First().Remove();
								ct.ArraySpecifiers.MoveTo(ace.AdditionalArraySpecifiers);
								ace.Type = ct;
								ace.Arguments.AddRange(args);
								return ace;
							}
						}
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
							return new Ast.ReturnStatement { Expression = arg1 };
						} else {
							return new Ast.ReturnStatement();
						}
					}
					case Code.Rethrow: return new Ast.ThrowStatement();
				case Code.Sizeof:
					return new Ast.SizeOfExpression { Type = operandAsTypeRef };
				case Code.Starg:
					return new Ast.AssignmentExpression(new Ast.IdentifierExpression(((ParameterDefinition)operand).Name).WithAnnotation(operand), arg1);
					case Code.Stloc: {
						ILVariable locVar = (ILVariable)operand;
						localVariablesToDefine.Add(locVar);
						return new Ast.AssignmentExpression(new Ast.IdentifierExpression(locVar.Name).WithAnnotation(locVar), arg1);
					}
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
				if ((cecilMethod.DeclaringType.IsGenericInstance ? cecilMethod.DeclaringType.GetElementType() : cecilMethod.DeclaringType) != methodDef.DeclaringType) {
					// If we're not calling a method in the current class; we must be calling one in the base class.
					target = new BaseReferenceExpression();
				}
			}
			
			if (cecilMethod.Name == "Get" && cecilMethod.DeclaringType is ArrayType && methodArgs.Count > 1) {
				return target.Indexer(methodArgs);
			} else if (cecilMethod.Name == "Set" && cecilMethod.DeclaringType is ArrayType && methodArgs.Count > 2) {
				return new AssignmentExpression(target.Indexer(methodArgs.GetRange(0, methodArgs.Count - 1)), methodArgs.Last());
			}
			
			// Resolve the method to figure out whether it is an accessor:
			Cecil.MethodDefinition cecilMethodDef = cecilMethod.Resolve();
			if (cecilMethodDef != null) {
				if (cecilMethodDef.IsGetter && methodArgs.Count == 0) {
					foreach (var prop in cecilMethodDef.DeclaringType.Properties) {
						if (prop.GetMethod == cecilMethodDef)
							return target.Member(prop.Name).WithAnnotation(prop);
					}
				} else if (cecilMethodDef.IsGetter) { // with parameters
					PropertyDefinition indexer = GetIndexer(cecilMethodDef);
					if (indexer != null)
						return target.Indexer(methodArgs).WithAnnotation(indexer);
				} else if (cecilMethodDef.IsSetter && methodArgs.Count == 1) {
					foreach (var prop in cecilMethodDef.DeclaringType.Properties) {
						if (prop.SetMethod == cecilMethodDef)
							return new Ast.AssignmentExpression(target.Member(prop.Name).WithAnnotation(prop), methodArgs[0]);
					}
				} else if (cecilMethodDef.IsSetter && methodArgs.Count > 1) {
					PropertyDefinition indexer = GetIndexer(cecilMethodDef);
					if (indexer != null)
						return new AssignmentExpression(
							target.Indexer(methodArgs.GetRange(0, methodArgs.Count - 1)).WithAnnotation(indexer),
							methodArgs[methodArgs.Count - 1]
						);
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
		
		static PropertyDefinition GetIndexer(MethodDefinition cecilMethodDef)
		{
			TypeDefinition typeDef = cecilMethodDef.DeclaringType;
			string indexerName = null;
			foreach (CustomAttribute ca in typeDef.CustomAttributes) {
				if (ca.Constructor.FullName == "System.Void System.Reflection.DefaultMemberAttribute::.ctor(System.String)") {
					indexerName = ca.ConstructorArguments.Single().Value as string;
					break;
				}
			}
			if (indexerName == null)
				return null;
			foreach (PropertyDefinition prop in typeDef.Properties) {
				if (prop.Name == indexerName) {
					if (prop.GetMethod == cecilMethodDef || prop.SetMethod == cecilMethodDef)
						return prop;
				}
			}
			return null;
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
		
		Ast.Expression Convert(Ast.Expression expr, Cecil.TypeReference actualType, Cecil.TypeReference reqType)
		{
			if (reqType == null || actualType == reqType) {
				return expr;
			} else {
				bool actualIsIntegerOrEnum = TypeAnalysis.IsIntegerOrEnum(actualType);
				bool requiredIsIntegerOrEnum = TypeAnalysis.IsIntegerOrEnum(reqType);
				
				if (TypeAnalysis.IsBoolean(reqType)) {
					if (TypeAnalysis.IsBoolean(actualType))
						return expr;
					if (actualIsIntegerOrEnum) {
						return new BinaryOperatorExpression(expr, BinaryOperatorType.InEquality, MakePrimitive(0, actualType));
					} else {
						return new BinaryOperatorExpression(expr, BinaryOperatorType.InEquality, new NullReferenceExpression());
					}
				}
				if (TypeAnalysis.IsBoolean(actualType) && requiredIsIntegerOrEnum) {
					return new ConditionalExpression {
						Condition = expr,
						TrueExpression = MakePrimitive(1, reqType),
						FalseExpression = MakePrimitive(0, reqType)
					};
				}
				if (actualIsIntegerOrEnum && requiredIsIntegerOrEnum) {
					return expr.CastTo(AstBuilder.ConvertType(reqType));
				}
				return expr;
			}
		}
		
		Expression MakePrimitive(long val, TypeReference type)
		{
			if (TypeAnalysis.IsBoolean(type) && val == 0)
				return new Ast.PrimitiveExpression(false);
			else if (TypeAnalysis.IsBoolean(type) && val == 1)
				return new Ast.PrimitiveExpression(true);
			if (type != null) { // cannot rely on type.IsValueType, it's not set for typerefs (but is set for typespecs)
				TypeDefinition enumDefinition = type.Resolve();
				if (enumDefinition != null && enumDefinition.IsEnum) {
					foreach (FieldDefinition field in enumDefinition.Fields) {
						if (field.IsStatic && object.Equals(CSharpPrimitiveCast.Cast(TypeCode.Int64, field.Constant, false), val))
							return AstBuilder.ConvertType(enumDefinition).Member(field.Name).WithAnnotation(field);
						else if (!field.IsStatic && field.IsRuntimeSpecialName)
							type = field.FieldType; // use primitive type of the enum
					}
				}
			}
			TypeCode code = TypeAnalysis.GetTypeCode(type);
			if (code == TypeCode.Object)
				return new Ast.PrimitiveExpression((int)val);
			else
				return new Ast.PrimitiveExpression(CSharpPrimitiveCast.Cast(code, val, false));
		}
	}
}
