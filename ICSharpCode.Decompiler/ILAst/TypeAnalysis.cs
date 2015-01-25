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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace ICSharpCode.Decompiler.ILAst
{
	/// <summary>
	/// Assigns C# types to IL expressions.
	/// </summary>
	/// <remarks>
	/// Types are inferred in a bidirectional manner:
	/// The expected type flows from the outside to the inside, the actual inferred type flows from the inside to the outside.
	/// </remarks>
	public class TypeAnalysis
	{
		public static void Run(DecompilerContext context, ILBlock method)
		{
			TypeAnalysis ta = new TypeAnalysis();
			ta.context = context;
			ta.module = context.CurrentMethod.Module;
			ta.corLib = ta.module.CorLibTypes;
			ta.method = method;
			ta.CreateDependencyGraph(method);
			ta.IdentifySingleLoadVariables();
			ta.RunInference();
		}
		
		sealed class ExpressionToInfer
		{
			public ILExpression Expression;
			
			public bool Done;
			
			/// <summary>
			/// Set for assignment expressions that should wait until the variable type is available
			/// from the context where the variable is used.
			/// </summary>
			public ILVariable DependsOnSingleLoad;
			
			/// <summary>
			/// The list variables that are read by this expression.
			/// </summary>
			public List<ILVariable> Dependencies = new List<ILVariable>();
			
			public override string ToString()
			{
				if (Done)
					return "[Done] " + Expression.ToString();
				else
					return Expression.ToString();
			}

		}
		
		DecompilerContext context;
		ICorLibTypes corLib;
		ILBlock method;
		ModuleDef module;
		List<ExpressionToInfer> allExpressions = new List<ExpressionToInfer>();
		DefaultDictionary<ILVariable, List<ExpressionToInfer>> assignmentExpressions = new DefaultDictionary<ILVariable, List<ExpressionToInfer>>(_ => new List<ExpressionToInfer>());
		HashSet<ILVariable> singleLoadVariables = new HashSet<ILVariable>();
		
		#region CreateDependencyGraph
		/// <summary>
		/// Creates the "ExpressionToInfer" instances (=nodes in dependency graph)
		/// </summary>
		/// <remarks>
		/// We are using a dependency graph to ensure that expressions are analyzed in the correct order.
		/// </remarks>
		void CreateDependencyGraph(ILNode node)
		{
			ILCondition cond = node as ILCondition;
			if (cond != null) {
				cond.Condition.ExpectedType = corLib.Boolean;
			}
			ILWhileLoop loop = node as ILWhileLoop;
			if (loop != null && loop.Condition != null) {
				loop.Condition.ExpectedType = corLib.Boolean;
			}
			ILTryCatchBlock.CatchBlock catchBlock = node as ILTryCatchBlock.CatchBlock;
			if (catchBlock != null && catchBlock.ExceptionVariable != null && catchBlock.ExceptionType != null && catchBlock.ExceptionVariable.Type == null) {
				catchBlock.ExceptionVariable.Type = catchBlock.ExceptionType;
			}
			ILExpression expr = node as ILExpression;
			if (expr != null) {
				ExpressionToInfer expressionToInfer = new ExpressionToInfer();
				expressionToInfer.Expression = expr;
				allExpressions.Add(expressionToInfer);
				FindNestedAssignments(expr, expressionToInfer);
				
				if (expr.Code == ILCode.Stloc && ((ILVariable)expr.Operand).Type == null)
					assignmentExpressions[(ILVariable)expr.Operand].Add(expressionToInfer);
				return;
			}
			foreach (ILNode child in node.GetChildren()) {
				CreateDependencyGraph(child);
			}
		}
		
		void FindNestedAssignments(ILExpression expr, ExpressionToInfer parent)
		{
			foreach (ILExpression arg in expr.Arguments) {
				if (arg.Code == ILCode.Stloc) {
					ExpressionToInfer expressionToInfer = new ExpressionToInfer();
					expressionToInfer.Expression = arg;
					allExpressions.Add(expressionToInfer);
					FindNestedAssignments(arg, expressionToInfer);
					ILVariable v = (ILVariable)arg.Operand;
					if (v.Type == null) {
						assignmentExpressions[v].Add(expressionToInfer);
						// the instruction that consumes the stloc result is handled as if it was reading the variable
						parent.Dependencies.Add(v);
					}
				} else {
					ILVariable v;
					if (arg.Match(ILCode.Ldloc, out v) && v.Type == null) {
						parent.Dependencies.Add(v);
					}
					FindNestedAssignments(arg, parent);
				}
			}
		}
		#endregion
		
		void IdentifySingleLoadVariables()
		{
			// Find all variables that are assigned to exactly a single time:
			var q = from expr in allExpressions
				from v in expr.Dependencies
				group expr by v;
			foreach (var g in q.ToArray()) {
				ILVariable v = g.Key;
				if (g.Count() == 1 && g.Single().Expression.GetSelfAndChildrenRecursive<ILExpression>().Count(e => e.Operand == v) == 1) {
					singleLoadVariables.Add(v);
					// Mark the assignments as dependent on the type from the single load:
					foreach (var assignment in assignmentExpressions[v]) {
						assignment.DependsOnSingleLoad = v;
					}
				}
			}
		}
		
		void RunInference()
		{
			int numberOfExpressionsAlreadyInferred = 0;
			// Two flags that allow resolving cycles:
			bool ignoreSingleLoadDependencies = false;
			bool assignVariableTypesBasedOnPartialInformation = false;
			while (numberOfExpressionsAlreadyInferred < allExpressions.Count) {
				int oldCount = numberOfExpressionsAlreadyInferred;
				foreach (ExpressionToInfer expr in allExpressions) {
					if (!expr.Done && expr.Dependencies.TrueForAll(v => v.Type != null || singleLoadVariables.Contains(v))
					    && (expr.DependsOnSingleLoad == null || expr.DependsOnSingleLoad.Type != null || ignoreSingleLoadDependencies))
					{
						RunInference(expr.Expression);
						expr.Done = true;
						numberOfExpressionsAlreadyInferred++;
					}
				}
				if (numberOfExpressionsAlreadyInferred == oldCount) {
					if (ignoreSingleLoadDependencies) {
						if (assignVariableTypesBasedOnPartialInformation) {
							//throw new InvalidOperationException("Could not infer any expression");
							//TODO: HACK so .NET Native (2014-12-19) System.IO.WinRTFileSystem.OpenAsync() doesn't throw
							numberOfExpressionsAlreadyInferred++;
						}
						else
							assignVariableTypesBasedOnPartialInformation = true;
					} else {
						// We have a cyclic dependency; we'll try if we can resolve it by ignoring single-load dependencies.
						// This can happen if the variable was not actually assigned an expected type by the single-load instruction.
						ignoreSingleLoadDependencies = true;
						continue;
					}
				} else {
					assignVariableTypesBasedOnPartialInformation = false;
					ignoreSingleLoadDependencies = false;
				}
				// Now infer types for variables:
				foreach (var pair in assignmentExpressions) {
					ILVariable v = pair.Key;
					if (v.Type == null && (assignVariableTypesBasedOnPartialInformation ? pair.Value.Any(e => e.Done) : pair.Value.All(e => e.Done))) {
						TypeSig inferredType = null;
						foreach (ExpressionToInfer expr in pair.Value) {
							Debug.Assert(expr.Expression.Code == ILCode.Stloc);
							ILExpression assignedValue = expr.Expression.Arguments.Single();
							if (assignedValue.InferredType != null) {
								if (inferredType == null) {
									inferredType = assignedValue.InferredType;
								} else {
									// pick the common base type
									inferredType = TypeWithMoreInformation(inferredType, assignedValue.InferredType);
								}
							}
						}
						if (inferredType == null)
							inferredType = corLib.Object;
						v.Type = inferredType;
						// Assign inferred type to all the assignments (in case they used different inferred types):
						foreach (ExpressionToInfer expr in pair.Value) {
							expr.Expression.InferredType = inferredType;
							// re-infer if the expected type has changed
							InferTypeForExpression(expr.Expression.Arguments.Single(), inferredType);
						}
					}
				}
			}
		}
		
		void RunInference(ILExpression expr)
		{
			bool anyArgumentIsMissingExpectedType = expr.Arguments.Any(a => a.ExpectedType == null);
			if (expr.InferredType == null || anyArgumentIsMissingExpectedType)
				InferTypeForExpression(expr, expr.ExpectedType, forceInferChildren: anyArgumentIsMissingExpectedType);
			foreach (var arg in expr.Arguments) {
				if (arg.Code != ILCode.Stloc) {
					RunInference(arg);
				}
			}
		}
		
		/// <summary>
		/// Infers the C# type of <paramref name="expr"/>.
		/// </summary>
		/// <param name="expr">The expression</param>
		/// <param name="expectedType">The expected type of the expression</param>
		/// <param name="forceInferChildren">Whether direct children should be inferred even if its not necessary. (does not apply to nested children!)</param>
		/// <returns>The inferred type</returns>
		TypeSig InferTypeForExpression(ILExpression expr, TypeSig expectedType, bool forceInferChildren = false)
		{
			if (expectedType != null && !IsSameType(expr.ExpectedType, expectedType)) {
				expr.ExpectedType = expectedType;
				if (expr.Code != ILCode.Stloc) // stloc is special case and never gets re-evaluated
					forceInferChildren = true;
			}
			if (forceInferChildren || expr.InferredType == null)
				expr.InferredType = DoInferTypeForExpression(expr, expectedType, forceInferChildren);
			return expr.InferredType;
		}
		
		TypeSig DoInferTypeForExpression(ILExpression expr, TypeSig expectedType, bool forceInferChildren = false)
		{
			switch (expr.Code) {
					#region Logical operators
				case ILCode.LogicNot:
					if (forceInferChildren) {
						InferTypeForExpression(expr.Arguments.Single(), corLib.Boolean);
					}
					return corLib.Boolean;
				case ILCode.LogicAnd:
				case ILCode.LogicOr:
					// if Operand is set the logic and/or expression is a custom operator
					// we can deal with it the same as a normal invocation.
					if (expr.Operand != null)
						goto case ILCode.Call;
					if (forceInferChildren) {
						InferTypeForExpression(expr.Arguments[0], corLib.Boolean);
						InferTypeForExpression(expr.Arguments[1], corLib.Boolean);
					}
					return corLib.Boolean;
				case ILCode.TernaryOp:
					if (forceInferChildren) {
						InferTypeForExpression(expr.Arguments[0], corLib.Boolean);
					}
					return InferBinaryArguments(expr.Arguments[1], expr.Arguments[2], expectedType, forceInferChildren);
				case ILCode.NullCoalescing:
					return InferBinaryArguments(expr.Arguments[0], expr.Arguments[1], expectedType, forceInferChildren);
					#endregion
					#region Variable load/store
				case ILCode.Stloc:
					{
						ILVariable v = (ILVariable)expr.Operand;
						if (forceInferChildren) {
							// do not use 'expectedType' in here!
							InferTypeForExpression(expr.Arguments.Single(), v.Type);
						}
						return v.Type;
					}
				case ILCode.Ldloc:
					{
						ILVariable v = (ILVariable)expr.Operand;
						if (v.Type == null && singleLoadVariables.Contains(v)) {
							v.Type = expectedType;
						}
						return v.Type;
					}
				case ILCode.Ldloca:
					{
						ILVariable v = (ILVariable)expr.Operand;
						if (v.Type != null)
							return new ByRefSig(v.Type);
						else
							return null;
					}
					#endregion
					#region Call / NewObj
				case ILCode.Call:
				case ILCode.Callvirt:
				case ILCode.CallGetter:
				case ILCode.CallvirtGetter:
				case ILCode.CallSetter:
				case ILCode.CallvirtSetter:
					{
						IMethod method = (IMethod)expr.Operand;
						var parameters = method.MethodSig.GetParameters();
						if (forceInferChildren) {
							for (int i = 0; i < expr.Arguments.Count; i++) {
								if (i == 0 && method.MethodSig.HasThis) {
									InferTypeForExpression(expr.Arguments[0], MakeRefIfValueType(method.DeclaringType.ToTypeSigInternal(), expr.GetPrefix(ILCode.Constrained)));
								} else {
									InferTypeForExpression(expr.Arguments[i], SubstituteTypeArgs(parameters[method.MethodSig.HasThis ? i - 1 : i], method: method));
								}
							}
						}
						if (expr.Code == ILCode.CallSetter || expr.Code == ILCode.CallvirtSetter) {
							return SubstituteTypeArgs(method.MethodSig.GetParameters().Last(), method: method);
						} else {
							return SubstituteTypeArgs(method.MethodSig.RetType, method: method);
						}
					}
				case ILCode.Newobj:
					{
						IMethod ctor = (IMethod)expr.Operand;
						if (forceInferChildren) {
							var parameters = ctor.MethodSig.GetParameters();
							for (int i = 0; i < parameters.Count; i++) {
								InferTypeForExpression(expr.Arguments[i], SubstituteTypeArgs(parameters[i], null, ctor));
							}
						}
						return ctor.DeclaringType.ToTypeSigInternal();
					}
				case ILCode.InitObject:
				case ILCode.InitCollection:
					return InferTypeForExpression(expr.Arguments[0], expectedType);
				case ILCode.InitializedObject:
					// expectedType should always be known due to the parent method call / property setter
					Debug.Assert(expectedType != null);
					return expectedType;
					#endregion
					#region Load/Store Fields
				case ILCode.Ldfld:
					if (forceInferChildren) {
						InferTypeForExpression(expr.Arguments[0], MakeRefIfValueType(((IField)expr.Operand).DeclaringType.ToTypeSigInternal(), expr.GetPrefix(ILCode.Constrained)));
					}
					return GetFieldType((IField)expr.Operand);
				case ILCode.Ldsfld:
					return GetFieldType((IField)expr.Operand);
				case ILCode.Ldflda:
					if (forceInferChildren) {
						InferTypeForExpression(expr.Arguments[0], MakeRefIfValueType(((IField)expr.Operand).DeclaringType.ToTypeSigInternal(), expr.GetPrefix(ILCode.Constrained)));
					}
					return new ByRefSig(GetFieldType((IField)expr.Operand));
				case ILCode.Ldsflda:
					return new ByRefSig(GetFieldType((IField)expr.Operand));
				case ILCode.Stfld:
					if (forceInferChildren) {
						InferTypeForExpression(expr.Arguments[0], MakeRefIfValueType(((IField)expr.Operand).DeclaringType.ToTypeSigInternal(), expr.GetPrefix(ILCode.Constrained)));
						InferTypeForExpression(expr.Arguments[1], GetFieldType((IField)expr.Operand));
					}
					return GetFieldType((IField)expr.Operand);
				case ILCode.Stsfld:
					if (forceInferChildren)
						InferTypeForExpression(expr.Arguments[0], GetFieldType((IField)expr.Operand));
					return GetFieldType((IField)expr.Operand);
					#endregion
					#region Reference/Pointer instructions
				case ILCode.Ldind_Ref:
					return UnpackPointer(InferTypeForExpression(expr.Arguments[0], null));
				case ILCode.Stind_Ref:
					if (forceInferChildren) {
						TypeSig elementType = UnpackPointer(InferTypeForExpression(expr.Arguments[0], null));
						InferTypeForExpression(expr.Arguments[1], elementType);
					}
					return null;
				case ILCode.Ldobj:
					{
						TypeSig type = ((ITypeDefOrRef)expr.Operand).ToTypeSigInternal();
						var argType = InferTypeForExpression(expr.Arguments[0], null);
						if (argType is PtrSig || argType is ByRefSig) {
							var elementType = argType.Next;
							int infoAmount = GetInformationAmount(elementType);
							if (infoAmount == 1 && GetInformationAmount(type) == 8) {
								// A bool can be loaded from both bytes and sbytes.
								type = elementType;
							}
							if (infoAmount >= 8 && infoAmount <= 64 && infoAmount == GetInformationAmount(type)) {
								// An integer can be loaded as another integer of the same size.
								// For integers smaller than 32 bit, the signs must match (as loading performs sign extension)
								bool? elementTypeIsSigned = IsSigned(elementType);
								bool? typeIsSigned = IsSigned(type);
								if (elementTypeIsSigned != null && typeIsSigned != null) {
									if (infoAmount >= 32 || elementTypeIsSigned == typeIsSigned)
										type = elementType;
								}
							}
						}
						if (argType is PtrSig)
							InferTypeForExpression(expr.Arguments[0], new PtrSig(type));
						else
							InferTypeForExpression(expr.Arguments[0], new ByRefSig(type));
						return type;
					}
				case ILCode.Stobj:
					{
						TypeSig operandType = ((ITypeDefOrRef)expr.Operand).ToTypeSigInternal();
						TypeSig pointerType = InferTypeForExpression(expr.Arguments[0], new ByRefSig(operandType));
						TypeSig elementType;
						if (pointerType is PtrSig)
							elementType = ((PtrSig)pointerType).Next;
						else if (pointerType is ByRefSig)
							elementType = ((ByRefSig)pointerType).Next;
						else
							elementType = null;
						if (elementType != null) {
							// An integer can be stored in any other integer of the same size.
							int infoAmount = GetInformationAmount(elementType);
							if (infoAmount == 1 && GetInformationAmount(operandType) == 8)
								operandType = elementType;
							else if (infoAmount == GetInformationAmount(operandType) && IsSigned(elementType) != null && IsSigned(operandType) != null)
								operandType = elementType;
						}
						if (forceInferChildren) {
							if (pointerType is PtrSig)
								InferTypeForExpression(expr.Arguments[0], new PtrSig(operandType));
							else if (!IsSameType(operandType, (expr.Operand as ITypeDefOrRef).ToTypeSigInternal()))
								InferTypeForExpression(expr.Arguments[0], new ByRefSig(operandType));
							InferTypeForExpression(expr.Arguments[1], operandType);
						}
						return operandType;
					}
				case ILCode.Initobj:
					return null;
				case ILCode.DefaultValue:
					return ((ITypeDefOrRef)expr.Operand).ToTypeSigInternal();
				case ILCode.Localloc:
					if (forceInferChildren) {
						InferTypeForExpression(expr.Arguments[0], corLib.Int32);
					}
					if (expectedType is PtrSig)
						return expectedType;
					else
						return corLib.IntPtr;
				case ILCode.Sizeof:
					return corLib.Int32;
				case ILCode.PostIncrement:
				case ILCode.PostIncrement_Ovf:
				case ILCode.PostIncrement_Ovf_Un:
					{
						TypeSig elementType = UnpackPointer(InferTypeForExpression(expr.Arguments[0], null));
						if (forceInferChildren && elementType != null) {
							// Assign expected type to the child expression
							InferTypeForExpression(expr.Arguments[0], new ByRefSig(elementType));
						}
						return elementType;
					}
				case ILCode.Mkrefany:
					if (forceInferChildren) {
						InferTypeForExpression(expr.Arguments[0], ((ITypeDefOrRef)expr.Operand).ToTypeSigInternal());
					}
					return corLib.TypedReference;
				case ILCode.Refanytype:
					if (forceInferChildren) {
						InferTypeForExpression(expr.Arguments[0], corLib.TypedReference);
					}
					return corLib.GetTypeRef("System", "RuntimeTypeHandle").ToTypeSigInternal();
				case ILCode.Refanyval:
					if (forceInferChildren) {
						InferTypeForExpression(expr.Arguments[0], corLib.TypedReference);
					}
					return new ByRefSig(((ITypeDefOrRef)expr.Operand).ToTypeSigInternal());
				case ILCode.AddressOf:
					{
						TypeSig t = InferTypeForExpression(expr.Arguments[0], UnpackPointer(expectedType));
						return t != null ? new ByRefSig(t) : null;
					}
				case ILCode.ValueOf:
					return GetNullableTypeArgument(InferTypeForExpression(expr.Arguments[0], CreateNullableType(expectedType)));
				case ILCode.NullableOf:
					return CreateNullableType(InferTypeForExpression(expr.Arguments[0], GetNullableTypeArgument(expectedType)));
					#endregion
					#region Arithmetic instructions
				case ILCode.Not: // bitwise complement
				case ILCode.Neg:
					return InferTypeForExpression(expr.Arguments.Single(), expectedType);
				case ILCode.Add:
					return InferArgumentsInAddition(expr, null, expectedType);
				case ILCode.Sub:
					return InferArgumentsInSubtraction(expr, null, expectedType);
				case ILCode.Mul:
				case ILCode.Or:
				case ILCode.And:
				case ILCode.Xor:
					return InferArgumentsInBinaryOperator(expr, null, expectedType);
				case ILCode.Add_Ovf:
					return InferArgumentsInAddition(expr, true, expectedType);
				case ILCode.Sub_Ovf:
					return InferArgumentsInSubtraction(expr, true, expectedType);
				case ILCode.Mul_Ovf:
				case ILCode.Div:
				case ILCode.Rem:
					return InferArgumentsInBinaryOperator(expr, true, expectedType);
				case ILCode.Add_Ovf_Un:
					return InferArgumentsInAddition(expr, false, expectedType);
				case ILCode.Sub_Ovf_Un:
					return InferArgumentsInSubtraction(expr, false, expectedType);
				case ILCode.Mul_Ovf_Un:
				case ILCode.Div_Un:
				case ILCode.Rem_Un:
					return InferArgumentsInBinaryOperator(expr, false, expectedType);
				case ILCode.Shl:
					if (forceInferChildren)
						InferTypeForExpression(expr.Arguments[1], corLib.Int32);
					if (expectedType != null && (
						expectedType.ElementType == ElementType.I4 || expectedType.ElementType == ElementType.U4 ||
						expectedType.ElementType == ElementType.I8 || expectedType.ElementType == ElementType.U8)
					   )
						return NumericPromotion(InferTypeForExpression(expr.Arguments[0], expectedType));
					else
						return NumericPromotion(InferTypeForExpression(expr.Arguments[0], null));
				case ILCode.Shr:
				case ILCode.Shr_Un:
					{
						if (forceInferChildren)
							InferTypeForExpression(expr.Arguments[1], corLib.Int32);
						TypeSig type = NumericPromotion(InferTypeForExpression(expr.Arguments[0], null));
						if (type == null)
							return null;
						TypeSig expectedInputType = null;
						switch (type.GetElementType()) {
							case ElementType.I4:
								if (expr.Code == ILCode.Shr_Un)
									expectedInputType = corLib.UInt32;
								break;
							case ElementType.U4:
								if (expr.Code == ILCode.Shr)
									expectedInputType = corLib.Int32;
								break;
							case ElementType.I8:
								if (expr.Code == ILCode.Shr_Un)
									expectedInputType = corLib.UInt64;
								break;
							case ElementType.U8:
								if (expr.Code == ILCode.Shr)
									expectedInputType = corLib.UInt64;
								break;
						}
						if (expectedInputType != null) {
							InferTypeForExpression(expr.Arguments[0], expectedInputType);
							return expectedInputType;
						} else {
							return type;
						}
					}
				case ILCode.CompoundAssignment:
					{
						var op = expr.Arguments[0];
						if (op.Code == ILCode.NullableOf) op = op.Arguments[0].Arguments[0];
						var varType = InferTypeForExpression(op.Arguments[0], null);
						if (forceInferChildren) {
							InferTypeForExpression(expr.Arguments[0], varType);
						}
						return varType;
					}
					#endregion
					#region Constant loading instructions
				case ILCode.Ldnull:
					return corLib.Object;
				case ILCode.Ldstr:
					return corLib.String;
				case ILCode.Ldftn:
				case ILCode.Ldvirtftn:
					return corLib.IntPtr;
				case ILCode.Ldc_I4:
					if (expectedType.GetElementType() == ElementType.Boolean && ((int)expr.Operand == 0 || (int)expr.Operand == 1))
						return corLib.Boolean;
					if (expectedType is PtrSig && (int)expr.Operand == 0)
						return expectedType;
					if (IsIntegerOrEnum(expectedType) && OperandFitsInType(expectedType, (int)expr.Operand))
						return expectedType;
					else
						return corLib.Int32;
				case ILCode.Ldc_I8:
					if (expectedType is PtrSig && (long)expr.Operand == 0)
						return expectedType;
					if (IsIntegerOrEnum(expectedType) && GetInformationAmount(expectedType) >= NativeInt)
						return expectedType;
					else
						return corLib.Int64;
				case ILCode.Ldc_R4:
					return corLib.Single;
				case ILCode.Ldc_R8:
					return corLib.Double;
				case ILCode.Ldc_Decimal:
					return corLib.GetTypeRef("System", "Decimal").ToTypeSigInternal();
				case ILCode.Ldtoken:
					if (expr.Operand is ITypeDefOrRef)
						return corLib.GetTypeRef("System", "RuntimeTypeHandle").ToTypeSigInternal();
					else if (expr.Operand is IField && ((IField)expr.Operand).FieldSig != null)
						return corLib.GetTypeRef("System", "RuntimeFieldHandle").ToTypeSigInternal();
					else
						return corLib.GetTypeRef("System", "RuntimeMethodHandle").ToTypeSigInternal();
				case ILCode.Arglist:
					return corLib.GetTypeRef("System", "RuntimeArgumentHandle").ToTypeSigInternal();
					#endregion
					#region Array instructions
				case ILCode.Newarr:
					if (forceInferChildren) {
						var lengthType = InferTypeForExpression(expr.Arguments.Single(), null);
						if (lengthType == corLib.IntPtr) {
							lengthType = corLib.Int64;
						} else if (lengthType == corLib.UIntPtr) {
							lengthType = corLib.UInt64;
						} else if (lengthType != corLib.UInt32 && lengthType != corLib.Int64 && lengthType != corLib.UInt64) {
							lengthType = corLib.Int32;
						}
						if (forceInferChildren) {
							InferTypeForExpression(expr.Arguments.Single(), lengthType);
						}
					}
					return new SZArraySig(((ITypeDefOrRef)expr.Operand).ToTypeSigInternal());
				case ILCode.InitArray:
					var operandSig = ((ITypeDefOrRef)expr.Operand).ToTypeSigInternal();
					if (forceInferChildren)
					{
						foreach (ILExpression arg in expr.Arguments)
							InferTypeForExpression(arg, operandSig.Next);
					}
					return operandSig;
				case ILCode.Ldlen:
					return corLib.Int32;
				case ILCode.Ldelem_U1:
				case ILCode.Ldelem_U2:
				case ILCode.Ldelem_U4:
				case ILCode.Ldelem_I1:
				case ILCode.Ldelem_I2:
				case ILCode.Ldelem_I4:
				case ILCode.Ldelem_I8:
				case ILCode.Ldelem_R4:
				case ILCode.Ldelem_R8:
				case ILCode.Ldelem_I:
				case ILCode.Ldelem_Ref:
					{
						SZArraySig arrayType = InferTypeForExpression(expr.Arguments[0], null) as SZArraySig;
						if (forceInferChildren) {
							InferTypeForExpression(expr.Arguments[1], corLib.Int32);
						}
						return arrayType != null ? arrayType.Next : null;
					}
				case ILCode.Ldelem:
					if (forceInferChildren) {
						InferTypeForExpression(expr.Arguments[1], corLib.Int32);
					}
					return ((ITypeDefOrRef)expr.Operand).ToTypeSigInternal();
				case ILCode.Ldelema:
					{
						SZArraySig arrayType = InferTypeForExpression(expr.Arguments[0], null) as SZArraySig;
						if (forceInferChildren)
							InferTypeForExpression(expr.Arguments[1], corLib.Int32);
						return arrayType != null ? new ByRefSig(arrayType.Next) : null;
					}
				case ILCode.Stelem_I:
				case ILCode.Stelem_I1:
				case ILCode.Stelem_I2:
				case ILCode.Stelem_I4:
				case ILCode.Stelem_I8:
				case ILCode.Stelem_R4:
				case ILCode.Stelem_R8:
				case ILCode.Stelem_Ref:
				case ILCode.Stelem:
					{
						SZArraySig arrayType = InferTypeForExpression(expr.Arguments[0], null) as SZArraySig;
						if (forceInferChildren) {
							InferTypeForExpression(expr.Arguments[1], corLib.Int32);
							if (arrayType != null) {
								InferTypeForExpression(expr.Arguments[2], arrayType.Next);
							}
						}
						return arrayType != null ? arrayType.Next : null;
					}
					#endregion
					#region Conversion instructions
				case ILCode.Conv_I1:
				case ILCode.Conv_Ovf_I1:
				case ILCode.Conv_Ovf_I1_Un:
					return HandleConversion(8, true, expr.Arguments[0], expectedType, corLib.SByte);
				case ILCode.Conv_I2:
				case ILCode.Conv_Ovf_I2:
				case ILCode.Conv_Ovf_I2_Un:
					return HandleConversion(16, true, expr.Arguments[0], expectedType, corLib.Int16);
				case ILCode.Conv_I4:
				case ILCode.Conv_Ovf_I4:
				case ILCode.Conv_Ovf_I4_Un:
					return HandleConversion(32, true, expr.Arguments[0], expectedType, corLib.Int32);
				case ILCode.Conv_I8:
				case ILCode.Conv_Ovf_I8:
				case ILCode.Conv_Ovf_I8_Un:
					return HandleConversion(64, true, expr.Arguments[0], expectedType, corLib.Int64);
				case ILCode.Conv_U1:
				case ILCode.Conv_Ovf_U1:
				case ILCode.Conv_Ovf_U1_Un:
					return HandleConversion(8, false, expr.Arguments[0], expectedType, corLib.Byte);
				case ILCode.Conv_U2:
				case ILCode.Conv_Ovf_U2:
				case ILCode.Conv_Ovf_U2_Un:
					return HandleConversion(16, false, expr.Arguments[0], expectedType, corLib.UInt16);
				case ILCode.Conv_U4:
				case ILCode.Conv_Ovf_U4:
				case ILCode.Conv_Ovf_U4_Un:
					return HandleConversion(32, false, expr.Arguments[0], expectedType, corLib.UInt32);
				case ILCode.Conv_U8:
				case ILCode.Conv_Ovf_U8:
				case ILCode.Conv_Ovf_U8_Un:
					return HandleConversion(64, false, expr.Arguments[0], expectedType, corLib.UInt64);
				case ILCode.Conv_I:
				case ILCode.Conv_Ovf_I:
				case ILCode.Conv_Ovf_I_Un:
					return HandleConversion(NativeInt, true, expr.Arguments[0], expectedType, corLib.IntPtr);
				case ILCode.Conv_U:
				case ILCode.Conv_Ovf_U:
				case ILCode.Conv_Ovf_U_Un:
					return HandleConversion(NativeInt, false, expr.Arguments[0], expectedType, corLib.UIntPtr);
				case ILCode.Conv_R4:
					if (forceInferChildren) {
						InferTypeForExpression(expr.Arguments[0], corLib.Single);
					}
					return corLib.Single;
				case ILCode.Conv_R8:
					if (forceInferChildren) {
						InferTypeForExpression(expr.Arguments[0], corLib.Double);
					}
					return corLib.Double;
				case ILCode.Conv_R_Un:
					return (expectedType != null  && expectedType.ElementType == ElementType.R4) ? corLib.Single : corLib.Double;
				case ILCode.Castclass:
				case ILCode.Unbox_Any:
					return ((ITypeDefOrRef)expr.Operand).ToTypeSigInternal();
				case ILCode.Unbox:
					return new ByRefSig(((ITypeDefOrRef)expr.Operand).ToTypeSigInternal());
				case ILCode.Isinst:
					{
						// isinst performs the equivalent of a cast only for reference types;
						// value types still need to be unboxed after an isinst instruction
						TypeSig tr = ((ITypeDefOrRef)expr.Operand).ToTypeSigInternal();
						return DnlibExtensions.IsValueType(tr) ? corLib.Object : tr;
					}
				case ILCode.Box:
					{
						var tr = ((ITypeDefOrRef)expr.Operand).ToTypeSigInternal();
						if (forceInferChildren)
							InferTypeForExpression(expr.Arguments.Single(), tr);
						return DnlibExtensions.IsValueType(tr) ? corLib.Object : tr;
					}
					#endregion
					#region Comparison instructions
				case ILCode.Ceq:
				case ILCode.Cne:
					if (forceInferChildren)
						InferArgumentsInBinaryOperator(expr, null, null);
					return corLib.Boolean;
				case ILCode.Clt:
				case ILCode.Cgt:
				case ILCode.Cle:
				case ILCode.Cge:
					if (forceInferChildren)
						InferArgumentsInBinaryOperator(expr, true, null);
					return corLib.Boolean;
				case ILCode.Clt_Un:
				case ILCode.Cgt_Un:
				case ILCode.Cle_Un:
				case ILCode.Cge_Un:
					if (forceInferChildren)
						InferArgumentsInBinaryOperator(expr, false, null);
					return corLib.Boolean;
					#endregion
					#region Branch instructions
				case ILCode.Brtrue:
					if (forceInferChildren)
						InferTypeForExpression(expr.Arguments.Single(), corLib.Boolean);
					return null;
				case ILCode.Br:
				case ILCode.Leave:
				case ILCode.Endfinally:
				case ILCode.Switch:
				case ILCode.Throw:
				case ILCode.Rethrow:
				case ILCode.LoopOrSwitchBreak:
				case ILCode.LoopContinue:
				case ILCode.YieldBreak:
					return null;
				case ILCode.Ret:
					if (forceInferChildren && expr.Arguments.Count == 1) {
						TypeSig returnType = context.CurrentMethod.ReturnType;
						if (context.CurrentMethodIsAsync && returnType != null && returnType.Namespace == "System.Threading.Tasks") {
							if (returnType.TypeName == "Task") {
								returnType = corLib.Void;
							} else if (returnType.TypeName == "Task`1" && returnType.IsGenericInstanceType) {
								returnType = ((GenericInstSig)returnType).GenericArguments[0];
							}
						}
						InferTypeForExpression(expr.Arguments[0], returnType);
					}
					return null;
				case ILCode.YieldReturn:
					if (forceInferChildren) {
						GenericInstSig genericType = context.CurrentMethod.ReturnType as GenericInstSig;
						if (genericType != null) { // IEnumerable<T> or IEnumerator<T>
							InferTypeForExpression(expr.Arguments[0], genericType.GenericArguments[0]);
						} else { // non-generic IEnumerable or IEnumerator
							InferTypeForExpression(expr.Arguments[0], corLib.Object);
						}
					}
					return null;
				case ILCode.Await:
					{
						TypeSig taskType = InferTypeForExpression(expr.Arguments[0], null);
						if (taskType != null && taskType.TypeName == "Task`1" && taskType.IsGenericInstanceType && taskType.Namespace == "System.Threading.Tasks") {
							return ((GenericInstSig)taskType).GenericArguments[0];
						}
						return null;
					}
					#endregion
				case ILCode.Pop:
					return null;
				case ILCode.Wrap:
				case ILCode.Dup:
					{
						var arg = expr.Arguments.Single();
						return arg.ExpectedType = InferTypeForExpression(arg, expectedType);
					}
				default:
					Debug.WriteLine("Type Inference: Can't handle " + expr.Code.GetName());
					return null;
			}
		}
		
		/// <summary>
		/// Wraps 'type' in a ByReferenceType if it is a value type. If a constrained prefix is specified,
		/// returns the constrained type wrapped in a ByReferenceType.
		/// </summary>
		TypeSig MakeRefIfValueType(TypeSig type, ILExpressionPrefix constrainedPrefix)
		{
			if (constrainedPrefix != null)
				return new ByRefSig(((ITypeDefOrRef)constrainedPrefix.Operand).ToTypeSigInternal());
			if (DnlibExtensions.IsValueType(type))
				return new ByRefSig(type);
			else
				return type;
		}
		
		/// <summary>
		/// Promotes primitive types smaller than int32 to int32.
		/// </summary>
		/// <remarks>
		/// Always promotes to signed int32.
		/// </remarks>
		TypeSig NumericPromotion(TypeSig type)
		{
			if (type == null)
				return null;
			switch (type.ElementType) {
				case ElementType.I1:
				case ElementType.I2:
				case ElementType.U1:
				case ElementType.U2:
					return corLib.Int32;
				default:
					return type;
			}
		}
		
		TypeSig HandleConversion(int targetBitSize, bool targetSigned, ILExpression arg, TypeSig expectedType, TypeSig targetType)
		{
			if (targetBitSize >= NativeInt && expectedType is PtrSig) {
				InferTypeForExpression(arg, expectedType);
				return expectedType;
			}
			TypeSig argType = InferTypeForExpression(arg, null);
			if (targetBitSize >= NativeInt && argType is ByRefSig) {
				// conv instructions on managed references mean that the GC should stop tracking them, so they become pointers:
				PtrSig ptrType = new PtrSig(((ByRefSig)argType).Next);
				InferTypeForExpression(arg, ptrType);
				return ptrType;
			} else if (targetBitSize >= NativeInt && argType is PtrSig) {
				return argType;
			}
			TypeSig resultType = (GetInformationAmount(expectedType) == targetBitSize && IsSigned(expectedType) == targetSigned) ? expectedType : targetType;
			arg.ExpectedType = resultType; // store the expected type in the argument so that AstMethodBodyBuilder will insert a cast
			return resultType;
		}
		
		public static TypeSig GetFieldType(IField field)
		{
			return SubstituteTypeArgs(field.FieldSig.Type.RemoveModifiers(), field.DeclaringType.ToTypeSigInternal());
		}
		
		public static TypeSig SubstituteTypeArgs(TypeSig type, TypeSig typeContext = null, IMethod method = null)
		{
			IList<TypeSig> typeArgs = null, methodArgs = null;

			if (typeContext == null)
				typeContext = method.DeclaringType.ToTypeSigInternal();

			if (typeContext is GenericInstSig)
				typeArgs = ((GenericInstSig)typeContext).GenericArguments;

			if (method is MethodSpec)
				methodArgs = ((MethodSpec)method).GenericInstMethodSig.GenericArguments;

			return GenericArgumentResolver.Resolve(type, typeArgs, methodArgs);
		}
		
		static TypeSig UnpackPointer(TypeSig pointerOrManagedReference)
		{
			ByRefSig refType = pointerOrManagedReference as ByRefSig;
			if (refType != null)
				return refType.Next;
			PtrSig ptrType = pointerOrManagedReference as PtrSig;
			if (ptrType != null)
				return ptrType.Next;
			return null;
		}

		static TypeSig GetNullableTypeArgument(TypeSig type)
		{
			var t = type as GenericInstSig;
			return IsNullableType(t) ? t.GenericArguments[0] : type;
		}

		TypeSig CreateNullableType(TypeSig type)
		{
			if (type == null) return null;
			var t = new GenericInstSig((ClassOrValueTypeSig)corLib.GetTypeRef("System", "Nullable`1").ToTypeSigInternal());
			t.GenericArguments.Add(type);
			return t;
		}
		
		TypeSig InferArgumentsInBinaryOperator(ILExpression expr, bool? isSigned, TypeSig expectedType)
		{
			return InferBinaryArguments(expr.Arguments[0], expr.Arguments[1], expectedType);
		}
		
		TypeSig InferArgumentsInAddition(ILExpression expr, bool? isSigned, TypeSig expectedType)
		{
			ILExpression left = expr.Arguments[0];
			ILExpression right = expr.Arguments[1];
			TypeSig leftPreferred = DoInferTypeForExpression(left, expectedType);
			if (leftPreferred is PtrSig) {
				left.InferredType = left.ExpectedType = leftPreferred;
				InferTypeForExpression(right, corLib.IntPtr);
				return leftPreferred;
			}
			if (IsEnum(leftPreferred)) {
				//E+U=E
				left.InferredType = left.ExpectedType = leftPreferred;
				InferTypeForExpression(right, GetEnumUnderlyingType(leftPreferred));
				return leftPreferred;
			}
			TypeSig rightPreferred = DoInferTypeForExpression(right, expectedType);
			if (rightPreferred is PtrSig) {
				InferTypeForExpression(left, corLib.IntPtr);
				right.InferredType = right.ExpectedType = rightPreferred;
				return rightPreferred;
			}
			if (IsEnum(rightPreferred)) {
				//U+E=E
				right.InferredType = right.ExpectedType = rightPreferred;
				InferTypeForExpression(left, GetEnumUnderlyingType(rightPreferred));
				return rightPreferred;
			}
			return InferBinaryArguments(left, right, expectedType, leftPreferred: leftPreferred, rightPreferred: rightPreferred);
		}
		
		TypeSig InferArgumentsInSubtraction(ILExpression expr, bool? isSigned, TypeSig expectedType)
		{
			ILExpression left = expr.Arguments[0];
			ILExpression right = expr.Arguments[1];
			TypeSig leftPreferred = DoInferTypeForExpression(left, expectedType);
			if (leftPreferred is PtrSig) {
				left.InferredType = left.ExpectedType = leftPreferred;
				InferTypeForExpression(right, corLib.IntPtr);
				return leftPreferred;
			}
			if (IsEnum(leftPreferred)) {
				if (expectedType != null && IsEnum(expectedType)) {
					// E-U=E
					left.InferredType = left.ExpectedType = leftPreferred;
					InferTypeForExpression(right, GetEnumUnderlyingType(leftPreferred));
					return leftPreferred;
				} else {
					// E-E=U
					left.InferredType = left.ExpectedType = leftPreferred;
					InferTypeForExpression(right, leftPreferred);
					return GetEnumUnderlyingType(leftPreferred);
				}
			}
			return InferBinaryArguments(left, right, expectedType, leftPreferred: leftPreferred);
		}

		TypeSig InferBinaryArguments(ILExpression left, ILExpression right, TypeSig expectedType, bool forceInferChildren = false, TypeSig leftPreferred = null, TypeSig rightPreferred = null)
		{
			if (leftPreferred == null) leftPreferred = DoInferTypeForExpression(left, expectedType, forceInferChildren);
			if (rightPreferred == null) rightPreferred = DoInferTypeForExpression(right, expectedType, forceInferChildren);
			if (IsSameType(leftPreferred, rightPreferred)) {
				return left.InferredType = right.InferredType = left.ExpectedType = right.ExpectedType = leftPreferred;
			} else if (IsSameType(rightPreferred, DoInferTypeForExpression(left, rightPreferred, forceInferChildren))) {
				return left.InferredType = right.InferredType = left.ExpectedType = right.ExpectedType = rightPreferred;
			} else if (IsSameType(leftPreferred, DoInferTypeForExpression(right, leftPreferred, forceInferChildren))) {
				// re-infer the left expression with the preferred type to reset any conflicts caused by the rightPreferred type
				DoInferTypeForExpression(left, leftPreferred, forceInferChildren);
				return left.InferredType = right.InferredType = left.ExpectedType = right.ExpectedType = leftPreferred;
			} else {
				left.ExpectedType = right.ExpectedType = TypeWithMoreInformation(leftPreferred, rightPreferred);
				left.InferredType = DoInferTypeForExpression(left, left.ExpectedType, forceInferChildren);
				right.InferredType = DoInferTypeForExpression(right, right.ExpectedType, forceInferChildren);
				return left.ExpectedType;
			}
		}
		
		TypeSig TypeWithMoreInformation(TypeSig leftPreferred, TypeSig rightPreferred)
		{
			int left = GetInformationAmount(leftPreferred);
			int right = GetInformationAmount(rightPreferred);
			if (left < right) {
				return rightPreferred;
			} else if (left > right) {
				return leftPreferred;
			} else {
				// TODO
				return leftPreferred;
			}
		}
		
		/// <summary>
		/// Information amount used for IntPtr.
		/// </summary>
		public const int NativeInt = 33; // treat native int as between int32 and int64
		
		/// <summary>
		/// Gets the underlying type, if the specified type is an enum.
		/// Otherwise, returns null.
		/// </summary>
		public static TypeSig GetEnumUnderlyingType(TypeSig enumType)
		{
			// unfortunately we cannot rely on enumType.IsValueType here - it's not set when the instruction operand is a typeref (as opposed to a typespec)
			if (enumType != null && !IsArrayPointerOrReference(enumType)) {
				// value type might be an enum
				TypeDef typeDef = enumType.Resolve();
				if (typeDef != null && typeDef.IsEnum) {
					return typeDef.GetEnumUnderlyingType();
				}
			}
			return null;
		}
		
		public static int GetInformationAmount(TypeSig type)
		{
			type = GetEnumUnderlyingType(type) ?? type;
			if (type == null)
				return 0;
			switch (type.ElementType) {
				case ElementType.Void:
					return 0;
				case ElementType.Boolean:
					return 1;
				case ElementType.I1:
				case ElementType.U1:
					return 8;
				case ElementType.Char:
				case ElementType.I2:
				case ElementType.U2:
					return 16;
				case ElementType.I4:
				case ElementType.U4:
				case ElementType.R4:
					return 32;
				case ElementType.I8:
				case ElementType.U8:
				case ElementType.R8:
					return 64;
				case ElementType.I:
				case ElementType.U:
					return NativeInt;
				default:
					return 100; // we consider structs/objects to have more information than any primitives
			}
		}
		
		public static bool IsIntegerOrEnum(TypeSig type)
		{
			return IsSigned(type) != null;
		}

		public static bool IsEnum(TypeSig type)
		{
			// Arrays/Pointers/ByReference resolve to their element type, but we don't want to consider those to be enums
			// However, GenericInstanceTypes, ModOpts etc. should be considered enums.
			if (type == null || IsArrayPointerOrReference(type))
				return false;
			var typeSig = type.RemovePinnedAndModifiers();
			TypeDef typeDef = typeSig.Resolve();
			return typeDef != null && typeDef.IsEnum;
		}
		
		static bool? IsSigned(TypeSig type)
		{
			type = GetEnumUnderlyingType(type) ?? type;
			if (type == null)
				return null;
			switch (type.ElementType) {
				case ElementType.I1:
				case ElementType.I2:
				case ElementType.I4:
				case ElementType.I8:
				case ElementType.I:
					return true;
				case ElementType.U1:
				case ElementType.Char:
				case ElementType.U2:
				case ElementType.U4:
				case ElementType.U8:
				case ElementType.U:
					return false;
				default:
					return null;
			}
		}
		
		static bool OperandFitsInType(TypeSig type, int num)
		{
			type = GetEnumUnderlyingType(type) ?? type;
			switch (type.ElementType) {
				case ElementType.I1:
					return sbyte.MinValue <= num && num <= sbyte.MaxValue;
				case ElementType.I2:
					return short.MinValue <= num && num <= short.MaxValue;
				case ElementType.U1:
					return byte.MinValue <= num && num <= byte.MaxValue;
				case ElementType.Char:
					return char.MinValue <= num && num <= char.MaxValue;
				case ElementType.U2:
					return ushort.MinValue <= num && num <= ushort.MaxValue;
				default:
					return true;
			}
		}
		
		static bool IsArrayPointerOrReference(TypeSig type)
		{
			while (type != null) {
				if (type is ArraySigBase || type is PtrSig || type is ByRefSig)
					return true;
				type = type.Next;
			}
			return false;
		}

		internal static bool IsNullableType(TypeSig type)
		{
			TypeDefOrRefSig sig = type as TypeDefOrRefSig;
			if (sig != null)
				return sig.TypeDefOrRef.Name == "Nullable`1" && sig.TypeDefOrRef.Namespace == "System";
			else
				return type is GenericInstSig && IsNullableType(((GenericInstSig)type).GenericType);
		}
		
		public static TypeCode GetTypeCode(TypeSig type)
		{
			if (type == null)
				return TypeCode.Empty;
			switch (type.ElementType) {
				case ElementType.Boolean:
					return TypeCode.Boolean;
				case ElementType.Char:
					return TypeCode.Char;
				case ElementType.I1:
					return TypeCode.SByte;
				case ElementType.U1:
					return TypeCode.Byte;
				case ElementType.I2:
					return TypeCode.Int16;
				case ElementType.U2:
					return TypeCode.UInt16;
				case ElementType.I4:
					return TypeCode.Int32;
				case ElementType.U4:
					return TypeCode.UInt32;
				case ElementType.I8:
					return TypeCode.Int64;
				case ElementType.U8:
					return TypeCode.UInt64;
				case ElementType.R4:
					return TypeCode.Single;
				case ElementType.R8:
					return TypeCode.Double;
				case ElementType.String:
					return TypeCode.String;
				default:
					return TypeCode.Object;
			}
		}
		
		/// <summary>
		/// Clears the type inference data on the method.
		/// </summary>
		public static void Reset(ILBlock method)
		{
			foreach (ILExpression expr in method.GetSelfAndChildrenRecursive<ILExpression>()) {
				expr.InferredType = null;
				expr.ExpectedType = null;
				ILVariable v = expr.Operand as ILVariable;
				if (v != null && v.IsGenerated)
					v.Type = null;
			}
		}
		
		public static bool IsSameType(IType type1, IType type2)
		{
			if (type1 == type2)
				return true;
			if (type1 == null || type2 == null)
				return false;
			return new SigComparer().Equals(type1, type2);
		}
	}
}
