// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using ICSharpCode.NRefactory.Utils;
using Mono.Cecil;

namespace ICSharpCode.Decompiler.ILAst
{
	public partial class ILAstOptimizer
	{
		#region TransformDecimalCtorToConstant
		static bool TransformDecimalCtorToConstant(List<ILNode> body, ILExpression expr, int pos)
		{
			MethodReference r;
			List<ILExpression> args;
			if (expr.Match(ILCode.Newobj, out r, out args) &&
			    r.DeclaringType.Namespace == "System" &&
			    r.DeclaringType.Name == "Decimal")
			{
				if (args.Count == 1) {
					int val;
					if (args[0].Match(ILCode.Ldc_I4, out val)) {
						expr.Code = ILCode.Ldc_Decimal;
						expr.Operand = new decimal(val);
						expr.InferredType = r.DeclaringType;
						expr.Arguments.Clear();
						return true;
					}
				} else if (args.Count == 5) {
					int lo, mid, hi, isNegative, scale;
					if (expr.Arguments[0].Match(ILCode.Ldc_I4, out lo) &&
					    expr.Arguments[1].Match(ILCode.Ldc_I4, out mid) &&
					    expr.Arguments[2].Match(ILCode.Ldc_I4, out hi) &&
					    expr.Arguments[3].Match(ILCode.Ldc_I4, out isNegative) &&
					    expr.Arguments[4].Match(ILCode.Ldc_I4, out scale))
					{
						expr.Code = ILCode.Ldc_Decimal;
						expr.Operand = new decimal(lo, mid, hi, isNegative != 0, (byte)scale);
						expr.InferredType = r.DeclaringType;
						expr.Arguments.Clear();
						return true;
					}
				}
			}
			bool modified = false;
			foreach(ILExpression arg in expr.Arguments) {
				modified |= TransformDecimalCtorToConstant(null, arg, -1);
			}
			return modified;
		}
		#endregion
		
		#region SimplifyLdObjAndStObj
		static bool SimplifyLdObjAndStObj(List<ILNode> body, ILExpression expr, int pos)
		{
			bool modified = false;
			expr = SimplifyLdObjAndStObj(expr, ref modified);
			if (modified && body != null)
				body[pos] = expr;
			for (int i = 0; i < expr.Arguments.Count; i++) {
				expr.Arguments[i] = SimplifyLdObjAndStObj(expr.Arguments[i], ref modified);
				modified |= SimplifyLdObjAndStObj(null, expr.Arguments[i], -1);
			}
			return modified;
		}
		
		static ILExpression SimplifyLdObjAndStObj(ILExpression expr, ref bool modified)
		{
			if (expr.Code == ILCode.Initobj) {
				expr.Code = ILCode.Stobj;
				expr.Arguments.Add(new ILExpression(ILCode.DefaultValue, expr.Operand));
				modified = true;
			}
			ILExpression arg, arg2;
			TypeReference type;
			ILCode? newCode = null;
			if (expr.Match(ILCode.Stobj, out type, out arg, out arg2)) {
				switch (arg.Code) {
						case ILCode.Ldelema: newCode = ILCode.Stelem_Any; break;
						case ILCode.Ldloca:  newCode = ILCode.Stloc; break;
						case ILCode.Ldflda:  newCode = ILCode.Stfld; break;
						case ILCode.Ldsflda: newCode = ILCode.Stsfld; break;
				}
			} else if (expr.Match(ILCode.Ldobj, out type, out arg)) {
				switch (arg.Code) {
						case ILCode.Ldelema: newCode = ILCode.Ldelem_Any; break;
						case ILCode.Ldloca:  newCode = ILCode.Ldloc; break;
						case ILCode.Ldflda:  newCode = ILCode.Ldfld; break;
						case ILCode.Ldsflda: newCode = ILCode.Ldsfld; break;
				}
			}
			if (newCode != null) {
				arg.Code = newCode.Value;
				if (expr.Code == ILCode.Stobj) {
					arg.InferredType = expr.InferredType;
					arg.ExpectedType = expr.ExpectedType;
					arg.Arguments.Add(arg2);
				}
				arg.ILRanges.AddRange(expr.ILRanges);
				modified = true;
				return arg;
			} else {
				return expr;
			}
		}
		#endregion
		
