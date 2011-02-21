// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Decompiler;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Decompiler
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
		}
		
		DecompilerContext context;
		TypeSystem typeSystem;
		ILBlock method;
		ModuleDefinition module;
		List<ILExpression> storedToGeneratedVariables = new List<ILExpression>();
		HashSet<ILVariable> inferredVariables = new HashSet<ILVariable>();
		
		void InferTypes(ILNode node)
		{
			ILExpression expr = node as ILExpression;
			if (expr != null) {
				ILVariable v = expr.Operand as ILVariable;
				if (v != null && v.IsGenerated && v.Type == null && expr.Code == ILCode.Stloc && !inferredVariables.Contains(v) && HasSingleLoad(v)) {
					// Don't deal with this node or its children yet,
					// wait for the expected type to be inferred first.
					// This happens with the arg_... variables introduced by the ILAst - we skip inferring the whole statement,
					// and first infer the statement that reads from the arg_... variable.
					// The ldloc inference will write the expected type to the variable, and the next InferRemainingStores() pass
					// will then infer this statement with the correct expected type.
					storedToGeneratedVariables.Add(expr);
					return;
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
			expr.ExpectedType = expectedType;
			if (forceInferChildren || expr.InferredType == null)
				expr.InferredType = DoInferTypeForExpression(expr, expectedType, forceInferChildren);
			return expr.InferredType;
		}
		
		TypeReference DoInferTypeForExpression(ILExpression expr, TypeReference expectedType, bool forceInferChildren = false)
		{
			switch (expr.Code) {
				case ILCode.LogicNot:
					if (forceInferChildren) {
						InferTypeForExpression(expr.Arguments.Single(), typeSystem.Boolean);
					}
					return typeSystem.Boolean;
				case ILCode.LogicAnd:
				case ILCode.LogicOr:
					if (forceInferChildren) {
						InferTypeForExpression(expr.Arguments[0], typeSystem.Boolean);
						InferTypeForExpression(expr.Arguments[0], typeSystem.Boolean);
					}
					return typeSystem.Boolean;
			}
			switch ((Code)expr.Code) {
					#region Variable load/store
				case Code.Stloc:
					{
						ILVariable v = (ILVariable)expr.Operand;
						if (forceInferChildren || v.Type == null) {
							TypeReference t = InferTypeForExpression(expr.Arguments.Single(), ((ILVariable)expr.Operand).Type);
							if (v.Type == null)
								v.Type = t;
						}
						return v.Type;
					}
				case Code.Ldloc:
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
				case Code.Starg:
					if (forceInferChildren)
						InferTypeForExpression(expr.Arguments.Single(), ((ParameterReference)expr.Operand).ParameterType);
					return null;
				case Code.Ldarg:
					return ((ParameterReference)expr.Operand).ParameterType;
				case Code.Ldloca:
					return new ByReferenceType(((ILVariable)expr.Operand).Type);
				case Code.Ldarga:
					return new ByReferenceType(((ParameterReference)expr.Operand).ParameterType);
					#endregion
					#region Call / NewObj
				case Code.Call:
				case Code.Callvirt:
					{
						MethodReference method = (MethodReference)expr.Operand;
						if (forceInferChildren) {
							for (int i = 0; i < expr.Arguments.Count; i++) {
								if (i == 0 && method.HasThis) {
									Instruction constraint = expr.GetPrefix(Code.Constrained);
									if (constraint != null)
										InferTypeForExpression(expr.Arguments[i], new ByReferenceType((TypeReference)constraint.Operand));
									else
										InferTypeForExpression(expr.Arguments[i], method.DeclaringType);
								} else {
									InferTypeForExpression(expr.Arguments[i], SubstituteTypeArgs(method.Parameters[method.HasThis ? i - 1: i].ParameterType, method));
								}
							}
						}
						return SubstituteTypeArgs(method.ReturnType, method);
					}
				case Code.Newobj:
					{
						MethodReference ctor = (MethodReference)expr.Operand;
						if (forceInferChildren) {
							for (int i = 0; i < ctor.Parameters.Count; i++) {
								InferTypeForExpression(expr.Arguments[i], SubstituteTypeArgs(ctor.Parameters[i].ParameterType, ctor));
							}
						}
						return ctor.DeclaringType;
					}
					#endregion
					#region Load/Store Fields
				case Code.Ldfld:
					if (forceInferChildren)
						InferTypeForExpression(expr.Arguments[0], ((FieldReference)expr.Operand).DeclaringType);
					return GetFieldType((FieldReference)expr.Operand);
				case Code.Ldsfld:
					return GetFieldType((FieldReference)expr.Operand);
				case Code.Ldflda:
				case Code.Ldsflda:
					return new ByReferenceType(GetFieldType((FieldReference)expr.Operand));
				case Code.Stfld:
					if (forceInferChildren) {
						InferTypeForExpression(expr.Arguments[0], ((FieldReference)expr.Operand).DeclaringType);
						InferTypeForExpression(expr.Arguments[1], GetFieldType((FieldReference)expr.Operand));
					}
					return null;
				case Code.Stsfld:
					if (forceInferChildren)
						InferTypeForExpression(expr.Arguments[0], GetFieldType((FieldReference)expr.Operand));
					return null;
					#endregion
					#region Reference/Pointer instructions
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
					return UnpackPointer(InferTypeForExpression(expr.Arguments[0], null));
				case Code.Stind_I1:
				case Code.Stind_I2:
				case Code.Stind_I4:
				case Code.Stind_I8:
				case Code.Stind_R4:
				case Code.Stind_R8:
				case Code.Stind_I:
				case Code.Stind_Ref:
					if (forceInferChildren) {
						TypeReference elementType = UnpackPointer(InferTypeForExpression(expr.Arguments[0], null));
						InferTypeForExpression(expr.Arguments[1], elementType);
					}
					return null;
				case Code.Ldobj:
					return (TypeReference)expr.Operand;
				case Code.Stobj:
					if (forceInferChildren) {
						InferTypeForExpression(expr.Arguments[1], (TypeReference)expr.Operand);
					}
					return null;
				case Code.Initobj:
					return null;
				case Code.Localloc:
					return typeSystem.IntPtr;
					#endregion
					#region Arithmetic instructions
				case Code.Not: // bitwise complement
				case Code.Neg:
					return InferTypeForExpression(expr.Arguments.Single(), expectedType);
				case Code.Add:
				case Code.Sub:
				case Code.Mul:
				case Code.Or:
				case Code.And:
				case Code.Xor:
					return InferArgumentsInBinaryOperator(expr, null);
				case Code.Add_Ovf:
				case Code.Sub_Ovf:
				case Code.Mul_Ovf:
				case Code.Div:
				case Code.Rem:
					return InferArgumentsInBinaryOperator(expr, true);
				case Code.Add_Ovf_Un:
				case Code.Sub_Ovf_Un:
				case Code.Mul_Ovf_Un:
				case Code.Div_Un:
				case Code.Rem_Un:
					return InferArgumentsInBinaryOperator(expr, false);
				case Code.Shl:
				case Code.Shr:
					if (forceInferChildren)
						InferTypeForExpression(expr.Arguments[1], typeSystem.Int32);
					return InferTypeForExpression(expr.Arguments[0], typeSystem.Int32);
				case Code.Shr_Un:
					if (forceInferChildren)
						InferTypeForExpression(expr.Arguments[1], typeSystem.Int32);
					return InferTypeForExpression(expr.Arguments[0], typeSystem.UInt32);
					#endregion
					#region Constant loading instructions
				case Code.Ldnull:
					return typeSystem.Object;
				case Code.Ldstr:
					return typeSystem.String;
				case Code.Ldftn:
				case Code.Ldvirtftn:
					return typeSystem.IntPtr;
				case Code.Ldc_I4:
					return (IsIntegerOrEnum(expectedType) || expectedType == typeSystem.Boolean) ? expectedType : typeSystem.Int32;
				case Code.Ldc_I8:
					return (IsIntegerOrEnum(expectedType)) ? expectedType : typeSystem.Int64;
				case Code.Ldc_R4:
					return typeSystem.Single;
				case Code.Ldc_R8:
					return typeSystem.Double;
				case Code.Ldtoken:
					if (expr.Operand is TypeReference)
						return new TypeReference("System", "RuntimeTypeHandle", module, module, true);
					else if (expr.Operand is FieldReference)
						return new TypeReference("System", "RuntimeFieldHandle", module, module, true);
					else
						return new TypeReference("System", "RuntimeMethodHandle", module, module, true);
				case Code.Arglist:
					return new TypeReference("System", "RuntimeArgumentHandle", module, module, true);
					#endregion
					#region Array instructions
				case Code.Newarr:
					if (forceInferChildren)
						InferTypeForExpression(expr.Arguments.Single(), typeSystem.Int32);
					return new ArrayType((TypeReference)expr.Operand);
				case Code.Ldlen:
					return typeSystem.Int32;
				case Code.Ldelem_U1:
				case Code.Ldelem_U2:
				case Code.Ldelem_U4:
				case Code.Ldelem_I1:
				case Code.Ldelem_I2:
				case Code.Ldelem_I4:
				case Code.Ldelem_I8:
				case Code.Ldelem_I:
				case Code.Ldelem_Ref:
					{
						ArrayType arrayType = InferTypeForExpression(expr.Arguments[0], null) as ArrayType;
						if (forceInferChildren) {
							InferTypeForExpression(expr.Arguments[0], new ArrayType(typeSystem.Byte));
							InferTypeForExpression(expr.Arguments[1], typeSystem.Int32);
						}
						return arrayType != null ? arrayType.ElementType : null;
					}
				case Code.Ldelem_Any:
					if (forceInferChildren) {
						InferTypeForExpression(expr.Arguments[1], typeSystem.Int32);
					}
					return (TypeReference)expr.Operand;
				case Code.Ldelema:
					{
						ArrayType arrayType = InferTypeForExpression(expr.Arguments[0], null) as ArrayType;
						if (forceInferChildren)
							InferTypeForExpression(expr.Arguments[1], typeSystem.Int32);
						return arrayType != null ? new ByReferenceType(arrayType.ElementType) : null;
					}
				case Code.Stelem_I:
				case Code.Stelem_I1:
				case Code.Stelem_I2:
				case Code.Stelem_I4:
				case Code.Stelem_I8:
				case Code.Stelem_R4:
				case Code.Stelem_R8:
				case Code.Stelem_Ref:
				case Code.Stelem_Any:
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
				case Code.Conv_I1:
				case Code.Conv_Ovf_I1:
					return (GetInformationAmount(expectedType) == 8 && IsSigned(expectedType) == true) ? expectedType : typeSystem.SByte;
				case Code.Conv_I2:
				case Code.Conv_Ovf_I2:
					return (GetInformationAmount(expectedType) == 16 && IsSigned(expectedType) == true) ? expectedType : typeSystem.Int16;
				case Code.Conv_I4:
				case Code.Conv_Ovf_I4:
					return (GetInformationAmount(expectedType) == 32 && IsSigned(expectedType) == true) ? expectedType : typeSystem.Int32;
				case Code.Conv_I8:
				case Code.Conv_Ovf_I8:
					return (GetInformationAmount(expectedType) == 64 && IsSigned(expectedType) == true) ? expectedType : typeSystem.Int64;
				case Code.Conv_U1:
				case Code.Conv_Ovf_U1:
					return (GetInformationAmount(expectedType) == 8 && IsSigned(expectedType) == false) ? expectedType : typeSystem.Byte;
				case Code.Conv_U2:
				case Code.Conv_Ovf_U2:
					return (GetInformationAmount(expectedType) == 16 && IsSigned(expectedType) == false) ? expectedType : typeSystem.UInt16;
				case Code.Conv_U4:
				case Code.Conv_Ovf_U4:
					return (GetInformationAmount(expectedType) == 32 && IsSigned(expectedType) == false) ? expectedType : typeSystem.UInt32;
				case Code.Conv_U8:
				case Code.Conv_Ovf_U8:
					return (GetInformationAmount(expectedType) == 64 && IsSigned(expectedType) == false) ? expectedType : typeSystem.UInt64;
				case Code.Conv_I:
				case Code.Conv_Ovf_I:
					return (GetInformationAmount(expectedType) == nativeInt && IsSigned(expectedType) == true) ? expectedType : typeSystem.IntPtr;
				case Code.Conv_U:
				case Code.Conv_Ovf_U:
					return (GetInformationAmount(expectedType) == nativeInt && IsSigned(expectedType) == false) ? expectedType : typeSystem.UIntPtr;
				case Code.Conv_R4:
					return typeSystem.Single;
				case Code.Conv_R8:
					return typeSystem.Double;
				case Code.Conv_R_Un:
					return (expectedType == typeSystem.Single) ? typeSystem.Single : typeSystem.Double;
				case Code.Castclass:
				case Code.Isinst:
				case Code.Unbox_Any:
					return (TypeReference)expr.Operand;
				case Code.Box:
					if (forceInferChildren)
						InferTypeForExpression(expr.Arguments.Single(), (TypeReference)expr.Operand);
					return (TypeReference)expr.Operand;
					#endregion
					#region Comparison instructions
				case Code.Ceq:
					if (forceInferChildren)
						InferArgumentsInBinaryOperator(expr, null);
					return typeSystem.Boolean;
				case Code.Clt:
				case Code.Cgt:
					if (forceInferChildren)
						InferArgumentsInBinaryOperator(expr, true);
					return typeSystem.Boolean;
				case Code.Clt_Un:
				case Code.Cgt_Un:
					if (forceInferChildren)
						InferArgumentsInBinaryOperator(expr, false);
					return typeSystem.Boolean;
					#endregion
					#region Branch instructions
				case Code.Beq:
				case Code.Bne_Un:
					if (forceInferChildren)
						InferArgumentsInBinaryOperator(expr, null);
					return null;
				case Code.Brtrue:
				case Code.Brfalse:
					if (forceInferChildren)
						InferTypeForExpression(expr.Arguments.Single(), typeSystem.Boolean);
					return null;
				case Code.Blt:
				case Code.Ble:
				case Code.Bgt:
				case Code.Bge:
					if (forceInferChildren)
						InferArgumentsInBinaryOperator(expr, true);
					return null;
				case Code.Blt_Un:
				case Code.Ble_Un:
				case Code.Bgt_Un:
				case Code.Bge_Un:
					if (forceInferChildren)
						InferArgumentsInBinaryOperator(expr, false);
					return null;
				case Code.Br:
				case Code.Leave:
				case Code.Endfinally:
				case Code.Switch:
				case Code.Throw:
				case Code.Rethrow:
					return null;
				case Code.Ret:
					if (forceInferChildren && expr.Arguments.Count == 1)
						InferTypeForExpression(expr.Arguments[0], context.CurrentMethod.ReturnType);
					return null;
					#endregion
				case Code.Pop:
					return null;
				case Code.Dup:
					return InferTypeForExpression(expr.Arguments.Single(), expectedType);
				default:
					Debug.WriteLine("Type Inference: Can't handle " + expr.Code.GetName());
					return null;
			}
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
		
		static TypeReference UnpackModifiers(TypeReference type)
		{
			while (type is OptionalModifierType || type is RequiredModifierType)
				type = ((TypeSpecification)type).ElementType;
			return type;
		}
		
		TypeReference InferArgumentsInBinaryOperator(ILExpression expr, bool? isSigned)
		{
			ILExpression left = expr.Arguments[0];
			ILExpression right = expr.Arguments[1];
			TypeReference leftPreferred = DoInferTypeForExpression(left, null);
			TypeReference rightPreferred = DoInferTypeForExpression(right, null);
			if (leftPreferred == rightPreferred) {
				return left.InferredType = right.InferredType = left.ExpectedType = right.ExpectedType = leftPreferred;
			} else if (rightPreferred == DoInferTypeForExpression(left, rightPreferred)) {
				return left.InferredType = right.InferredType = left.ExpectedType = right.ExpectedType = rightPreferred;
			} else if (leftPreferred == DoInferTypeForExpression(right, leftPreferred)) {
				return left.InferredType = right.InferredType = left.ExpectedType = right.ExpectedType = leftPreferred;
			} else {
				left.ExpectedType = right.ExpectedType = TypeWithMoreInformation(leftPreferred, rightPreferred);
				left.InferredType = DoInferTypeForExpression(left, left.ExpectedType);
				right.InferredType = DoInferTypeForExpression(left, right.ExpectedType);
				return left.ExpectedType;
			}
		}
		
		TypeReference TypeWithMoreInformation(TypeReference leftPreferred, TypeReference rightPreferred)
		{
			int left = GetInformationAmount(typeSystem, leftPreferred);
			int right = GetInformationAmount(typeSystem, rightPreferred);
			if (left < right)
				return rightPreferred;
			else
				return leftPreferred;
		}
		
		int GetInformationAmount(TypeReference type)
		{
			return GetInformationAmount(typeSystem, type);
		}
		
		const int nativeInt = 33; // treat native int as between int32 and int64
		
		static int GetInformationAmount(TypeSystem typeSystem, TypeReference type)
		{
			if (type == null)
				return 0;
			if (type.IsValueType) {
				// value type might be an enum
				TypeDefinition typeDef = type.Resolve() as TypeDefinition;
				if (typeDef != null && typeDef.IsEnum) {
					TypeReference underlyingType = typeDef.Fields.Single(f => f.IsRuntimeSpecialName && !f.IsStatic).FieldType;
					return GetInformationAmount(typeDef.Module.TypeSystem, underlyingType);
				}
			}
			if (type == typeSystem.Boolean)
				return 1;
			else if (type == typeSystem.Byte || type == typeSystem.SByte)
				return 8;
			else if (type == typeSystem.Int16 || type == typeSystem.UInt16)
				return 16;
			else if (type == typeSystem.Int32 || type == typeSystem.UInt32)
				return 32;
			else if (type == typeSystem.IntPtr || type == typeSystem.UIntPtr)
				return nativeInt;
			else if (type == typeSystem.Int64 || type == typeSystem.UInt64)
				return 64;
			return 100; // we consider structs/objects to have more information than any primitives
		}
		
		bool IsIntegerOrEnum(TypeReference type)
		{
			return IsIntegerOrEnum(typeSystem, type);
		}
		
		public static bool IsIntegerOrEnum(TypeSystem typeSystem, TypeReference type)
		{
			return IsSigned(typeSystem, type) != null;
		}
		
		bool? IsSigned(TypeReference type)
		{
			return IsSigned(typeSystem, type);
		}
		
		static bool? IsSigned(TypeSystem typeSystem, TypeReference type)
		{
			if (type == null)
				return null;
			// unfortunately we cannot rely on type.IsValueType here - it's not set when the instruction operand is a typeref (as opposed to a typespec)
			TypeDefinition typeDef = type.Resolve() as TypeDefinition;
			if (typeDef != null && typeDef.IsEnum) {
				TypeReference underlyingType = typeDef.Fields.Single(f => f.IsRuntimeSpecialName && !f.IsStatic).FieldType;
				return IsSigned(typeDef.Module.TypeSystem, underlyingType);
			}
			if (type == typeSystem.Byte || type == typeSystem.UInt16 || type == typeSystem.UInt32 || type == typeSystem.UInt64 || type == typeSystem.UIntPtr)
				return false;
			if (type == typeSystem.SByte || type == typeSystem.Int16 || type == typeSystem.Int32 || type == typeSystem.Int64 || type == typeSystem.IntPtr)
				return true;
			return null;
		}
		
		public static TypeCode GetTypeCode(TypeSystem typeSystem, TypeReference type)
		{
			if (type == typeSystem.Boolean)
				return TypeCode.Boolean;
			else if (type == typeSystem.Byte)
				return TypeCode.Byte;
			else if (type == typeSystem.Char)
				return TypeCode.Char;
			else if (type == typeSystem.Double)
				return TypeCode.Double;
			else if (type == typeSystem.Int16)
				return TypeCode.Int16;
			else if (type == typeSystem.Int32)
				return TypeCode.Int32;
			else if (type == typeSystem.Int64)
				return TypeCode.Int64;
			else if (type == typeSystem.Single)
				return TypeCode.Single;
			else if (type == typeSystem.Double)
				return TypeCode.Double;
			else if (type == typeSystem.SByte)
				return TypeCode.SByte;
			else if (type == typeSystem.UInt16)
				return TypeCode.UInt16;
			else if (type == typeSystem.UInt32)
				return TypeCode.UInt32;
			else if (type == typeSystem.UInt64)
				return TypeCode.UInt64;
			else if (type == typeSystem.String)
				return TypeCode.String;
			else
				return TypeCode.Object;
		}
	}
}
