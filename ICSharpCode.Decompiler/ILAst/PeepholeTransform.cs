// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Diagnostics;
using System.Linq;
using ICSharpCode.NRefactory.Utils;
using Mono.Cecil;

namespace ICSharpCode.Decompiler.ILAst
{
	public delegate void PeepholeTransform(ILBlock block, ref int i);
	
	/// <summary>
	/// Handles peephole transformations on the ILAst.
	/// </summary>
	public class PeepholeTransforms
	{
		DecompilerContext context;
		ILBlock method;
		
		public static void Run(DecompilerContext context, ILBlock method)
		{
			PeepholeTransforms transforms = new PeepholeTransforms();
			transforms.context = context;
			transforms.method = method;
			
			InitializerPeepholeTransforms initializerTransforms = new InitializerPeepholeTransforms(method);
			PeepholeTransform[] blockTransforms = {
				initializerTransforms.TransformArrayInitializers,
				initializerTransforms.TransformCollectionInitializers,
				transforms.CachedDelegateInitialization,
				transforms.MakeAssignmentExpression,
				transforms.IntroduceFixedStatements
			};
			Func<ILExpression, ILExpression>[] exprTransforms = {
				HandleDecimalConstants,
				SimplifyLdObjAndStObj
			};
			// Traverse in post order so that nested blocks are transformed first. This is required so that
			// patterns on the parent block can assume that all nested blocks are already transformed.
			foreach (var node in TreeTraversal.PostOrder<ILNode>(method, c => c != null ? c.GetChildren() : null)) {
				ILBlock block = node as ILBlock;
				ILExpression expr;
				if (block != null) {
					// go through the instructions in reverse so that transforms can build up nested structures inside-out
					for (int i = block.Body.Count - 1; i >= 0; i--) {
						context.CancellationToken.ThrowIfCancellationRequested();
						expr = block.Body[i] as ILExpression;
						if (expr != null) {
							// apply expr transforms to top-level expr in block
							bool modified = ApplyExpressionTransforms(ref expr, exprTransforms);
							block.Body[i] = expr;
							if (modified) {
								ILInlining inlining = new ILInlining(method);
								if (inlining.InlineIfPossible(block, ref i)) {
									i++; // retry all transforms on the new combined instruction
									continue;
								}
							}
						}
						// apply block transforms
						foreach (var t in blockTransforms) {
							t(block, ref i);
							Debug.Assert(i <= block.Body.Count && i >= 0);
							if (i == block.Body.Count) // special case: retry all transforms
								break;
						}
					}
				}
				expr = node as ILExpression;
				if (expr != null) {
					// apply expr transforms to all arguments
					for (int i = 0; i < expr.Arguments.Count; i++) {
						ILExpression arg = expr.Arguments[i];
						ApplyExpressionTransforms(ref arg, exprTransforms);
						expr.Arguments[i] = arg;
					}
				}
			}
		}
		
		static bool ApplyExpressionTransforms(ref ILExpression expr, Func<ILExpression, ILExpression>[] exprTransforms)
		{
			bool modifiedInAnyIteration = false;
			bool modified;
			do {
				modified = false;
				ILExpression oldExpr = expr;
				ILCode oldOpCode = oldExpr.Code;
				foreach (var t in exprTransforms)
					expr = t(expr);
				if (expr != oldExpr || oldOpCode != expr.Code) {
					modified = true;
					modifiedInAnyIteration = true;
				}
			} while (modified);
			return modifiedInAnyIteration;
		}
		
