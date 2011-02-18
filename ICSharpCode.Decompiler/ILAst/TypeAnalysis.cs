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
		public static void Run(TypeSystem typeSystem, ILNode node)
		{
			TypeAnalysis ta = new TypeAnalysis();
			ta.typeSystem = typeSystem;
			ta.InferTypes(node);
		}
		
		TypeSystem typeSystem;
		List<ILExpression> storedToGeneratedVariables = new List<ILExpression>();
		
		void InferTypes(ILNode node)
		{
			foreach (ILNode child in node.GetChildren()) {
				ILExpression expr = child as ILExpression;
				if (expr != null) {
					ILVariable v = expr.Operand as ILVariable;
					if (v != null && v.IsGenerated && v.Type == null && expr.OpCode == OpCodes.Stloc) {
						// don't deal with this node or its children yet,
						// wait for the expected type to be inferred first
						storedToGeneratedVariables.Add(expr);
						continue;
					}
					bool anyArgumentIsMissingType = expr.Arguments.Any(a => a.InferredType == null);
					if (expr.InferredType == null || anyArgumentIsMissingType)
						expr.InferredType = InferTypeForExpression(expr, null, forceInferChildren: anyArgumentIsMissingType);
				}
				InferTypes(child);
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
			if (forceInferChildren || expr.InferredType == null)
				expr.InferredType = DoInferTypeForExpression(expr, expectedType, forceInferChildren);
			return expr.InferredType;
		}
		
		TypeReference DoInferTypeForExpression(ILExpression expr, TypeReference expectedType, bool forceInferChildren = false)
		{
			switch (expr.OpCode.Code) {
				case Code.Stloc:
					if (forceInferChildren)
						InferTypeForExpression(expr.Arguments.Single(), ((ILVariable)expr.Operand).Type);
					return null;
				case Code.Ldloc:
					return ((ILVariable)expr.Operand).Type;
				case Code.Ldarg:
					return ((ParameterDefinition)expr.Operand).ParameterType;
				case Code.Call:
				case Code.Callvirt:
					{
						MethodReference method = (MethodReference)expr.Operand;
						if (forceInferChildren) {
							for (int i = 0; i < expr.Arguments.Count; i++) {
								if (i == 0 && method.HasThis)
									InferTypeForExpression(expr.Arguments[i], method.DeclaringType);
								else
									InferTypeForExpression(expr.Arguments[i], method.Parameters[method.HasThis ? i - 1: i].ParameterType);
							}
						}
						return method.ReturnType;
					}
				case Code.Newobj:
					{
						MethodReference ctor = (MethodReference)expr.Operand;
						if (forceInferChildren) {
							for (int i = 0; i < ctor.Parameters.Count; i++) {
								InferTypeForExpression(expr.Arguments[i], ctor.Parameters[i].ParameterType);
							}
						}
						return ctor.DeclaringType;
					}
				case Code.Ldfld:
					return UnpackModifiers(((FieldReference)expr.Operand).FieldType);
				case Code.Ldsfld:
					return UnpackModifiers(((FieldReference)expr.Operand).FieldType);
				case Code.Or:
					return InferArgumentsInBinaryOperator(expr);
				case Code.Shl:
				case Code.Shr:
					if (forceInferChildren)
						InferTypeForExpression(expr.Arguments[1], typeSystem.Int32);
					return InferTypeForExpression(expr.Arguments[0], expectedType);
				case Code.Ldc_I4:
					return (IsIntegerOrEnum(expectedType) || expectedType == typeSystem.Boolean) ? expectedType : typeSystem.Int32;
				case Code.Ldc_I8:
					return (IsIntegerOrEnum(expectedType)) ? expectedType : typeSystem.Int64;
				case Code.Conv_I8:
					return (GetInformationAmount(expectedType) == 64 && IsSigned(expectedType) == true) ? expectedType : typeSystem.Int64;
				case Code.Dup:
					return InferTypeForExpression(expr.Arguments.Single(), expectedType);
				case Code.Ceq:
				case Code.Clt:
					if (forceInferChildren)
						InferArgumentsInBinaryOperator(expr);
					return typeSystem.Boolean;
				case Code.Beq:
				case Code.Blt:
					if (forceInferChildren)
						InferArgumentsInBinaryOperator(expr);
					return null;
				case Code.Brtrue:
				case Code.Brfalse:
					if (forceInferChildren)
						InferTypeForExpression(expr.Arguments.Single(), typeSystem.Boolean);
					return null;
				default:
					//throw new NotImplementedException("Can't handle " + expr.OpCode.Name);
					return null;
			}
		}
		
		static TypeReference UnpackModifiers(TypeReference type)
		{
			while (type is OptionalModifierType || type is RequiredModifierType)
				type = ((TypeSpecification)type).ElementType;
			return type;
		}
		
		TypeReference InferArgumentsInBinaryOperator(ILExpression expr)
		{
			ILExpression left = expr.Arguments[0];
			ILExpression right = expr.Arguments[1];
			TypeReference leftPreferred = DoInferTypeForExpression(left, null);
			TypeReference rightPreferred = DoInferTypeForExpression(right, null);
			if (leftPreferred == rightPreferred) {
				return left.InferredType = right.InferredType = leftPreferred;
			} else if (rightPreferred == DoInferTypeForExpression(left, rightPreferred)) {
				return left.InferredType = right.InferredType = rightPreferred;
			} else if (leftPreferred == DoInferTypeForExpression(right, leftPreferred)) {
				return left.InferredType = right.InferredType = leftPreferred;
			} else {
				return left.InferredType = right.InferredType = TypeWithMoreInformation(leftPreferred, rightPreferred);
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
				return 33; // treat native int as between int32 and int64
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
			if (type.IsValueType) {
				// value type might be an enum
				TypeDefinition typeDef = type.Resolve() as TypeDefinition;
				if (typeDef != null && typeDef.IsEnum) {
					TypeReference underlyingType = typeDef.Fields.Single(f => f.IsRuntimeSpecialName && !f.IsStatic).FieldType;
					return IsSigned(typeDef.Module.TypeSystem, underlyingType);
				}
			}
			if (type == typeSystem.Byte || type == typeSystem.UInt16 || type == typeSystem.UInt32 || type == typeSystem.UInt64 || type == typeSystem.UIntPtr)
				return false;
			if (type == typeSystem.SByte || type == typeSystem.Int16 || type == typeSystem.Int32 || type == typeSystem.Int64 || type == typeSystem.IntPtr)
				return true;
			return null;
		}
	}
}