		#region SimplifyLdcI4ConvI8
		static bool SimplifyLdcI4ConvI8(List<ILNode> body, ILExpression expr, int pos)
		{
			ILExpression ldc;
			int val;
			if (expr.Match(ILCode.Conv_I8, out ldc) && ldc.Match(ILCode.Ldc_I4, out val)) {
				expr.Code = ILCode.Ldc_I8;
				expr.Operand = (long)val;
				expr.Arguments.Clear();
				return true;
			}
			bool modified = false;
			foreach(ILExpression arg in expr.Arguments) {
				modified |= SimplifyLdcI4ConvI8(null, arg, -1);
			}
			return modified;
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
							i -= new ILInlining(method).InlineInto(block.Body, i, aggressive: true);
							return;
						}
					}
				}
			}
		}
		#endregion
		
		#region MakeAssignmentExpression
		bool MakeAssignmentExpression(List<ILNode> body, ILExpression expr, int pos)
		{
			// exprVar = ...
			// stloc(v, exprVar)
			// ->
			// exprVar = stloc(v, ...))
			ILVariable exprVar;
			ILExpression initializer;
			if (!(expr.Match(ILCode.Stloc, out exprVar, out initializer) && exprVar.IsGenerated))
				return false;
			ILExpression nextExpr = body.ElementAtOrDefault(pos + 1) as ILExpression;
			ILVariable v;
			ILExpression stLocArg;
			if (nextExpr.Match(ILCode.Stloc, out v, out stLocArg) && stLocArg.MatchLdloc(exprVar)) {
				ILExpression store2 = body.ElementAtOrDefault(pos + 2) as ILExpression;
				if (StoreCanBeConvertedToAssignment(store2, exprVar)) {
					// expr_44 = ...
					// stloc(v1, expr_44)
					// anystore(v2, expr_44)
					// ->
					// stloc(v1, anystore(v2, ...))
					ILInlining inlining = new ILInlining(method);
					if (inlining.numLdloc.GetOrDefault(exprVar) == 2 && inlining.numStloc.GetOrDefault(exprVar) == 1) {
						body.RemoveAt(pos + 2); // remove store2
						body.RemoveAt(pos); // remove expr = ...
						nextExpr.Arguments[0] = store2;
						store2.Arguments[store2.Arguments.Count - 1] = initializer;
						
						inlining.InlineIfPossible(body, ref pos);
						
						return true;
					}
				}
				
				body.RemoveAt(pos + 1); // remove stloc
				nextExpr.Arguments[0] = initializer;
				((ILExpression)body[pos]).Arguments[0] = nextExpr;
				return true;
			} else {
				// exprVar = ...
				// stsfld(fld, exprVar)
				// ->
				// exprVar = stsfld(fld, ...))
				FieldReference field;
				if (nextExpr.Match(ILCode.Stsfld, out field, out stLocArg) && stLocArg.MatchLdloc(exprVar)) {
					body.RemoveAt(pos + 1); // remove stfld
					nextExpr.Arguments[0] = initializer;
					((ILExpression)body[pos]).Arguments[0] = nextExpr;
					return true;
				}
			}
			return false;
		}
		
		bool StoreCanBeConvertedToAssignment(ILExpression store, ILVariable exprVar)
		{
			if (store != null && (store.Code == ILCode.Stloc || store.Code == ILCode.Stfld || store.Code == ILCode.Stsfld || IsArrayStore(store.Code))) {
				return store.Arguments.Last().Code == ILCode.Ldloc && store.Arguments.Last().Operand == exprVar;
			}
			return false;
		}
		
		static bool IsArrayStore(ILCode code)
		{
			switch (code) {
				case ILCode.Stelem_Any:
				case ILCode.Stelem_Ref:
				case ILCode.Stelem_I:
				case ILCode.Stelem_I1:
				case ILCode.Stelem_I2:
				case ILCode.Stelem_I4:
				case ILCode.Stelem_I8:
				case ILCode.Stelem_R4:
				case ILCode.Stelem_R8:
					return true;
			}
			return false;
		}
		#endregion
		
		#region MakeCompoundAssignments
		bool MakeCompoundAssignments(List<ILNode> body, ILExpression expr, int pos)
		{
			bool modified = false;
			modified |= MakeCompoundAssignmentForArray(expr);
			modified |= MakeCompoundAssignmentForInstanceField(expr);
			// Static fields and local variables are not handled here - those are expressions without side effects
			// and get handled by ReplaceMethodCallsWithOperators
			// (which does a reversible transform to the short operator form, as the introduction of checked/unchecked might have to revert to the long form).
			foreach (ILExpression arg in expr.Arguments) {
				modified |= MakeCompoundAssignments(null, arg, -1);
			}
			if (modified && body != null)
				new ILInlining(method).InlineInto(body, pos, aggressive: false);
			return modified;
		}
		
		bool MakeCompoundAssignmentForArray(ILExpression expr)
		{
			// stelem.any(..., ldloc(array), ldloc(pos), <OP>(ldelem.any(int32, ldloc(array), ldloc(pos)), <RIGHT>))
			if (!IsArrayStore(expr.Code))
				return false;
			ILVariable arrayVar, posVar;
			if (!(expr.Arguments[0].Match(ILCode.Ldloc, out arrayVar) && expr.Arguments[1].Match(ILCode.Ldloc, out posVar)))
				return false;
			// At least one of the variables must be generated; otherwise we just keep the expanded form.
			if (!(arrayVar.IsGenerated || posVar.IsGenerated))
				return false;
			ILExpression op = expr.Arguments[2];
			if (!CanBeRepresentedAsCompoundAssignment(op.Code))
				return false;
			ILExpression ldelem = op.Arguments[0];
			if (!(ldelem.Code == ILCode.Ldelem_Any && ldelem.Arguments[0].MatchLdloc(arrayVar) && ldelem.Arguments[1].MatchLdloc(posVar)))
				return false;
			expr.Code = ILCode.CompoundAssignment;
			expr.Operand = null;
			expr.Arguments.RemoveRange(0, 2);
			// result is "CompoundAssignment(<OP>(ldelem.any(...), <RIGHT>))"
			return true;
		}
		
		bool MakeCompoundAssignmentForInstanceField(ILExpression expr)
		{
			// stfld(field, expr, <OP>(ldfld(field, expr), <RIGHT>))
			FieldReference field;
			ILExpression firstLoad, op;
			ILVariable exprVar;
			if (!(expr.Match(ILCode.Stfld, out field, out firstLoad, out op) && firstLoad.Match(ILCode.Ldloc, out exprVar) && exprVar.IsGenerated))
				return false;
			if (!CanBeRepresentedAsCompoundAssignment(op.Code))
				return false;
			ILExpression ldfld = op.Arguments[0];
			if (!(ldfld.Code == ILCode.Ldfld && ldfld.Operand == field && ldfld.Arguments[0].MatchLdloc(exprVar)))
				return false;
			expr.Code = ILCode.CompoundAssignment;
			expr.Operand = null;
			expr.Arguments.RemoveAt(0);
			// result is "CompoundAssignment(<OP>(ldfld(...), <RIGHT>))"
			return true;
		}
		
		static bool CanBeRepresentedAsCompoundAssignment(ILCode code)
		{
			switch (code) {
				case ILCode.Add:
				case ILCode.Add_Ovf:
				case ILCode.Add_Ovf_Un:
				case ILCode.Sub:
				case ILCode.Sub_Ovf:
				case ILCode.Sub_Ovf_Un:
				case ILCode.Mul:
				case ILCode.Mul_Ovf:
				case ILCode.Mul_Ovf_Un:
				case ILCode.Div:
				case ILCode.Div_Un:
				case ILCode.Rem:
				case ILCode.Rem_Un:
				case ILCode.And:
				case ILCode.Or:
				case ILCode.Xor:
				case ILCode.Shl:
				case ILCode.Shr:
				case ILCode.Shr_Un:
					return true;
				default:
					return false;
			}
		}
		#endregion
		
		#region IntroduceFixedStatements
		bool IntroduceFixedStatements(List<ILNode> body, int i)
		{
			ILExpression initValue;
			ILVariable pinnedVar;
			int initEndPos;
			if (!MatchFixedInitializer(body, i, out pinnedVar, out initValue, out initEndPos))
				return false;
			
			ILFixedStatement fixedStmt = body.ElementAtOrDefault(initEndPos) as ILFixedStatement;
			if (fixedStmt != null) {
				ILExpression expr = fixedStmt.BodyBlock.Body.LastOrDefault() as ILExpression;
				if (expr != null && expr.Code == ILCode.Stloc && expr.Operand == pinnedVar && IsNullOrZero(expr.Arguments[0])) {
					// we found a second initializer for the existing fixed statement
					fixedStmt.Initializers.Insert(0, initValue);
					body.RemoveRange(i, initEndPos - i);
					fixedStmt.BodyBlock.Body.RemoveAt(fixedStmt.BodyBlock.Body.Count - 1);
					if (pinnedVar.Type.IsByReference)
						pinnedVar.Type = new PointerType(((ByReferenceType)pinnedVar.Type).ElementType);
					return true;
				}
			}
			
			// find where pinnedVar is reset to 0:
			int j;
			for (j = initEndPos; j < body.Count; j++) {
				ILVariable v2;
				ILExpression storedVal;
				// stloc(pinned_Var, conv.u(ldc.i4(0)))
				if (body[j].Match(ILCode.Stloc, out v2, out storedVal) && v2 == pinnedVar) {
					if (IsNullOrZero(storedVal)) {
						break;
					}
				}
			}
			// Create fixed statement from i to j
			fixedStmt = new ILFixedStatement();
			fixedStmt.Initializers.Add(initValue);
			fixedStmt.BodyBlock = new ILBlock(body.GetRange(initEndPos, j - initEndPos)); // from initEndPos to j-1 (inclusive)
			body.RemoveRange(i + 1, Math.Min(j, body.Count - 1) - i); // from i+1 to j (inclusive)
			body[i] = fixedStmt;
			if (pinnedVar.Type.IsByReference)
				pinnedVar.Type = new PointerType(((ByReferenceType)pinnedVar.Type).ElementType);
			
			return true;
		}
		
		bool IsNullOrZero(ILExpression expr)
		{
			if (expr.Code == ILCode.Conv_U || expr.Code == ILCode.Conv_I)
				expr = expr.Arguments[0];
			return (expr.Code == ILCode.Ldc_I4 && (int)expr.Operand == 0) || expr.Code == ILCode.Ldnull;
		}
		
		bool MatchFixedInitializer(List<ILNode> body, int i, out ILVariable pinnedVar, out ILExpression initValue, out int nextPos)
		{
			if (body[i].Match(ILCode.Stloc, out pinnedVar, out initValue) && pinnedVar.IsPinned && !IsNullOrZero(initValue)) {
				initValue = (ILExpression)body[i];
				nextPos = i + 1;
				HandleStringFixing(pinnedVar, body, ref nextPos, ref initValue);
				return true;
			}
			ILCondition ifStmt = body[i] as ILCondition;
			ILExpression arrayLoadingExpr;
			if (ifStmt != null && MatchFixedArrayInitializerCondition(ifStmt.Condition, out arrayLoadingExpr)) {
				ILVariable arrayVariable = (ILVariable)arrayLoadingExpr.Operand;
				ILExpression trueValue;
				if (ifStmt.TrueBlock != null && ifStmt.TrueBlock.Body.Count == 1
				    && ifStmt.TrueBlock.Body[0].Match(ILCode.Stloc, out pinnedVar, out trueValue)
				    && pinnedVar.IsPinned && IsNullOrZero(trueValue))
				{
					if (ifStmt.FalseBlock != null && ifStmt.FalseBlock.Body.Count == 1 && ifStmt.FalseBlock.Body[0] is ILFixedStatement) {
						ILFixedStatement fixedStmt = (ILFixedStatement)ifStmt.FalseBlock.Body[0];
						ILVariable stlocVar;
						ILExpression falseValue;
						if (fixedStmt.Initializers.Count == 1 && fixedStmt.BodyBlock.Body.Count == 0
						    && fixedStmt.Initializers[0].Match(ILCode.Stloc, out stlocVar, out falseValue) && stlocVar == pinnedVar)
						{
							ILVariable loadedVariable;
							if (falseValue.Code == ILCode.Ldelema
							    && falseValue.Arguments[0].Match(ILCode.Ldloc, out loadedVariable) && loadedVariable == arrayVariable
							    && IsNullOrZero(falseValue.Arguments[1]))
							{
								initValue = new ILExpression(ILCode.Stloc, pinnedVar, arrayLoadingExpr);
								nextPos = i + 1;
								return true;
							}
						}
					}
				}
			}
			initValue = null;
			nextPos = -1;
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
		
		bool HandleStringFixing(ILVariable pinnedVar, List<ILNode> body, ref int pos, ref ILExpression fixedStmtInitializer)
		{
			// fixed (stloc(pinnedVar, ldloc(text))) {
			//   var1 = var2 = conv.i(ldloc(pinnedVar))
			//   if (logicnot(logicnot(var1))) {
			//     var2 = add(var1, call(RuntimeHelpers::get_OffsetToStringData))
			//   }
			//   stloc(ptrVar, var2)
			//   ...
			
			if (pos >= body.Count)
				return false;
			
			ILVariable var1, var2;
			ILExpression varAssignment, ptrInitialization;
			if (!(body[pos].Match(ILCode.Stloc, out var1, out varAssignment) && varAssignment.Match(ILCode.Stloc, out var2, out ptrInitialization)))
				return false;
			if (!(var1.IsGenerated && var2.IsGenerated))
				return false;
			if (ptrInitialization.Code == ILCode.Conv_I || ptrInitialization.Code == ILCode.Conv_U)
				ptrInitialization = ptrInitialization.Arguments[0];
			if (!ptrInitialization.MatchLdloc(pinnedVar))
				return false;
			
			ILCondition ifStmt = body[pos + 1] as ILCondition;
			if (!(ifStmt != null && ifStmt.TrueBlock != null && ifStmt.TrueBlock.Body.Count == 1 && (ifStmt.FalseBlock == null || ifStmt.FalseBlock.Body.Count == 0)))
				return false;
			if (!UnpackDoubleNegation(ifStmt.Condition).MatchLdloc(var1))
				return false;
			ILVariable assignedVar;
			ILExpression assignedExpr;
			if (!(ifStmt.TrueBlock.Body[0].Match(ILCode.Stloc, out assignedVar, out assignedExpr) && assignedVar == var2 && assignedExpr.Code == ILCode.Add))
				return false;
			MethodReference calledMethod;
			if (!(assignedExpr.Arguments[0].MatchLdloc(var1) && assignedExpr.Arguments[1].Match(ILCode.Call, out calledMethod)))
				return false;
			if (!(calledMethod.Name == "get_OffsetToStringData" && calledMethod.DeclaringType.FullName == "System.Runtime.CompilerServices.RuntimeHelpers"))
				return false;
			
			ILVariable pointerVar;
			if (body[pos + 2].Match(ILCode.Stloc, out pointerVar, out assignedExpr) && assignedExpr.MatchLdloc(var2)) {
				pos += 3;
				fixedStmtInitializer.Operand = pointerVar;
				return true;
			}
			return false;
		}
		#endregion
	}
}