		#region HandleDecimalConstants
		static ILExpression HandleDecimalConstants(ILExpression expr)
		{
			if (expr.Code == ILCode.Newobj) {
				MethodReference r = (MethodReference)expr.Operand;
				if (r.DeclaringType.Name == "Decimal" && r.DeclaringType.Namespace == "System") {
					if (expr.Arguments.Count == 1) {
						int? val = GetI4Constant(expr.Arguments[0]);
						if (val != null) {
							expr.Arguments.Clear();
							expr.Code = ILCode.Ldc_Decimal;
							expr.Operand = new decimal(val.Value);
							expr.InferredType = r.DeclaringType;
						}
					} else if (expr.Arguments.Count == 5) {
						int? lo = GetI4Constant(expr.Arguments[0]);
						int? mid = GetI4Constant(expr.Arguments[1]);
						int? hi = GetI4Constant(expr.Arguments[2]);
						int? isNegative = GetI4Constant(expr.Arguments[3]);
						int? scale = GetI4Constant(expr.Arguments[4]);
						if (lo != null && mid != null && hi != null && isNegative != null && scale != null) {
							expr.Arguments.Clear();
							expr.Code = ILCode.Ldc_Decimal;
							expr.Operand = new decimal(lo.Value, mid.Value, hi.Value, isNegative.Value != 0, (byte)scale);
							expr.InferredType = r.DeclaringType;
						}
					}
				}
			}
			return expr;
		}
		
		static int? GetI4Constant(ILExpression expr)
		{
			if (expr != null && expr.Code == ILCode.Ldc_I4)
				return (int)expr.Operand;
			else
				return null;
		}
		#endregion
		
		#region SimplifyLdObjAndStObj
		static ILExpression SimplifyLdObjAndStObj(ILExpression expr)
		{
			if (expr.Code == ILCode.Initobj) {
				expr.Code = ILCode.Stobj;
				expr.Arguments.Add(new ILExpression(ILCode.DefaultValue, expr.Operand));
			}
			if (expr.Code == ILCode.Stobj) {
				switch (expr.Arguments[0].Code) {
					case ILCode.Ldelema:
						return SimplifyLdObjOrStObj(expr, ILCode.Stelem_Any);
					case ILCode.Ldloca:
						return SimplifyLdObjOrStObj(expr, ILCode.Stloc);
					case ILCode.Ldflda:
						return SimplifyLdObjOrStObj(expr, ILCode.Stfld);
					case ILCode.Ldsflda:
						return SimplifyLdObjOrStObj(expr, ILCode.Stsfld);
				}
			} else if (expr.Code == ILCode.Ldobj) {
				switch (expr.Arguments[0].Code) {
					case ILCode.Ldelema:
						return SimplifyLdObjOrStObj(expr, ILCode.Ldelem_Any);
					case ILCode.Ldloca:
						return SimplifyLdObjOrStObj(expr, ILCode.Ldloc);
					case ILCode.Ldflda:
						return SimplifyLdObjOrStObj(expr, ILCode.Ldfld);
					case ILCode.Ldsflda:
						return SimplifyLdObjOrStObj(expr, ILCode.Ldsfld);
				}
			}
			return expr;
		}
		
		static ILExpression SimplifyLdObjOrStObj(ILExpression expr, ILCode newCode)
		{
			ILExpression lda = expr.Arguments[0];
			lda.Code = newCode;
			if (expr.Code == ILCode.Stobj)
				lda.Arguments.Add(expr.Arguments[1]);
			lda.ILRanges.AddRange(expr.ILRanges);
			return lda;
		}
		#endregion
		
