using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using ICSharpCode.Decompiler.ILAst;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ICSharpCode.Decompiler.Ast
{
	using Ast = ICSharpCode.NRefactory.CSharp;
	using Cecil = Mono.Cecil;
	
	public class AstMethodBodyBuilder
	{
		MethodDefinition methodDef;
		TypeSystem typeSystem;
		DecompilerContext context;
		HashSet<ILVariable> localVariablesToDefine = new HashSet<ILVariable>(); // local variables that are missing a definition
		HashSet<ILVariable> implicitlyDefinedVariables = new HashSet<ILVariable>(); // local variables that are implicitly defined (e.g. catch handler)
		
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
			
			var allVariables = ilMethod.GetSelfAndChildrenRecursive<ILExpression>().Select(e => e.Operand as ILVariable).Where(v => v != null && !v.IsGenerated).Distinct();
			NameVariables.AssignNamesToVariables(methodDef.Parameters.Select(p => p.Name), allVariables, ilMethod);
			
			context.CancellationToken.ThrowIfCancellationRequested();
			Ast.BlockStatement astBlock = TransformBlock(ilMethod);
			CommentStatement.ReplaceAll(astBlock); // convert CommentStatements to Comments
			foreach (ILVariable v in localVariablesToDefine.Except(implicitlyDefinedVariables)) {
				DeclareVariableInSmallestScope.DeclareVariable(astBlock, AstBuilder.ConvertType(v.Type), v.Name);
			}
			
			// store the variables - used for debugger
			int token = methodDef.MetadataToken.ToInt32();
			ILAstBuilder.MemberLocalVariables.AddOrUpdate(
								token, allVariables, (key, oldValue) => allVariables);
			
			return astBlock;
		}
		
		Ast.BlockStatement TransformBlock(ILBlock block)
		{
			Ast.BlockStatement astBlock = new BlockStatement();
			if (block != null) {
				foreach(ILNode node in block.GetChildren()) {
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
					Condition = ilLoop.Condition != null ? (Expression)TransformExpression(ilLoop.Condition) : new PrimitiveExpression(true),
					EmbeddedStatement = TransformBlock(ilLoop.BodyBlock)
				};
				yield return whileStmt;
			} else if (node is ILCondition) {
				ILCondition conditionalNode = (ILCondition)node;
				bool hasFalseBlock = conditionalNode.FalseBlock.EntryGoto != null || conditionalNode.FalseBlock.Body.Count > 0;
				yield return new Ast.IfElseStatement {
					Condition = (Expression)TransformExpression(conditionalNode.Condition),
					TrueStatement = TransformBlock(conditionalNode.TrueBlock),
					FalseStatement = hasFalseBlock ? TransformBlock(conditionalNode.FalseBlock) : null
				};
			} else if (node is ILSwitch) {
				ILSwitch ilSwitch = (ILSwitch)node;
				SwitchStatement switchStmt = new SwitchStatement() { Expression = (Expression)TransformExpression(ilSwitch.Condition) };
				foreach (var caseBlock in ilSwitch.CaseBlocks) {
					SwitchSection section = new SwitchSection();
					if (caseBlock.Values != null) {
						section.CaseLabels.AddRange(caseBlock.Values.Select(i => new CaseLabel() { Expression = AstBuilder.MakePrimitive(i, ilSwitch.Condition.InferredType) }));
					} else {
						section.CaseLabels.Add(new CaseLabel());
					}
					section.Statements.Add(TransformBlock(caseBlock));
					switchStmt.SwitchSections.Add(section);
				}
				yield return switchStmt;
			} else if (node is ILTryCatchBlock) {
				ILTryCatchBlock tryCatchNode = ((ILTryCatchBlock)node);
				var tryCatchStmt = new Ast.TryCatchStatement();
				tryCatchStmt.TryBlock = TransformBlock(tryCatchNode.TryBlock);
				foreach (var catchClause in tryCatchNode.CatchBlocks) {
					if (catchClause.ExceptionVariable != null)
						implicitlyDefinedVariables.Add(catchClause.ExceptionVariable);
					tryCatchStmt.CatchClauses.Add(
						new Ast.CatchClause {
							Type = AstBuilder.ConvertType(catchClause.ExceptionType),
							VariableName = catchClause.ExceptionVariable == null ? null : catchClause.ExceptionVariable.Name,
							Body = TransformBlock(catchClause)
						});
				}
				if (tryCatchNode.FinallyBlock != null)
					tryCatchStmt.FinallyBlock = TransformBlock(tryCatchNode.FinallyBlock);
				if (tryCatchNode.FaultBlock != null) {
					CatchClause cc = new CatchClause();
					cc.Body = TransformBlock(tryCatchNode.FaultBlock);
					cc.Body.Add(new ThrowStatement()); // rethrow
					tryCatchStmt.CatchClauses.Add(cc);
				}
				yield return tryCatchStmt;
			} else if (node is ILBlock) {
				yield return TransformBlock((ILBlock)node);
			} else if (node is ILComment) {
				yield return new CommentStatement(((ILComment)node).Text).WithAnnotation(((ILComment)node).ILRanges);
			} else {
				throw new Exception("Unknown node type");
			}
		}
		
		AstNode TransformExpression(ILExpression expr)
		{
			AstNode node = TransformByteCode(expr);
			Expression astExpr = node as Expression;
			
			// get IL ranges - used in debugger
			List<ILRange> ilRanges = expr.GetILRanges();
			AstNode result;
			
			if (astExpr != null)
				result = Convert(astExpr, expr.InferredType, expr.ExpectedType);
			else
				result = node;
			
			if (result != null)
				return result.WithAnnotation(ilRanges);
			
			return result;
		}
		
		AstNode TransformByteCode(ILExpression byteCode)
		{
			object operand = byteCode.Operand;
			AstType operandAsTypeRef = AstBuilder.ConvertType(operand as Cecil.TypeReference);

			List<Ast.Expression> args = new List<Expression>();
			foreach(ILExpression arg in byteCode.Arguments) {
				args.Add((Ast.Expression)TransformExpression(arg));
			}
			Ast.Expression arg1 = args.Count >= 1 ? args[0] : null;
			Ast.Expression arg2 = args.Count >= 2 ? args[1] : null;
			Ast.Expression arg3 = args.Count >= 3 ? args[2] : null;
			
			switch(byteCode.Code) {
				#region Arithmetic
				case ILCode.Add:        return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Add, arg2);
				case ILCode.Add_Ovf:    return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Add, arg2);
				case ILCode.Add_Ovf_Un: return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Add, arg2);
				case ILCode.Div:        return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Divide, arg2);
				case ILCode.Div_Un:     return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Divide, arg2);
				case ILCode.Mul:        return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Multiply, arg2);
				case ILCode.Mul_Ovf:    return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Multiply, arg2);
				case ILCode.Mul_Ovf_Un: return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Multiply, arg2);
				case ILCode.Rem:        return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Modulus, arg2);
				case ILCode.Rem_Un:     return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Modulus, arg2);
				case ILCode.Sub:        return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Subtract, arg2);
				case ILCode.Sub_Ovf:    return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Subtract, arg2);
				case ILCode.Sub_Ovf_Un: return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Subtract, arg2);
				case ILCode.And:        return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.BitwiseAnd, arg2);
				case ILCode.Or:         return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.BitwiseOr, arg2);
				case ILCode.Xor:        return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.ExclusiveOr, arg2);
				case ILCode.Shl:        return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.ShiftLeft, arg2);
				case ILCode.Shr:        return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.ShiftRight, arg2);
				case ILCode.Shr_Un:     return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.ShiftRight, arg2);
				case ILCode.Neg:        return new Ast.UnaryOperatorExpression(UnaryOperatorType.Minus, arg1);
				case ILCode.Not:        return new Ast.UnaryOperatorExpression(UnaryOperatorType.BitNot, arg1);
				#endregion
				#region Arrays
				case ILCode.Newarr:
				case ILCode.InitArray: {
					var ace = new Ast.ArrayCreateExpression();
					ace.Type = operandAsTypeRef;
					ComposedType ct = operandAsTypeRef as ComposedType;
					if (ct != null) {
						// change "new (int[,])[10] to new int[10][,]"
						ct.ArraySpecifiers.MoveTo(ace.AdditionalArraySpecifiers);
					}
					if (byteCode.Code == ILCode.InitArray) {
						ace.Initializer = new ArrayInitializerExpression();
						ace.Initializer.Elements.AddRange(args);
					} else {
						ace.Arguments.Add(arg1);
					}
					return ace;
				}
				case ILCode.Ldlen: return arg1.Member("Length");
				case ILCode.Ldelem_I:
				case ILCode.Ldelem_I1:
				case ILCode.Ldelem_I2:
				case ILCode.Ldelem_I4:
				case ILCode.Ldelem_I8:
				case ILCode.Ldelem_U1:
				case ILCode.Ldelem_U2:
				case ILCode.Ldelem_U4:
				case ILCode.Ldelem_R4:
				case ILCode.Ldelem_R8:
				case ILCode.Ldelem_Ref:
				case ILCode.Ldelem_Any:
					return arg1.Indexer(arg2);
				case ILCode.Ldelema: return MakeRef(arg1.Indexer(arg2));
				case ILCode.Stelem_I:
				case ILCode.Stelem_I1:
				case ILCode.Stelem_I2:
				case ILCode.Stelem_I4:
				case ILCode.Stelem_I8:
				case ILCode.Stelem_R4:
				case ILCode.Stelem_R8:
				case ILCode.Stelem_Ref:
				case ILCode.Stelem_Any:
					return new Ast.AssignmentExpression(arg1.Indexer(arg2), arg3);
				#endregion
				#region Comparison
				case ILCode.Ceq: return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Equality, arg2);
				case ILCode.Cgt: return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.GreaterThan, arg2);
				case ILCode.Cgt_Un: {
					// can also mean Inequality, when used with object references
					TypeReference arg1Type = byteCode.Arguments[0].InferredType;
					if (arg1Type != null && !arg1Type.IsValueType)
						return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.InEquality, arg2);
					else
						return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.GreaterThan, arg2);
				}
				case ILCode.Clt:    return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.LessThan, arg2);
				case ILCode.Clt_Un: return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.LessThan, arg2);
				#endregion
				#region Logical
				case ILCode.LogicNot:   return new Ast.UnaryOperatorExpression(UnaryOperatorType.Not, arg1);
				case ILCode.LogicAnd:   return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.ConditionalAnd, arg2);
				case ILCode.LogicOr:    return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.ConditionalOr, arg2);
				case ILCode.TernaryOp:  return new Ast.ConditionalExpression() { Condition = arg1, TrueExpression = arg2, FalseExpression = arg3 };
				#endregion
				#region Branch
				case ILCode.Br:         return new Ast.GotoStatement(((ILLabel)byteCode.Operand).Name);
				case ILCode.Brtrue:
					return new Ast.IfElseStatement() {
						Condition = arg1,
						TrueStatement = new BlockStatement() {
							new Ast.GotoStatement(((ILLabel)byteCode.Operand).Name)
						}
					};
				case ILCode.LoopOrSwitchBreak: return new Ast.BreakStatement();
				case ILCode.LoopContinue:      return new Ast.ContinueStatement();
				#endregion
				#region Conversions
				case ILCode.Conv_I1:
				case ILCode.Conv_I2:
				case ILCode.Conv_I4:
				case ILCode.Conv_I8:
				case ILCode.Conv_U1:
				case ILCode.Conv_U2:
				case ILCode.Conv_U4:
				case ILCode.Conv_U8:
					return arg1; // conversion is handled by Convert() function using the info from type analysis
				case ILCode.Conv_I:    return arg1.CastTo(typeof(IntPtr)); // TODO
				case ILCode.Conv_U:    return arg1.CastTo(typeof(UIntPtr)); // TODO
				case ILCode.Conv_R4:   return arg1.CastTo(typeof(float));
				case ILCode.Conv_R8:   return arg1.CastTo(typeof(double));
				case ILCode.Conv_R_Un: return arg1.CastTo(typeof(double)); // TODO
				case ILCode.Conv_Ovf_I1:
				case ILCode.Conv_Ovf_I2:
				case ILCode.Conv_Ovf_I4:
				case ILCode.Conv_Ovf_I8:
				case ILCode.Conv_Ovf_U1:
				case ILCode.Conv_Ovf_U2:
				case ILCode.Conv_Ovf_U4:
				case ILCode.Conv_Ovf_U8:
				case ILCode.Conv_Ovf_I1_Un:
				case ILCode.Conv_Ovf_I2_Un:
				case ILCode.Conv_Ovf_I4_Un:
				case ILCode.Conv_Ovf_I8_Un:
				case ILCode.Conv_Ovf_U1_Un:
				case ILCode.Conv_Ovf_U2_Un:
				case ILCode.Conv_Ovf_U4_Un:
				case ILCode.Conv_Ovf_U8_Un:
					return arg1; // conversion was handled by Convert() function using the info from type analysis
				case ILCode.Conv_Ovf_I:     return arg1.CastTo(typeof(IntPtr)); // TODO
				case ILCode.Conv_Ovf_U:     return arg1.CastTo(typeof(UIntPtr));
				case ILCode.Conv_Ovf_I_Un:  return arg1.CastTo(typeof(IntPtr));
				case ILCode.Conv_Ovf_U_Un:  return arg1.CastTo(typeof(UIntPtr));
				case ILCode.Castclass:      return arg1.CastTo(operandAsTypeRef);
				case ILCode.Unbox_Any:      return arg1.CastTo(operandAsTypeRef);
				case ILCode.Isinst:         return arg1.CastAs(operandAsTypeRef);
				case ILCode.Box:            return arg1;
				case ILCode.Unbox:          return InlineAssembly(byteCode, args);
				#endregion
				#region Indirect
				case ILCode.Ldind_I:
				case ILCode.Ldind_I1:
				case ILCode.Ldind_I2:
				case ILCode.Ldind_I4:
				case ILCode.Ldind_I8:
				case ILCode.Ldind_U1:
				case ILCode.Ldind_U2:
				case ILCode.Ldind_U4:
				case ILCode.Ldind_R4:
				case ILCode.Ldind_R8:
				case ILCode.Ldind_Ref:
				case ILCode.Ldobj:
					if (args[0] is DirectionExpression)
						return ((DirectionExpression)args[0]).Expression.Detach();
					else
						return InlineAssembly(byteCode, args);
				case ILCode.Stind_I:
				case ILCode.Stind_I1:
				case ILCode.Stind_I2:
				case ILCode.Stind_I4:
				case ILCode.Stind_I8:
				case ILCode.Stind_R4:
				case ILCode.Stind_R8:
				case ILCode.Stind_Ref:
				case ILCode.Stobj:
					if (args[0] is DirectionExpression)
						return new AssignmentExpression(((DirectionExpression)args[0]).Expression.Detach(), args[1]);
					else
						return InlineAssembly(byteCode, args);
				#endregion
				case ILCode.Arglist:  return InlineAssembly(byteCode, args);
				case ILCode.Break:    return InlineAssembly(byteCode, args);
				case ILCode.Call:     return TransformCall(false, operand, methodDef, args);
				case ILCode.Callvirt: return TransformCall(true, operand, methodDef, args);
				case ILCode.Ldftn: {
					Cecil.MethodReference cecilMethod = ((MethodReference)operand);
					var expr = new Ast.IdentifierExpression(cecilMethod.Name);
					expr.TypeArguments.AddRange(ConvertTypeArguments(cecilMethod));
					expr.AddAnnotation(cecilMethod);
					return new IdentifierExpression("ldftn").Invoke(expr)
						.WithAnnotation(new Transforms.DelegateConstruction.Annotation(false));
				}
				case ILCode.Ldvirtftn: {
					Cecil.MethodReference cecilMethod = ((MethodReference)operand);
					var expr = new Ast.IdentifierExpression(cecilMethod.Name);
					expr.TypeArguments.AddRange(ConvertTypeArguments(cecilMethod));
					expr.AddAnnotation(cecilMethod);
					return new IdentifierExpression("ldvirtftn").Invoke(expr)
						.WithAnnotation(new Transforms.DelegateConstruction.Annotation(true));
				}
				case ILCode.Calli:       return InlineAssembly(byteCode, args);
				case ILCode.Ckfinite:    return InlineAssembly(byteCode, args);
				case ILCode.Constrained: return InlineAssembly(byteCode, args);
				case ILCode.Cpblk:       return InlineAssembly(byteCode, args);
				case ILCode.Cpobj:       return InlineAssembly(byteCode, args);
				case ILCode.Dup:         return arg1;
				case ILCode.Endfilter:   return InlineAssembly(byteCode, args);
				case ILCode.Endfinally:  return null;
				case ILCode.Initblk:     return InlineAssembly(byteCode, args);
				case ILCode.Initobj:
					if (args[0] is DirectionExpression)
						return new AssignmentExpression(((DirectionExpression)args[0]).Expression.Detach(), MakeDefaultValue((TypeReference)operand));
					else
						return InlineAssembly(byteCode, args);
				case ILCode.DefaultValue:
					return MakeDefaultValue((TypeReference)operand);
				case ILCode.Jmp: return InlineAssembly(byteCode, args);
				case ILCode.Ldarg: {
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
				}
				case ILCode.Ldarga:
					if (methodDef.HasThis && ((ParameterDefinition)operand).Index < 0) {
						return MakeRef(new Ast.ThisReferenceExpression());
					} else {
						return MakeRef(new Ast.IdentifierExpression(((ParameterDefinition)operand).Name).WithAnnotation(operand));
					}
				case ILCode.Ldc_I4: return AstBuilder.MakePrimitive((int)operand, byteCode.InferredType);
				case ILCode.Ldc_I8: return AstBuilder.MakePrimitive((long)operand, byteCode.InferredType);
				case ILCode.Ldc_R4:
				case ILCode.Ldc_R8:
				case ILCode.Ldc_Decimal:
					return new Ast.PrimitiveExpression(operand);
				case ILCode.Ldfld:
					if (arg1 is DirectionExpression)
						arg1 = ((DirectionExpression)arg1).Expression.Detach();
					return arg1.Member(((FieldReference) operand).Name).WithAnnotation(operand);
				case ILCode.Ldsfld:
					return AstBuilder.ConvertType(((FieldReference)operand).DeclaringType)
						.Member(((FieldReference)operand).Name).WithAnnotation(operand);
				case ILCode.Stfld:
					if (arg1 is DirectionExpression)
						arg1 = ((DirectionExpression)arg1).Expression.Detach();
					return new AssignmentExpression(arg1.Member(((FieldReference) operand).Name).WithAnnotation(operand), arg2);
				case ILCode.Stsfld:
					return new AssignmentExpression(
						AstBuilder.ConvertType(((FieldReference)operand).DeclaringType)
						.Member(((FieldReference)operand).Name).WithAnnotation(operand),
						arg1);
				case ILCode.Ldflda:  return MakeRef(arg1.Member(((FieldReference) operand).Name).WithAnnotation(operand));
				case ILCode.Ldsflda:
					return MakeRef(
						AstBuilder.ConvertType(((FieldReference)operand).DeclaringType)
						.Member(((FieldReference)operand).Name).WithAnnotation(operand));
				case ILCode.Ldloc:
					localVariablesToDefine.Add((ILVariable)operand);
					return new Ast.IdentifierExpression(((ILVariable)operand).Name).WithAnnotation(operand);
				case ILCode.Ldloca:
					localVariablesToDefine.Add((ILVariable)operand);
					return MakeRef(new Ast.IdentifierExpression(((ILVariable)operand).Name).WithAnnotation(operand));
				case ILCode.Ldnull: return new Ast.NullReferenceExpression();
				case ILCode.Ldstr:  return new Ast.PrimitiveExpression(operand);
				case ILCode.Ldtoken:
					if (operand is Cecil.TypeReference) {
						return new Ast.TypeOfExpression { Type = operandAsTypeRef }.Member("TypeHandle");
					} else {
						return InlineAssembly(byteCode, args);
					}
				case ILCode.Leave:    return new GotoStatement() { Label = ((ILLabel)operand).Name };
				case ILCode.Localloc: return InlineAssembly(byteCode, args);
				case ILCode.Mkrefany: return InlineAssembly(byteCode, args);
				case ILCode.Newobj: {
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
				case ILCode.No: return InlineAssembly(byteCode, args);
				case ILCode.Nop: return null;
				case ILCode.Pop: return arg1;
				case ILCode.Readonly: return InlineAssembly(byteCode, args);
				case ILCode.Refanytype: return InlineAssembly(byteCode, args);
				case ILCode.Refanyval: return InlineAssembly(byteCode, args);
				case ILCode.Ret:
					if (methodDef.ReturnType.FullName != "System.Void") {
						return new Ast.ReturnStatement { Expression = arg1 };
					} else {
						return new Ast.ReturnStatement();
					}
				case ILCode.Rethrow: return new Ast.ThrowStatement();
				case ILCode.Sizeof:  return new Ast.SizeOfExpression { Type = operandAsTypeRef };
				case ILCode.Starg:   return new Ast.AssignmentExpression(new Ast.IdentifierExpression(((ParameterDefinition)operand).Name).WithAnnotation(operand), arg1);
				case ILCode.Stloc: {
					ILVariable locVar = (ILVariable)operand;
					localVariablesToDefine.Add(locVar);
					return new Ast.AssignmentExpression(new Ast.IdentifierExpression(locVar.Name).WithAnnotation(locVar), arg1);
				}
				case ILCode.Switch: return InlineAssembly(byteCode, args);
				case ILCode.Tail: return InlineAssembly(byteCode, args);
				case ILCode.Throw: return new Ast.ThrowStatement { Expression = arg1 };
				case ILCode.Unaligned: return InlineAssembly(byteCode, args);
				case ILCode.Volatile: return InlineAssembly(byteCode, args);
				case ILCode.YieldBreak:
					return new Ast.YieldBreakStatement();
				case ILCode.YieldReturn:
					return new Ast.YieldStatement { Expression = arg1 };
				case ILCode.InitCollection: {
					ObjectCreateExpression oce = (ObjectCreateExpression)arg1;
					oce.Initializer = new ArrayInitializerExpression();
					for (int i = 1; i < args.Count; i++) {
						ArrayInitializerExpression aie = args[i] as ArrayInitializerExpression;
						if (aie != null && aie.Elements.Count == 1)
							oce.Initializer.Elements.Add(aie.Elements.Single().Detach());
						else
							oce.Initializer.Elements.Add(args[i]);
					}
					return oce;
				}
				case ILCode.InitCollectionAddMethod: {
					var collectionInit = new ArrayInitializerExpression();
					collectionInit.Elements.AddRange(args);
					return collectionInit;
				}
				default: throw new Exception("Unknown OpCode: " + byteCode.Code);
			}
		}
		
		Expression MakeDefaultValue(TypeReference type)
		{
			TypeDefinition typeDef = type.Resolve();
			if (typeDef != null) {
				if (TypeAnalysis.IsIntegerOrEnum(typeDef))
					return AstBuilder.MakePrimitive(0, typeDef);
				else if (!typeDef.IsValueType)
					return new NullReferenceExpression();
				switch (typeDef.FullName) {
					case "System.Nullable`1":
						return new NullReferenceExpression();
					case "System.Single":
						return new PrimitiveExpression(0f);
					case "System.Double":
						return new PrimitiveExpression(0.0);
					case "System.Decimal":
						return new PrimitiveExpression(0m);
				}
			}
			return new DefaultValueExpression { Type = AstBuilder.ConvertType(type) };
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
			
			if (cecilMethod.Name == ".ctor" && cecilMethod.DeclaringType.IsValueType) {
				// On value types, the constructor can be called.
				// This is equivalent to 'target = new ValueType(args);'.
				ObjectCreateExpression oce = new ObjectCreateExpression();
				oce.Type = AstBuilder.ConvertType(cecilMethod.DeclaringType);
				AdjustArgumentsForMethodCall(cecilMethod, methodArgs);
				oce.Arguments.AddRange(methodArgs);
				return new AssignmentExpression(target, oce);
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
				} else if (cecilMethodDef.Name == "Invoke" && cecilMethodDef.DeclaringType.BaseType != null && cecilMethodDef.DeclaringType.BaseType.FullName == "System.MulticastDelegate") {
					AdjustArgumentsForMethodCall(cecilMethod, methodArgs);
					return target.Invoke(methodArgs);
				}
			}
			// Default invocation
			AdjustArgumentsForMethodCall(cecilMethodDef ?? cecilMethod, methodArgs);
			return target.Invoke(cecilMethod.Name, ConvertTypeArguments(cecilMethod), methodArgs).WithAnnotation(cecilMethod);
		}
		
		static void AdjustArgumentsForMethodCall(MethodReference cecilMethod, List<Expression> methodArgs)
		{
			// Convert 'ref' into 'out' where necessary
			for (int i = 0; i < methodArgs.Count && i < cecilMethod.Parameters.Count; i++) {
				DirectionExpression dir = methodArgs[i] as DirectionExpression;
				if (dir != null && cecilMethod.Parameters[i].IsOut)
					dir.FieldDirection = FieldDirection.Out;
			}
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
						return new BinaryOperatorExpression(expr, BinaryOperatorType.InEquality, AstBuilder.MakePrimitive(0, actualType));
					} else {
						return new BinaryOperatorExpression(expr, BinaryOperatorType.InEquality, new NullReferenceExpression());
					}
				}
				if (TypeAnalysis.IsBoolean(actualType) && requiredIsIntegerOrEnum) {
					return new ConditionalExpression {
						Condition = expr,
						TrueExpression = AstBuilder.MakePrimitive(1, reqType),
						FalseExpression = AstBuilder.MakePrimitive(0, reqType)
					};
				}

				if (expr is PrimitiveExpression && !requiredIsIntegerOrEnum && TypeAnalysis.IsEnum(actualType))
				{
					return expr.CastTo(AstBuilder.ConvertType(actualType));
				}

				if (actualIsIntegerOrEnum && requiredIsIntegerOrEnum) {
					if (actualType.FullName == reqType.FullName)
						return expr;
					return expr.CastTo(AstBuilder.ConvertType(reqType));
				}
				return expr;
			}
		}
	}
}
