// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

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
			ta.InferTypes(method);
			ta.InferRemainingStores();
			// Now that stores were inferred, we can infer the remaining instructions that depended on those stored
			// (but which didn't provide an expected type for the store)
			// For example, this is necessary to make a switch() over a generated variable work correctly.
			ta.InferTypes(method);
		}
		
		DecompilerContext context;
		TypeSystem typeSystem;
		ILBlock method;
		ModuleDefinition module;
		List<ILExpression> storedToGeneratedVariables = new List<ILExpression>();
		HashSet<ILVariable> inferredVariables = new HashSet<ILVariable>();
		
		void InferTypes(ILNode node)
		{
			ILCondition cond = node as ILCondition;
			if (cond != null) {
				cond.Condition.ExpectedType = typeSystem.Boolean;
			}
			ILWhileLoop loop = node as ILWhileLoop;
			if (loop != null && loop.Condition != null) {
				loop.Condition.ExpectedType = typeSystem.Boolean;
			}
			ILExpression expr = node as ILExpression;
			if (expr != null) {
				foreach (ILExpression store in expr.GetSelfAndChildrenRecursive<ILExpression>(e => e.Code == ILCode.Stloc)) {
					ILVariable v = (ILVariable)store.Operand;
					if (v.IsGenerated && v.Type == null && !inferredVariables.Contains(v) && HasSingleLoad(v)) {
						// Don't deal with this node or its children yet,
						// wait for the expected type to be inferred first.
						// This happens with the arg_... variables introduced by the ILAst - we skip inferring the whole statement,
						// and first infer the statement that reads from the arg_... variable.
						// The ldloc inference will write the expected type to the variable, and the next InferRemainingStores() pass
						// will then infer this statement with the correct expected type.
						storedToGeneratedVariables.Add(expr);
						// However, it is possible that this statement both writes to and reads from the variable (think inlined assignments).
						if (expr.GetSelfAndChildrenRecursive<ILExpression>(e => e.Code == ILCode.Ldlen && e.Operand == v).Any()) {
							// In this case, we analyze it now anyways, and will re-evaluate it later
							break;
						} else {
							return;
						}
					}
				}
				bool anyArgumentIsMissingType = expr.Arguments.Any(a => a.InferredType == null);
				if (expr.InferredType == null || anyArgumentIsMissingType)
					expr.InferredType = InferTypeForExpression(expr, expr.ExpectedType, forceInferChildren: anyArgumentIsMissingType);
			}
			foreach (ILNode child in node.GetChildren()) {
				InferTypes(child);
			}
		}
		
		bool HasSingleLoad(ILVariable v)
		{
			int loads = 0;
			foreach (ILExpression expr in method.GetSelfAndChildrenRecursive<ILExpression>()) {
				if (expr.Operand == v) {
					if (expr.Code == ILCode.Ldloc)
						loads++;
					else if (expr.Code != ILCode.Stloc)
						return false;
				}
			}
			return loads == 1;
		}
		
		void InferRemainingStores()
		{
			while (storedToGeneratedVariables.Count > 0) {
				List<ILExpression> stored = storedToGeneratedVariables;
				storedToGeneratedVariables = new List<ILExpression>();
				foreach (ILExpression expr in stored)
					InferTypes(expr);
				if (!(storedToGeneratedVariables.Count < stored.Count))
					throw new InvalidOperationException("Infinite loop in type analysis detected.");
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
			if (expectedType != null && expr.ExpectedType != expectedType) {
				expr.ExpectedType = expectedType;
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
					if (forceInferChildren) {
						InferTypeForExpression(expr.Arguments[0], typeSystem.Boolean);
						InferTypeForExpression(expr.Arguments[1], typeSystem.Boolean);
					}
					return typeSystem.Boolean;
				case ILCode.TernaryOp:
					if (forceInferChildren) {
						InferTypeForExpression(expr.Arguments[0], typeSystem.Boolean);
					}
					return TypeWithMoreInformation(
						InferTypeForExpression(expr.Arguments[1], expectedType, forceInferChildren),
						InferTypeForExpression(expr.Arguments[2], expectedType, forceInferChildren)
					);
				case ILCode.NullCoalescing:
					return TypeWithMoreInformation(
						InferTypeForExpression(expr.Arguments[0], expectedType, forceInferChildren),
						InferTypeForExpression(expr.Arguments[1], expectedType, forceInferChildren)
					);
					#endregion
					#region Variable load/store
				case ILCode.Stloc:
					{
						ILVariable v = (ILVariable)expr.Operand;
						if (forceInferChildren || v.Type == null) {
							TypeReference t = InferTypeForExpression(expr.Arguments.Single(), ((ILVariable)expr.Operand).Type);
							if (v.Type == null)
								v.Type = t;
						}
						return v.Type;
					}
				case ILCode.Ldloc:
					{
						ILVariable v = (ILVariable)expr.Operand;
						if (v.Type == null) {
							v.Type = expectedType;
							// Mark the variable as inferred. This is necessary because expectedType might be null
							// (e.g. the only use of an arg_*-Variable is a pop statement),
							// so we can't tell from v.Type whether it was already inferred.
							inferredVariables.Add(v);
						}
						return v.Type;
					}
				case ILCode.Ldloca:
					return new ByReferenceType(((ILVariable)expr.Operand).Type);
					#endregion
					#region Call / NewObj
				case ILCode.Call:
				case ILCode.Callvirt:
					{
						MethodReference method = (MethodReference)expr.Operand;
						if (forceInferChildren) {
							for (int i = 0; i < expr.Arguments.Count; i++) {
								if (i == 0 && method.HasThis) {
									Instruction constraint = expr.GetPrefix(Code.Constrained);
									if (constraint != null)
										InferTypeForExpression(expr.Arguments[i], new ByReferenceType((TypeReference)constraint.Operand));
									else if (method.DeclaringType.IsValueType)
										InferTypeForExpression(expr.Arguments[i], new ByReferenceType(method.DeclaringType));
									else
										InferTypeForExpression(expr.Arguments[i], method.DeclaringType);
								} else {
									InferTypeForExpression(expr.Arguments[i], SubstituteTypeArgs(method.Parameters[method.HasThis ? i - 1: i].ParameterType, method));
								}
							}
						}
						return SubstituteTypeArgs(method.ReturnType, method);
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
				case ILCode.InitCollection:
					return InferTypeForExpression(expr.Arguments[0], expectedType);
				case ILCode.InitCollectionAddMethod:
					{
						MethodReference addMethod = (MethodReference)expr.Operand;
						if (forceInferChildren) {
							for (int i = 1; i < addMethod.Parameters.Count; i++) {
								InferTypeForExpression(expr.Arguments[i-1], SubstituteTypeArgs(addMethod.Parameters[i].ParameterType, addMethod));
							}
						}
						return addMethod.DeclaringType;
					}
					#endregion
					#region Load/Store Fields
				case ILCode.Ldfld:
					if (forceInferChildren)
						InferTypeForExpression(expr.Arguments[0], ((FieldReference)expr.Operand).DeclaringType);
					return GetFieldType((FieldReference)expr.Operand);
				case ILCode.Ldsfld:
					return GetFieldType((FieldReference)expr.Operand);
				case ILCode.Ldflda:
				case ILCode.Ldsflda:
					return new ByReferenceType(GetFieldType((FieldReference)expr.Operand));
				case ILCode.Stfld:
					if (forceInferChildren) {
						InferTypeForExpression(expr.Arguments[0], ((FieldReference)expr.Operand).DeclaringType);
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
							if (infoAmount == 1) infoAmount = 8;
							if (infoAmount == GetInformationAmount(operandType) && IsSigned(elementType) != null && IsSigned(operandType) != null)
								operandType = elementType;
						}
						if (forceInferChildren) {
							if (pointerType is PointerType)
								InferTypeForExpression(expr.Arguments[0], new PointerType(operandType));
							else if (operandType != expr.Operand)
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
					#endregion
					#region Arithmetic instructions
				case ILCode.Not: // bitwise complement
				case ILCode.Neg:
					return InferTypeForExpression(expr.Arguments.Single(), expectedType);
				case ILCode.Add:
				case ILCode.Sub:
				case ILCode.Mul:
				case ILCode.Or:
				case ILCode.And:
				case ILCode.Xor:
					return InferArgumentsInBinaryOperator(expr, null, expectedType);
				case ILCode.Add_Ovf:
				case ILCode.Sub_Ovf:
				case ILCode.Mul_Ovf:
				case ILCode.Div:
				case ILCode.Rem:
					return InferArgumentsInBinaryOperator(expr, true, expectedType);
				case ILCode.Add_Ovf_Un:
				case ILCode.Sub_Ovf_Un:
				case ILCode.Mul_Ovf_Un:
				case ILCode.Div_Un:
				case ILCode.Rem_Un:
					return InferArgumentsInBinaryOperator(expr, false, expectedType);
				case ILCode.Shl:
				case ILCode.Shr:
					if (forceInferChildren)
						InferTypeForExpression(expr.Arguments[1], typeSystem.Int32);
					return InferTypeForExpression(expr.Arguments[0], typeSystem.Int32);
				case ILCode.Shr_Un:
					if (forceInferChildren)
						InferTypeForExpression(expr.Arguments[1], typeSystem.Int32);
					return InferTypeForExpression(expr.Arguments[0], typeSystem.UInt32);
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
					return (IsIntegerOrEnum(expectedType) || expectedType is PointerType) ? expectedType : typeSystem.Int32;
				case ILCode.Ldc_I8:
					return (IsIntegerOrEnum(expectedType) || expectedType is PointerType) ? expectedType : typeSystem.Int64;
				case ILCode.Ldc_R4:
					return typeSystem.Single;
				case ILCode.Ldc_R8:
					return typeSystem.Double;
				case ILCode.Ldc_Decimal:
					Debug.Assert(expr.InferredType != null && expr.InferredType.FullName == "System.Decimal");
					return expr.InferredType;
				case ILCode.Ldtoken:
					if (expr.Operand is TypeReference)
						return new TypeReference("System", "RuntimeTypeHandle", module, module, true);
					else if (expr.Operand is FieldReference)
						return new TypeReference("System", "RuntimeFieldHandle", module, module, true);
					else
						return new TypeReference("System", "RuntimeMethodHandle", module, module, true);
				case ILCode.Arglist:
					return new TypeReference("System", "RuntimeArgumentHandle", module, module, true);
					#endregion
					#region Array instructions
				case ILCode.Newarr:
					if (forceInferChildren)
						InferTypeForExpression(expr.Arguments.Single(), typeSystem.Int32);
					return new ArrayType((TypeReference)expr.Operand);
				case ILCode.InitArray:
					if (forceInferChildren) {
						foreach (ILExpression arg in expr.Arguments)
							InferTypeForExpression(arg, (TypeReference)expr.Operand);
					}
					return new ArrayType((TypeReference)expr.Operand);
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
					if (forceInferChildren) {
						ArrayType arrayType = InferTypeForExpression(expr.Arguments[0], null) as ArrayType;
						InferTypeForExpression(expr.Arguments[1], typeSystem.Int32);
						if (arrayType != null) {
							InferTypeForExpression(expr.Arguments[2], arrayType.ElementType);
						}
					}
					return null;
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
					return HandleConversion(nativeInt, true, expr.Arguments[0], expectedType, typeSystem.IntPtr);
				case ILCode.Conv_U:
				case ILCode.Conv_Ovf_U:
				case ILCode.Conv_Ovf_U_Un:
					return HandleConversion(nativeInt, false, expr.Arguments[0], expectedType, typeSystem.UIntPtr);
				case ILCode.Conv_R4:
					return typeSystem.Single;
				case ILCode.Conv_R8:
					return typeSystem.Double;
				case ILCode.Conv_R_Un:
					return (expectedType != null  && expectedType.MetadataType == MetadataType.Single) ? typeSystem.Single : typeSystem.Double;
				case ILCode.Castclass:
				case ILCode.Isinst:
				case ILCode.Unbox_Any:
					return (TypeReference)expr.Operand;
				case ILCode.Box:
					if (forceInferChildren)
						InferTypeForExpression(expr.Arguments.Single(), (TypeReference)expr.Operand);
					return (TypeReference)expr.Operand;
					#endregion
					#region Comparison instructions
				case ILCode.Ceq:
					if (forceInferChildren)
						InferArgumentsInBinaryOperator(expr, null, null);
					return typeSystem.Boolean;
				case ILCode.Clt:
				case ILCode.Cgt:
					if (forceInferChildren)
						InferArgumentsInBinaryOperator(expr, true, null);
					return typeSystem.Boolean;
				case ILCode.Clt_Un:
				case ILCode.Cgt_Un:
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
					if (forceInferChildren && expr.Arguments.Count == 1)
						InferTypeForExpression(expr.Arguments[0], context.CurrentMethod.ReturnType);
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
					#endregion
				case ILCode.Pop:
					return null;
				case ILCode.Dup:
					return InferTypeForExpression(expr.Arguments.Single(), expectedType);
				default:
					Debug.WriteLine("Type Inference: Can't handle " + expr.Code.GetName());
					return null;
			}
		}
		
		TypeReference HandleConversion(int targetBitSize, bool targetSigned, ILExpression arg, TypeReference expectedType, TypeReference targetType)
		{
			if (targetBitSize >= nativeInt && expectedType is PointerType) {
				InferTypeForExpression(arg, expectedType);
				return expectedType;
			}
			TypeReference argType = InferTypeForExpression(arg, null);
			if (targetBitSize >= nativeInt && argType is ByReferenceType) {
				// conv instructions on managed references mean that the GC should stop tracking them, so they become pointers:
				PointerType ptrType = new PointerType(((ByReferenceType)argType).ElementType);
				InferTypeForExpression(arg, ptrType);
				return ptrType;
			} else if (targetBitSize >= nativeInt && argType is PointerType) {
				return argType;
			}
			return (GetInformationAmount(expectedType) == targetBitSize && IsSigned(expectedType) == targetSigned) ? expectedType : targetType;
		}
		
		static TypeReference GetFieldType(FieldReference fieldReference)
		{
			return SubstituteTypeArgs(UnpackModifiers(fieldReference.FieldType), fieldReference);
		}
		
		static TypeReference SubstituteTypeArgs(TypeReference type, MemberReference member)
		{
			if (type is TypeSpecification) {
				ArrayType arrayType = type as ArrayType;
				if (arrayType != null) {
					TypeReference elementType = SubstituteTypeArgs(arrayType.ElementType, member);
					if (elementType != arrayType.ElementType) {
						ArrayType newArrayType = new ArrayType(elementType);
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
				if (gp.Owner.GenericParameterType == GenericParameterType.Method) {
					return ((GenericInstanceMethod)member).GenericArguments[gp.Position];
				} else {
					if (member.DeclaringType is ArrayType) {
						return ((ArrayType)member.DeclaringType).ElementType;
					} else {
						return ((GenericInstanceType)member.DeclaringType).GenericArguments[gp.Position];
					}
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
		
		static TypeReference UnpackModifiers(TypeReference type)
		{
			while (type is OptionalModifierType || type is RequiredModifierType)
				type = ((TypeSpecification)type).ElementType;
			return type;
		}
		
		TypeReference InferArgumentsInBinaryOperator(ILExpression expr, bool? isSigned, TypeReference expectedType)
		{
			ILExpression left = expr.Arguments[0];
			ILExpression right = expr.Arguments[1];
			TypeReference leftPreferred = DoInferTypeForExpression(left, expectedType);
			TypeReference rightPreferred = DoInferTypeForExpression(right, expectedType);
			if (leftPreferred == rightPreferred) {
				return left.InferredType = right.InferredType = left.ExpectedType = right.ExpectedType = leftPreferred;
			} else if (rightPreferred == DoInferTypeForExpression(left, rightPreferred)) {
				return left.InferredType = right.InferredType = left.ExpectedType = right.ExpectedType = rightPreferred;
			} else if (leftPreferred == DoInferTypeForExpression(right, leftPreferred)) {
				return left.InferredType = right.InferredType = left.ExpectedType = right.ExpectedType = leftPreferred;
			} else {
				left.ExpectedType = right.ExpectedType = TypeWithMoreInformation(leftPreferred, rightPreferred);
				left.InferredType = DoInferTypeForExpression(left, left.ExpectedType);
				right.InferredType = DoInferTypeForExpression(right, right.ExpectedType);
				return left.ExpectedType;
			}
		}
		
		TypeReference TypeWithMoreInformation(TypeReference leftPreferred, TypeReference rightPreferred)
		{
			int left = GetInformationAmount(leftPreferred);
			int right = GetInformationAmount(rightPreferred);
			if (left < right)
				return rightPreferred;
			else
				return leftPreferred;
		}
		
		const int nativeInt = 33; // treat native int as between int32 and int64
		
		static int GetInformationAmount(TypeReference type)
		{
			if (type == null)
				return 0;
			if (type.IsValueType) {
				// value type might be an enum
				TypeDefinition typeDef = type.Resolve() as TypeDefinition;
				if (typeDef != null && typeDef.IsEnum) {
					TypeReference underlyingType = typeDef.Fields.Single(f => f.IsRuntimeSpecialName && !f.IsStatic).FieldType;
					return GetInformationAmount(underlyingType);
				}
			}
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
					return nativeInt;
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
			if (type == null)
				return false;
			// unfortunately we cannot rely on type.IsValueType here - it's not set when the instruction operand is a typeref (as opposed to a typespec)
			TypeDefinition typeDef = type.Resolve() as TypeDefinition;
			return typeDef != null && typeDef.IsEnum;
		}
		
		static bool? IsSigned(TypeReference type)
		{
			if (type == null)
				return null;
			// unfortunately we cannot rely on type.IsValueType here - it's not set when the instruction operand is a typeref (as opposed to a typespec)
			TypeDefinition typeDef = type.Resolve() as TypeDefinition;
			if (typeDef != null && typeDef.IsEnum) {
				TypeReference underlyingType = typeDef.Fields.Single(f => f.IsRuntimeSpecialName && !f.IsStatic).FieldType;
				return IsSigned(underlyingType);
			}
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
	}
}