		#region CachedDelegateInitialization
		void CachedDelegateInitialization(ILBlock block, ref int i)
		{
			// if (logicnot(ldsfld(field))) {
			//     stsfld(field, newobj(Action::.ctor, ldnull(), ldftn(method)))
			// } else {
			// }
			// ...(..., ldsfld(field), ...)
			
			ILCondition c = block.Body[i] as ILCondition;
			if (c == null || c.Condition == null && c.TrueBlock == null || c.FalseBlock == null)
				return;
			if (!(c.TrueBlock.Body.Count == 1 && c.FalseBlock.Body.Count == 0))
				return;
			if (!c.Condition.Match(ILCode.LogicNot))
				return;
			ILExpression condition = c.Condition.Arguments.Single() as ILExpression;
			if (condition == null || condition.Code != ILCode.Ldsfld)
				return;
			FieldDefinition field = ((FieldReference)condition.Operand).ResolveWithinSameModule(); // field is defined in current assembly
			if (field == null || !field.IsCompilerGeneratedOrIsInCompilerGeneratedClass())
				return;
			ILExpression stsfld = c.TrueBlock.Body[0] as ILExpression;
			if (!(stsfld != null && stsfld.Code == ILCode.Stsfld && ((FieldReference)stsfld.Operand).ResolveWithinSameModule() == field))
				return;
			ILExpression newObj = stsfld.Arguments[0];
			if (!(newObj.Code == ILCode.Newobj && newObj.Arguments.Count == 2))
				return;
			if (newObj.Arguments[0].Code != ILCode.Ldnull)
				return;
			if (newObj.Arguments[1].Code != ILCode.Ldftn)
				return;
			MethodDefinition anonymousMethod = ((MethodReference)newObj.Arguments[1].Operand).ResolveWithinSameModule(); // method is defined in current assembly
			if (!Ast.Transforms.DelegateConstruction.IsAnonymousMethod(context, anonymousMethod))
				return;
			
			ILNode followingNode = block.Body.ElementAtOrDefault(i + 1);
			if (followingNode != null && followingNode.GetSelfAndChildrenRecursive<ILExpression>().Count(
				e => e.Code == ILCode.Ldsfld && ((FieldReference)e.Operand).ResolveWithinSameModule() == field) == 1)
			{
				foreach (ILExpression parent in followingNode.GetSelfAndChildrenRecursive<ILExpression>()) {
					for (int j = 0; j < parent.Arguments.Count; j++) {
						if (parent.Arguments[j].Code == ILCode.Ldsfld && ((FieldReference)parent.Arguments[j].Operand).ResolveWithinSameModule() == field) {
							parent.Arguments[j] = newObj;
							block.Body.RemoveAt(i);
							i -= new ILInlining(method).InlineInto(block, i, aggressive: true);
							return;
						}
					}
				}
			}
		}
		#endregion
		
		#region MakeAssignmentExpression
		void MakeAssignmentExpression(ILBlock block, ref int i)
		{
			// expr_44 = ...
			// stloc(v, expr_44)
			// ->
			// expr_44 = stloc(v, ...))
			ILVariable exprVar;
			ILExpression initializer;
			if (!(block.Body[i].Match(ILCode.Stloc, out exprVar, out initializer) && exprVar.IsGenerated))
				return;
			ILExpression stloc1 = block.Body.ElementAtOrDefault(i + 1) as ILExpression;
			if (!(stloc1 != null && stloc1.Code == ILCode.Stloc && stloc1.Arguments[0].Code == ILCode.Ldloc && stloc1.Arguments[0].Operand == exprVar))
				return;
			
			ILInlining inlining;
			ILExpression store2 = block.Body.ElementAtOrDefault(i + 2) as ILExpression;
			if (StoreCanBeConvertedToAssignment(store2, exprVar)) {
				// expr_44 = ...
				// stloc(v1, expr_44)
				// anystore(v2, expr_44)
				// ->
				// stloc(v1, anystore(v2, ...))
				inlining = new ILInlining(method);
				if (inlining.numLdloc.GetOrDefault(exprVar) == 2 && inlining.numStloc.GetOrDefault(exprVar) == 1) {
					block.Body.RemoveAt(i + 2); // remove store2
					block.Body.RemoveAt(i); // remove expr = ...
					stloc1.Arguments[0] = store2;
					store2.Arguments[store2.Arguments.Count - 1] = initializer;
					
					if (inlining.InlineIfPossible(block, ref i)) {
						i++; // retry transformations on the new combined instruction
					}
					return;
				}
			}
			
			
			block.Body.RemoveAt(i + 1); // remove stloc
			stloc1.Arguments[0] = initializer;
			((ILExpression)block.Body[i]).Arguments[0] = stloc1;
			
			inlining = new ILInlining(method);
			if (inlining.InlineIfPossible(block, ref i)) {
				i++; // retry transformations on the new combined instruction
			}
		}
		
