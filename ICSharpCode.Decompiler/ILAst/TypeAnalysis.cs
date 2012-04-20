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
using Mono.Cecil;
using Mono.Cecil.Cil;

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
			ta.typeSystem = ta.module.TypeSystem;
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
		TypeSystem typeSystem;
		ILBlock method;
		ModuleDefinition module;
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
				cond.Condition.ExpectedType = typeSystem.Boolean;
			}
			ILWhileLoop loop = node as ILWhileLoop;
			if (loop != null && loop.Condition != null) {
				loop.Condition.ExpectedType = typeSystem.Boolean;
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
						if (assignVariableTypesBasedOnPartialInformation)
							throw new InvalidOperationException("Could not infer any expression");
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
						TypeReference inferredType = null;
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
							inferredType = typeSystem.Object;
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
		TypeReference InferTypeForExpression(ILExpression expr, TypeReference expectedType, bool forceInferChildren = false)
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
		
		TypeReference DoInferTypeForExpression(ILExpression expr, TypeReference expectedType, bool forceInferChildren = false)
		{
			switch (expr.Code) {
					#region Logical operators
				case ILCode.LogicNot:
					if (forceInferChildren) {
						InferTypeForExpression(expr.Arguments.Single(), typeSystem.Boolean);
					}
					return typeSystem.Boolean;
				case ILCode.LogicAnd:
				case ILCode.LogicOr:
					// if Operand is set the logic and/or expression is a custom operator
					// we can deal with it the same as a normal invocation.
					if (expr.Operand != null)
						goto case ILCode.Call;
					if (forceInferChildren) {
						InferTypeForExpression(expr.Arguments[0], typeSystem.Boolean);
						InferTypeForExpression(expr.Arguments[1], typeSystem.Boolean);
					}
					return typeSystem.Boolean;
				case ILCode.TernaryOp:
					if (forceInferChildren) {
						InferTypeForExpression(expr.Arguments[0], typeSystem.Boolean);
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
					return new ByReferenceType(((ILVariable)expr.Operand).Type);
					#endregion
					#region Call / NewObj
				case ILCode.Call:
				case ILCode.Callvirt:
				case ILCode.CallGetter:
				case ILCode.CallvirtGetter:
				case ILCode.CallSetter:
				case ILCode.CallvirtSetter:
					{
						MethodReference method = (MethodReference)expr.Operand;
						if (forceInferChildren) {
							for (int i = 0; i < expr.Arguments.Count; i++) {
								if (i == 0 && method.HasThis) {
									InferTypeForExpression(expr.Arguments[0], MakeRefIfValueType(method.DeclaringType, expr.GetPrefix(ILCode.Constrained)));
								} else {
									InferTypeForExpression(expr.Arguments[i], SubstituteTypeArgs(method.Parameters[method.HasThis ? i - 1 : i].ParameterType, method));
								}
							}
						}
						if (expr.Code == ILCode.CallSetter || expr.Code == ILCode.CallvirtSetter) {
							return SubstituteTypeArgs(method.Parameters.Last().ParameterType, method);
						} else {
							return SubstituteTypeArgs(method.ReturnType, method);
						}
					}
				case ILCode.Newobj:
					{
						MethodReference ctor = (MethodReference)expr.Operand;
						if (forceInferChildren) {
							for (int i = 0; i < ctor.Parameters.Count; i++) {
								InferTypeForExpression(expr.Arguments[i], SubstituteTypeArgs(ctor.Parameters[i].ParameterType, ctor));
							}
						}
						return ctor.DeclaringType;
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
						InferTypeForExpression(expr.Arguments[0], MakeRefIfValueType(((FieldReference)expr.Operand).DeclaringType, expr.GetPrefix(ILCode.Constrained)));
					}
					return GetFieldType((FieldReference)expr.Operand);
				case ILCode.Ldsfld:
					return GetFieldType((FieldReference)expr.Operand);
				case ILCode.Ldflda:
					if (forceInferChildren) {
						InferTypeForExpression(expr.Arguments[0], MakeRefIfValueType(((FieldReference)expr.Operand).DeclaringType, expr.GetPrefix(ILCode.Constrained)));
					}
					return new ByReferenceType(GetFieldType((FieldReference)expr.Operand));
				case ILCode.Ldsflda:
					return new ByReferenceType(GetFieldType((FieldReference)expr.Operand));
				case ILCode.Stfld:
					if (forceInferChildren) {
						InferTypeForExpression(expr.Arguments[0], MakeRefIfValueType(((FieldReference)expr.Operand).DeclaringType, expr.GetPrefix(ILCode.Constrained)));
						InferTypeForExpression(expr.Arguments[1], GetFieldType((FieldReference)expr.Operand));
					}
					return GetFieldType((FieldReference)expr.Operand);
				case ILCode.Stsfld:
					if (forceInferChildren)
						InferTypeForExpression(expr.Arguments[0], GetFieldType((FieldReference)expr.Operand));
					return GetFieldType((FieldReference)expr.Operand);
					#endregion
					#region Reference/Pointer instructions
				case ILCode.Ldind_Ref:
					return UnpackPointer(InferTypeForExpression(expr.Arguments[0], null));
				case ILCode.Stind_Ref:
					if (forceInferChildren) {
						TypeReference elementType = UnpackPointer(InferTypeForExpression(expr.Arguments[0], null));
						InferTypeForExpression(expr.Arguments[1], elementType);
					}
					return null;
				case ILCode.Ldobj:
					{
						TypeReference type = (TypeReference)expr.Operand;
						if (expectedType != null) {
							int infoAmount = GetInformationAmount(expectedType);
							if (infoAmount == 1 && GetInformationAmount(type) == 8) {
								// A bool can be loaded from both bytes and sbytes.
								type = expectedType;
							}
							if (infoAmount >= 8 && infoAmount <= 64 && infoAmount == GetInformationAmount(type)) {
								// An integer can be loaded as another integer of the same size.
								// For integers smaller than 32 bit, the signs must match (as loading performs sign extension)
								if (infoAmount >= 32 || IsSigned(expectedType) == IsSigned(type))
									type = expectedType;
							}
						}
						if (forceInferChildren) {
							if (InferTypeForExpression(expr.Arguments[0], new ByReferenceType(type)) is PointerType)
								InferTypeForExpression(expr.Arguments[0], new PointerType(type));
						}
						return type;
					}
				case ILCode.Stobj:
					{
						TypeReference operandType = (TypeReference)expr.Operand;
						TypeReference pointerType = InferTypeForExpression(expr.Arguments[0], new ByReferenceType(operandType));
						TypeReference elementType;
						if (pointerType is PointerType)
							elementType = ((PointerType)pointerType).ElementType;
						else if (pointerType is ByReferenceType)
							elementType = ((ByReferenceType)pointerType).ElementType;
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
							if (pointerType is PointerType)
								InferTypeForExpression(expr.Arguments[0], new PointerType(operandType));
							else if (!IsSameType(operandType, expr.Operand as TypeReference))
								InferTypeForExpression(expr.Arguments[0], new ByReferenceType(operandType));
							InferTypeForExpression(expr.Arguments[1], operandType);
						}
						return operandType;
					}
				case ILCode.Initobj:
					return null;
				case ILCode.DefaultValue:
					return (TypeReference)expr.Operand;
				case ILCode.Localloc:
					if (forceInferChildren) {
						InferTypeForExpression(expr.Arguments[0], typeSystem.Int32);
					}
					if (expectedType is PointerType)
						return expectedType;
					else
						return typeSystem.IntPtr;
				case ILCode.Sizeof:
					return typeSystem.Int32;
				case ILCode.PostIncrement:
				case ILCode.PostIncrement_Ovf:
				case ILCode.PostIncrement_Ovf_Un:
					{
						TypeReference elementType = UnpackPointer(InferTypeForExpression(expr.Arguments[0], null));
						if (forceInferChildren && elementType != null) {
							// Assign expected type to the child expression
							InferTypeForExpression(expr.Arguments[0], new ByReferenceType(elementType));
						}
						return elementType;
					}
				case ILCode.Mkrefany:
					if (forceInferChildren) {
						InferTypeForExpression(expr.Arguments[0], (TypeReference)expr.Operand);
					}
					return typeSystem.TypedReference;
				case ILCode.Refanytype:
					if (forceInferChildren) {
						InferTypeForExpression(expr.Arguments[0], typeSystem.TypedReference);
					}
					return new TypeReference("System", "RuntimeTypeHandle", module, module.TypeSystem.Corlib, true);
				case ILCode.Refanyval:
					if (forceInferChildren) {
						InferTypeForExpression(expr.Arguments[0], typeSystem.TypedReference);
					}
					return new ByReferenceType((TypeReference)expr.Operand);
				case ILCode.AddressOf:
					{
						TypeReference t = InferTypeForExpression(expr.Arguments[0], UnpackPointer(expectedType));
						return t != null ? new ByReferenceType(t) : null;
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
						InferTypeForExpression(expr.Arguments[1], typeSystem.Int32);
					if (expectedType != null && (
						expectedType.MetadataType == MetadataType.Int32 || expectedType.MetadataType == MetadataType.UInt32 ||
						expectedType.MetadataType == MetadataType.Int64 || expectedType.MetadataType == MetadataType.UInt64)
					   )
						return NumericPromotion(InferTypeForExpression(expr.Arguments[0], expectedType));
					else
						return NumericPromotion(InferTypeForExpression(expr.Arguments[0], null));
				case ILCode.Shr:
				case ILCode.Shr_Un:
					{
						if (forceInferChildren)
							InferTypeForExpression(expr.Arguments[1], typeSystem.Int32);
						TypeReference type = NumericPromotion(InferTypeForExpression(expr.Arguments[0], null));
						TypeReference expectedInputType = null;
						switch (type.MetadataType) {
							case MetadataType.Int32:
								if (expr.Code == ILCode.Shr_Un)
									expectedInputType = typeSystem.UInt32;
								break;
							case MetadataType.UInt32:
								if (expr.Code == ILCode.Shr)
									expectedInputType = typeSystem.Int32;
								break;
							case MetadataType.Int64:
								if (expr.Code == ILCode.Shr_Un)
									expectedInputType = typeSystem.UInt64;
								break;
							case MetadataType.UInt64:
								if (expr.Code == ILCode.Shr)
									expectedInputType = typeSystem.UInt64;
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
					return typeSystem.Object;
				case ILCode.Ldstr:
					return typeSystem.String;
				case ILCode.Ldftn:
				case ILCode.Ldvirtftn:
					return typeSystem.IntPtr;
				case ILCode.Ldc_I4:
					if (IsBoolean(expectedType) && ((int)expr.Operand == 0 || (int)expr.Operand == 1))
						return typeSystem.Boolean;
					if (expectedType is PointerType && (int)expr.Operand == 0)
						return expectedType;
					if (IsIntegerOrEnum(expectedType) && OperandFitsInType(expectedType, (int)expr.Operand))
						return expectedType;
					else
						return typeSystem.Int32;
				case ILCode.Ldc_I8:
					if (expectedType is PointerType && (long)expr.Operand == 0)
						return expectedType;
					if (IsIntegerOrEnum(expectedType) && GetInformationAmount(expectedType) >= NativeInt)
						return expectedType;
					else
						return typeSystem.Int64;
				case ILCode.Ldc_R4:
					return typeSystem.Single;
				case ILCode.Ldc_R8:
					return typeSystem.Double;
				case ILCode.Ldc_Decimal:
					return new TypeReference("System", "Decimal", module, module.TypeSystem.Corlib, true);
				case ILCode.Ldtoken:
					if (expr.Operand is TypeReference)
						return new TypeReference("System", "RuntimeTypeHandle", module, module.TypeSystem.Corlib, true);
					else if (expr.Operand is FieldReference)
						return new TypeReference("System", "RuntimeFieldHandle", module, module.TypeSystem.Corlib, true);
					else
						return new TypeReference("System", "RuntimeMethodHandle", module, module.TypeSystem.Corlib, true);
				case ILCode.Arglist:
					return new TypeReference("System", "RuntimeArgumentHandle", module, module.TypeSystem.Corlib, true);
					#endregion
					#region Array instructions
				case ILCode.Newarr:
					if (forceInferChildren) {
						var lengthType = InferTypeForExpression(expr.Arguments.Single(), null);
						if (lengthType == typeSystem.IntPtr) {
							lengthType = typeSystem.Int64;
						} else if (lengthType == typeSystem.UIntPtr) {
							lengthType = typeSystem.UInt64;
						} else if (lengthType != typeSystem.UInt32 && lengthType != typeSystem.Int64 && lengthType != typeSystem.UInt64) {
							lengthType = typeSystem.Int32;
						}
						if (forceInferChildren) {
							InferTypeForExpression(expr.Arguments.Single(), lengthType);
						}
					}
					return new ArrayType((TypeReference)expr.Operand);
				case ILCode.InitArray:
					var operandAsArrayType = (ArrayType)expr.Operand;
					if (forceInferChildren)
					{
						foreach (ILExpression arg in expr.Arguments)
							InferTypeForExpression(arg, operandAsArrayType.ElementType);
					}
					return operandAsArrayType;
				case ILCode.Ldlen:
					return typeSystem.Int32;
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
						ArrayType arrayType = InferTypeForExpression(expr.Arguments[0], null) as ArrayType;
						if (forceInferChildren) {
							InferTypeForExpression(expr.Arguments[1], typeSystem.Int32);
						}
						return arrayType != null ? arrayType.ElementType : null;
					}
				case ILCode.Ldelem_Any:
					if (forceInferChildren) {
						InferTypeForExpression(expr.Arguments[1], typeSystem.Int32);
					}
					return (TypeReference)expr.Operand;
				case ILCode.Ldelema:
					{
						ArrayType arrayType = InferTypeForExpression(expr.Arguments[0], null) as ArrayType;
						if (forceInferChildren)
							InferTypeForExpression(expr.Arguments[1], typeSystem.Int32);
						return arrayType != null ? new ByReferenceType(arrayType.ElementType) : null;
					}
				case ILCode.Stelem_I:
				case ILCode.Stelem_I1:
				case ILCode.Stelem_I2:
				case ILCode.Stelem_I4:
				case ILCode.Stelem_I8:
				case ILCode.Stelem_R4:
				case ILCode.Stelem_R8:
				case ILCode.Stelem_Ref:
				case ILCode.Stelem_Any:
					{
						ArrayType arrayType = InferTypeForExpression(expr.Arguments[0], null) as ArrayType;
						if (forceInferChildren) {
							InferTypeForExpression(expr.Arguments[1], typeSystem.Int32);
							if (arrayType != null) {
								InferTypeForExpression(expr.Arguments[2], arrayType.ElementType);
							}
						}
						return arrayType != null ? arrayType.ElementType : null;
					}
					#endregion
					#region Conversion instructions
				case ILCode.Conv_I1:
				case ILCode.Conv_Ovf_I1:
				case ILCode.Conv_Ovf_I1_Un:
					return HandleConversion(8, true, expr.Arguments[0], expectedType, typeSystem.SByte);
				case ILCode.Conv_I2:
				case ILCode.Conv_Ovf_I2:
				case ILCode.Conv_Ovf_I2_Un:
					return HandleConversion(16, true, expr.Arguments[0], expectedType, typeSystem.Int16);
				case ILCode.Conv_I4:
				case ILCode.Conv_Ovf_I4:
				case ILCode.Conv_Ovf_I4_Un:
					return HandleConversion(32, true, expr.Arguments[0], expectedType, typeSystem.Int32);
				case ILCode.Conv_I8:
				case ILCode.Conv_Ovf_I8:
				case ILCode.Conv_Ovf_I8_Un:
					return HandleConversion(64, true, expr.Arguments[0], expectedType, typeSystem.Int64);
				case ILCode.Conv_U1:
				case ILCode.Conv_Ovf_U1:
				case ILCode.Conv_Ovf_U1_Un:
					return HandleConversion(8, false, expr.Arguments[0], expectedType, typeSystem.Byte);
				case ILCode.Conv_U2:
				case ILCode.Conv_Ovf_U2:
				case ILCode.Conv_Ovf_U2_Un:
					return HandleConversion(16, false, expr.Arguments[0], expectedType, typeSystem.UInt16);
				case ILCode.Conv_U4:
				case ILCode.Conv_Ovf_U4:
				case ILCode.Conv_Ovf_U4_Un:
					return HandleConversion(32, false, expr.Arguments[0], expectedType, typeSystem.UInt32);
				case ILCode.Conv_U8:
				case ILCode.Conv_Ovf_U8:
				case ILCode.Conv_Ovf_U8_Un:
					return HandleConversion(64, false, expr.Arguments[0], expectedType, typeSystem.UInt64);
				case ILCode.Conv_I:
				case ILCode.Conv_Ovf_I:
				case ILCode.Conv_Ovf_I_Un:
					return HandleConversion(NativeInt, true, expr.Arguments[0], expectedType, typeSystem.IntPtr);
				case ILCode.Conv_U:
				case ILCode.Conv_Ovf_U:
				case ILCode.Conv_Ovf_U_Un:
					return HandleConversion(NativeInt, false, expr.Arguments[0], expectedType, typeSystem.UIntPtr);
				case ILCode.Conv_R4:
					if (forceInferChildren) {
						InferTypeForExpression(expr.Arguments[0], typeSystem.Single);
					}
					return typeSystem.Single;
				case ILCode.Conv_R8:
					if (forceInferChildren) {
						InferTypeForExpression(expr.Arguments[0], typeSystem.Double);
					}
					return typeSystem.Double;
				case ILCode.Conv_R_Un:
					return (expectedType != null  && expectedType.MetadataType == MetadataType.Single) ? typeSystem.Single : typeSystem.Double;
				case ILCode.Castclass:
				case ILCode.Unbox_Any:
					return (TypeReference)expr.Operand;
				case ILCode.Unbox:
					return new ByReferenceType((TypeReference)expr.Operand);
				case ILCode.Isinst:
					{
						// isinst performs the equivalent of a cast only for reference types;
						// value types still need to be unboxed after an isinst instruction
						TypeReference tr = (TypeReference)expr.Operand;
						return tr.IsValueType ? typeSystem.Object : tr;
					}
				case ILCode.Box:
					{
						var tr = (TypeReference)expr.Operand;
						if (forceInferChildren)
							InferTypeForExpression(expr.Arguments.Single(), tr);
						return tr.IsValueType ? typeSystem.Object : tr;
					}
					#endregion
					#region Comparison instructions
				case ILCode.Ceq:
				case ILCode.Cne:
					if (forceInferChildren)
						InferArgumentsInBinaryOperator(expr, null, null);
					return typeSystem.Boolean;
				case ILCode.Clt:
				case ILCode.Cgt:
				case ILCode.Cle:
				case ILCode.Cge:
					if (forceInferChildren)
						InferArgumentsInBinaryOperator(expr, true, null);
					return typeSystem.Boolean;
				case ILCode.Clt_Un:
				case ILCode.Cgt_Un:
				case ILCode.Cle_Un:
				case ILCode.Cge_Un:
					if (forceInferChildren)
						InferArgumentsInBinaryOperator(expr, false, null);
					return typeSystem.Boolean;
					#endregion
					#region Branch instructions
				case ILCode.Brtrue:
					if (forceInferChildren)
						InferTypeForExpression(expr.Arguments.Single(), typeSystem.Boolean);
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
						TypeReference returnType = context.CurrentMethod.ReturnType;
						if (context.CurrentMethodIsAsync && returnType != null && returnType.Namespace == "System.Threading.Tasks") {
							if (returnType.Name == "Task") {
								returnType = typeSystem.Void;
							} else if (returnType.Name == "Task`1" && returnType.IsGenericInstance) {
								returnType = ((GenericInstanceType)returnType).GenericArguments[0];
							}
						}
						InferTypeForExpression(expr.Arguments[0], returnType);
					}
					return null;
				case ILCode.YieldReturn:
					if (forceInferChildren) {
						GenericInstanceType genericType = context.CurrentMethod.ReturnType as GenericInstanceType;
						if (genericType != null) { // IEnumerable<T> or IEnumerator<T>
							InferTypeForExpression(expr.Arguments[0], genericType.GenericArguments[0]);
						} else { // non-generic IEnumerable or IEnumerator
							InferTypeForExpression(expr.Arguments[0], typeSystem.Object);
						}
					}
					return null;
				case ILCode.Await:
					{
						TypeReference taskType = InferTypeForExpression(expr.Arguments[0], null);
						if (taskType.Name == "Task`1" && taskType.IsGenericInstance && taskType.Namespace == "System.Threading.Tasks") {
							return ((GenericInstanceType)taskType).GenericArguments[0];
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
		TypeReference MakeRefIfValueType(TypeReference type, ILExpressionPrefix constrainedPrefix)
		{
			if (constrainedPrefix != null)
				return new ByReferenceType((TypeReference)constrainedPrefix.Operand);
			if (type.IsValueType)
				return new ByReferenceType(type);
			else
				return type;
		}
		
		/// <summary>
		/// Promotes primitive types smaller than int32 to int32.
		/// </summary>
		/// <remarks>
		/// Always promotes to signed int32.
		/// </remarks>
		TypeReference NumericPromotion(TypeReference type)
		{
			if (type == null)
				return null;
			switch (type.MetadataType) {
				case MetadataType.SByte:
				case MetadataType.Int16:
				case MetadataType.Byte:
				case MetadataType.UInt16:
					return typeSystem.Int32;
				default:
					return type;
			}
		}
		
		TypeReference HandleConversion(int targetBitSize, bool targetSigned, ILExpression arg, TypeReference expectedType, TypeReference targetType)
		{
			if (targetBitSize >= NativeInt && expectedType is PointerType) {
				InferTypeForExpression(arg, expectedType);
				return expectedType;
			}
			TypeReference argType = InferTypeForExpression(arg, null);
			if (targetBitSize >= NativeInt && argType is ByReferenceType) {
				// conv instructions on managed references mean that the GC should stop tracking them, so they become pointers:
				PointerType ptrType = new PointerType(((ByReferenceType)argType).ElementType);
				InferTypeForExpression(arg, ptrType);
				return ptrType;
			} else if (targetBitSize >= NativeInt && argType is PointerType) {
				return argType;
			}
			TypeReference resultType = (GetInformationAmount(expectedType) == targetBitSize && IsSigned(expectedType) == targetSigned) ? expectedType : targetType;
			arg.ExpectedType = resultType; // store the expected type in the argument so that AstMethodBodyBuilder will insert a cast
			return resultType;
		}
		
		public static TypeReference GetFieldType(FieldReference fieldReference)
		{
			return SubstituteTypeArgs(UnpackModifiers(fieldReference.FieldType), fieldReference);
		}
		
		public static TypeReference SubstituteTypeArgs(TypeReference type, MemberReference member)
		{
			if (type is TypeSpecification) {
				ArrayType arrayType = type as ArrayType;
				if (arrayType != null) {
					TypeReference elementType = SubstituteTypeArgs(arrayType.ElementType, member);
					if (elementType != arrayType.ElementType) {
						ArrayType newArrayType = new ArrayType(elementType);
						newArrayType.Dimensions.Clear(); // remove the single dimension that Cecil adds by default
						foreach (ArrayDimension d in arrayType.Dimensions)
							newArrayType.Dimensions.Add(d);
						return newArrayType;
					} else {
						return type;
					}
				}
				ByReferenceType refType = type as ByReferenceType;
				if (refType != null) {
					TypeReference elementType = SubstituteTypeArgs(refType.ElementType, member);
					return elementType != refType.ElementType ? new ByReferenceType(elementType) : type;
				}
				GenericInstanceType giType = type as GenericInstanceType;
				if (giType != null) {
					GenericInstanceType newType = new GenericInstanceType(giType.ElementType);
					bool isChanged = false;
					for (int i = 0; i < giType.GenericArguments.Count; i++) {
						newType.GenericArguments.Add(SubstituteTypeArgs(giType.GenericArguments[i], member));
						isChanged |= newType.GenericArguments[i] != giType.GenericArguments[i];
					}
					return isChanged ? newType : type;
				}
				OptionalModifierType optmodType = type as OptionalModifierType;
				if (optmodType != null) {
					TypeReference elementType = SubstituteTypeArgs(optmodType.ElementType, member);
					return elementType != optmodType.ElementType ? new OptionalModifierType(optmodType.ModifierType, elementType) : type;
				}
				RequiredModifierType reqmodType = type as RequiredModifierType;
				if (reqmodType != null) {
					TypeReference elementType = SubstituteTypeArgs(reqmodType.ElementType, member);
					return elementType != reqmodType.ElementType ? new RequiredModifierType(reqmodType.ModifierType, elementType) : type;
				}
				PointerType ptrType = type as PointerType;
				if (ptrType != null) {
					TypeReference elementType = SubstituteTypeArgs(ptrType.ElementType, member);
					return elementType != ptrType.ElementType ? new PointerType(elementType) : type;
				}
			}
			GenericParameter gp = type as GenericParameter;
			if (gp != null) {
				if (member.DeclaringType is ArrayType) {
					return ((ArrayType)member.DeclaringType).ElementType;
				} else if (gp.Owner.GenericParameterType == GenericParameterType.Method) {
					return ((GenericInstanceMethod)member).GenericArguments[gp.Position];
				} else  {
					return ((GenericInstanceType)member.DeclaringType).GenericArguments[gp.Position];
				}
			}
			return type;
		}
		
		static TypeReference UnpackPointer(TypeReference pointerOrManagedReference)
		{
			ByReferenceType refType = pointerOrManagedReference as ByReferenceType;
			if (refType != null)
				return refType.ElementType;
			PointerType ptrType = pointerOrManagedReference as PointerType;
			if (ptrType != null)
				return ptrType.ElementType;
			return null;
		}
		
		internal static TypeReference UnpackModifiers(TypeReference type)
		{
			while (type is OptionalModifierType || type is RequiredModifierType)
				type = ((TypeSpecification)type).ElementType;
			return type;
		}

		static TypeReference GetNullableTypeArgument(TypeReference type)
		{
			var t = type as GenericInstanceType;
			return IsNullableType(t) ? t.GenericArguments[0] : type;
		}

		GenericInstanceType CreateNullableType(TypeReference type)
		{
			if (type == null) return null;
			var t = new GenericInstanceType(new TypeReference("System", "Nullable`1", module, module.TypeSystem.Corlib, true));
			t.GenericArguments.Add(type);
			return t;
		}
		
		TypeReference InferArgumentsInBinaryOperator(ILExpression expr, bool? isSigned, TypeReference expectedType)
		{
			return InferBinaryArguments(expr.Arguments[0], expr.Arguments[1], expectedType);
		}
		
		TypeReference InferArgumentsInAddition(ILExpression expr, bool? isSigned, TypeReference expectedType)
		{
			ILExpression left = expr.Arguments[0];
			ILExpression right = expr.Arguments[1];
			TypeReference leftPreferred = DoInferTypeForExpression(left, expectedType);
			if (leftPreferred is PointerType) {
				left.InferredType = left.ExpectedType = leftPreferred;
				InferTypeForExpression(right, typeSystem.IntPtr);
				return leftPreferred;
			}
			if (IsEnum(leftPreferred)) {
				//E+U=E
				left.InferredType = left.ExpectedType = leftPreferred;
				InferTypeForExpression(right, GetEnumUnderlyingType(leftPreferred));
				return leftPreferred;
			}
			TypeReference rightPreferred = DoInferTypeForExpression(right, expectedType);
			if (rightPreferred is PointerType) {
				InferTypeForExpression(left, typeSystem.IntPtr);
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
		
		TypeReference InferArgumentsInSubtraction(ILExpression expr, bool? isSigned, TypeReference expectedType)
		{
			ILExpression left = expr.Arguments[0];
			ILExpression right = expr.Arguments[1];
			TypeReference leftPreferred = DoInferTypeForExpression(left, expectedType);
			if (leftPreferred is PointerType) {
				left.InferredType = left.ExpectedType = leftPreferred;
				InferTypeForExpression(right, typeSystem.IntPtr);
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

		TypeReference InferBinaryArguments(ILExpression left, ILExpression right, TypeReference expectedType, bool forceInferChildren = false, TypeReference leftPreferred = null, TypeReference rightPreferred = null)
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
		
		TypeReference TypeWithMoreInformation(TypeReference leftPreferred, TypeReference rightPreferred)
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
		public static TypeReference GetEnumUnderlyingType(TypeReference enumType)
		{
			// unfortunately we cannot rely on enumType.IsValueType here - it's not set when the instruction operand is a typeref (as opposed to a typespec)
			if (enumType != null && !IsArrayPointerOrReference(enumType)) {
				// value type might be an enum
				TypeDefinition typeDef = enumType.Resolve() as TypeDefinition;
				if (typeDef != null && typeDef.IsEnum) {
					return typeDef.Fields.Single(f => !f.IsStatic).FieldType;
				}
			}
			return null;
		}
		
		public static int GetInformationAmount(TypeReference type)
		{
			type = GetEnumUnderlyingType(type) ?? type;
			if (type == null)
				return 0;
			switch (type.MetadataType) {
				case MetadataType.Void:
					return 0;
				case MetadataType.Boolean:
					return 1;
				case MetadataType.SByte:
				case MetadataType.Byte:
					return 8;
				case MetadataType.Char:
				case MetadataType.Int16:
				case MetadataType.UInt16:
					return 16;
				case MetadataType.Int32:
				case MetadataType.UInt32:
				case MetadataType.Single:
					return 32;
				case MetadataType.Int64:
				case MetadataType.UInt64:
				case MetadataType.Double:
					return 64;
				case MetadataType.IntPtr:
				case MetadataType.UIntPtr:
					return NativeInt;
				default:
					return 100; // we consider structs/objects to have more information than any primitives
			}
		}
		
		public static bool IsBoolean(TypeReference type)
		{
			return type != null && type.MetadataType == MetadataType.Boolean;
		}
		
		public static bool IsIntegerOrEnum(TypeReference type)
		{
			return IsSigned(type) != null;
		}

		public static bool IsEnum(TypeReference type)
		{
			// Arrays/Pointers/ByReference resolve to their element type, but we don't want to consider those to be enums
			// However, GenericInstanceTypes, ModOpts etc. should be considered enums.
			if (type == null || IsArrayPointerOrReference(type))
				return false;
			// unfortunately we cannot rely on type.IsValueType here - it's not set when the instruction operand is a typeref (as opposed to a typespec)
			TypeDefinition typeDef = type.Resolve() as TypeDefinition;
			return typeDef != null && typeDef.IsEnum;
		}
		
		static bool? IsSigned(TypeReference type)
		{
			type = GetEnumUnderlyingType(type) ?? type;
			if (type == null)
				return null;
			switch (type.MetadataType) {
				case MetadataType.SByte:
				case MetadataType.Int16:
				case MetadataType.Int32:
				case MetadataType.Int64:
				case MetadataType.IntPtr:
					return true;
				case MetadataType.Byte:
				case MetadataType.Char:
				case MetadataType.UInt16:
				case MetadataType.UInt32:
				case MetadataType.UInt64:
				case MetadataType.UIntPtr:
					return false;
				default:
					return null;
			}
		}
		
		static bool OperandFitsInType(TypeReference type, int num)
		{
			type = GetEnumUnderlyingType(type) ?? type;
			switch (type.MetadataType) {
				case MetadataType.SByte:
					return sbyte.MinValue <= num && num <= sbyte.MaxValue;
				case MetadataType.Int16:
					return short.MinValue <= num && num <= short.MaxValue;
				case MetadataType.Byte:
					return byte.MinValue <= num && num <= byte.MaxValue;
				case MetadataType.Char:
					return char.MinValue <= num && num <= char.MaxValue;
				case MetadataType.UInt16:
					return ushort.MinValue <= num && num <= ushort.MaxValue;
				default:
					return true;
			}
		}
		
		static bool IsArrayPointerOrReference(TypeReference type)
		{
			TypeSpecification typeSpec = type as TypeSpecification;
			while (typeSpec != null) {
				if (typeSpec is ArrayType || typeSpec is PointerType || typeSpec is ByReferenceType)
					return true;
				typeSpec = typeSpec.ElementType as TypeSpecification;
			}
			return false;
		}

		internal static bool IsNullableType(TypeReference type)
		{
			return type != null && type.Name == "Nullable`1" && type.Namespace == "System";
		}
		
		public static TypeCode GetTypeCode(TypeReference type)
		{
			if (type == null)
				return TypeCode.Empty;
			switch (type.MetadataType) {
				case MetadataType.Boolean:
					return TypeCode.Boolean;
				case MetadataType.Char:
					return TypeCode.Char;
				case MetadataType.SByte:
					return TypeCode.SByte;
				case MetadataType.Byte:
					return TypeCode.Byte;
				case MetadataType.Int16:
					return TypeCode.Int16;
				case MetadataType.UInt16:
					return TypeCode.UInt16;
				case MetadataType.Int32:
					return TypeCode.Int32;
				case MetadataType.UInt32:
					return TypeCode.UInt32;
				case MetadataType.Int64:
					return TypeCode.Int64;
				case MetadataType.UInt64:
					return TypeCode.UInt64;
				case MetadataType.Single:
					return TypeCode.Single;
				case MetadataType.Double:
					return TypeCode.Double;
				case MetadataType.String:
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
		
		public static bool IsSameType(TypeReference type1, TypeReference type2)
		{
			if (type1 == type2)
				return true;
			if (type1 == null || type2 == null)
				return false;
			return type1.FullName == type2.FullName; // TODO: implement this more efficiently?
		}
	}
}
