// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using ICSharpCode.Decompiler.Ast.Transforms;
using ICSharpCode.Decompiler.ILAst;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.PatternMatching;
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
		
		/// <summary>
		/// Creates the body for the method definition.
		/// </summary>
		/// <param name="methodDef">Method definition to decompile.</param>
		/// <param name="context">Decompilation context.</param>
		/// <param name="parameters">Parameter declarations of the method being decompiled.
		/// These are used to update the parameter names when the decompiler generates names for the parameters.</param>
		/// <returns>Block for the method body</returns>
		public static BlockStatement CreateMethodBody(MethodDefinition methodDef,
		                                              DecompilerContext context,
		                                              IEnumerable<ParameterDeclaration> parameters = null)
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
					return builder.CreateMethodBody(parameters);
				} else {
					try {
						return builder.CreateMethodBody(parameters);
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
		
		public BlockStatement CreateMethodBody(IEnumerable<ParameterDeclaration> parameters)
		{
			if (methodDef.Body == null) {
				return null;
			}
			
			context.CancellationToken.ThrowIfCancellationRequested();
			ILBlock ilMethod = new ILBlock();
			ILAstBuilder astBuilder = new ILAstBuilder();
			ilMethod.Body = astBuilder.Build(methodDef, true, context);
			
			context.CancellationToken.ThrowIfCancellationRequested();
			ILAstOptimizer bodyGraph = new ILAstOptimizer();
			bodyGraph.Optimize(context, ilMethod);
			context.CancellationToken.ThrowIfCancellationRequested();
			
			var localVariables = ilMethod.GetSelfAndChildrenRecursive<ILExpression>().Select(e => e.Operand as ILVariable)
				.Where(v => v != null && !v.IsParameter).Distinct();
			Debug.Assert(context.CurrentMethod == methodDef);
			NameVariables.AssignNamesToVariables(context, astBuilder.Parameters, localVariables, ilMethod);
			
			if (parameters != null) {
				foreach (var pair in (from p in parameters
				                      join v in astBuilder.Parameters on p.Annotation<ParameterDefinition>() equals v.OriginalParameter
				                      select new { p, v.Name }))
				{
					pair.p.Name = pair.Name;
				}
			}
			
			context.CancellationToken.ThrowIfCancellationRequested();
			Ast.BlockStatement astBlock = TransformBlock(ilMethod);
			CommentStatement.ReplaceAll(astBlock); // convert CommentStatements to Comments
			
			Statement insertionPoint = astBlock.Statements.FirstOrDefault();
			foreach (ILVariable v in localVariablesToDefine) {
				AstType type;
				if (v.Type.ContainsAnonymousType())
					type = new SimpleType("var");
				else
					type = AstBuilder.ConvertType(v.Type);
				var newVarDecl = new VariableDeclarationStatement(type, v.Name);
				newVarDecl.Variables.Single().AddAnnotation(v);
				astBlock.Statements.InsertBefore(insertionPoint, newVarDecl);
			}
			
			astBlock.AddAnnotation(new MemberMapping(methodDef) { LocalVariables = localVariables });
			
			return astBlock;
		}
		
		Ast.BlockStatement TransformBlock(ILBlock block)
		{
			Ast.BlockStatement astBlock = new BlockStatement();
			if (block != null) {
				foreach(ILNode node in block.GetChildren()) {
					astBlock.Statements.AddRange(TransformNode(node));
				}
			}
			return astBlock;
		}
		
		IEnumerable<Statement> TransformNode(ILNode node)
		{
			if (node is ILLabel) {
				yield return new Ast.LabelStatement { Label = ((ILLabel)node).Name };
			} else if (node is ILExpression) {
				List<ILRange> ilRanges = ILRange.OrderAndJoint(node.GetSelfAndChildrenRecursive<ILExpression>().SelectMany(e => e.ILRanges));
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
				if (TypeAnalysis.IsBoolean(ilSwitch.Condition.InferredType) && ilSwitch.CaseBlocks.SelectMany(cb => cb.Values).Any(val => val != 0 && val != 1)) {
					// If switch cases contain values other then 0 and 1, force the condition to be non-boolean
					ilSwitch.Condition.ExpectedType = typeSystem.Int32;
				}
				SwitchStatement switchStmt = new SwitchStatement() { Expression = (Expression)TransformExpression(ilSwitch.Condition) };
				foreach (var caseBlock in ilSwitch.CaseBlocks) {
					SwitchSection section = new SwitchSection();
					if (caseBlock.Values != null) {
						section.CaseLabels.AddRange(caseBlock.Values.Select(i => new CaseLabel() { Expression = AstBuilder.MakePrimitive(i, ilSwitch.Condition.ExpectedType ?? ilSwitch.Condition.InferredType) }));
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
					if (catchClause.ExceptionVariable == null
					    && (catchClause.ExceptionType == null || catchClause.ExceptionType.MetadataType == MetadataType.Object))
					{
						tryCatchStmt.CatchClauses.Add(new Ast.CatchClause { Body = TransformBlock(catchClause) });
					} else {
						tryCatchStmt.CatchClauses.Add(
							new Ast.CatchClause {
								Type = AstBuilder.ConvertType(catchClause.ExceptionType),
								VariableName = catchClause.ExceptionVariable == null ? null : catchClause.ExceptionVariable.Name,
								Body = TransformBlock(catchClause)
							}.WithAnnotation(catchClause.ExceptionVariable));
					}
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
			} else if (node is ILFixedStatement) {
				ILFixedStatement fixedNode = (ILFixedStatement)node;
				FixedStatement fixedStatement = new FixedStatement();
				foreach (ILExpression initializer in fixedNode.Initializers) {
					Debug.Assert(initializer.Code == ILCode.Stloc);
					ILVariable v = (ILVariable)initializer.Operand;
					fixedStatement.Variables.Add(
						new VariableInitializer {
							Name = v.Name,
							Initializer = (Expression)TransformExpression(initializer.Arguments[0])
						}.WithAnnotation(v));
				}
				fixedStatement.Type = AstBuilder.ConvertType(((ILVariable)fixedNode.Initializers[0].Operand).Type);
				fixedStatement.EmbeddedStatement = TransformBlock(fixedNode.BodyBlock);
				yield return fixedStatement;
			} else if (node is ILBlock) {
				yield return TransformBlock((ILBlock)node);
			} else {
				throw new Exception("Unknown node type");
			}
		}
		
		AstNode TransformExpression(ILExpression expr)
		{
			AstNode node = TransformByteCode(expr);
			Expression astExpr = node as Expression;
			
			// get IL ranges - used in debugger
			List<ILRange> ilRanges = ILRange.OrderAndJoint(expr.GetSelfAndChildrenRecursive<ILExpression>().SelectMany(e => e.ILRanges));
			AstNode result;
			
			if (astExpr != null)
				result = Convert(astExpr, expr.InferredType, expr.ExpectedType);
			else
				result = node;
			
			if (result != null)
				result = result.WithAnnotation(new TypeInformation(expr.InferredType));
			
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
			
			switch (byteCode.Code) {
					#region Arithmetic
				case ILCode.Add:
				case ILCode.Add_Ovf:
				case ILCode.Add_Ovf_Un:
					{
						BinaryOperatorExpression boe;
						if (byteCode.InferredType is PointerType) {
							if (byteCode.Arguments[0].ExpectedType is PointerType) {
								arg2 = DivideBySize(arg2, ((PointerType)byteCode.InferredType).ElementType);
								boe = new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Add, arg2);
								boe.AddAnnotation(IntroduceUnsafeModifier.PointerArithmeticAnnotation);
							} else if (byteCode.Arguments[1].ExpectedType is PointerType) {
								arg1 = DivideBySize(arg1, ((PointerType)byteCode.InferredType).ElementType);
								boe = new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Add, arg2);
								boe.AddAnnotation(IntroduceUnsafeModifier.PointerArithmeticAnnotation);
							} else {
								boe = new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Add, arg2);
							}
						} else {
							boe = new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Add, arg2);
						}
						boe.AddAnnotation(byteCode.Code == ILCode.Add ? AddCheckedBlocks.UncheckedAnnotation : AddCheckedBlocks.CheckedAnnotation);
						return boe;
					}
				case ILCode.Sub:
				case ILCode.Sub_Ovf:
				case ILCode.Sub_Ovf_Un:
					{
						BinaryOperatorExpression boe;
						if (byteCode.InferredType is PointerType) {
							if (byteCode.Arguments[0].ExpectedType is PointerType) {
								arg2 = DivideBySize(arg2, ((PointerType)byteCode.InferredType).ElementType);
								boe = new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Subtract, arg2);
								boe.WithAnnotation(IntroduceUnsafeModifier.PointerArithmeticAnnotation);
							} else {
								boe = new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Subtract, arg2);
							}
						} else {
							boe = new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Subtract, arg2);
						}
						boe.AddAnnotation(byteCode.Code == ILCode.Sub ? AddCheckedBlocks.UncheckedAnnotation : AddCheckedBlocks.CheckedAnnotation);
						return boe;
					}
					case ILCode.Div:        return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Divide, arg2);
					case ILCode.Div_Un:     return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Divide, arg2);
					case ILCode.Mul:        return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Multiply, arg2).WithAnnotation(AddCheckedBlocks.UncheckedAnnotation);
					case ILCode.Mul_Ovf:    return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Multiply, arg2).WithAnnotation(AddCheckedBlocks.CheckedAnnotation);
					case ILCode.Mul_Ovf_Un: return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Multiply, arg2).WithAnnotation(AddCheckedBlocks.CheckedAnnotation);
					case ILCode.Rem:        return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Modulus, arg2);
					case ILCode.Rem_Un:     return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Modulus, arg2);
					case ILCode.And:        return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.BitwiseAnd, arg2);
					case ILCode.Or:         return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.BitwiseOr, arg2);
					case ILCode.Xor:        return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.ExclusiveOr, arg2);
					case ILCode.Shl:        return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.ShiftLeft, arg2);
					case ILCode.Shr:        return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.ShiftRight, arg2);
					case ILCode.Shr_Un:     return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.ShiftRight, arg2);
					case ILCode.Neg:        return new Ast.UnaryOperatorExpression(UnaryOperatorType.Minus, arg1).WithAnnotation(AddCheckedBlocks.UncheckedAnnotation);
					case ILCode.Not:        return new Ast.UnaryOperatorExpression(UnaryOperatorType.BitNot, arg1);
				case ILCode.PostIncrement:
				case ILCode.PostIncrement_Ovf:
				case ILCode.PostIncrement_Ovf_Un:
					{
						if (arg1 is DirectionExpression)
							arg1 = ((DirectionExpression)arg1).Expression.Detach();
						var uoe = new Ast.UnaryOperatorExpression(
							(int)byteCode.Operand > 0 ? UnaryOperatorType.PostIncrement : UnaryOperatorType.PostDecrement, arg1);
						uoe.AddAnnotation((byteCode.Code == ILCode.PostIncrement) ? AddCheckedBlocks.UncheckedAnnotation : AddCheckedBlocks.CheckedAnnotation);
						return uoe;
					}
					#endregion
					#region Arrays
					case ILCode.Newarr: {
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
					case ILCode.InitArray: {
						var ace = new Ast.ArrayCreateExpression();
						ace.Type = operandAsTypeRef;
						ComposedType ct = operandAsTypeRef as ComposedType;
						var arrayType = (ArrayType) operand;
						if (ct != null)
						{
							// change "new (int[,])[10] to new int[10][,]"
							ct.ArraySpecifiers.MoveTo(ace.AdditionalArraySpecifiers);
							ace.Initializer = new ArrayInitializerExpression();
						}
						var newArgs = new List<Expression>();
						foreach (var arrayDimension in arrayType.Dimensions.Skip(1).Reverse())
						{
							int length = (int)arrayDimension.UpperBound - (int)arrayDimension.LowerBound;
							for (int j = 0; j < args.Count; j += length)
							{
								var child = new ArrayInitializerExpression();
								child.Elements.AddRange(args.GetRange(j, length));
								newArgs.Add(child);
							}
							var temp = args;
							args = newArgs;
							newArgs = temp;
							newArgs.Clear();
						}
						ace.Initializer.Elements.AddRange(args);
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
				case ILCode.Ldelema:
					return MakeRef(arg1.Indexer(arg2));
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
				case ILCode.CompoundAssignment:
					{
						CastExpression cast = arg1 as CastExpression;
						var boe = cast != null ? (BinaryOperatorExpression)cast.Expression : arg1 as BinaryOperatorExpression;
						// AssignmentExpression doesn't support overloaded operators so they have to be processed to BinaryOperatorExpression
						if (boe == null) {
							var tmp = new ParenthesizedExpression(arg1);
							ReplaceMethodCallsWithOperators.ProcessInvocationExpression((InvocationExpression)arg1);
							boe = (BinaryOperatorExpression)tmp.Expression;
						}
						var assignment = new Ast.AssignmentExpression {
							Left = boe.Left.Detach(),
							Operator = ReplaceMethodCallsWithOperators.GetAssignmentOperatorForBinaryOperator(boe.Operator),
							Right = boe.Right.Detach()
						}.CopyAnnotationsFrom(boe);
						// We do not mark the resulting assignment as RestoreOriginalAssignOperatorAnnotation, because
						// the operator cannot be translated back to the expanded form (as the left-hand expression
						// would be evaluated twice, and might have side-effects)
						if (cast != null) {
							cast.Expression = assignment;
							return cast;
						} else {
							return assignment;
						}
					}
					#endregion
					#region Comparison
					case ILCode.Ceq: return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Equality, arg2);
					case ILCode.Cne: return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.InEquality, arg2);
					case ILCode.Cgt: return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.GreaterThan, arg2);
					case ILCode.Cgt_Un: {
						// can also mean Inequality, when used with object references
						TypeReference arg1Type = byteCode.Arguments[0].InferredType;
						if (arg1Type != null && !arg1Type.IsValueType) goto case ILCode.Cne;
						goto case ILCode.Cgt;
					}
					case ILCode.Cle_Un: {
						// can also mean Equality, when used with object references
						TypeReference arg1Type = byteCode.Arguments[0].InferredType;
						if (arg1Type != null && !arg1Type.IsValueType) goto case ILCode.Ceq;
						goto case ILCode.Cle;
					}
					case ILCode.Cle: return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.LessThanOrEqual, arg2);
				case ILCode.Cge_Un:
					case ILCode.Cge: return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.GreaterThanOrEqual, arg2);
				case ILCode.Clt_Un:
					case ILCode.Clt:    return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.LessThan, arg2);
					#endregion
					#region Logical
					case ILCode.LogicNot:   return new Ast.UnaryOperatorExpression(UnaryOperatorType.Not, arg1);
					case ILCode.LogicAnd:   return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.ConditionalAnd, arg2);
					case ILCode.LogicOr:    return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.ConditionalOr, arg2);
					case ILCode.TernaryOp:  return new Ast.ConditionalExpression() { Condition = arg1, TrueExpression = arg2, FalseExpression = arg3 };
					case ILCode.NullCoalescing: 	return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.NullCoalescing, arg2);
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
				case ILCode.Conv_I:
				case ILCode.Conv_U:
					{
						// conversion was handled by Convert() function using the info from type analysis
						CastExpression cast = arg1 as CastExpression;
						if (cast != null) {
							cast.AddAnnotation(AddCheckedBlocks.UncheckedAnnotation);
						}
						return arg1;
					}
				case ILCode.Conv_R4:
				case ILCode.Conv_R8:
				case ILCode.Conv_R_Un: // TODO
					return arg1;
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
					{
						// conversion was handled by Convert() function using the info from type analysis
						CastExpression cast = arg1 as CastExpression;
						if (cast != null) {
							cast.AddAnnotation(AddCheckedBlocks.CheckedAnnotation);
						}
						return arg1;
					}
					case ILCode.Conv_Ovf_I:     return arg1.CastTo(typeof(IntPtr)); // TODO
					case ILCode.Conv_Ovf_U:     return arg1.CastTo(typeof(UIntPtr));
					case ILCode.Conv_Ovf_I_Un:  return arg1.CastTo(typeof(IntPtr));
					case ILCode.Conv_Ovf_U_Un:  return arg1.CastTo(typeof(UIntPtr));
					case ILCode.Castclass:      return arg1.CastTo(operandAsTypeRef);
				case ILCode.Unbox_Any:
					// unboxing does not require a cast if the argument was an isinst instruction
					if (arg1 is AsExpression && byteCode.Arguments[0].Code == ILCode.Isinst && TypeAnalysis.IsSameType(operand as TypeReference, byteCode.Arguments[0].Operand as TypeReference))
						return arg1;
					else
						return arg1.CastTo(operandAsTypeRef);
				case ILCode.Isinst:
					return arg1.CastAs(operandAsTypeRef);
				case ILCode.Box:
					return arg1;
				case ILCode.Unbox:
					return MakeRef(arg1.CastTo(operandAsTypeRef));
					#endregion
					#region Indirect
				case ILCode.Ldind_Ref:
				case ILCode.Ldobj:
					if (arg1 is DirectionExpression)
						return ((DirectionExpression)arg1).Expression.Detach();
					else
						return new UnaryOperatorExpression(UnaryOperatorType.Dereference, arg1);
				case ILCode.Stind_Ref:
				case ILCode.Stobj:
					if (arg1 is DirectionExpression)
						return new AssignmentExpression(((DirectionExpression)arg1).Expression.Detach(), arg2);
					else
						return new AssignmentExpression(new UnaryOperatorExpression(UnaryOperatorType.Dereference, arg1), arg2);
					#endregion
				case ILCode.Arglist:
					return new UndocumentedExpression { UndocumentedExpressionType = UndocumentedExpressionType.ArgListAccess };
					case ILCode.Break:    return InlineAssembly(byteCode, args);
				case ILCode.Call:
				case ILCode.CallGetter:
				case ILCode.CallSetter:
					return TransformCall(false, byteCode, args);
				case ILCode.Callvirt:
				case ILCode.CallvirtGetter:
				case ILCode.CallvirtSetter:
					return TransformCall(true, byteCode,  args);
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
					case ILCode.Initobj:      return InlineAssembly(byteCode, args);
				case ILCode.DefaultValue:
					return MakeDefaultValue((TypeReference)operand);
					case ILCode.Jmp: return InlineAssembly(byteCode, args);
				case ILCode.Ldc_I4:
					return AstBuilder.MakePrimitive((int)operand, byteCode.InferredType);
				case ILCode.Ldc_I8:
					return AstBuilder.MakePrimitive((long)operand, byteCode.InferredType);
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
				case ILCode.Ldflda:
					if (arg1 is DirectionExpression)
						arg1 = ((DirectionExpression)arg1).Expression.Detach();
					return MakeRef(arg1.Member(((FieldReference) operand).Name).WithAnnotation(operand));
				case ILCode.Ldsflda:
					return MakeRef(
						AstBuilder.ConvertType(((FieldReference)operand).DeclaringType)
						.Member(((FieldReference)operand).Name).WithAnnotation(operand));
					case ILCode.Ldloc: {
						ILVariable v = (ILVariable)operand;
						if (!v.IsParameter)
							localVariablesToDefine.Add((ILVariable)operand);
						Expression expr;
						if (v.IsParameter && v.OriginalParameter.Index < 0)
							expr = new ThisReferenceExpression();
						else
							expr = new Ast.IdentifierExpression(((ILVariable)operand).Name).WithAnnotation(operand);
						return v.IsParameter && v.Type is ByReferenceType ? MakeRef(expr) : expr;
					}
					case ILCode.Ldloca: {
						ILVariable v = (ILVariable)operand;
						if (v.IsParameter && v.OriginalParameter.Index < 0)
							return MakeRef(new ThisReferenceExpression());
						if (!v.IsParameter)
							localVariablesToDefine.Add((ILVariable)operand);
						return MakeRef(new Ast.IdentifierExpression(((ILVariable)operand).Name).WithAnnotation(operand));
					}
					case ILCode.Ldnull: return new Ast.NullReferenceExpression();
					case ILCode.Ldstr:  return new Ast.PrimitiveExpression(operand);
				case ILCode.Ldtoken:
					if (operand is Cecil.TypeReference) {
						return AstBuilder.CreateTypeOfExpression((TypeReference)operand).Member("TypeHandle");
					} else {
						Expression referencedEntity;
						string loadName;
						string handleName;
						if (operand is Cecil.FieldReference) {
							loadName = "fieldof";
							handleName = "FieldHandle";
							FieldReference fr = (FieldReference)operand;
							referencedEntity = AstBuilder.ConvertType(fr.DeclaringType).Member(fr.Name).WithAnnotation(fr);
						} else if (operand is Cecil.MethodReference) {
							loadName = "methodof";
							handleName = "MethodHandle";
							MethodReference mr = (MethodReference)operand;
							var methodParameters = mr.Parameters.Select(p => new TypeReferenceExpression(AstBuilder.ConvertType(p.ParameterType)));
							referencedEntity = AstBuilder.ConvertType(mr.DeclaringType).Invoke(mr.Name, methodParameters).WithAnnotation(mr);
						} else {
							loadName = "ldtoken";
							handleName = "Handle";
							referencedEntity = new IdentifierExpression(FormatByteCodeOperand(byteCode.Operand));
						}
						return new IdentifierExpression(loadName).Invoke(referencedEntity).WithAnnotation(new LdTokenAnnotation()).Member(handleName);
					}
					case ILCode.Leave:    return new GotoStatement() { Label = ((ILLabel)operand).Name };
				case ILCode.Localloc:
					{
						PointerType ptrType = byteCode.InferredType as PointerType;
						TypeReference type;
						if (ptrType != null) {
							type = ptrType.ElementType;
						} else {
							type = typeSystem.Byte;
						}
						return new StackAllocExpression {
							Type = AstBuilder.ConvertType(type),
							CountExpression = DivideBySize(arg1, type)
						};
					}
				case ILCode.Mkrefany:
					{
						DirectionExpression dir = arg1 as DirectionExpression;
						if (dir != null) {
							return new UndocumentedExpression {
								UndocumentedExpressionType = UndocumentedExpressionType.MakeRef,
								Arguments = { dir.Expression.Detach() }
							};
						} else {
							return InlineAssembly(byteCode, args);
						}
					}
				case ILCode.Refanytype:
					return new UndocumentedExpression {
						UndocumentedExpressionType = UndocumentedExpressionType.RefType,
						Arguments = { arg1 }
					}.Member("TypeHandle");
				case ILCode.Refanyval:
					return MakeRef(
						new UndocumentedExpression {
							UndocumentedExpressionType = UndocumentedExpressionType.RefValue,
							Arguments = { arg1, new TypeReferenceExpression(operandAsTypeRef) }
						});
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
						if (declaringType.IsAnonymousType()) {
							MethodDefinition ctor = ((MethodReference)operand).Resolve();
							if (methodDef != null) {
								AnonymousTypeCreateExpression atce = new AnonymousTypeCreateExpression();
								if (CanInferAnonymousTypePropertyNamesFromArguments(args, ctor.Parameters)) {
									atce.Initializers.AddRange(args);
								} else {
									for (int i = 0; i < args.Count; i++) {
										atce.Initializers.Add(
											new NamedExpression {
												Identifier = ctor.Parameters[i].Name,
												Expression = args[i]
											});
									}
								}
								return atce;
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
				case ILCode.Ret:
					if (methodDef.ReturnType.FullName != "System.Void") {
						return new Ast.ReturnStatement { Expression = arg1 };
					} else {
						return new Ast.ReturnStatement();
					}
					case ILCode.Rethrow: return new Ast.ThrowStatement();
					case ILCode.Sizeof:  return new Ast.SizeOfExpression { Type = operandAsTypeRef };
					case ILCode.Stloc: {
						ILVariable locVar = (ILVariable)operand;
						if (!locVar.IsParameter)
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
					return new Ast.YieldReturnStatement { Expression = arg1 };
				case ILCode.InitObject:
				case ILCode.InitCollection:
					{
						ArrayInitializerExpression initializer = new ArrayInitializerExpression();
						for (int i = 1; i < args.Count; i++) {
							Match m = objectInitializerPattern.Match(args[i]);
							if (m.Success) {
								MemberReferenceExpression mre = m.Get<MemberReferenceExpression>("left").Single();
								initializer.Elements.Add(
									new NamedExpression {
										Identifier = mre.MemberName,
										Expression = m.Get<Expression>("right").Single().Detach()
									}.CopyAnnotationsFrom(mre));
							} else {
								m = collectionInitializerPattern.Match(args[i]);
								if (m.Success) {
									if (m.Get("arg").Count() == 1) {
										initializer.Elements.Add(m.Get<Expression>("arg").Single().Detach());
									} else {
										ArrayInitializerExpression argList = new ArrayInitializerExpression();
										foreach (var expr in m.Get<Expression>("arg")) {
											argList.Elements.Add(expr.Detach());
										}
										initializer.Elements.Add(argList);
									}
								} else {
									initializer.Elements.Add(args[i]);
								}
							}
						}
						ObjectCreateExpression oce = arg1 as ObjectCreateExpression;
						DefaultValueExpression dve = arg1 as DefaultValueExpression;
						if (oce != null) {
							oce.Initializer = initializer;
							return oce;
						} else if (dve != null) {
							oce = new ObjectCreateExpression(dve.Type.Detach());
							oce.CopyAnnotationsFrom(dve);
							oce.Initializer = initializer;
							return oce;
						} else {
							return new AssignmentExpression(arg1, initializer);
						}
					}
				case ILCode.InitializedObject:
					return new InitializedObjectExpression();
				case ILCode.Wrap:
					return arg1.WithAnnotation(PushNegation.LiftedOperatorAnnotation);
				case ILCode.AddressOf:
					return MakeRef(arg1);
				case ILCode.ExpressionTreeParameterDeclarations:
					args[args.Count - 1].AddAnnotation(new ParameterDeclarationAnnotation(byteCode));
					return args[args.Count - 1];
				case ILCode.NullableOf:
					case ILCode.ValueOf: return arg1;
				default:
					throw new Exception("Unknown OpCode: " + byteCode.Code);
			}
		}
		
		internal static bool CanInferAnonymousTypePropertyNamesFromArguments(IList<Expression> args, IList<ParameterDefinition> parameters)
		{
			for (int i = 0; i < args.Count; i++) {
				string inferredName;
				if (args[i] is IdentifierExpression)
					inferredName = ((IdentifierExpression)args[i]).Identifier;
				else if (args[i] is MemberReferenceExpression)
					inferredName = ((MemberReferenceExpression)args[i]).MemberName;
				else
					inferredName = null;
				
				if (inferredName != parameters[i].Name) {
					return false;
				}
			}
			return true;
		}
		
		static readonly AstNode objectInitializerPattern = new AssignmentExpression(
			new MemberReferenceExpression {
				Target = new InitializedObjectExpression(),
				MemberName = Pattern.AnyString
			}.WithName("left"),
			new AnyNode("right")
		);
		
		static readonly AstNode collectionInitializerPattern = new InvocationExpression {
			Target = new MemberReferenceExpression {
				Target = new InitializedObjectExpression(),
				MemberName = "Add"
			},
			Arguments = { new Repeat(new AnyNode("arg")) }
		};
		
		sealed class InitializedObjectExpression : IdentifierExpression
		{
			public InitializedObjectExpression() : base("__initialized_object__") {}
			
			protected override bool DoMatch(AstNode other, Match match)
			{
				return other is InitializedObjectExpression;
			}
		}
		
		/// <summary>
		/// Divides expr by the size of 'type'.
		/// </summary>
		Expression DivideBySize(Expression expr, TypeReference type)
		{
			CastExpression cast = expr as CastExpression;
			if (cast != null && cast.Type is PrimitiveType && ((PrimitiveType)cast.Type).Keyword == "int")
				expr = cast.Expression.Detach();
			
			Expression sizeOfExpression;
			switch (TypeAnalysis.GetInformationAmount(type)) {
				case 1:
				case 8:
					sizeOfExpression = new PrimitiveExpression(1);
					break;
				case 16:
					sizeOfExpression = new PrimitiveExpression(2);
					break;
				case 32:
					sizeOfExpression = new PrimitiveExpression(4);
					break;
				case 64:
					sizeOfExpression = new PrimitiveExpression(8);
					break;
				default:
					sizeOfExpression = new SizeOfExpression { Type = AstBuilder.ConvertType(type) };
					break;
			}
			
			BinaryOperatorExpression boe = expr as BinaryOperatorExpression;
			if (boe != null && boe.Operator == BinaryOperatorType.Multiply && sizeOfExpression.IsMatch(boe.Right))
				return boe.Left.Detach();
			
			if (sizeOfExpression.IsMatch(expr))
				return new PrimitiveExpression(1);
			
			return new BinaryOperatorExpression(expr, BinaryOperatorType.Divide, sizeOfExpression);
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
		
		AstNode TransformCall(bool isVirtual, ILExpression byteCode, List<Ast.Expression> args)
		{
			Cecil.MethodReference cecilMethod = (MethodReference)byteCode.Operand;
			Cecil.MethodDefinition cecilMethodDef = cecilMethod.Resolve();
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
				
				if (cecilMethodDef != null) {
					// convert null.ToLower() to ((string)null).ToLower()
					if (target is NullReferenceExpression)
						target = target.CastTo(AstBuilder.ConvertType(cecilMethod.DeclaringType));
					
					if (cecilMethodDef.DeclaringType.IsInterface) {
						TypeReference tr = byteCode.Arguments[0].InferredType;
						if (tr != null) {
							TypeDefinition td = tr.Resolve();
							if (td != null && !td.IsInterface) {
								// Calling an interface method on a non-interface object:
								// we need to introduce an explicit cast
								target = target.CastTo(AstBuilder.ConvertType(cecilMethod.DeclaringType));
							}
						}
					}
				}
			} else {
				target = new TypeReferenceExpression { Type = AstBuilder.ConvertType(cecilMethod.DeclaringType) };
			}
			if (target is ThisReferenceExpression && !isVirtual) {
				// a non-virtual call on "this" might be a "base"-call.
				if (cecilMethod.DeclaringType.GetElementType() != methodDef.DeclaringType) {
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
			
			// Test whether the method is an accessor:
			if (cecilMethodDef != null) {
				if (cecilMethodDef.IsGetter && methodArgs.Count == 0) {
					foreach (var prop in cecilMethodDef.DeclaringType.Properties) {
						if (prop.GetMethod == cecilMethodDef)
							return target.Member(prop.Name).WithAnnotation(prop).WithAnnotation(cecilMethod);
					}
				} else if (cecilMethodDef.IsGetter) { // with parameters
					PropertyDefinition indexer = GetIndexer(cecilMethodDef);
					if (indexer != null)
						return target.Indexer(methodArgs).WithAnnotation(indexer).WithAnnotation(cecilMethod);
				} else if (cecilMethodDef.IsSetter && methodArgs.Count == 1) {
					foreach (var prop in cecilMethodDef.DeclaringType.Properties) {
						if (prop.SetMethod == cecilMethodDef)
							return new Ast.AssignmentExpression(target.Member(prop.Name).WithAnnotation(prop).WithAnnotation(cecilMethod), methodArgs[0]);
					}
				} else if (cecilMethodDef.IsSetter && methodArgs.Count > 1) {
					PropertyDefinition indexer = GetIndexer(cecilMethodDef);
					if (indexer != null)
						return new AssignmentExpression(
							target.Indexer(methodArgs.GetRange(0, methodArgs.Count - 1)).WithAnnotation(indexer).WithAnnotation(cecilMethod),
							methodArgs[methodArgs.Count - 1]
						);
				} else if (cecilMethodDef.IsAddOn && methodArgs.Count == 1) {
					foreach (var ev in cecilMethodDef.DeclaringType.Events) {
						if (ev.AddMethod == cecilMethodDef) {
							return new Ast.AssignmentExpression {
								Left = target.Member(ev.Name).WithAnnotation(ev).WithAnnotation(cecilMethod),
								Operator = AssignmentOperatorType.Add,
								Right = methodArgs[0]
							};
						}
					}
				} else if (cecilMethodDef.IsRemoveOn && methodArgs.Count == 1) {
					foreach (var ev in cecilMethodDef.DeclaringType.Events) {
						if (ev.RemoveMethod == cecilMethodDef) {
							return new Ast.AssignmentExpression {
								Left = target.Member(ev.Name).WithAnnotation(ev).WithAnnotation(cecilMethod),
								Operator = AssignmentOperatorType.Subtract,
								Right = methodArgs[0]
							};
						}
					}
				} else if (cecilMethodDef.Name == "Invoke" && cecilMethodDef.DeclaringType.BaseType != null && cecilMethodDef.DeclaringType.BaseType.FullName == "System.MulticastDelegate") {
					AdjustArgumentsForMethodCall(cecilMethod, methodArgs);
					return target.Invoke(methodArgs).WithAnnotation(cecilMethod);
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
				ParameterDefinition p = cecilMethod.Parameters[i];
				if (dir != null && p.IsOut && !p.IsIn)
					dir.FieldDirection = FieldDirection.Out;
			}
		}
		
		internal static PropertyDefinition GetIndexer(MethodDefinition cecilMethodDef)
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
			if (g.GenericArguments.Any(ta => ta.ContainsAnonymousType()))
				return null;
			return g.GenericArguments.Select(t => AstBuilder.ConvertType(t));
		}
		
		static Ast.DirectionExpression MakeRef(Ast.Expression expr)
		{
			return new DirectionExpression { Expression = expr, FieldDirection = FieldDirection.Ref };
		}
		
		Ast.Expression Convert(Ast.Expression expr, Cecil.TypeReference actualType, Cecil.TypeReference reqType)
		{
			if (actualType == null || reqType == null || TypeAnalysis.IsSameType(actualType, reqType)) {
				return expr;
			} else if (actualType is ByReferenceType && reqType is PointerType && expr is DirectionExpression) {
				return Convert(
					new UnaryOperatorExpression(UnaryOperatorType.AddressOf, ((DirectionExpression)expr).Expression.Detach()),
					new PointerType(((ByReferenceType)actualType).ElementType),
					reqType);
			} else if (actualType is PointerType && reqType is ByReferenceType) {
				expr = Convert(expr, actualType, new PointerType(((ByReferenceType)reqType).ElementType));
				return new DirectionExpression {
					FieldDirection = FieldDirection.Ref,
					Expression = new UnaryOperatorExpression(UnaryOperatorType.Dereference, expr)
				};
			} else if (actualType is PointerType && reqType is PointerType) {
				if (actualType.FullName != reqType.FullName)
					return expr.CastTo(AstBuilder.ConvertType(reqType));
				else
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
				
				bool actualIsPrimitiveType = actualIsIntegerOrEnum
					|| actualType.MetadataType == MetadataType.Single || actualType.MetadataType == MetadataType.Double;
				bool requiredIsPrimitiveType = requiredIsIntegerOrEnum
					|| reqType.MetadataType == MetadataType.Single || reqType.MetadataType == MetadataType.Double;
				if (actualIsPrimitiveType && requiredIsPrimitiveType) {
					return expr.CastTo(AstBuilder.ConvertType(reqType));
				}
				return expr;
			}
		}
	}
}