		bool StoreCanBeConvertedToAssignment(ILExpression store, ILVariable exprVar)
		{
			if (store != null && (store.Code == ILCode.Stloc || store.Code == ILCode.Stfld || store.Code == ILCode.Stsfld)) {
				return store.Arguments.Last().Code == ILCode.Ldloc && store.Arguments.Last().Operand == exprVar;
			}
			return false;
		}
		#endregion
		
		#region IntroduceFixedStatements
		void IntroduceFixedStatements(ILBlock block, ref int i)
		{
			// stloc(pinned_Var, conv.u(ldc.i4(0)))
			ILExpression initValue;
			ILVariable pinnedVar;
			if (!MatchFixedInitializer(block, i, out pinnedVar, out initValue))
				return;
			// find initialization of v:
			int j;
			for (j = i + 1; j < block.Body.Count; j++) {
				ILVariable v2;
				ILExpression storedVal;
				if (block.Body[j].Match(ILCode.Stloc, out v2, out storedVal) && v2 == pinnedVar) {
					if (IsNullOrZero(storedVal)) {
						// Create fixed statement from i to j
						ILFixedStatement stmt = new ILFixedStatement();
						stmt.Initializer = initValue;
						stmt.BodyBlock = new ILBlock(block.Body.GetRange(i + 1, j - i - 1)); // from i+1 to j-1 (inclusive)
						block.Body.RemoveRange(i + 1, j - i); // from j+1 to i (inclusive)
						block.Body[i] = stmt;
						if (pinnedVar.Type.IsByReference)
							pinnedVar.Type = new PointerType(((ByReferenceType)pinnedVar.Type).ElementType);
						
						HandleStringFixing(stmt);
						break;
					}
				}
			}
		}
		
		bool IsNullOrZero(ILExpression expr)
		{
			if (expr.Code == ILCode.Conv_U || expr.Code == ILCode.Conv_I)
				expr = expr.Arguments[0];
			return (expr.Code == ILCode.Ldc_I4 && (int)expr.Operand == 0) || expr.Code == ILCode.Ldnull;
		}
		
		bool MatchFixedInitializer(ILBlock block, int i, out ILVariable pinnedVar, out ILExpression initValue)
		{
			if (block.Body[i].Match(ILCode.Stloc, out pinnedVar, out initValue)) {
				initValue = (ILExpression)block.Body[i];
				return pinnedVar.IsPinned;
			}
			ILCondition ifStmt = block.Body[i] as ILCondition;
			ILExpression arrayLoadingExpr;
			if (ifStmt != null && MatchFixedArrayInitializerCondition(ifStmt.Condition, out arrayLoadingExpr)) {
				ILVariable arrayVariable = (ILVariable)arrayLoadingExpr.Operand;
				ILExpression trueValue;
				if (ifStmt.TrueBlock != null && ifStmt.TrueBlock.Body.Count == 1
				    && ifStmt.TrueBlock.Body[0].Match(ILCode.Stloc, out pinnedVar, out trueValue)
				    && pinnedVar.IsPinned && IsNullOrZero(trueValue))
				{
					ILVariable stlocVar;
					ILExpression falseValue;
					if (ifStmt.FalseBlock != null && ifStmt.FalseBlock.Body.Count == 1
					    && ifStmt.FalseBlock.Body[0].Match(ILCode.Stloc, out stlocVar, out falseValue) && stlocVar == pinnedVar)
					{
						ILVariable loadedVariable;
						if (falseValue.Code == ILCode.Ldelema
						    && falseValue.Arguments[0].Match(ILCode.Ldloc, out loadedVariable) && loadedVariable == arrayVariable
						    && IsNullOrZero(falseValue.Arguments[1]))
						{
							initValue = new ILExpression(ILCode.Stloc, pinnedVar, arrayLoadingExpr);
							return true;
						}
					}
				}
			}
			initValue = null;
			return false;
		}
		
		bool MatchFixedArrayInitializerCondition(ILExpression condition, out ILExpression initValue)
		{
			ILExpression logicAnd;
			ILVariable arrayVar1, arrayVar2;
			if (condition.Match(ILCode.LogicNot, out logicAnd) && logicAnd.Code == ILCode.LogicAnd) {
				initValue = UnpackDoubleNegation(logicAnd.Arguments[0]);
				if (initValue.Match(ILCode.Ldloc, out arrayVar1)) {
					ILExpression arrayLength = logicAnd.Arguments[1];
					if (arrayLength.Code == ILCode.Conv_I4)
						arrayLength = arrayLength.Arguments[0];
					if (arrayLength.Code == ILCode.Ldlen && arrayLength.Arguments[0].Match(ILCode.Ldloc, out arrayVar2)) {
						return arrayVar1 == arrayVar2;
					}
				}
			}
			initValue = null;
			return false;
		}
		
		ILExpression UnpackDoubleNegation(ILExpression expr)
		{
			ILExpression negated;
			if (expr.Match(ILCode.LogicNot, out negated) && negated.Match(ILCode.LogicNot, out negated))
				return negated;
			else
				return expr;
		}
		
		void HandleStringFixing(ILFixedStatement fixedStatement)
		{
			// fixed (stloc(pinnedVar, ldloc(text))) {
			//   var1 = var2 = conv.i(ldloc(pinnedVar))
			//   if (logicnot(logicnot(var1))) {
			//     var2 = add(var1, call(RuntimeHelpers::get_OffsetToStringData))
			//   }
			//   stloc(ptrVar, var2)
			//   ...
			
			ILVariable pinnedVar = (ILVariable)fixedStatement.Initializer.Operand;
			Debug.Assert(pinnedVar.IsPinned);
			var body = fixedStatement.BodyBlock.Body;
			if (body.Count < 3)
				return;
			
			ILVariable var1, var2;
			ILExpression varAssignment, ptrInitialization;
			if (!(body[0].Match(ILCode.Stloc, out var1, out varAssignment) && varAssignment.Match(ILCode.Stloc, out var2, out ptrInitialization)))
				return;
			if (!(var1.IsGenerated && var2.IsGenerated))
				return;
			if (ptrInitialization.Code == ILCode.Conv_I || ptrInitialization.Code == ILCode.Conv_U)
				ptrInitialization = ptrInitialization.Arguments[0];
			if (!ptrInitialization.MatchLdloc(pinnedVar))
				return;
			
			ILCondition ifStmt = body[1] as ILCondition;
			if (!(ifStmt != null && ifStmt.TrueBlock != null && ifStmt.TrueBlock.Body.Count == 1 && (ifStmt.FalseBlock == null || ifStmt.FalseBlock.Body.Count == 0)))
				return;
			if (!UnpackDoubleNegation(ifStmt.Condition).MatchLdloc(var1))
				return;
			ILVariable assignedVar;
			ILExpression assignedExpr;
			if (!(ifStmt.TrueBlock.Body[0].Match(ILCode.Stloc, out assignedVar, out assignedExpr) && assignedVar == var2 && assignedExpr.Code == ILCode.Add))
				return;
			MethodReference calledMethod;
			if (!(assignedExpr.Arguments[0].MatchLdloc(var1) && assignedExpr.Arguments[1].Match(ILCode.Call, out calledMethod)))
				return;
			if (!(calledMethod.Name == "get_OffsetToStringData" && calledMethod.DeclaringType.FullName == "System.Runtime.CompilerServices.RuntimeHelpers"))
				return;
			
			ILVariable pointerVar;
			if (body[2].Match(ILCode.Stloc, out pointerVar, out assignedExpr) && assignedExpr.MatchLdloc(var2)) {
				body.RemoveRange(0, 3);
				fixedStatement.Initializer.Operand = pointerVar;
			}
		}
		#endregion
	}
}
